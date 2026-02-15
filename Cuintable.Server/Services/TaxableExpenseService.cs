using Cuintable.Server.Data;
using Cuintable.Server.DTOs.TaxableExpenses;
using Cuintable.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuintable.Server.Services;

public class TaxableExpenseService : ITaxableExpenseService
{
    private readonly AppDbContext _db;

    public TaxableExpenseService(AppDbContext db) => _db = db;

    public async Task<List<TaxableExpenseResponse>> GetAllAsync(Guid tenantId)
    {
        return await _db.TaxableExpenses
            .Include(t => t.CreditCard)
            .Include(t => t.Expense)
            .Where(t => t.TenantId == tenantId)
            .OrderByDescending(t => t.Date)
            .Select(t => MapToResponse(t))
            .ToListAsync();
    }

    public async Task<TaxableExpenseResponse?> GetByIdAsync(Guid tenantId, Guid id)
    {
        var item = await _db.TaxableExpenses
            .Include(t => t.CreditCard)
            .Include(t => t.Expense)
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);
        return item is null ? null : MapToResponse(item);
    }

    public async Task<TaxableExpenseResponse> CreateAsync(Guid tenantId, Guid userId, CreateTaxableExpenseRequest request)
    {
        var item = new TaxableExpense
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Category = request.Category,
            CreditCardId = request.CreditCardId,
            ExpenseId = request.ExpenseId,
            Date = request.Date,
            AmountMXN = request.AmountMXN,
            Description = request.Description,
            Vendor = request.Vendor
        };

        _db.TaxableExpenses.Add(item);
        await _db.SaveChangesAsync();

        await _db.Entry(item).Reference(t => t.CreditCard).LoadAsync();
        await _db.Entry(item).Reference(t => t.Expense).LoadAsync();
        return MapToResponse(item);
    }

    public async Task<TaxableExpenseResponse?> UpdateAsync(Guid tenantId, Guid id, UpdateTaxableExpenseRequest request)
    {
        var item = await _db.TaxableExpenses
            .Include(t => t.CreditCard)
            .Include(t => t.Expense)
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);
        if (item is null) return null;

        item.Category = request.Category;
        item.CreditCardId = request.CreditCardId;
        item.ExpenseId = request.ExpenseId;
        item.Date = request.Date;
        item.AmountMXN = request.AmountMXN;
        item.Description = request.Description;
        item.Vendor = request.Vendor;

        await _db.SaveChangesAsync();

        await _db.Entry(item).Reference(t => t.CreditCard).LoadAsync();
        await _db.Entry(item).Reference(t => t.Expense).LoadAsync();
        return MapToResponse(item);
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id)
    {
        var item = await _db.TaxableExpenses
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);
        if (item is null) return false;

        _db.TaxableExpenses.Remove(item);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<TaxableExpenseResponse?> UpdateFileUrlsAsync(Guid tenantId, Guid id, string? pdfUrl, string? xmlUrl, string? xmlMetadata)
    {
        var item = await _db.TaxableExpenses
            .Include(t => t.CreditCard)
            .Include(t => t.Expense)
            .FirstOrDefaultAsync(t => t.Id == id && t.TenantId == tenantId);
        if (item is null) return null;

        if (pdfUrl is not null) item.InvoicePdfUrl = pdfUrl;
        if (xmlUrl is not null) item.InvoiceXmlUrl = xmlUrl;
        if (xmlMetadata is not null) item.XmlMetadata = xmlMetadata;

        await _db.SaveChangesAsync();
        return MapToResponse(item);
    }

    private static TaxableExpenseResponse MapToResponse(TaxableExpense t) => new()
    {
        Id = t.Id,
        Category = t.Category,
        CreditCardId = t.CreditCardId,
        CreditCardLabel = t.CreditCard is not null
            ? $"{t.CreditCard.Nickname} (****{t.CreditCard.LastFourDigits})"
            : null,
        ExpenseId = t.ExpenseId,
        LinkedExpenseLabel = t.Expense is not null
            ? $"{t.Expense.Date:yyyy-MM-dd} — ${t.Expense.AmountMXN:N2}"
            : null,
        Date = t.Date,
        AmountMXN = t.AmountMXN,
        Description = t.Description,
        Vendor = t.Vendor,
        InvoicePdfUrl = t.InvoicePdfUrl,
        InvoiceXmlUrl = t.InvoiceXmlUrl,
        XmlMetadata = t.XmlMetadata,
        CreatedAt = t.CreatedAt,
        UpdatedAt = t.UpdatedAt
    };
}
