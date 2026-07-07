using Cuintable.Server.Models;

namespace Cuintable.Server.Services;

public record AntExpenseCluster(string Merchant, StatementCategory Category, int Count, decimal TotalMXN, decimal AnnualProjectionMXN);

public record ReconciliationSummary(
    int MatchedPayments,
    int UnmatchedStatementPayments,
    int UnmatchedPlatformPayments,
    decimal MatchedAmountMXN,
    decimal UnmatchedStatementAmountMXN,
    decimal UnmatchedPlatformAmountMXN);

/// <summary>
/// Deterministic analysis rules applied to extracted statement transactions.
/// Pure logic, no I/O — mirrors the HonorarioCalculator pattern so it stays unit-testable.
/// </summary>
public static class StatementRuleEngine
{
    public const decimal AntExpenseThresholdMXN = 300m;
    public const int AntExpenseMinOccurrences = 3;
    public const int PaymentMatchToleranceDays = 5;

    // Suspicious reason codes (comma-joined in SuspiciousReason, translated in the frontend)
    public const string ReasonDuplicate = "DUPLICATE";
    public const string ReasonForeign = "FOREIGN";
    public const string ReasonNewMerchant = "NEW_MERCHANT";
    public const string ReasonFeeOrInterest = "FEE_OR_INTEREST";

    private static readonly HashSet<StatementCategory> AntProneCategories =
    [
        StatementCategory.ComidaDomicilio,
        StatementCategory.RestaurantesCafes,
        StatementCategory.TiendaConveniencia,
        StatementCategory.Suscripciones,
        StatementCategory.Otro
    ];

    /// <summary>
    /// True for charges that represent actual consumption. Transfers and credit card
    /// payments move money, they don't spend it (and would double-count against the
    /// card's own statement), so spending KPIs and hormiga rules skip them.
    /// </summary>
    public static bool IsSpendingCharge(StatementTransaction t) =>
        t.Type == StatementTransactionType.Charge &&
        t.Category != StatementCategory.Transferencias &&
        t.Category != StatementCategory.PagosAbonos;

    /// <summary>
    /// Flags gastos hormiga: small charges in hormiga-prone categories, or any merchant
    /// hit repeatedly with small charges within the period.
    /// </summary>
    public static void FlagAntExpenses(IList<StatementTransaction> transactions)
    {
        var charges = transactions.Where(IsSpendingCharge).ToList();

        foreach (var t in charges)
        {
            if (t.AmountMXN < AntExpenseThresholdMXN && AntProneCategories.Contains(t.Category))
                t.IsAntExpense = true;
        }

        var frequentSmallMerchants = charges
            .Where(t => t.AmountMXN < AntExpenseThresholdMXN)
            .GroupBy(t => NormalizeMerchant(t.Merchant))
            .Where(g => g.Count() >= AntExpenseMinOccurrences);

        foreach (var group in frequentSmallMerchants)
        {
            foreach (var t in group)
                t.IsAntExpense = true;
        }
    }

    /// <summary>Groups flagged ant expenses per merchant with a x12 annual projection.</summary>
    public static List<AntExpenseCluster> ComputeAntClusters(IEnumerable<StatementTransaction> transactions)
    {
        return transactions
            .Where(t => t.IsAntExpense)
            .GroupBy(t => NormalizeMerchant(t.Merchant))
            .Select(g =>
            {
                var total = g.Sum(t => t.AmountMXN);
                return new AntExpenseCluster(
                    g.First().Merchant,
                    g.First().Category,
                    g.Count(),
                    total,
                    total * 12);
            })
            .OrderByDescending(c => c.TotalMXN)
            .ToList();
    }

    /// <summary>
    /// Flags suspicious transactions. <paramref name="knownMerchants"/> is the merchant
    /// history from prior statements of the same card; pass null when there is no history
    /// yet (the new-merchant rule is skipped in that case).
    /// </summary>
    public static void FlagSuspicious(IList<StatementTransaction> transactions, ISet<string>? knownMerchants)
    {
        // Duplicates: same merchant + amount + date more than once
        var duplicateGroups = transactions
            .Where(t => t.Type == StatementTransactionType.Charge)
            .GroupBy(t => (NormalizeMerchant(t.Merchant), t.AmountMXN, t.Date))
            .Where(g => g.Count() > 1);

        foreach (var group in duplicateGroups)
        {
            foreach (var t in group)
                AddReason(t, ReasonDuplicate);
        }

        foreach (var t in transactions)
        {
            if (t.IsForeign && t.Type == StatementTransactionType.Charge)
                AddReason(t, ReasonForeign);

            if (t.Type is StatementTransactionType.Fee or StatementTransactionType.Interest && t.AmountMXN > 0)
                AddReason(t, ReasonFeeOrInterest);

            if (knownMerchants is not null &&
                t.Type == StatementTransactionType.Charge &&
                !knownMerchants.Contains(NormalizeMerchant(t.Merchant)))
            {
                AddReason(t, ReasonNewMerchant);
            }
        }
    }

    /// <summary>
    /// Matches statement Payment transactions against platform PagoTarjeta expenses
    /// (equal amount, date within tolerance). Each expense matches at most once;
    /// closest date wins. Sets MatchedExpenseId on matched transactions.
    /// </summary>
    public static ReconciliationSummary MatchPayments(
        IList<StatementTransaction> transactions,
        IList<Expense> platformPayments)
    {
        var statementPayments = transactions
            .Where(t => t.Type == StatementTransactionType.Payment)
            .ToList();

        var availableExpenses = platformPayments.ToList();

        foreach (var payment in statementPayments.OrderBy(p => p.Date))
        {
            var candidate = availableExpenses
                .Where(e => e.AmountMXN == payment.AmountMXN &&
                            Math.Abs(e.Date.DayNumber - payment.Date.DayNumber) <= PaymentMatchToleranceDays)
                .OrderBy(e => Math.Abs(e.Date.DayNumber - payment.Date.DayNumber))
                .FirstOrDefault();

            if (candidate is not null)
            {
                payment.MatchedExpenseId = candidate.Id;
                availableExpenses.Remove(candidate);
            }
        }

        var matched = statementPayments.Where(p => p.MatchedExpenseId is not null).ToList();
        var unmatchedStatement = statementPayments.Where(p => p.MatchedExpenseId is null).ToList();

        return new ReconciliationSummary(
            matched.Count,
            unmatchedStatement.Count,
            availableExpenses.Count,
            matched.Sum(p => p.AmountMXN),
            unmatchedStatement.Sum(p => p.AmountMXN),
            availableExpenses.Sum(e => e.AmountMXN));
    }

    public static string NormalizeMerchant(string merchant) =>
        merchant.Trim().ToUpperInvariant();

    private static void AddReason(StatementTransaction t, string reason)
    {
        var reasons = string.IsNullOrEmpty(t.SuspiciousReason)
            ? []
            : t.SuspiciousReason.Split(',').ToList();

        if (!reasons.Contains(reason))
            reasons.Add(reason);

        t.IsSuspicious = true;
        t.SuspiciousReason = string.Join(',', reasons);
    }
}
