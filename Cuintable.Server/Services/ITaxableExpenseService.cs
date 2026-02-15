using Cuintable.Server.DTOs.TaxableExpenses;

namespace Cuintable.Server.Services;

public interface ITaxableExpenseService
{
    Task<List<TaxableExpenseResponse>> GetAllAsync(Guid userId);
    Task<TaxableExpenseResponse?> GetByIdAsync(Guid userId, Guid id);
    Task<TaxableExpenseResponse> CreateAsync(Guid userId, CreateTaxableExpenseRequest request);
    Task<TaxableExpenseResponse?> UpdateAsync(Guid userId, Guid id, UpdateTaxableExpenseRequest request);
    Task<bool> DeleteAsync(Guid userId, Guid id);
    Task<TaxableExpenseResponse?> UpdateFileUrlsAsync(Guid userId, Guid id, string? pdfUrl, string? xmlUrl, string? xmlMetadata);
}
