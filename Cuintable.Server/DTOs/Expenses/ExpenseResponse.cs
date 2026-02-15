using Cuintable.Server.Models;

namespace Cuintable.Server.DTOs.Expenses;

public class ExpenseResponse
{
    public Guid Id { get; set; }
    public ExpenseCategory Category { get; set; }
    public Guid? CreditCardId { get; set; }
    public string? CreditCardLabel { get; set; }
    public DateOnly Date { get; set; }
    public decimal AmountMXN { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
