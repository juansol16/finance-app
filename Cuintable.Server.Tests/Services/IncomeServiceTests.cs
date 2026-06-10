using Cuintable.Server.Data;
using Cuintable.Server.DTOs.Incomes;
using Cuintable.Server.Models;
using Cuintable.Server.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cuintable.Server.Tests.Services;

public class IncomeServiceTests
{
    private readonly AppDbContext _context;
    private readonly IncomeService _service;
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public IncomeServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new IncomeService(_context);
    }

    [Fact]
    public async Task CreateAsync_WithExchangeRate_StoresHonorarioBreakdown()
    {
        var request = new CreateIncomeRequest
        {
            Type = IncomeType.Nomina,
            Source = "US Company",
            Date = new DateOnly(2026, 6, 1),
            AmountMXN = 75_281.45m, // net deposited
            ExchangeRate = 16.78m
        };

        var response = await _service.CreateAsync(_tenantId, _userId, request);

        Assert.Equal(72_327.59m, response.HonorarioMXN);
        Assert.Equal(11_572.41m, response.IvaMXN);
        Assert.Equal(83_900.00m, response.SubtotalMXN);
        Assert.Equal(904.09m, response.IsrWithheldMXN);
        Assert.Equal(7_714.46m, response.IvaWithheldMXN);
        Assert.Equal(5_000.00m, response.TakeHomePayUSD);

        var stored = await _context.Incomes.SingleAsync(i => i.Id == response.Id);
        Assert.Equal(72_327.59m, stored.HonorarioMXN);
        Assert.Equal(5_000.00m, stored.TakeHomePayUSD);
    }

    [Fact]
    public async Task CreateAsync_WithoutExchangeRate_LeavesBreakdownNull()
    {
        var request = new CreateIncomeRequest
        {
            Type = IncomeType.Honorarios,
            Source = "MX Client",
            Date = new DateOnly(2026, 6, 1),
            AmountMXN = 5_000.00m
        };

        var response = await _service.CreateAsync(_tenantId, _userId, request);

        Assert.Null(response.HonorarioMXN);
        Assert.Null(response.IvaMXN);
        Assert.Null(response.SubtotalMXN);
        Assert.Null(response.IsrWithheldMXN);
        Assert.Null(response.IvaWithheldMXN);
        Assert.Null(response.TakeHomePayUSD);
    }

    [Fact]
    public async Task UpdateAsync_RemovingExchangeRate_ClearsBreakdown()
    {
        var created = await _service.CreateAsync(_tenantId, _userId, new CreateIncomeRequest
        {
            Type = IncomeType.Nomina,
            Source = "US Company",
            Date = new DateOnly(2026, 6, 1),
            AmountMXN = 75_281.45m,
            ExchangeRate = 16.78m
        });

        var updated = await _service.UpdateAsync(_tenantId, created.Id, new UpdateIncomeRequest
        {
            Type = IncomeType.Honorarios,
            Source = "MX Client",
            Date = new DateOnly(2026, 6, 1),
            AmountMXN = 75_281.45m,
            ExchangeRate = null
        });

        Assert.NotNull(updated);
        Assert.Null(updated!.HonorarioMXN);
        Assert.Null(updated.TakeHomePayUSD);
    }

    [Fact]
    public async Task UpdateAsync_ChangingNetAmount_RecalculatesBreakdown()
    {
        var created = await _service.CreateAsync(_tenantId, _userId, new CreateIncomeRequest
        {
            Type = IncomeType.Nomina,
            Source = "US Company",
            Date = new DateOnly(2026, 6, 1),
            AmountMXN = 75_281.45m,
            ExchangeRate = 16.78m
        });

        var updated = await _service.UpdateAsync(_tenantId, created.Id, new UpdateIncomeRequest
        {
            Type = IncomeType.Nomina,
            Source = "US Company",
            Date = new DateOnly(2026, 6, 1),
            AmountMXN = 10_408.40m,
            ExchangeRate = 16.00m
        });

        Assert.NotNull(updated);
        Assert.Equal(10_000.00m, updated!.HonorarioMXN);
        Assert.Equal(725.00m, updated.TakeHomePayUSD);
    }
}
