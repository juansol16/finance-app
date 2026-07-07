using System.Globalization;
using System.Text.Json;
using Cuintable.Server.Data;
using Cuintable.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuintable.Server.Services;

public class StatementAnalysisService : IStatementAnalysisService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly AppDbContext _context;
    private readonly IGeminiClient _gemini;
    private readonly IFileStorageService _fileStorage;
    private readonly ILogger<StatementAnalysisService> _logger;
    private readonly string _extractionModel;
    private readonly string _adviceModel;

    public StatementAnalysisService(
        AppDbContext context,
        IGeminiClient gemini,
        IFileStorageService fileStorage,
        IConfiguration configuration,
        ILogger<StatementAnalysisService> logger)
    {
        _context = context;
        _gemini = gemini;
        _fileStorage = fileStorage;
        _logger = logger;
        _extractionModel = configuration["Gemini:ExtractionModel"] ?? "gemini-flash-latest";
        _adviceModel = configuration["Gemini:AdviceModel"] ?? "gemini-3.1-pro-preview";
    }

    public async Task ProcessAsync(Guid tenantId, Guid statementId)
    {
        var statement = await _context.CardStatements
            .Include(s => s.Transactions)
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == statementId);

        if (statement is null) return;

        statement.Status = StatementStatus.Processing;
        statement.ErrorMessage = null;
        await _context.SaveChangesAsync();

        try
        {
            await ExtractAsync(statement);
            AnalyzeStatement(statement);
            statement.Status = StatementStatus.Completed;
            statement.ProcessedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Statement {StatementId} processing failed", statementId);
            statement.Status = StatementStatus.Failed;
            statement.ErrorMessage = Truncate(ex.Message, 1000);
            await _context.SaveChangesAsync();
            return;
        }

        // Advice is best-effort: a failure here must not fail the completed statement
        try
        {
            await AdviseAsync(statement);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Advice generation failed for statement {StatementId}", statementId);
        }
    }

    // ---------- 1. Extraction ----------

    private async Task ExtractAsync(CardStatement statement)
    {
        var file = await _fileStorage.GetStreamAsync(statement.PdfUrl)
            ?? throw new InvalidOperationException("Statement PDF not found in storage.");

        using var source = file.Stream;
        using var memory = new MemoryStream();
        await source.CopyToAsync(memory);
        var pdfBytes = memory.ToArray();

        var json = await _gemini.GenerateJsonAsync(
            _extractionModel,
            ExtractionSystemPrompt,
            "Extract the statement summary and every movement from this estado de cuenta. Movements usually span several pages — read all pages.",
            (pdfBytes, "application/pdf"),
            ExtractionSchema);

        statement.RawExtractionJson = json;

        var result = JsonSerializer.Deserialize<ExtractionResult>(json, JsonOptions)
            ?? throw new InvalidOperationException("Extraction returned empty JSON.");

        if (result.Transactions.Count == 0)
            throw new InvalidOperationException("No movements found in the document — is it a bank or credit card statement PDF?");

        statement.AccountType = ParseEnum(result.AccountType, StatementAccountType.CreditCard);
        statement.BankName = result.BankName ?? statement.BankName;
        statement.CardLastFour = result.CardLastFour ?? statement.CardLastFour;
        statement.PeriodStart = ParseDate(result.PeriodStart);
        statement.PeriodEnd = ParseDate(result.PeriodEnd);
        statement.PaymentDueDate = ParseDate(result.PaymentDueDate);
        statement.PreviousBalance = result.PreviousBalance;
        statement.TotalPayments = result.TotalPayments;
        statement.TotalCharges = result.TotalCharges;
        statement.InterestCharged = result.InterestCharged;
        statement.FeesCharged = result.FeesCharged;
        statement.NewBalance = result.NewBalance;
        statement.MinimumPayment = result.MinimumPayment;
        statement.NoInterestPayment = result.NoInterestPayment;
        statement.CreditLimit = result.CreditLimit;
        statement.AvailableCredit = result.AvailableCredit;

        var period = statement.PeriodEnd ?? DateOnly.FromDateTime(statement.CreatedAt);
        statement.PeriodYear = period.Year;
        statement.PeriodMonth = period.Month;

        // Auto-match the registered card by last 4 digits (explicit selection wins).
        // Bank-account statements have no credit card to match.
        if (statement.AccountType == StatementAccountType.CreditCard &&
            statement.CreditCardId is null && !string.IsNullOrEmpty(statement.CardLastFour))
        {
            var card = await _context.CreditCards
                .Where(c => c.TenantId == statement.TenantId && c.LastFourDigits == statement.CardLastFour)
                .OrderByDescending(c => c.IsActive)
                .FirstOrDefaultAsync();
            statement.CreditCardId = card?.Id;
        }

        // Reprocessing replaces previous transactions
        _context.StatementTransactions.RemoveRange(statement.Transactions);
        statement.Transactions.Clear();

        foreach (var t in result.Transactions)
        {
            statement.Transactions.Add(new StatementTransaction
            {
                TenantId = statement.TenantId,
                StatementId = statement.Id,
                Date = ParseDate(t.Date) ?? period,
                RawDescription = Truncate(t.Description ?? string.Empty, 500),
                Merchant = Truncate(string.IsNullOrWhiteSpace(t.Merchant) ? (t.Description ?? "?") : t.Merchant, 200),
                Category = ParseEnum(t.Category, StatementCategory.Otro),
                Type = ParseEnum(t.Type, StatementTransactionType.Charge),
                AmountMXN = Math.Abs(t.AmountMXN),
                IsMsi = t.IsMsi,
                MsiCurrent = t.MsiCurrent,
                MsiTotal = t.MsiTotal,
                IsForeign = t.IsForeign,
                IsRecurring = t.IsRecurring
            });
        }
    }

    // ---------- 2. Deterministic analysis ----------

    private void AnalyzeStatement(CardStatement statement)
    {
        var transactions = statement.Transactions.ToList();

        StatementRuleEngine.FlagAntExpenses(transactions);

        var knownMerchants = GetKnownMerchants(statement);
        StatementRuleEngine.FlagSuspicious(transactions, knownMerchants);

        var platformPayments = GetPlatformPayments(statement);
        if (platformPayments is not null)
            StatementRuleEngine.MatchPayments(transactions, platformPayments);
    }

    /// <summary>Merchant history from prior completed statements of the same card; null when no history.</summary>
    private HashSet<string>? GetKnownMerchants(CardStatement statement)
    {
        if (statement.CreditCardId is null) return null;

        var merchants = _context.StatementTransactions
            .Where(t => t.TenantId == statement.TenantId &&
                        t.StatementId != statement.Id &&
                        t.Statement.CreditCardId == statement.CreditCardId &&
                        t.Statement.Status == StatementStatus.Completed)
            .Select(t => t.Merchant)
            .Distinct()
            .ToList();

        if (merchants.Count == 0) return null;

        return merchants.Select(StatementRuleEngine.NormalizeMerchant).ToHashSet();
    }

    /// <summary>PagoTarjeta expenses for the statement's card within the period (± tolerance); null when not reconcilable.</summary>
    private List<Expense>? GetPlatformPayments(CardStatement statement)
    {
        if (statement.CreditCardId is null) return null;

        var start = (statement.PeriodStart ?? new DateOnly(statement.PeriodYear, statement.PeriodMonth, 1))
            .AddDays(-StatementRuleEngine.PaymentMatchToleranceDays);
        var endBase = statement.PeriodEnd
            ?? new DateOnly(statement.PeriodYear, statement.PeriodMonth, 1).AddMonths(1).AddDays(-1);
        var end = endBase.AddDays(StatementRuleEngine.PaymentMatchToleranceDays);

        return _context.Expenses
            .Where(e => e.TenantId == statement.TenantId &&
                        e.Category == ExpenseCategory.PagoTarjeta &&
                        e.CreditCardId == statement.CreditCardId &&
                        e.Date >= start && e.Date <= end)
            .ToList();
    }

    // ---------- 3. AI advice ----------

    private async Task AdviseAsync(CardStatement statement)
    {
        var transactions = statement.Transactions.ToList();
        // Consumption only; transfers and credit card payments are reported separately below
        var charges = transactions.Where(StatementRuleEngine.IsSpendingCharge).ToList();
        var allCharges = transactions.Where(t => t.Type == StatementTransactionType.Charge).ToList();

        var categoryTotals = charges
            .GroupBy(t => t.Category)
            .Select(g => new { category = g.Key.ToString(), totalMXN = g.Sum(t => t.AmountMXN), count = g.Count() })
            .OrderByDescending(x => x.totalMXN)
            .ToList();

        var antClusters = StatementRuleEngine.ComputeAntClusters(transactions);

        // History must come from the same account. Without a matched card (bank accounts,
        // unrecognized cards) a null CreditCardId would match every other card-less
        // statement, so identify the account by type + bank + last 4 instead.
        var priorStatements = _context.CardStatements
            .Where(s => s.TenantId == statement.TenantId &&
                        s.Id != statement.Id &&
                        s.Status == StatementStatus.Completed &&
                        (s.PeriodYear < statement.PeriodYear ||
                         (s.PeriodYear == statement.PeriodYear && s.PeriodMonth < statement.PeriodMonth)));

        IQueryable<CardStatement>? sameAccount = null;
        if (statement.CreditCardId is not null)
        {
            sameAccount = priorStatements.Where(s => s.CreditCardId == statement.CreditCardId);
        }
        else if (!string.IsNullOrEmpty(statement.BankName))
        {
            sameAccount = priorStatements.Where(s => s.CreditCardId == null &&
                                                     s.AccountType == statement.AccountType &&
                                                     s.BankName == statement.BankName &&
                                                     s.CardLastFour == statement.CardLastFour);
        }

        var previousCharges = sameAccount is null
            ? null
            : await sameAccount
                .OrderByDescending(s => s.PeriodYear).ThenByDescending(s => s.PeriodMonth)
                .Select(s => s.TotalCharges)
                .FirstOrDefaultAsync();

        var monthlyIncome = await _context.Incomes
            .Where(i => i.TenantId == statement.TenantId &&
                        i.Date.Year == statement.PeriodYear &&
                        i.Date.Month == statement.PeriodMonth)
            .SumAsync(i => (decimal?)i.AmountMXN) ?? 0m;

        var aggregates = new
        {
            accountType = statement.AccountType.ToString(),
            bank = statement.BankName,
            period = $"{statement.PeriodYear}-{statement.PeriodMonth:00}",
            totalSpendingMXN = charges.Sum(t => t.AmountMXN),
            transfersOutMXN = allCharges
                .Where(t => t.Category == StatementCategory.Transferencias)
                .Sum(t => t.AmountMXN),
            creditCardPaymentsMXN = allCharges
                .Where(t => t.Category == StatementCategory.PagosAbonos)
                .Sum(t => t.AmountMXN),
            previousStatementChargesMXN = previousCharges,
            interestChargedMXN = statement.InterestCharged,
            feesChargedMXN = statement.FeesCharged,
            newBalanceMXN = statement.NewBalance,
            minimumPaymentMXN = statement.MinimumPayment,
            noInterestPaymentMXN = statement.NoInterestPayment,
            creditLimitMXN = statement.CreditLimit,
            monthlyNetIncomeMXN = monthlyIncome > 0 ? monthlyIncome : (decimal?)null,
            categoryTotals,
            antExpenses = new
            {
                totalMXN = transactions.Where(t => t.IsAntExpense).Sum(t => t.AmountMXN),
                clusters = antClusters.Select(c => new { c.Merchant, c.Count, c.TotalMXN, c.AnnualProjectionMXN })
            },
            msiMonthlyLoadMXN = charges.Where(t => t.IsMsi).Sum(t => t.AmountMXN),
            subscriptionsMXN = charges.Where(t => t.IsRecurring).Sum(t => t.AmountMXN),
            suspiciousPendingReview = transactions.Count(t => t.IsSuspicious && t.ReviewStatus == TransactionReviewStatus.None),
            unmatchedStatementPayments = transactions.Count(t => t.Type == StatementTransactionType.Payment && t.MatchedExpenseId == null)
        };

        var language = statement.User.PreferredLanguage == "en" ? "English" : "Spanish (Mexico)";

        var systemPrompt =
            $"You are a warm, practical personal financial advisor for a freelancer in Mexico (RESICO regime). " +
            $"You receive aggregated data from one monthly statement — accountType says whether it is a credit card " +
            $"or a bank account (débito). totalSpendingMXN is consumption; transfersOutMXN and creditCardPaymentsMXN " +
            $"are money movement, not spending. " +
            $"Write your entire response in {language}. " +
            "Give a short summary of the month and 3 to 5 specific, actionable suggestions based ONLY on the data provided. " +
            "Quantify impact in MXN when possible (impactMXN = estimated monthly savings or cost). " +
            "Prioritize: interest/fees being paid, gastos hormiga with their annual projection, subscription creep, " +
            "MSI load vs credit limit, and spending vs income when income is provided. " +
            "Be encouraging and concrete (mention merchants and amounts), never judgmental. Do not invent data.";

        statement.AdviceJson = await _gemini.GenerateJsonAsync(
            _adviceModel,
            systemPrompt,
            JsonSerializer.Serialize(aggregates, JsonOptions),
            null,
            AdviceSchema);
    }

    // ---------- Helpers & schemas ----------

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, out var date) ? date : null;

    private static TEnum ParseEnum<TEnum>(string? value, TEnum fallback) where TEnum : struct =>
        Enum.TryParse<TEnum>(value, true, out var parsed) ? parsed : fallback;

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];

    private record ExtractionResult(
        string? AccountType,
        string? BankName,
        string? CardLastFour,
        string? PeriodStart,
        string? PeriodEnd,
        string? PaymentDueDate,
        decimal? PreviousBalance,
        decimal? TotalPayments,
        decimal? TotalCharges,
        decimal? InterestCharged,
        decimal? FeesCharged,
        decimal? NewBalance,
        decimal? MinimumPayment,
        decimal? NoInterestPayment,
        decimal? CreditLimit,
        decimal? AvailableCredit,
        List<ExtractedTransaction> Transactions)
    {
        public List<ExtractedTransaction> Transactions { get; init; } = Transactions ?? [];
    }

    private record ExtractedTransaction(
        string? Date,
        string? Description,
        string? Merchant,
        string? Category,
        string? Type,
        decimal AmountMXN,
        bool IsMsi,
        int? MsiCurrent,
        int? MsiTotal,
        bool IsForeign,
        bool IsRecurring);

    private const string ExtractionSystemPrompt =
        "You are a meticulous parser of Mexican bank statements (estados de cuenta) from any bank " +
        "(BBVA, Banorte, Santander, Banamex, HSBC, Amex, Nu, etc.). You receive one statement PDF, which may be " +
        "a CREDIT CARD statement or a BANK ACCOUNT statement (cuenta de débito/cheques/nómina, e.g. BBVA 'Libretón' " +
        "or 'Cuenta Digital' with a CLABE). Detect which one it is and set accountType " +
        "('CreditCard' or 'BankAccount'). Extract the summary fields and EVERY movement row — movements usually span " +
        "several pages, so read the whole document.\n" +
        "General rules:\n" +
        "- Dates in ISO YYYY-MM-DD. Statements print dates like '15/MAY' or '15-ENE-2026' with Spanish month " +
        "abbreviations (ENE FEB MAR ABR MAY JUN JUL AGO SEP OCT NOV DIC); resolve the year from the statement period. " +
        "When a row shows OPER and LIQ dates, use the OPER date.\n" +
        "- amountMXN: always a positive number; the 'type' field carries direction " +
        "(Charge = cargo/retiro/compra, Payment = abono/depósito/pago, Refund = devolución/bonificación, " +
        "Fee = comisión, Interest = intereses).\n" +
        "- CAUTION: many statements print running balance columns (SALDO / OPERACIÓN / LIQUIDACIÓN) after the amount. " +
        "Those are balances, NOT amounts — take the value from the CARGOS or ABONOS column only.\n" +
        "- merchant: short normalized name without branch numbers or city suffixes (e.g. 'OXXO', 'UBER EATS', " +
        "'LIVERPOOL', 'SAT'). For SPEI transfers use the counterparty name when shown (e.g. 'SPEI - FELLOWS LATAM').\n" +
        "- description: the full raw row text including references.\n" +
        "- category: single best fit. TiendaConveniencia = OXXO/7-Eleven style; ComidaDomicilio = delivery apps " +
        "(Uber Eats, Rappi, DiDi Food); Suscripciones = recurring digital services (Netflix, Spotify, iCloud); " +
        "Transferencias = SPEI enviado/recibido, TRASPASO, PAGO CUENTA DE TERCERO; " +
        "PagosAbonos = payments to/of a credit card ('SU PAGO GRACIAS', 'PAGO TARJETA DE CREDITO', payments to Amex/Nu); " +
        "RetiroEfectivo = cash withdrawals/ATM; Servicios = utilities, insurance, taxes (SAT), loan payments; " +
        "ComisionesIntereses = fee and interest rows.\n" +
        "- isMsi: true for meses-sin-intereses plan rows; msiCurrent/msiTotal from patterns like '03 DE 12' or '3/12'.\n" +
        "- isForeign: true for foreign-currency or cross-border purchases (USD amounts, 'TIPO DE CAMBIO', foreign country codes).\n" +
        "- isRecurring: true for subscription-like recurring services.\n" +
        "- Do NOT emit summary rows (saldo anterior, totals, rendimiento, apartados) as transactions.\n" +
        "Summary fields (null when not present):\n" +
        "- periodEnd is the fecha de corte; periodStart from 'Periodo DEL ... AL ...' when shown.\n" +
        "- CREDIT CARD statements: newBalance = saldo al corte, minimumPayment = pago mínimo, noInterestPayment = " +
        "'pago para no generar intereses', paymentDueDate = fecha límite de pago, creditLimit/availableCredit, " +
        "cardLastFour = last 4 digits of the card number shown.\n" +
        "- BANK ACCOUNT statements: previousBalance = Saldo Anterior, totalPayments = total Depósitos/Abonos, " +
        "totalCharges = total Retiros/Cargos, newBalance = Saldo Final; paymentDueDate, minimumPayment, " +
        "noInterestPayment, creditLimit, availableCredit and cardLastFour must be null.\n" +
        "- Return an empty transactions array ONLY if the document contains no account movements at all " +
        "(i.e., it is not a bank or credit card statement).";

    private const string ExtractionSchema = """
    {
      "type": "OBJECT",
      "properties": {
        "accountType": { "type": "STRING", "enum": ["CreditCard", "BankAccount"] },
        "bankName": { "type": "STRING", "nullable": true },
        "cardLastFour": { "type": "STRING", "nullable": true },
        "periodStart": { "type": "STRING", "nullable": true },
        "periodEnd": { "type": "STRING", "nullable": true },
        "paymentDueDate": { "type": "STRING", "nullable": true },
        "previousBalance": { "type": "NUMBER", "nullable": true },
        "totalPayments": { "type": "NUMBER", "nullable": true },
        "totalCharges": { "type": "NUMBER", "nullable": true },
        "interestCharged": { "type": "NUMBER", "nullable": true },
        "feesCharged": { "type": "NUMBER", "nullable": true },
        "newBalance": { "type": "NUMBER", "nullable": true },
        "minimumPayment": { "type": "NUMBER", "nullable": true },
        "noInterestPayment": { "type": "NUMBER", "nullable": true },
        "creditLimit": { "type": "NUMBER", "nullable": true },
        "availableCredit": { "type": "NUMBER", "nullable": true },
        "transactions": {
          "type": "ARRAY",
          "items": {
            "type": "OBJECT",
            "properties": {
              "date": { "type": "STRING" },
              "description": { "type": "STRING" },
              "merchant": { "type": "STRING" },
              "category": {
                "type": "STRING",
                "enum": ["ComidaDomicilio", "RestaurantesCafes", "Supermercado", "TiendaConveniencia",
                         "Suscripciones", "Transporte", "Gasolina", "ComprasOnline", "Salud", "Viajes",
                         "Entretenimiento", "Servicios", "ComisionesIntereses", "PagosAbonos",
                         "RetiroEfectivo", "Otro", "Transferencias"]
              },
              "type": { "type": "STRING", "enum": ["Charge", "Payment", "Fee", "Interest", "Refund"] },
              "amountMXN": { "type": "NUMBER" },
              "isMsi": { "type": "BOOLEAN" },
              "msiCurrent": { "type": "INTEGER", "nullable": true },
              "msiTotal": { "type": "INTEGER", "nullable": true },
              "isForeign": { "type": "BOOLEAN" },
              "isRecurring": { "type": "BOOLEAN" }
            },
            "required": ["date", "description", "merchant", "category", "type", "amountMXN",
                         "isMsi", "isForeign", "isRecurring"]
          }
        }
      },
      "required": ["accountType", "transactions"]
    }
    """;

    private const string AdviceSchema = """
    {
      "type": "OBJECT",
      "properties": {
        "summary": { "type": "STRING" },
        "suggestions": {
          "type": "ARRAY",
          "items": {
            "type": "OBJECT",
            "properties": {
              "title": { "type": "STRING" },
              "detail": { "type": "STRING" },
              "impactMXN": { "type": "NUMBER", "nullable": true }
            },
            "required": ["title", "detail"]
          }
        }
      },
      "required": ["summary", "suggestions"]
    }
    """;
}
