using Cuintable.Server.DTOs.Expenses;

namespace Cuintable.Server.Services;

public interface IExpenseService
{
    Task<List<ExpenseResponse>> GetAllAsync(Guid tenantId, DateOnly? startDate = null, DateOnly? endDate = null);
    Task<ExpenseResponse?> GetByIdAsync(Guid tenantId, Guid id);
    Task<ExpenseResponse> CreateAsync(Guid tenantId, Guid userId, CreateExpenseRequest request);
    Task<ExpenseResponse?> UpdateAsync(Guid tenantId, Guid id, UpdateExpenseRequest request);
    Task<bool> DeleteAsync(Guid tenantId, Guid id);
}
