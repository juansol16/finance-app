using Cuintable.Server.Models;

namespace Cuintable.Server.DTOs.TaxableExpenses;

public class TaxableExpenseResponse
{
    public Guid Id { get; set; }
    public TaxableExpenseCategory Category { get; set; }
    public Guid? CreditCardId { get; set; }
    public string? CreditCardLabel { get; set; }
    public Guid? ExpenseId { get; set; }
    public string? LinkedExpenseLabel { get; set; }
    public DateOnly Date { get; set; }
    public decimal AmountMXN { get; set; }
    public string? Description { get; set; }
    public string Vendor { get; set; } = string.Empty;
    public string? InvoicePdfUrl { get; set; }
    public string? InvoiceXmlUrl { get; set; }
    public string? XmlMetadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
