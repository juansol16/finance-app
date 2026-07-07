using Cuintable.Server.Data;
using Cuintable.Server.DTOs.FinancialAdvisor;
using Cuintable.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuintable.Server.Services;

public class FinancialAdvisorService : IFinancialAdvisorService
{
    private readonly AppDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public FinancialAdvisorService(AppDbContext context, IFileStorageService fileStorage)
    {
        _context = context;
        _fileStorage = fileStorage;
    }

    public async Task<CardStatement> CreateStatementAsync(Guid tenantId, Guid userId, Guid statementId, Guid? creditCardId, string pdfUrl)
    {
        var now = DateTime.UtcNow;
        var statement = new CardStatement
        {
            Id = statementId,
            TenantId = tenantId,
            UserId = userId,
            CreditCardId = creditCardId,
            PdfUrl = pdfUrl,
            Status = StatementStatus.Uploaded,
            PeriodYear = now.Year,
            PeriodMonth = now.Month
        };

        _context.CardStatements.Add(statement);
        await _context.SaveChangesAsync();
        return statement;
    }

    public Task<CardStatement?> GetEntityAsync(Guid tenantId, Guid id) =>
        _context.CardStatements
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == id);

    public async Task<List<StatementSummaryResponse>> GetAllAsync(Guid tenantId, int? year, Guid? creditCardId)
    {
        var query = _context.CardStatements
            .Include(s => s.CreditCard)
            .Include(s => s.Transactions)
            .Where(s => s.TenantId == tenantId);

        if (year.HasValue)
            query = query.Where(s => s.PeriodYear == year.Value);
        if (creditCardId.HasValue)
            query = query.Where(s => s.CreditCardId == creditCardId.Value);

        var statements = await query
            .OrderByDescending(s => s.PeriodYear)
            .ThenByDescending(s => s.PeriodMonth)
            .ThenByDescending(s => s.CreatedAt)
            .ToListAsync();

        return statements.Select(MapToSummary).ToList();
    }

    public async Task<StatementDetailResponse?> GetByIdAsync(Guid tenantId, Guid id)
    {
        var statement = await _context.CardStatements
            .Include(s => s.CreditCard)
            .Include(s => s.Transactions)
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == id);

        if (statement is null) return null;

        var detail = new StatementDetailResponse
        {
            PeriodStart = statement.PeriodStart,
            PreviousBalance = statement.PreviousBalance,
            TotalPayments = statement.TotalPayments,
            InterestCharged = statement.InterestCharged,
            FeesCharged = statement.FeesCharged,
            CreditLimit = statement.CreditLimit,
            AvailableCredit = statement.AvailableCredit,
            ProcessedAt = statement.ProcessedAt,
            AdviceJson = statement.AdviceJson,
            Transactions = statement.Transactions
                .OrderBy(t => t.Date)
                .ThenBy(t => t.CreatedAt)
                .Select(MapToTransaction)
                .ToList(),
            AntClusters = StatementRuleEngine.ComputeAntClusters(statement.Transactions)
                .Select(MapToCluster)
                .ToList(),
            Reconciliation = await ComputeReconciliationAsync(statement)
        };

        CopySummary(statement, detail);
        return detail;
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id)
    {
        var statement = await _context.CardStatements
            .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.Id == id);

        if (statement is null) return false;

        if (!string.IsNullOrEmpty(statement.PdfUrl))
            await _fileStorage.DeleteAsync(statement.PdfUrl);

        _context.CardStatements.Remove(statement);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<StatementTransactionResponse?> ReviewTransactionAsync(Guid tenantId, Guid transactionId, TransactionReviewStatus status)
    {
        var transaction = await _context.StatementTransactions
            .FirstOrDefaultAsync(t => t.TenantId == tenantId && t.Id == transactionId);

        if (transaction is null) return null;

        transaction.ReviewStatus = status;
        await _context.SaveChangesAsync();
        return MapToTransaction(transaction);
    }

    public async Task<AdvisorDashboardResponse> GetDashboardAsync(Guid tenantId, int year, int month)
    {
        var statements = await _context.CardStatements
            .Include(s => s.Transactions)
            .Where(s => s.TenantId == tenantId &&
                        s.PeriodYear == year &&
                        s.PeriodMonth == month &&
                        s.Status == StatementStatus.Completed)
            .ToListAsync();

        var transactions = statements.SelectMany(s => s.Transactions).ToList();
        // Spending only: transfers and credit card payments are money movement, not consumption
        var charges = transactions.Where(StatementRuleEngine.IsSpendingCharge).ToList();
        var antTotal = transactions.Where(t => t.IsAntExpense).Sum(t => t.AmountMXN);

        var response = new AdvisorDashboardResponse
        {
            Year = year,
            Month = month,
            StatementCount = statements.Count,
            TotalChargesMXN = charges.Sum(t => t.AmountMXN),
            AntTotalMXN = antTotal,
            AntAnnualProjectionMXN = antTotal * 12,
            SuspiciousCount = transactions.Count(t => t.IsSuspicious),
            SuspiciousPendingCount = transactions.Count(t => t.IsSuspicious && t.ReviewStatus == TransactionReviewStatus.None),
            SubscriptionsMXN = charges.Where(t => t.IsRecurring).Sum(t => t.AmountMXN),
            MsiLoadMXN = charges.Where(t => t.IsMsi).Sum(t => t.AmountMXN),
            InterestAndFeesMXN = transactions
                .Where(t => t.Type is StatementTransactionType.Fee or StatementTransactionType.Interest)
                .Sum(t => t.AmountMXN),
            CategoryTotals = charges
                .GroupBy(t => t.Category)
                .Select(g => new CategoryTotalItem
                {
                    Category = g.Key,
                    TotalMXN = g.Sum(t => t.AmountMXN),
                    Count = g.Count()
                })
                .OrderByDescending(c => c.TotalMXN)
                .ToList(),
            AntClusters = StatementRuleEngine.ComputeAntClusters(transactions)
                .Select(MapToCluster)
                .ToList(),
            Trend = await GetTrendAsync(tenantId, year, month),
            Reconciliation = await ComputeMonthReconciliationAsync(tenantId, year, month, statements)
        };

        return response;
    }

    // ---------- Helpers ----------

    /// <summary>Charge totals for the selected period and the 5 periods before it.</summary>
    private async Task<List<TrendPointItem>> GetTrendAsync(Guid tenantId, int year, int month)
    {
        var points = await _context.StatementTransactions
            .Where(t => t.TenantId == tenantId &&
                        t.Statement.Status == StatementStatus.Completed &&
                        t.Type == StatementTransactionType.Charge &&
                        t.Category != StatementCategory.Transferencias &&
                        t.Category != StatementCategory.PagosAbonos &&
                        (t.Statement.PeriodYear < year ||
                         (t.Statement.PeriodYear == year && t.Statement.PeriodMonth <= month)))
            .GroupBy(t => new { t.Statement.PeriodYear, t.Statement.PeriodMonth })
            .Select(g => new TrendPointItem
            {
                Year = g.Key.PeriodYear,
                Month = g.Key.PeriodMonth,
                TotalChargesMXN = g.Sum(t => t.AmountMXN)
            })
            .OrderByDescending(p => p.Year)
            .ThenByDescending(p => p.Month)
            .Take(6)
            .ToListAsync();

        points.Reverse();
        return points;
    }

    /// <summary>Reconciliation summary for a single statement, from persisted match results.</summary>
    private async Task<ReconciliationSummaryResponse?> ComputeReconciliationAsync(CardStatement statement)
    {
        if (statement.CreditCardId is null || statement.Status != StatementStatus.Completed)
            return null;

        var payments = statement.Transactions
            .Where(t => t.Type == StatementTransactionType.Payment)
            .ToList();

        var start = (statement.PeriodStart ?? new DateOnly(statement.PeriodYear, statement.PeriodMonth, 1))
            .AddDays(-StatementRuleEngine.PaymentMatchToleranceDays);
        var endBase = statement.PeriodEnd
            ?? new DateOnly(statement.PeriodYear, statement.PeriodMonth, 1).AddMonths(1).AddDays(-1);
        var end = endBase.AddDays(StatementRuleEngine.PaymentMatchToleranceDays);

        var matchedIds = payments
            .Where(p => p.MatchedExpenseId is not null)
            .Select(p => p.MatchedExpenseId!.Value)
            .ToList();

        var unmatchedPlatform = await _context.Expenses
            .Where(e => e.TenantId == statement.TenantId &&
                        e.Category == ExpenseCategory.PagoTarjeta &&
                        e.CreditCardId == statement.CreditCardId &&
                        e.Date >= start && e.Date <= end &&
                        !matchedIds.Contains(e.Id))
            .ToListAsync();

        var matched = payments.Where(p => p.MatchedExpenseId is not null).ToList();
        var unmatchedStatement = payments.Where(p => p.MatchedExpenseId is null).ToList();

        return new ReconciliationSummaryResponse
        {
            MatchedPayments = matched.Count,
            UnmatchedStatementPayments = unmatchedStatement.Count,
            UnmatchedPlatformPayments = unmatchedPlatform.Count,
            MatchedAmountMXN = matched.Sum(p => p.AmountMXN),
            UnmatchedStatementAmountMXN = unmatchedStatement.Sum(p => p.AmountMXN),
            UnmatchedPlatformAmountMXN = unmatchedPlatform.Sum(e => e.AmountMXN)
        };
    }

    /// <summary>Month-level reconciliation across all cards: statement payments vs PagoTarjeta expenses.</summary>
    private async Task<ReconciliationSummaryResponse?> ComputeMonthReconciliationAsync(
        Guid tenantId, int year, int month, List<CardStatement> statements)
    {
        // Credit card statements only: on a bank account statement a Payment is a
        // deposit (income, SPEI received), not a credit card payment to reconcile.
        var cardTransactions = statements
            .Where(s => s.AccountType == StatementAccountType.CreditCard)
            .SelectMany(s => s.Transactions)
            .ToList();

        if (cardTransactions.Count == 0) return null;

        var payments = cardTransactions.Where(t => t.Type == StatementTransactionType.Payment).ToList();
        var matchedIds = payments
            .Where(p => p.MatchedExpenseId is not null)
            .Select(p => p.MatchedExpenseId!.Value)
            .ToList();

        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var unmatchedPlatform = await _context.Expenses
            .Where(e => e.TenantId == tenantId &&
                        e.Category == ExpenseCategory.PagoTarjeta &&
                        e.Date >= monthStart && e.Date <= monthEnd &&
                        !matchedIds.Contains(e.Id))
            .ToListAsync();

        var matched = payments.Where(p => p.MatchedExpenseId is not null).ToList();
        var unmatchedStatement = payments.Where(p => p.MatchedExpenseId is null).ToList();

        return new ReconciliationSummaryResponse
        {
            MatchedPayments = matched.Count,
            UnmatchedStatementPayments = unmatchedStatement.Count,
            UnmatchedPlatformPayments = unmatchedPlatform.Count,
            MatchedAmountMXN = matched.Sum(p => p.AmountMXN),
            UnmatchedStatementAmountMXN = unmatchedStatement.Sum(p => p.AmountMXN),
            UnmatchedPlatformAmountMXN = unmatchedPlatform.Sum(e => e.AmountMXN)
        };
    }

    private static StatementSummaryResponse MapToSummary(CardStatement s)
    {
        var summary = new StatementSummaryResponse();
        CopySummary(s, summary);
        return summary;
    }

    private static void CopySummary(CardStatement s, StatementSummaryResponse target)
    {
        target.Id = s.Id;
        target.AccountType = s.AccountType;
        target.CreditCardId = s.CreditCardId;
        target.CardNickname = s.CreditCard?.Nickname;
        target.BankName = s.BankName;
        target.CardLastFour = s.CardLastFour;
        target.PeriodYear = s.PeriodYear;
        target.PeriodMonth = s.PeriodMonth;
        target.PeriodEnd = s.PeriodEnd;
        target.PaymentDueDate = s.PaymentDueDate;
        target.TotalCharges = s.TotalCharges;
        target.NewBalance = s.NewBalance;
        target.MinimumPayment = s.MinimumPayment;
        target.NoInterestPayment = s.NoInterestPayment;
        target.Status = s.Status;
        target.ErrorMessage = s.ErrorMessage;
        target.TransactionCount = s.Transactions.Count;
        target.SuspiciousCount = s.Transactions.Count(t => t.IsSuspicious);
        target.AntExpenseCount = s.Transactions.Count(t => t.IsAntExpense);
        target.CreatedAt = s.CreatedAt;
    }

    private static StatementTransactionResponse MapToTransaction(StatementTransaction t) => new()
    {
        Id = t.Id,
        Date = t.Date,
        RawDescription = t.RawDescription,
        Merchant = t.Merchant,
        Category = t.Category,
        Type = t.Type,
        AmountMXN = t.AmountMXN,
        IsMsi = t.IsMsi,
        MsiCurrent = t.MsiCurrent,
        MsiTotal = t.MsiTotal,
        IsForeign = t.IsForeign,
        IsRecurring = t.IsRecurring,
        IsAntExpense = t.IsAntExpense,
        IsSuspicious = t.IsSuspicious,
        SuspiciousReason = t.SuspiciousReason,
        ReviewStatus = t.ReviewStatus,
        MatchedExpenseId = t.MatchedExpenseId
    };

    private static AntExpenseClusterResponse MapToCluster(AntExpenseCluster c) => new()
    {
        Merchant = c.Merchant,
        Category = c.Category,
        Count = c.Count,
        TotalMXN = c.TotalMXN,
        AnnualProjectionMXN = c.AnnualProjectionMXN
    };
}
