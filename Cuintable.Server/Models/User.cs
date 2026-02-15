namespace Cuintable.Server.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PreferredLanguage { get; set; } = "es";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Income> Incomes { get; set; } = [];
    public ICollection<CreditCard> CreditCards { get; set; } = [];
    public ICollection<Expense> Expenses { get; set; } = [];
    public ICollection<TaxableExpense> TaxableExpenses { get; set; } = [];
    public ICollection<TaxPayment> TaxPayments { get; set; } = [];
}
