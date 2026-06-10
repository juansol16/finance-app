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
