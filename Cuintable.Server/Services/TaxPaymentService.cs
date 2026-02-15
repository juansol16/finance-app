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

    public async Task<List<TaxPaymentResponse>> GetAllAsync(Guid userId)
    {
        var payments = await _context.TaxPayments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.PeriodYear)
            .ThenByDescending(p => p.PeriodMonth)
            .ToListAsync();

        return payments.Select(MapToResponse).ToList();
    }

    public async Task<TaxPaymentResponse?> GetByIdAsync(Guid userId, Guid id)
    {
        var payment = await _context.TaxPayments
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);
        
        return payment is null ? null : MapToResponse(payment);
    }

    public async Task<TaxPaymentResponse> CreateAsync(Guid userId, CreateTaxPaymentRequest request)
    {
        var payment = new TaxPayment
        {
            Id = Guid.NewGuid(),
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

    public async Task<TaxPaymentResponse?> UpdateAsync(Guid userId, Guid id, UpdateTaxPaymentRequest request)
    {
        var payment = await _context.TaxPayments
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (payment is null) return null;

        payment.AmountDue = request.AmountDue;
        payment.DueDate = request.DueDate;

        await _context.SaveChangesAsync();
        return MapToResponse(payment);
    }

    public async Task<TaxPaymentResponse?> UpdateDeterminationUrlAsync(Guid userId, Guid id, string determinationUrl)
    {
        var payment = await _context.TaxPayments
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (payment is null) return null;

        payment.DeterminationPdfUrl = determinationUrl;
        await _context.SaveChangesAsync();
        return MapToResponse(payment);
    }

    public async Task<TaxPaymentResponse?> UpdateReceiptUrlAsync(Guid userId, Guid id, string receiptUrl)
    {
        var payment = await _context.TaxPayments
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

        if (payment is null) return null;

        payment.PaymentReceiptUrl = receiptUrl;
        await _context.SaveChangesAsync();
        return MapToResponse(payment);
    }

    public async Task<TaxPaymentResponse?> MarkAsPaidAsync(Guid userId, Guid id, MarkAsPaidRequest request, string? receiptUrl)
    {
        var payment = await _context.TaxPayments
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

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

    public async Task<bool> DeleteAsync(Guid userId, Guid id)
    {
        var payment = await _context.TaxPayments
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

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
