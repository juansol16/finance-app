using System.Text.Json;
using Cuintable.Server.Data;
using Cuintable.Server.Models;
using Cuintable.Server.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Cuintable.Server.Tests.Services;

public class MonthlyAdviceServiceTests
{
    private const string CannedAdvice = """{"summary":"ok","suggestions":[]}""";

    private readonly AppDbContext _context;
    private readonly Mock<IGeminiClient> _gemini;
    private readonly MonthlyAdviceService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public MonthlyAdviceServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _gemini = new Mock<IGeminiClient>();
        _gemini
            .Setup(g => g.GenerateJsonAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<(byte[], string)?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CannedAdvice);

        var configuration = new ConfigurationBuilder().Build();
        _service = new MonthlyAdviceService(_context, _gemini.Object, configuration);
    }

    private CardStatement AddStatement(
        int year, int month,
        StatementStatus status = StatementStatus.Completed,
        params StatementTransaction[] transactions)
    {
        var statement = new CardStatement
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            UserId = _userId,
            PeriodYear = year,
            PeriodMonth = month,
            Status = status,
            PdfUrl = "statements/test.pdf",
            ProcessedAt = DateTime.UtcNow
        };
        foreach (var t in transactions)
        {
            t.Id = Guid.NewGuid();
            t.TenantId = _tenantId;
            t.StatementId = statement.Id;
            statement.Transactions.Add(t);
        }
        _context.CardStatements.Add(statement);
        return statement;
    }

    private static StatementTransaction Charge(decimal amount, StatementCategory category = StatementCategory.Supermercado) =>
        new() { Date = new DateOnly(2026, 6, 15), Merchant = "TEST", AmountMXN = amount, Category = category, Type = StatementTransactionType.Charge };

    [Fact]
    public async Task GenerateAsync_WithNoCompletedStatements_ReturnsNullAndSkipsGemini()
    {
        AddStatement(2026, 6, StatementStatus.Failed, Charge(100));
        await _context.SaveChangesAsync();

        var result = await _service.GenerateAsync(_tenantId, _userId, 2026, 6);

        Assert.Null(result);
        _gemini.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GenerateAsync_PersistsSingleRowPerPeriod_AndUpdatesOnRegenerate()
    {
        AddStatement(2026, 6, StatementStatus.Completed, Charge(500));
        await _context.SaveChangesAsync();

        var first = await _service.GenerateAsync(_tenantId, _userId, 2026, 6);

        AddStatement(2026, 6, StatementStatus.Completed, Charge(300));
        await _context.SaveChangesAsync();

        var second = await _service.GenerateAsync(_tenantId, _userId, 2026, 6);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(1, first.StatementCount);
        Assert.Equal(2, second.StatementCount);
        Assert.False(second.IsStale);
        Assert.Equal(CannedAdvice, second.AdviceJson);

        var rows = await _context.MonthlyAdvices.ToListAsync();
        Assert.Single(rows);
        Assert.Equal(2, rows[0].StatementCount);
    }

    [Fact]
    public async Task GenerateAsync_CombinesSpendingAcrossStatements_ExcludingMoneyMovement()
    {
        AddStatement(2026, 6, StatementStatus.Completed,
            Charge(500),
            Charge(1000, StatementCategory.Transferencias)); // movement, not spending
        AddStatement(2026, 6, StatementStatus.Completed, Charge(300));
        // Other period and other tenant must not leak in
        AddStatement(2026, 5, StatementStatus.Completed, Charge(9999));
        await _context.SaveChangesAsync();

        string? capturedPrompt = null;
        _gemini
            .Setup(g => g.GenerateJsonAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<(byte[], string)?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, (byte[], string)?, string, CancellationToken>(
                (_, _, prompt, _, _, _) => capturedPrompt = prompt)
            .ReturnsAsync(CannedAdvice);

        var result = await _service.GenerateAsync(_tenantId, _userId, 2026, 6);

        Assert.NotNull(result);
        Assert.NotNull(capturedPrompt);

        using var doc = JsonDocument.Parse(capturedPrompt);
        var root = doc.RootElement;
        Assert.Equal(2, root.GetProperty("statementCount").GetInt32());
        Assert.Equal(2, root.GetProperty("accounts").GetArrayLength());
        var combined = root.GetProperty("combined");
        Assert.Equal(800m, combined.GetProperty("totalSpendingMXN").GetDecimal());
        Assert.Equal(1000m, combined.GetProperty("transfersOutMXN").GetDecimal());
    }

    [Fact]
    public async Task GenerateAsync_IncludesMonthlyIncome()
    {
        AddStatement(2026, 6, StatementStatus.Completed, Charge(500));
        _context.Incomes.Add(new Income
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantId,
            UserId = _userId,
            Date = new DateOnly(2026, 6, 10),
            AmountMXN = 45000m
        });
        await _context.SaveChangesAsync();

        string? capturedPrompt = null;
        _gemini
            .Setup(g => g.GenerateJsonAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<(byte[], string)?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, (byte[], string)?, string, CancellationToken>(
                (_, _, prompt, _, _, _) => capturedPrompt = prompt)
            .ReturnsAsync(CannedAdvice);

        await _service.GenerateAsync(_tenantId, _userId, 2026, 6);

        Assert.NotNull(capturedPrompt);
        using var doc = JsonDocument.Parse(capturedPrompt);
        Assert.Equal(45000m, doc.RootElement.GetProperty("monthlyNetIncomeMXN").GetDecimal());
    }
}
