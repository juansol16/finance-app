using Cuintable.Server.Models;
using Cuintable.Server.Services;
using Xunit;

namespace Cuintable.Server.Tests.Services;

public class StatementRuleEngineTests
{
    private static StatementTransaction Charge(
        string merchant,
        decimal amount,
        StatementCategory category = StatementCategory.Otro,
        int day = 10,
        StatementTransactionType type = StatementTransactionType.Charge,
        bool isForeign = false)
        => new()
        {
            Id = Guid.NewGuid(),
            Merchant = merchant,
            AmountMXN = amount,
            Category = category,
            Type = type,
            Date = new DateOnly(2026, 6, day),
            IsForeign = isForeign
        };

    private static Expense Payment(decimal amount, int day) => new()
    {
        Id = Guid.NewGuid(),
        Category = ExpenseCategory.PagoTarjeta,
        AmountMXN = amount,
        Date = new DateOnly(2026, 6, day)
    };

    // ---------- Gastos hormiga ----------

    [Fact]
    public void FlagAntExpenses_SmallChargeInProneCategory_IsFlagged()
    {
        var txns = new List<StatementTransaction>
        {
            Charge("STARBUCKS", 95m, StatementCategory.RestaurantesCafes),
            Charge("OXXO", 58.50m, StatementCategory.TiendaConveniencia)
        };

        StatementRuleEngine.FlagAntExpenses(txns);

        Assert.All(txns, t => Assert.True(t.IsAntExpense));
    }

    [Fact]
    public void FlagAntExpenses_LargeChargeInProneCategory_NotFlagged()
    {
        var txns = new List<StatementTransaction>
        {
            Charge("RESTAURANTE PUJOL", 2_800m, StatementCategory.RestaurantesCafes)
        };

        StatementRuleEngine.FlagAntExpenses(txns);

        Assert.False(txns[0].IsAntExpense);
    }

    [Fact]
    public void FlagAntExpenses_SmallChargeInNonProneCategory_NotFlaggedWhenInfrequent()
    {
        var txns = new List<StatementTransaction>
        {
            Charge("FARMACIA GUADALAJARA", 120m, StatementCategory.Salud)
        };

        StatementRuleEngine.FlagAntExpenses(txns);

        Assert.False(txns[0].IsAntExpense);
    }

    [Fact]
    public void FlagAntExpenses_FrequentSmallMerchant_FlaggedRegardlessOfCategory()
    {
        var txns = new List<StatementTransaction>
        {
            Charge("UBER", 85m, StatementCategory.Transporte, day: 3),
            Charge("UBER", 92m, StatementCategory.Transporte, day: 11),
            Charge("uber ", 78m, StatementCategory.Transporte, day: 20) // case/space-insensitive grouping
        };

        StatementRuleEngine.FlagAntExpenses(txns);

        Assert.All(txns, t => Assert.True(t.IsAntExpense));
    }

    [Fact]
    public void FlagAntExpenses_FrequentSmallTransfers_NotFlagged()
    {
        // Repeated small SPEIs are money movement, not gastos hormiga
        var txns = new List<StatementTransaction>
        {
            Charge("SPEI - EMMANUEL", 150m, StatementCategory.Transferencias, day: 3),
            Charge("SPEI - EMMANUEL", 200m, StatementCategory.Transferencias, day: 12),
            Charge("SPEI - EMMANUEL", 180m, StatementCategory.Transferencias, day: 21)
        };

        StatementRuleEngine.FlagAntExpenses(txns);

        Assert.All(txns, t => Assert.False(t.IsAntExpense));
    }

    [Fact]
    public void FlagAntExpenses_PaymentsAreNeverFlagged()
    {
        var txns = new List<StatementTransaction>
        {
            Charge("SU PAGO GRACIAS", 100m, StatementCategory.PagosAbonos, type: StatementTransactionType.Payment)
        };

        StatementRuleEngine.FlagAntExpenses(txns);

        Assert.False(txns[0].IsAntExpense);
    }

    [Fact]
    public void ComputeAntClusters_GroupsByMerchantWithAnnualProjection()
    {
        var txns = new List<StatementTransaction>
        {
            Charge("RAPPI", 150m, StatementCategory.ComidaDomicilio, day: 2),
            Charge("RAPPI", 250m, StatementCategory.ComidaDomicilio, day: 9),
            Charge("OXXO", 60m, StatementCategory.TiendaConveniencia, day: 5)
        };
        StatementRuleEngine.FlagAntExpenses(txns);

        var clusters = StatementRuleEngine.ComputeAntClusters(txns);

        Assert.Equal(2, clusters.Count);
        Assert.Equal("RAPPI", clusters[0].Merchant); // biggest total first
        Assert.Equal(400m, clusters[0].TotalMXN);
        Assert.Equal(4_800m, clusters[0].AnnualProjectionMXN);
        Assert.Equal(2, clusters[0].Count);
    }

    // ---------- Suspicious charges ----------

    [Fact]
    public void FlagSuspicious_DuplicateSameMerchantAmountAndDay_FlagsBoth()
    {
        var txns = new List<StatementTransaction>
        {
            Charge("AMAZON MX", 549m, StatementCategory.ComprasOnline, day: 14),
            Charge("AMAZON MX", 549m, StatementCategory.ComprasOnline, day: 14),
            Charge("AMAZON MX", 549m, StatementCategory.ComprasOnline, day: 20) // different day: not part of dup
        };

        StatementRuleEngine.FlagSuspicious(txns, knownMerchants: null);

        Assert.True(txns[0].IsSuspicious);
        Assert.Contains(StatementRuleEngine.ReasonDuplicate, txns[0].SuspiciousReason);
        Assert.True(txns[1].IsSuspicious);
        Assert.False(txns[2].IsSuspicious);
    }

    [Fact]
    public void FlagSuspicious_ForeignCharge_Flagged()
    {
        var txns = new List<StatementTransaction>
        {
            Charge("STEAM PURCHASE", 320m, StatementCategory.Entretenimiento, isForeign: true)
        };

        StatementRuleEngine.FlagSuspicious(txns, knownMerchants: null);

        Assert.True(txns[0].IsSuspicious);
        Assert.Equal(StatementRuleEngine.ReasonForeign, txns[0].SuspiciousReason);
    }

    [Fact]
    public void FlagSuspicious_FeeAndInterest_Flagged()
    {
        var txns = new List<StatementTransaction>
        {
            Charge("COMISION ANUALIDAD", 690m, StatementCategory.ComisionesIntereses, type: StatementTransactionType.Fee),
            Charge("INTERESES", 312.45m, StatementCategory.ComisionesIntereses, type: StatementTransactionType.Interest)
        };

        StatementRuleEngine.FlagSuspicious(txns, knownMerchants: null);

        Assert.All(txns, t =>
        {
            Assert.True(t.IsSuspicious);
            Assert.Equal(StatementRuleEngine.ReasonFeeOrInterest, t.SuspiciousReason);
        });
    }

    [Fact]
    public void FlagSuspicious_NewMerchant_FlaggedOnlyWhenHistoryExists()
    {
        var known = new HashSet<string> { "OXXO", "UBER" };
        var withHistory = new List<StatementTransaction> { Charge("TIENDA RARA XYZ", 1_500m) };
        var withoutHistory = new List<StatementTransaction> { Charge("TIENDA RARA XYZ", 1_500m) };

        StatementRuleEngine.FlagSuspicious(withHistory, known);
        StatementRuleEngine.FlagSuspicious(withoutHistory, knownMerchants: null);

        Assert.True(withHistory[0].IsSuspicious);
        Assert.Equal(StatementRuleEngine.ReasonNewMerchant, withHistory[0].SuspiciousReason);
        Assert.False(withoutHistory[0].IsSuspicious);
    }

    [Fact]
    public void FlagSuspicious_KnownMerchant_NotFlagged()
    {
        var known = new HashSet<string> { "OXXO" };
        var txns = new List<StatementTransaction> { Charge("oxxo", 85m, StatementCategory.TiendaConveniencia) };

        StatementRuleEngine.FlagSuspicious(txns, known);

        Assert.False(txns[0].IsSuspicious);
    }

    [Fact]
    public void FlagSuspicious_MultipleReasons_AreCommaJoinedWithoutRepeats()
    {
        var txns = new List<StatementTransaction>
        {
            Charge("SHOP ABROAD", 900m, day: 8, isForeign: true),
            Charge("SHOP ABROAD", 900m, day: 8, isForeign: true)
        };

        StatementRuleEngine.FlagSuspicious(txns, new HashSet<string> { "OXXO" });

        var reasons = txns[0].SuspiciousReason!.Split(',');
        Assert.Equal(3, reasons.Length);
        Assert.Contains(StatementRuleEngine.ReasonDuplicate, reasons);
        Assert.Contains(StatementRuleEngine.ReasonForeign, reasons);
        Assert.Contains(StatementRuleEngine.ReasonNewMerchant, reasons);
        Assert.Equal(reasons.Length, reasons.Distinct().Count());
    }

    // ---------- Reconciliation ----------

    [Fact]
    public void MatchPayments_EqualAmountWithinTolerance_Matches()
    {
        var payment = Charge("SU PAGO GRACIAS", 12_000m, StatementCategory.PagosAbonos, day: 15,
            type: StatementTransactionType.Payment);
        var expense = Payment(12_000m, day: 17); // 2 days later

        var summary = StatementRuleEngine.MatchPayments([payment], [expense]);

        Assert.Equal(expense.Id, payment.MatchedExpenseId);
        Assert.Equal(1, summary.MatchedPayments);
        Assert.Equal(0, summary.UnmatchedStatementPayments);
        Assert.Equal(0, summary.UnmatchedPlatformPayments);
        Assert.Equal(12_000m, summary.MatchedAmountMXN);
    }

    [Fact]
    public void MatchPayments_OutsideTolerance_DoesNotMatch()
    {
        var payment = Charge("SU PAGO", 5_000m, StatementCategory.PagosAbonos, day: 1,
            type: StatementTransactionType.Payment);
        var expense = Payment(5_000m, day: 10); // 9 days apart > 5

        var summary = StatementRuleEngine.MatchPayments([payment], [expense]);

        Assert.Null(payment.MatchedExpenseId);
        Assert.Equal(1, summary.UnmatchedStatementPayments);
        Assert.Equal(1, summary.UnmatchedPlatformPayments);
        Assert.Equal(5_000m, summary.UnmatchedStatementAmountMXN);
        Assert.Equal(5_000m, summary.UnmatchedPlatformAmountMXN);
    }

    [Fact]
    public void MatchPayments_ExpenseIsNotMatchedTwice_ClosestDateWins()
    {
        var early = Charge("SU PAGO", 3_000m, StatementCategory.PagosAbonos, day: 10,
            type: StatementTransactionType.Payment);
        var late = Charge("SU PAGO", 3_000m, StatementCategory.PagosAbonos, day: 20,
            type: StatementTransactionType.Payment);
        var expense = Payment(3_000m, day: 11);

        var summary = StatementRuleEngine.MatchPayments([early, late], [expense]);

        Assert.Equal(expense.Id, early.MatchedExpenseId);
        Assert.Null(late.MatchedExpenseId);
        Assert.Equal(1, summary.MatchedPayments);
        Assert.Equal(1, summary.UnmatchedStatementPayments);
    }

    [Fact]
    public void MatchPayments_ChargesAreIgnored()
    {
        var charge = Charge("OXXO", 100m, StatementCategory.TiendaConveniencia);
        var expense = Payment(100m, day: 10);

        var summary = StatementRuleEngine.MatchPayments([charge], [expense]);

        Assert.Null(charge.MatchedExpenseId);
        Assert.Equal(0, summary.MatchedPayments);
        Assert.Equal(1, summary.UnmatchedPlatformPayments);
    }
}
