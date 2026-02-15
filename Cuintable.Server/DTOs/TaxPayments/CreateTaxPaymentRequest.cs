namespace Cuintable.Server.DTOs.TaxPayments;

public class CreateTaxPaymentRequest
{
    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }
    public decimal AmountDue { get; set; }
    public DateOnly DueDate { get; set; }
}
