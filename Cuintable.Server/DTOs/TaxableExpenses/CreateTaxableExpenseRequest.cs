using Cuintable.Server.Models;

namespace Cuintable.Server.DTOs.TaxableExpenses;

public class CreateTaxableExpenseRequest
{
    public TaxableExpenseCategory Category { get; set; }
    public Guid? CreditCardId { get; set; }
    public Guid? ExpenseId { get; set; }
    public DateOnly Date { get; set; }
    public decimal AmountMXN { get; set; }
    public decimal? IvaMXN { get; set; }
    public TaxableExpenseValidationStatus ValidationStatus { get; set; } = TaxableExpenseValidationStatus.Pendiente;
    public string? ValidationComment { get; set; }
    public string? Description { get; set; }
    public string Vendor { get; set; } = string.Empty;
}
