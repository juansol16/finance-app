namespace Cuintable.Server.Models;

public class TaxPayment
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int PeriodMonth { get; set; }
    public int PeriodYear { get; set; }
    public decimal AmountDue { get; set; }
    public DateOnly DueDate { get; set; }
    public TaxPaymentStatus Status { get; set; } = TaxPaymentStatus.Pendiente;
    public DateOnly? PaymentDate { get; set; }
    public string? DeterminationPdfUrl { get; set; }
    public string? PaymentReceiptUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
