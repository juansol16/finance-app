using Cuintable.Server.DTOs.Incomes;

namespace Cuintable.Server.Services;

public interface IIncomeService
{
    Task<List<IncomeResponse>> GetAllAsync(Guid tenantId, DateOnly? startDate = null, DateOnly? endDate = null);
    Task<IncomeResponse?> GetByIdAsync(Guid tenantId, Guid id);
    Task<IncomeResponse> CreateAsync(Guid tenantId, Guid userId, CreateIncomeRequest request);
    Task<IncomeResponse?> UpdateAsync(Guid tenantId, Guid id, UpdateIncomeRequest request);
    Task<bool> DeleteAsync(Guid tenantId, Guid id);
    Task<IncomeResponse?> UpdateFileUrlsAsync(Guid tenantId, Guid id, string? pdfUrl, string? xmlUrl, string? xmlMetadata);
}
