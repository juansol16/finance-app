using Cuintable.Server.Data;
using Cuintable.Server.DTOs.TaxPayments;
using Cuintable.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuintable.Server.Services;

public class TaxPaymentService : ITaxPaymentService
{
    private readonly AppDbContext _context;

    public TaxPaymentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TaxPaymentResponse>> GetAllAsync(Guid tenantId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var query = _context.TaxPayments
            .Where(p => p.TenantId == tenantId);

        if (startDate.HasValue)
        {
            query = query.Where(p => (p.Status == TaxPaymentStatus.Pagado && p.PaymentDate >= startDate.Value) || 
                                     (p.Status != TaxPaymentStatus.Pagado && p.DueDate >= startDate.Value));
        }

        if (endDate.HasValue)
        {
            query = query.Where(p => (p.Status == TaxPaymentStatus.Pagado && p.PaymentDate <= endDate.Value) || 
                                     (p.Status != TaxPaymentStatus.Pagado && p.DueDate <= endDate.Value));
        }

        return await query
            .OrderByDescending(p => p.PeriodYear)
            .ThenByDescending(p => p.PeriodMonth)
            .Select(p => MapToResponse(p))
            .ToListAsync();
    }

    public async Task<TaxPaymentResponse?> GetByIdAsync(Guid tenantId, Guid id)
    {
        var payment = await _context.TaxPayments
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        return payment is null ? null : MapToResponse(payment);
    }

    public async Task<TaxPaymentResponse> CreateAsync(Guid tenantId, Guid userId, CreateTaxPaymentRequest request)
    {
        var payment = new TaxPayment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            PeriodMonth = request.PeriodMonth,
            PeriodYear = request.PeriodYear,
            AmountDue = request.AmountDue,
            DueDate = request.DueDate,
            Status = TaxPaymentStatus.Pendiente
        };

        _context.TaxPayments.Add(payment);
        await _context.SaveChangesAsync();
        return MapToResponse(payment);
    }

    public async Task<TaxPaymentResponse?> UpdateAsync(Guid tenantId, Guid id, UpdateTaxPaymentRequest request)
    {
        var payment = await _context.TaxPayments
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (payment is null) return null;

        payment.AmountDue = request.AmountDue;
        payment.DueDate = request.DueDate;

        await _context.SaveChangesAsync();
        return MapToResponse(payment);
    }

    public async Task<TaxPaymentResponse?> UpdateDeterminationUrlAsync(Guid tenantId, Guid id, string determinationUrl)
    {
        var payment = await _context.TaxPayments
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (payment is null) return null;

        payment.DeterminationPdfUrl = determinationUrl;
        await _context.SaveChangesAsync();
        return MapToResponse(payment);
    }

    public async Task<TaxPaymentResponse?> UpdateReceiptUrlAsync(Guid tenantId, Guid id, string receiptUrl)
    {
        var payment = await _context.TaxPayments
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (payment is null) return null;

        payment.PaymentReceiptUrl = receiptUrl;
        await _context.SaveChangesAsync();
        return MapToResponse(payment);
    }

    public async Task<TaxPaymentResponse?> MarkAsPaidAsync(Guid tenantId, Guid id, MarkAsPaidRequest request, string? receiptUrl)
    {
        var payment = await _context.TaxPayments
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (payment is null) return null;

        payment.Status = TaxPaymentStatus.Pagado;
        payment.PaymentDate = request.PaymentDate;
        if (receiptUrl is not null)
        {
            payment.PaymentReceiptUrl = receiptUrl;
        }

        await _context.SaveChangesAsync();
        return MapToResponse(payment);
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id)
    {
        var payment = await _context.TaxPayments
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId);

        if (payment is null) return false;

        _context.TaxPayments.Remove(payment);
        await _context.SaveChangesAsync();
        return true;
    }

    private static TaxPaymentResponse MapToResponse(TaxPayment p) => new()
    {
        Id = p.Id,
        PeriodMonth = p.PeriodMonth,
        PeriodYear = p.PeriodYear,
        AmountDue = p.AmountDue,
        DueDate = p.DueDate,
        Status = p.Status,
        PaymentDate = p.PaymentDate,
        DeterminationPdfUrl = p.DeterminationPdfUrl,
        PaymentReceiptUrl = p.PaymentReceiptUrl,
        CreatedAt = p.CreatedAt
    };
}
