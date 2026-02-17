using Cuintable.Server.Data;
using Cuintable.Server.DTOs.Expenses;
using Cuintable.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuintable.Server.Services;

public class ExpenseService : IExpenseService
{
    private readonly AppDbContext _db;

    public ExpenseService(AppDbContext db) => _db = db;

    public async Task<List<ExpenseResponse>> GetAllAsync(Guid tenantId, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        var query = _db.Expenses
            .Include(e => e.CreditCard)
            .Where(e => e.TenantId == tenantId);

        if (startDate.HasValue) query = query.Where(e => e.Date >= startDate.Value);
        if (endDate.HasValue) query = query.Where(e => e.Date <= endDate.Value);

        return await query
            .OrderByDescending(e => e.Date)
            .Select(e => MapToResponse(e))
            .ToListAsync();
    }

    public async Task<ExpenseResponse?> GetByIdAsync(Guid tenantId, Guid id)
    {
        var expense = await _db.Expenses
            .Include(e => e.CreditCard)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);
        return expense is null ? null : MapToResponse(expense);
    }

    public async Task<ExpenseResponse> CreateAsync(Guid tenantId, Guid userId, CreateExpenseRequest request)
    {
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            Category = request.Category,
            CreditCardId = request.CreditCardId,
            Date = request.Date,
            AmountMXN = request.AmountMXN,
            Description = request.Description
        };

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        // Reload with credit card info
        await _db.Entry(expense).Reference(e => e.CreditCard).LoadAsync();
        return MapToResponse(expense);
    }

    public async Task<ExpenseResponse?> UpdateAsync(Guid tenantId, Guid id, UpdateExpenseRequest request)
    {
        var expense = await _db.Expenses
            .Include(e => e.CreditCard)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);
        if (expense is null) return null;

        expense.Category = request.Category;
        expense.CreditCardId = request.CreditCardId;
        expense.Date = request.Date;
        expense.AmountMXN = request.AmountMXN;
        expense.Description = request.Description;

        await _db.SaveChangesAsync();

        // Reload credit card if changed
        await _db.Entry(expense).Reference(e => e.CreditCard).LoadAsync();
        return MapToResponse(expense);
    }

    public async Task<bool> DeleteAsync(Guid tenantId, Guid id)
    {
        var expense = await _db.Expenses
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);
        if (expense is null) return false;

        _db.Expenses.Remove(expense);
        await _db.SaveChangesAsync();
        return true;
    }

    private static ExpenseResponse MapToResponse(Expense expense) => new()
    {
        Id = expense.Id,
        Category = expense.Category,
        CreditCardId = expense.CreditCardId,
        CreditCardLabel = expense.CreditCard is not null
            ? $"{expense.CreditCard.Nickname} (****{expense.CreditCard.LastFourDigits})"
            : null,
        Date = expense.Date,
        AmountMXN = expense.AmountMXN,
        Description = expense.Description,
        CreatedAt = expense.CreatedAt,
        UpdatedAt = expense.UpdatedAt
    };
}
