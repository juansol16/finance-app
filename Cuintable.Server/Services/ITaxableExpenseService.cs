using Cuintable.Server.DTOs.TaxableExpenses;

namespace Cuintable.Server.Services;

public interface ITaxableExpenseService
{
    Task<List<TaxableExpenseResponse>> GetAllAsync(Guid tenantId, DateOnly? startDate = null, DateOnly? endDate = null);
    Task<TaxableExpenseResponse?> GetByIdAsync(Guid tenantId, Guid id);
    Task<TaxableExpenseResponse> CreateAsync(Guid tenantId, Guid userId, CreateTaxableExpenseRequest request);
    Task<TaxableExpenseResponse?> UpdateAsync(Guid tenantId, Guid id, UpdateTaxableExpenseRequest request);
    Task<bool> DeleteAsync(Guid tenantId, Guid id);
    Task<TaxableExpenseResponse?> UpdateFileUrlsAsync(Guid tenantId, Guid id, string? pdfUrl, string? xmlUrl, string? xmlMetadata);
}
