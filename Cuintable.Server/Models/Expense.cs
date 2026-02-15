namespace Cuintable.Server.Models;

public class Expense
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public ExpenseCategory Category { get; set; }
    public Guid? CreditCardId { get; set; }
    public DateOnly Date { get; set; }
    public decimal AmountMXN { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public CreditCard? CreditCard { get; set; }
    public ICollection<TaxableExpense> TaxableExpenses { get; set; } = [];
}
