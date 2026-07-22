using Cuintable.Server.Data;
using Cuintable.Server.Models;
using Cuintable.Server.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cuintable.Server.Tests.Services;

public class FinancialAdvisorServiceTests
{
    private readonly AppDbContext _context;
    private readonly FinancialAdvisorService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public FinancialAdvisorServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _service = new FinancialAdvisorService(_context, new Mock<IFileStorageService>().Object);
    }

    private void AddStatement(int year, int month, DateTime processedAt)
    {
        _context.CardStatements.Add(new CardStatement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            UserId = _userId,
            PeriodYear = year,
            PeriodMonth = month,
            Status = StatementStatus.Completed,
            PdfUrl = "statements/test.pdf",
            ProcessedAt = processedAt
        });
    }

    private void AddAdvice(int year, int month, DateTime generatedAt, int statementCount)
    {
        _context.MonthlyAdvices.Add(new MonthlyAdvice
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            PeriodYear = year,
            PeriodMonth = month,
            AdviceJson = """{"summary":"ok","suggestions":[]}""",
            StatementCount = statementCount,
            GeneratedAt = generatedAt
        });
    }

    [Fact]
    public async Task GetDashboardAsync_WithoutAdvice_ReturnsNullMonthlyAdvice()
    {
        AddStatement(2026, 6, DateTime.UtcNow);
        await _context.SaveChangesAsync();

        var dashboard = await _service.GetDashboardAsync(_tenantId, 2026, 6);

        Assert.Null(dashboard.MonthlyAdvice);
    }

    [Fact]
    public async Task GetDashboardAsync_AdviceCoveringAllStatements_IsNotStale()
    {
        AddStatement(2026, 6, DateTime.UtcNow.AddHours(-2));
        AddStatement(2026, 6, DateTime.UtcNow.AddHours(-1));
        AddAdvice(2026, 6, DateTime.UtcNow, statementCount: 2);
        await _context.SaveChangesAsync();

        var dashboard = await _service.GetDashboardAsync(_tenantId, 2026, 6);

        Assert.NotNull(dashboard.MonthlyAdvice);
        Assert.False(dashboard.MonthlyAdvice.IsStale);
    }

    [Fact]
    public async Task GetDashboardAsync_StatementUploadedAfterAdvice_IsStale()
    {
        AddStatement(2026, 6, DateTime.UtcNow.AddHours(-2));
        AddAdvice(2026, 6, DateTime.UtcNow.AddHours(-1), statementCount: 1);
        // Second statement processed after the advice was generated
        AddStatement(2026, 6, DateTime.UtcNow);
        await _context.SaveChangesAsync();

        var dashboard = await _service.GetDashboardAsync(_tenantId, 2026, 6);

        Assert.NotNull(dashboard.MonthlyAdvice);
        Assert.True(dashboard.MonthlyAdvice.IsStale);
    }

    [Fact]
    public async Task GetDashboardAsync_StatementDeletedAfterAdvice_IsStale()
    {
        AddStatement(2026, 6, DateTime.UtcNow.AddHours(-2));
        AddAdvice(2026, 6, DateTime.UtcNow, statementCount: 2);
        await _context.SaveChangesAsync();

        var dashboard = await _service.GetDashboardAsync(_tenantId, 2026, 6);

        Assert.NotNull(dashboard.MonthlyAdvice);
        Assert.True(dashboard.MonthlyAdvice.IsStale);
    }

    [Fact]
    public async Task GetDashboardAsync_StatementReprocessedAfterAdvice_IsStale()
    {
        AddStatement(2026, 6, DateTime.UtcNow); // reprocessed just now
        AddAdvice(2026, 6, DateTime.UtcNow.AddHours(-1), statementCount: 1);
        await _context.SaveChangesAsync();

        var dashboard = await _service.GetDashboardAsync(_tenantId, 2026, 6);

        Assert.NotNull(dashboard.MonthlyAdvice);
        Assert.True(dashboard.MonthlyAdvice.IsStale);
    }
}
