using Cuintable.Server.Data;
using Cuintable.Server.Models;
using Cuintable.Server.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cuintable.Server.Tests.Services;

public class ResicoTaxServiceTests
{
    private readonly AppDbContext _context;
    private readonly ResicoTaxService _service;

    public ResicoTaxServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new ResicoTaxService(_context);
    }

    [Theory]
    [InlineData(10000, 100)]      // 10,000 * 1.00% = 100
    [InlineData(25000, 250)]      // 25,000 * 1.00% = 250
    [InlineData(30000, 330)]      // 30,000 * 1.10% = 330
    [InlineData(60000, 900)]      // 60,000 * 1.50% = 900
    [InlineData(100000, 2000)]    // 100,000 * 2.00% = 2000
    [InlineData(250000, 6250)]    // 250,000 * 2.50% = 6250
    public void CalculateResicoISR_ReturnsCorrectAmount(decimal income, decimal expectedTax)
    {
        var result = _service.CalculateResicoISR(income);
        Assert.Equal(expectedTax, result);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_ReturnsCorrectSummary()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var year = 2024;
        var month = 5;

        // Add 2 Incomes
        _context.Incomes.AddRange(
            new Income { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Date = new DateOnly(year, month, 5), AmountMXN = 20000, Source="Job", Type=IncomeType.Nomina },
            new Income { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Date = new DateOnly(year, month, 15), AmountMXN = 30000, Source="Freelance", Type=IncomeType.Honorarios }
        );

        // Add 1 Deductible Expense
        _context.TaxableExpenses.Add(
            new TaxableExpense { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Date = new DateOnly(year, month, 10), AmountMXN = 5000, Vendor="CFE", Category=TaxableExpenseCategory.Luz }
        );

        // Add 1 Expense outside date range
        _context.TaxableExpenses.Add(
            new TaxableExpense { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Date = new DateOnly(year, month + 1, 1), AmountMXN = 1000, Vendor="Telmex", Category=TaxableExpenseCategory.Internet }
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetMonthlySummaryAsync(tenantId, month, year);

        // Assert
        Assert.Equal(month, result.Month);
        Assert.Equal(year, result.Year);
        Assert.Equal(50000, result.TotalIncome); // 20k + 30k
        Assert.Equal(5000, result.TotalDeductibleExpenses);
        Assert.Equal(45000, result.TaxableBase); // 50k - 5k

        // Tax on 50k: 50,000 <= 50,000 => 1.10% => 550
        Assert.Equal(550, result.EstimatedISR);
        Assert.Equal(0.011m, result.EffectiveTaxRate, 4); // 550 / 50000 = 0.011

        // IVA: charged 16% of 50k = 8,000; expense without CFDI IVA is
        // estimated at 16/116 of 5,000 = 689.66 creditable
        Assert.Equal(8_000m, result.EstimatedIVA);
        Assert.Equal(689.66m, result.IvaCreditable);
        Assert.Equal(7_310.34m, result.IvaNetDue);
        Assert.Equal(550m + 7_310.34m, result.TotalDue);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_MatchesAccountantMonthlyStatement()
    {
        // Regression: mirrors the accountant's May 2026 statement.
        // Invoiced 87,328.07 => ISR 2% (1,746.56) minus 1.25% withheld (1,091.60) => 655
        // IVA charged 13,972.49 minus withheld 9,314.41 minus creditable 1,660 => 2,998
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _context.Incomes.Add(new Income
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            Date = new DateOnly(2026, 5, 15), Source = "US Company", Type = IncomeType.Honorarios,
            AmountMXN = 90_894.55m,
            HonorarioMXN = 87_328.07m, IvaMXN = 13_972.49m,
            IsrWithheldMXN = 1_091.60m, IvaWithheldMXN = 9_314.41m
        });

        _context.TaxableExpenses.Add(new TaxableExpense
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            Date = new DateOnly(2026, 5, 20), AmountMXN = 12_035.00m, IvaMXN = 1_660.00m,
            Vendor = "Amazon MX", Category = TaxableExpenseCategory.EquipoOficina,
            ValidationStatus = TaxableExpenseValidationStatus.Valida
        });

        await _context.SaveChangesAsync();

        var result = await _service.GetMonthlySummaryAsync(tenantId, 5, 2026);

        Assert.Equal(87_328.07m, result.TotalIncome);
        Assert.Equal(1_746.5614m, result.EstimatedISR);       // 2% bracket
        Assert.Equal(654.9614m, result.IsrNetDue);            // accountant: $655
        Assert.Equal(13_972.49m, result.EstimatedIVA);
        Assert.Equal(9_314.41m, result.IvaWithheld);
        Assert.Equal(1_660.00m, result.IvaCreditable);
        Assert.Equal(2_998.08m, result.IvaNetDue);            // accountant: $2,998
        Assert.Equal(3_653.0414m, result.TotalDue);           // accountant: $3,653
        Assert.Equal(0m, result.IvaFavorBalance);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_RejectedExpensesAreExcluded()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _context.Incomes.Add(new Income
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            Date = new DateOnly(2026, 5, 5), AmountMXN = 50_000m, Source = "Job", Type = IncomeType.Honorarios
        });

        _context.TaxableExpenses.AddRange(
            new TaxableExpense
            {
                Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
                Date = new DateOnly(2026, 5, 10), AmountMXN = 5_800m, IvaMXN = 800m,
                Vendor = "Telmex", Category = TaxableExpenseCategory.Internet,
                ValidationStatus = TaxableExpenseValidationStatus.Valida
            },
            new TaxableExpense
            {
                Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
                Date = new DateOnly(2026, 5, 12), AmountMXN = 11_600m, IvaMXN = 1_600m,
                Vendor = "Liverpool", Category = TaxableExpenseCategory.Otro,
                ValidationStatus = TaxableExpenseValidationStatus.Rechazada,
                ValidationComment = "Gasto personal, no deducible"
            });

        await _context.SaveChangesAsync();

        var result = await _service.GetMonthlySummaryAsync(tenantId, 5, 2026);

        // Only the approved expense counts for deduction and IVA credit
        Assert.Equal(5_800m, result.TotalDeductibleExpenses);
        Assert.Equal(44_200m, result.TaxableBase);
        Assert.Equal(800m, result.IvaCreditable);
        Assert.Equal(8_000m - 800m, result.IvaNetDue);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_IvaFavorCarriesToNextMonth()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Month 1: IVA charged 1,600 but 2,000 creditable => 400 in favor, nothing due
        _context.Incomes.Add(new Income
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            Date = new DateOnly(2026, 1, 10), AmountMXN = 10_000m, Source = "Job", Type = IncomeType.Honorarios
        });
        _context.TaxableExpenses.Add(new TaxableExpense
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            Date = new DateOnly(2026, 1, 15), AmountMXN = 14_500m, IvaMXN = 2_000m,
            Vendor = "Office Depot", Category = TaxableExpenseCategory.EquipoOficina
        });

        // Month 2: IVA charged 1,600, no expenses => 1,600 minus 400 carried over
        _context.Incomes.Add(new Income
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            Date = new DateOnly(2026, 2, 10), AmountMXN = 10_000m, Source = "Job", Type = IncomeType.Honorarios
        });

        await _context.SaveChangesAsync();

        var january = await _service.GetMonthlySummaryAsync(tenantId, 1, 2026);
        Assert.Equal(0m, january.IvaNetDue);
        Assert.Equal(400m, january.IvaFavorBalance);

        var february = await _service.GetMonthlySummaryAsync(tenantId, 2, 2026);
        Assert.Equal(1_200m, february.IvaNetDue);
        Assert.Equal(0m, february.IvaFavorBalance);
    }

    [Fact]
    public async Task GetMonthlySummaryAsync_WithHonorarioBreakdown_UsesFiscalIncomeAndCreditsRetentions()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var year = 2026;
        var month = 6;

        // USD income with withholding client (accountant's sheet values):
        // net deposited 75,281.45, honorario 72,327.59, IVA 11,572.41,
        // ISR ret 904.09, IVA ret 7,714.46
        _context.Incomes.Add(new Income
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            Date = new DateOnly(year, month, 5), Source = "US Company", Type = IncomeType.Nomina,
            AmountMXN = 75_281.45m, ExchangeRate = 16.78m,
            HonorarioMXN = 72_327.59m, IvaMXN = 11_572.41m, SubtotalMXN = 83_900.00m,
            IsrWithheldMXN = 904.09m, IvaWithheldMXN = 7_714.46m, TakeHomePayUSD = 5_000.00m
        });

        // Plain MXN income, no breakdown: taxed on AmountMXN as before
        _context.Incomes.Add(new Income
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            Date = new DateOnly(year, month, 15), Source = "MX Client", Type = IncomeType.Honorarios,
            AmountMXN = 10_000m
        });

        await _context.SaveChangesAsync();

        var result = await _service.GetMonthlySummaryAsync(tenantId, month, year);

        // Fiscal income = honorario (72,327.59) + plain amount (10,000), NOT the net deposit
        Assert.Equal(82_327.59m, result.TotalIncome);
        Assert.Equal(82_327.59m, result.AnnualAccumulatedIncome);

        // ISR: 82,327.59 <= 83,333.33 => 1.50% => 1,234.91385, minus 904.09 withheld
        Assert.Equal(1_234.91385m, result.EstimatedISR);
        Assert.Equal(904.09m, result.IsrWithheld);
        Assert.Equal(330.82385m, result.IsrNetDue);

        // IVA: stored 11,572.41 + 16% of 10,000, minus 7,714.46 withheld
        Assert.Equal(13_172.41m, result.EstimatedIVA);
        Assert.Equal(7_714.46m, result.IvaWithheld);
        Assert.Equal(5_457.95m, result.IvaNetDue);
    }

    [Fact]
    public async Task GetDashboardChartsAsync_CashFlowUsesNetDeposit_OperationsUseFiscalIncome()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        _context.Incomes.Add(new Income
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            Date = new DateOnly(today.Year, today.Month, 1), Source = "US Company", Type = IncomeType.Nomina,
            AmountMXN = 75_281.45m, ExchangeRate = 16.78m,
            HonorarioMXN = 72_327.59m, IvaMXN = 11_572.41m, SubtotalMXN = 83_900.00m,
            IsrWithheldMXN = 904.09m, IvaWithheldMXN = 7_714.46m, TakeHomePayUSD = 5_000.00m
        });

        await _context.SaveChangesAsync();

        var result = await _service.GetDashboardChartsAsync(tenantId, 12);

        var cashFlow = result.CashFlow.Single(c => c.Month == today.Month && c.Year == today.Year);
        Assert.Equal(75_281.45m, cashFlow.TotalIncome); // real money deposited

        var ops = result.Operations.Single(o => o.Month == today.Month && o.Year == today.Year);
        Assert.Equal(72_327.59m, ops.Income); // invoiced honorario
        // ISR: 72,327.59 * 1.50% = 1,084.91385 - 904.09 withheld
        Assert.Equal(180.82385m, ops.ISR);
        // IVA: 11,572.41 - 7,714.46 withheld
        Assert.Equal(3_857.95m, ops.IVANet);
    }

    [Fact]
    public async Task GetLastUsdIncomeAsync_ReturnsMostRecentUsdIncome()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Older USD income
        _context.Incomes.Add(new Income
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            Date = new DateOnly(2026, 4, 30), Source = "US Company", Type = IncomeType.Nomina,
            AmountMXN = 70_000m, ExchangeRate = 17.20m, TakeHomePayUSD = 4_536.31m
        });
        // Most recent USD income
        _context.Incomes.Add(new Income
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            Date = new DateOnly(2026, 5, 31), Source = "US Company", Type = IncomeType.Nomina,
            AmountMXN = 75_281.45m, ExchangeRate = 16.78m, TakeHomePayUSD = 5_000.00m
        });
        // Newer MXN income without USD data: must be ignored
        _context.Incomes.Add(new Income
        {
            Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId,
            Date = new DateOnly(2026, 6, 5), Source = "MX Client", Type = IncomeType.Honorarios,
            AmountMXN = 10_000m
        });

        await _context.SaveChangesAsync();

        var result = await _service.GetLastUsdIncomeAsync(tenantId);

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 5, 31), result!.Date);
        Assert.Equal(5_000.00m, result.TakeHomePayUSD);
        Assert.Equal(16.78m, result.ExchangeRate);
        Assert.Equal(75_281.45m, result.NetReceivedMXN);
    }

    [Fact]
    public async Task GetLastUsdIncomeAsync_ReturnsNullWhenNoUsdIncomes()
    {
        var result = await _service.GetLastUsdIncomeAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAnnualSummaryAsync_ReturnsCorrectAnnualTotals()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var year = 2024;

        // Month 1: 20k income (tax 200)
        _context.Incomes.Add(new Income { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Date = new DateOnly(year, 1, 15), AmountMXN = 20000, Source="Job", Type=IncomeType.Nomina });

        // Month 2: 100k income (tax 2000)
        _context.Incomes.Add(new Income { Id = Guid.NewGuid(), TenantId = tenantId, UserId = userId, Date = new DateOnly(year, 2, 15), AmountMXN = 100000, Source="Job", Type=IncomeType.Honorarios });

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAnnualSummaryAsync(tenantId, year);

        // Assert
        Assert.Equal(year, result.Year);
        Assert.Equal(12, result.MonthlySummaries.Count);
        Assert.Equal(120000, result.TotalAnnualIncome);
        Assert.Equal(2200, result.TotalAnnualISR); // 200 + 2000
    }
}
