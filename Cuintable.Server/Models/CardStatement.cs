namespace Cuintable.Server.Models;

public class CardStatement
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? CreditCardId { get; set; }

    public StatementAccountType AccountType { get; set; } = StatementAccountType.CreditCard;
    public string? BankName { get; set; }
    public string? CardLastFour { get; set; }

    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateOnly? PeriodStart { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public DateOnly? PaymentDueDate { get; set; }

    public decimal? PreviousBalance { get; set; }
    public decimal? TotalPayments { get; set; }
    public decimal? TotalCharges { get; set; }
    public decimal? InterestCharged { get; set; }
    public decimal? FeesCharged { get; set; }
    public decimal? NewBalance { get; set; }
    public decimal? MinimumPayment { get; set; }
    public decimal? NoInterestPayment { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal? AvailableCredit { get; set; }

    public string PdfUrl { get; set; } = string.Empty;
    public StatementStatus Status { get; set; } = StatementStatus.Uploaded;
    public string? ErrorMessage { get; set; }
    public string? RawExtractionJson { get; set; }
    public string? AdviceJson { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public User User { get; set; } = null!;
    public CreditCard? CreditCard { get; set; }
    public ICollection<StatementTransaction> Transactions { get; set; } = [];
}
