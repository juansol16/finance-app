namespace Cuintable.Server.DTOs.TaxPayments;

public class UpdateTaxPaymentRequest
{
    public decimal AmountDue { get; set; }
    public DateOnly DueDate { get; set; }
}
