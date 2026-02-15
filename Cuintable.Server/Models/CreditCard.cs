namespace Cuintable.Server.Models;

public class CreditCard
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Bank { get; set; } = string.Empty;
    public string Nickname { get; set; } = string.Empty;
    public string LastFourDigits { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<Expense> Expenses { get; set; } = [];
    public ICollection<TaxableExpense> TaxableExpenses { get; set; } = [];
}
