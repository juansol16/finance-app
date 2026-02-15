using Cuintable.Server.Models;

namespace Cuintable.Server.DTOs.TaxPayments;

public class TaxPaymentResponse
{
    public Guid Id { get; set; }
    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }
    public decimal AmountDue { get; set; }
    public DateOnly DueDate { get; set; }
    public TaxPaymentStatus Status { get; set; }
    public DateOnly? PaymentDate { get; set; }
    public string? DeterminationPdfUrl { get; set; }
    public string? PaymentReceiptUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}
