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
        var userId = Guid.NewGuid();
        var year = 2024;
        var month = 5;

        // Add 2 Incomes
        _context.Incomes.AddRange(
            new Income { Id = Guid.NewGuid(), UserId = userId, Date = new DateOnly(year, month, 5), AmountMXN = 20000, Source="Job", Type=IncomeType.Nomina },
            new Income { Id = Guid.NewGuid(), UserId = userId, Date = new DateOnly(year, month, 15), AmountMXN = 30000, Source="Freelance", Type=IncomeType.Honorarios }
        );

        // Add 1 Deductible Expense
        _context.TaxableExpenses.Add(
            new TaxableExpense { Id = Guid.NewGuid(), UserId = userId, Date = new DateOnly(year, month, 10), AmountMXN = 5000, Vendor="CFE", Category=TaxableExpenseCategory.Luz }
        );

        // Add 1 Expense outside date range
        _context.TaxableExpenses.Add(
            new TaxableExpense { Id = Guid.NewGuid(), UserId = userId, Date = new DateOnly(year, month + 1, 1), AmountMXN = 1000, Vendor="Telmex", Category=TaxableExpenseCategory.Internet }
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetMonthlySummaryAsync(userId, month, year);

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
    public async Task GetAnnualSummaryAsync_ReturnsCorrectAnnualTotals()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var year = 2024;

        // Month 1: 20k income (tax 200)
        _context.Incomes.Add(new Income { Id = Guid.NewGuid(), UserId = userId, Date = new DateOnly(year, 1, 15), AmountMXN = 20000, Source="Job", Type=IncomeType.Nomina });
        
        // Month 2: 100k income (tax 2000)
        _context.Incomes.Add(new Income { Id = Guid.NewGuid(), UserId = userId, Date = new DateOnly(year, 2, 15), AmountMXN = 100000, Source="Job", Type=IncomeType.Honorarios });

        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAnnualSummaryAsync(userId, year);

        // Assert
        Assert.Equal(year, result.Year);
        Assert.Equal(12, result.MonthlySummaries.Count);
        Assert.Equal(120000, result.TotalAnnualIncome);
        Assert.Equal(2200, result.TotalAnnualISR); // 200 + 2000
    }
}
