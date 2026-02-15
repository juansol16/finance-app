using Cuintable.Server.Models;

namespace Cuintable.Server.DTOs.Expenses;

public class CreateExpenseRequest
{
    public ExpenseCategory Category { get; set; }
    public Guid? CreditCardId { get; set; }
    public DateOnly Date { get; set; }
    public decimal AmountMXN { get; set; }
    public string? Description { get; set; }
}
