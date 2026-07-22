using System.Text.Json;
using Cuintable.Server.Data;
using Cuintable.Server.DTOs.FinancialAdvisor;
using Cuintable.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuintable.Server.Services;

public class MonthlyAdviceService : IMonthlyAdviceService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly AppDbContext _context;
    private readonly IGeminiClient _gemini;
    private readonly string _adviceModel;

    public MonthlyAdviceService(AppDbContext context, IGeminiClient gemini, IConfiguration configuration)
    {
        _context = context;
        _gemini = gemini;
        _adviceModel = configuration["Gemini:AdviceModel"] ?? "gemini-3.1-pro-preview";
    }

    public async Task<MonthlyAdviceResponse?> GenerateAsync(Guid tenantId, Guid userId, int year, int month)
    {
        var statements = await _context.CardStatements
            .Include(s => s.Transactions)
            .Include(s => s.CreditCard)
            .Where(s => s.TenantId == tenantId &&
                        s.PeriodYear == year &&
                        s.PeriodMonth == month &&
                        s.Status == StatementStatus.Completed)
            .ToListAsync();

        if (statements.Count == 0) return null;

        var transactions = statements.SelectMany(s => s.Transactions).ToList();
        // Consumption only; transfers and credit card payments are money movement, not spending
        var charges = transactions.Where(StatementRuleEngine.IsSpendingCharge).ToList();
        var allCharges = transactions.Where(t => t.Type == StatementTransactionType.Charge).ToList();

        // One entry per statement so the model can compare accounts (which card
        // costs interest, where the MSI load lives, etc.)
        var accounts = statements.Select(s =>
        {
            var own = s.Transactions.Where(StatementRuleEngine.IsSpendingCharge).ToList();
            return new
            {
                accountType = s.AccountType.ToString(),
                bank = s.BankName,
                account = s.CreditCard?.Nickname
                    ?? (string.IsNullOrEmpty(s.CardLastFour) ? s.BankName : $"****{s.CardLastFour}"),
                totalSpendingMXN = own.Sum(t => t.AmountMXN),
                interestChargedMXN = s.InterestCharged,
                feesChargedMXN = s.FeesCharged,
                newBalanceMXN = s.NewBalance,
                minimumPaymentMXN = s.MinimumPayment,
                noInterestPaymentMXN = s.NoInterestPayment,
                creditLimitMXN = s.CreditLimit,
                msiMonthlyLoadMXN = own.Where(t => t.IsMsi).Sum(t => t.AmountMXN),
                subscriptionsMXN = own.Where(t => t.IsRecurring).Sum(t => t.AmountMXN)
            };
        }).ToList();

        var categoryTotals = charges
            .GroupBy(t => t.Category)
            .Select(g => new { category = g.Key.ToString(), totalMXN = g.Sum(t => t.AmountMXN), count = g.Count() })
            .OrderByDescending(x => x.totalMXN)
            .ToList();

        var antClusters = StatementRuleEngine.ComputeAntClusters(transactions);

        var monthlyIncome = await _context.Incomes
            .Where(i => i.TenantId == tenantId &&
                        i.Date.Year == year &&
                        i.Date.Month == month)
            .SumAsync(i => (decimal?)i.AmountMXN) ?? 0m;

        var aggregates = new
        {
            period = $"{year}-{month:00}",
            statementCount = statements.Count,
            monthlyNetIncomeMXN = monthlyIncome > 0 ? monthlyIncome : (decimal?)null,
            accounts,
            combined = new
            {
                totalSpendingMXN = charges.Sum(t => t.AmountMXN),
                transfersOutMXN = allCharges
                    .Where(t => t.Category == StatementCategory.Transferencias)
                    .Sum(t => t.AmountMXN),
                creditCardPaymentsMXN = allCharges
                    .Where(t => t.Category == StatementCategory.PagosAbonos)
                    .Sum(t => t.AmountMXN),
                interestAndFeesMXN = transactions
                    .Where(t => t.Type is StatementTransactionType.Fee or StatementTransactionType.Interest)
                    .Sum(t => t.AmountMXN),
                msiMonthlyLoadMXN = charges.Where(t => t.IsMsi).Sum(t => t.AmountMXN),
                subscriptionsMXN = charges.Where(t => t.IsRecurring).Sum(t => t.AmountMXN),
                categoryTotals,
                antExpenses = new
                {
                    totalMXN = transactions.Where(t => t.IsAntExpense).Sum(t => t.AmountMXN),
                    clusters = antClusters.Select(c => new { c.Merchant, c.Count, c.TotalMXN, c.AnnualProjectionMXN })
                },
                suspiciousPendingReview = transactions.Count(t => t.IsSuspicious && t.ReviewStatus == TransactionReviewStatus.None)
            }
        };

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        var language = user?.PreferredLanguage == "en" ? "English" : "Spanish (Mexico)";

        var systemPrompt =
            $"You are a warm, practical personal financial advisor for a freelancer in Mexico (RESICO regime). " +
            $"You receive aggregated data for ONE calendar month covering ALL of the user's statements: " +
            $"'accounts' lists each credit card / bank account separately, 'combined' holds the month-wide totals. " +
            $"totalSpendingMXN is consumption; transfersOutMXN and creditCardPaymentsMXN are money movement, not spending. " +
            $"Write your entire response in {language}. " +
            "Give a short summary of the whole month and 3 to 6 specific, actionable suggestions based ONLY on the data provided. " +
            "Take the cross-account view the individual statements cannot see: total spending vs income when income is provided, " +
            "which account generates interest/fees, combined MSI load vs credit limits, gastos hormiga with their annual " +
            "projection, and subscriptions spread across accounts. " +
            "Quantify impact in MXN when possible (impactMXN = estimated monthly savings or cost). " +
            "Be encouraging and concrete (mention accounts, merchants and amounts), never judgmental. Do not invent data.";

        var json = await _gemini.GenerateJsonAsync(
            _adviceModel,
            systemPrompt,
            JsonSerializer.Serialize(aggregates, JsonOptions),
            null,
            AdviceContract.Schema);

        var advice = await _context.MonthlyAdvices
            .FirstOrDefaultAsync(a => a.TenantId == tenantId && a.PeriodYear == year && a.PeriodMonth == month);

        if (advice is null)
        {
            advice = new MonthlyAdvice
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                PeriodYear = year,
                PeriodMonth = month
            };
            _context.MonthlyAdvices.Add(advice);
        }

        advice.AdviceJson = json;
        advice.StatementCount = statements.Count;
        advice.GeneratedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new MonthlyAdviceResponse
        {
            AdviceJson = advice.AdviceJson,
            GeneratedAt = advice.GeneratedAt,
            StatementCount = advice.StatementCount,
            IsStale = false
        };
    }
}
