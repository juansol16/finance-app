namespace Cuintable.Server.Models;

public class TaxableExpense
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public TaxableExpenseCategory Category { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? ExpenseId { get; set; }
    public DateOnly Date { get; set; }
    public decimal AmountMXN { get; set; }
    public string? Description { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public string? InvoicePdfUrl { get; set; }
    public string? InvoiceXmlUrl { get; set; }
    public string? XmlMetadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public User User { get; set; } = null!;
    public CreditCard? CreditCard { get; set; }
    public Expense? Expense { get; set; }
}
