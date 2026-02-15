using Cuintable.Server.DTOs.Expenses;

namespace Cuintable.Server.Services;

public interface IExpenseService
{
    Task<List<ExpenseResponse>> GetAllAsync(Guid userId);
    Task<ExpenseResponse?> GetByIdAsync(Guid userId, Guid id);
    Task<ExpenseResponse> CreateAsync(Guid userId, CreateExpenseRequest request);
    Task<ExpenseResponse?> UpdateAsync(Guid userId, Guid id, UpdateExpenseRequest request);
    Task<bool> DeleteAsync(Guid userId, Guid id);
}
