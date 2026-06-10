using Cuintable.Server.Data;
using Cuintable.Server.DTOs.Incomes;
using Cuintable.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuintable.Server.Services;

public class IncomeService : IIncomeService
{
    private readonly AppDbContext _db;

    public IncomeService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<IncomeResponse>> GetAllAsync(Guid tenantId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var query = _db.Incomes.Where(i => i.TenantId == tenantId);

        if (startDate.HasValue) query = query.Where(i => i.Date >= startDate.Value);
        if (endDate.HasValue) query = query.Where(i => i.Date <= endDate.Value);

        return await query
            .OrderByDescending(i => i.Date)
            .Select(i => MapToResponse(i))
            .ToListAsync();
    }

    public async Task<IncomeResponse?> GetByIdAsync(Guid tenantId, Guid id)
    {
        var income = await _db.Incomes
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        return income is null ? null : MapToResponse(income);
    }

    public async Task<IncomeResponse> CreateAsync(Guid tenantId, Guid userId, CreateIncomeRequest request)
    {
        var income = new Income
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Type = request.Type,
            Source = request.Source,
            Date = request.Date,
            AmountMXN = request.AmountMXN,
            ExchangeRate = request.ExchangeRate,
            AmountUSD = request.AmountUSD,
            Description = request.Description
        };

        ApplyHonorarioBreakdown(income);

        _db.Incomes.Add(income);
        await _db.SaveChangesAsync();

        return MapToResponse(income);
    }

    public async Task<IncomeResponse?> UpdateAsync(Guid tenantId, Guid id, UpdateIncomeRequest request)
    {
        var income = await _db.Incomes
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        if (income is null) return null;

        income.Type = request.Type;
        income.Source = request.Source;
        income.Date = request.Date;
        income.AmountMXN = request.AmountMXN;
        income.ExchangeRate = request.ExchangeRate;
        income.AmountUSD = request.AmountUSD;
        income.Description = request.Description;

        ApplyHonorarioBreakdown(income);

        await _db.SaveChangesAsync();

        return MapToResponse(income);
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id)
    {
        var income = await _db.Incomes
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        if (income is null) return false;

        _db.Incomes.Remove(income);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<IncomeResponse?> UpdateFileUrlsAsync(Guid tenantId, Guid id, string? pdfUrl, string? xmlUrl, string? xmlMetadata)
    {
        var income = await _db.Incomes
            .FirstOrDefaultAsync(i => i.Id == id && i.TenantId == tenantId);

        if (income is null) return null;

        if (pdfUrl is not null) income.InvoicePdfUrl = pdfUrl;
        if (xmlUrl is not null) income.InvoiceXmlUrl = xmlUrl;
        if (xmlMetadata is not null) income.XmlMetadata = xmlMetadata;

        await _db.SaveChangesAsync();

        return MapToResponse(income);
    }

    // AmountMXN holds the net amount deposited; when an exchange rate exists the
    // income comes from a withholding client, so derive the full honorario breakdown.
    private static void ApplyHonorarioBreakdown(Income income)
    {
        if (income.ExchangeRate is > 0)
        {
            var breakdown = HonorarioCalculator.FromNetAmount(income.AmountMXN, income.ExchangeRate.Value);
            income.HonorarioMXN = breakdown.HonorarioMXN;
            income.IvaMXN = breakdown.IvaMXN;
            income.SubtotalMXN = breakdown.SubtotalMXN;
            income.IsrWithheldMXN = breakdown.IsrWithheldMXN;
            income.IvaWithheldMXN = breakdown.IvaWithheldMXN;
            income.TakeHomePayUSD = breakdown.TakeHomePayUSD;
        }
        else
        {
            income.HonorarioMXN = null;
            income.IvaMXN = null;
            income.SubtotalMXN = null;
            income.IsrWithheldMXN = null;
            income.IvaWithheldMXN = null;
            income.TakeHomePayUSD = null;
        }
    }

    private static IncomeResponse MapToResponse(Income income)
    {
        return new IncomeResponse
        {
            Id = income.Id,
            Type = income.Type,
            Source = income.Source,
            Date = income.Date,
            AmountMXN = income.AmountMXN,
            ExchangeRate = income.ExchangeRate,
            AmountUSD = income.AmountUSD,
            HonorarioMXN = income.HonorarioMXN,
            IvaMXN = income.IvaMXN,
            SubtotalMXN = income.SubtotalMXN,
            IsrWithheldMXN = income.IsrWithheldMXN,
            IvaWithheldMXN = income.IvaWithheldMXN,
            TakeHomePayUSD = income.TakeHomePayUSD,
            Description = income.Description,
            InvoicePdfUrl = income.InvoicePdfUrl,
            InvoiceXmlUrl = income.InvoiceXmlUrl,
            XmlMetadata = income.XmlMetadata,
            CreatedAt = income.CreatedAt,
            UpdatedAt = income.UpdatedAt
        };
    }
}
