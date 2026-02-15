using Cuintable.Server.Data;
using Cuintable.Server.DTOs.TaxPayments;
using Cuintable.Server.Models;
using Cuintable.Server.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cuintable.Server.Tests.Services;

public class TaxPaymentServiceTests
{
    private readonly AppDbContext _context;
    private readonly TaxPaymentService _service;
    private readonly Guid _tenantId;
    private readonly Guid _userId;

    public TaxPaymentServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new TaxPaymentService(_context);
        _tenantId = Guid.NewGuid();
        _userId = Guid.NewGuid();
    }

    [Fact]
    public async Task CreateAsync_ShouldCreatePayment()
    {
        // Arrange
        var request = new CreateTaxPaymentRequest
        {
            PeriodMonth = 1,
            PeriodYear = 2024,
            AmountDue = 1000m,
            DueDate = new DateOnly(2024, 2, 17)
        };

        // Act
        var result = await _service.CreateAsync(_tenantId, _userId, request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(request.AmountDue, result.AmountDue);
        Assert.Equal(TaxPaymentStatus.Pendiente, result.Status);

        var inDb = await _context.TaxPayments.FindAsync(result.Id);
        Assert.NotNull(inDb);
        Assert.Equal(_tenantId, inDb.TenantId);
        Assert.Equal(_userId, inDb.UserId);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnTenantPayments()
    {
        // Arrange
        var otherTenantId = Guid.NewGuid();
        _context.TaxPayments.AddRange(
            new TaxPayment { Id = Guid.NewGuid(), TenantId = _tenantId, UserId = _userId, PeriodMonth = 1, PeriodYear = 2024, AmountDue = 100 },
            new TaxPayment { Id = Guid.NewGuid(), TenantId = _tenantId, UserId = _userId, PeriodMonth = 2, PeriodYear = 2024, AmountDue = 200 },
            new TaxPayment { Id = Guid.NewGuid(), TenantId = otherTenantId, UserId = Guid.NewGuid(), PeriodMonth = 1, PeriodYear = 2024, AmountDue = 300 }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetAllAsync(_tenantId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, p => p.AmountDue == 100);
        Assert.Contains(result, p => p.AmountDue == 200);
    }

    [Fact]
    public async Task MarkAsPaidAsync_ShouldUpdateStatusAndReceipt()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        _context.TaxPayments.Add(new TaxPayment
        {
            Id = paymentId,
            TenantId = _tenantId,
            UserId = _userId,
            Status = TaxPaymentStatus.Pendiente,
            PeriodMonth = 1,
            PeriodYear = 2024,
            AmountDue = 500
        });
        await _context.SaveChangesAsync();

        var request = new MarkAsPaidRequest { PaymentDate = new DateOnly(2024, 2, 15) };
        var receiptUrl = "http://example.com/receipt.pdf";

        // Act
        var result = await _service.MarkAsPaidAsync(_tenantId, paymentId, request, receiptUrl);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TaxPaymentStatus.Pagado, result.Status);
        Assert.Equal(request.PaymentDate, result.PaymentDate);
        Assert.Equal(receiptUrl, result.PaymentReceiptUrl);

        var inDb = await _context.TaxPayments.FindAsync(paymentId);
        Assert.Equal(TaxPaymentStatus.Pagado, inDb!.Status);
    }

    [Fact]
    public async Task UpdateDeterminationUrlAsync_ShouldUpdateUrl()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        _context.TaxPayments.Add(new TaxPayment
        {
            Id = paymentId,
            TenantId = _tenantId,
            UserId = _userId,
            Status = TaxPaymentStatus.Pendiente,
            PeriodMonth = 1,
            PeriodYear = 2024,
            AmountDue = 500
        });
        await _context.SaveChangesAsync();

        var url = "http://example.com/determination.pdf";

        // Act
        var result = await _service.UpdateDeterminationUrlAsync(_tenantId, paymentId, url);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(url, result.DeterminationPdfUrl);
    }
}
