using Cuintable.Server.DTOs.Incomes;

namespace Cuintable.Server.Services;

public interface IIncomeService
{
    Task<List<IncomeResponse>> GetAllAsync(Guid userId);
    Task<IncomeResponse?> GetByIdAsync(Guid userId, Guid id);
    Task<IncomeResponse> CreateAsync(Guid userId, CreateIncomeRequest request);
    Task<IncomeResponse?> UpdateAsync(Guid userId, Guid id, UpdateIncomeRequest request);
    Task<bool> DeleteAsync(Guid userId, Guid id);
    Task<IncomeResponse?> UpdateFileUrlsAsync(Guid userId, Guid id, string? pdfUrl, string? xmlUrl, string? xmlMetadata);
}
