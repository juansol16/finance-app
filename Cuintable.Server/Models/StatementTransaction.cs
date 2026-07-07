namespace Cuintable.Server.Models;

public class StatementTransaction
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StatementId { get; set; }

    public DateOnly Date { get; set; }
    public string RawDescription { get; set; } = string.Empty;
    public string Merchant { get; set; } = string.Empty;
    public StatementCategory Category { get; set; } = StatementCategory.Otro;
    public StatementTransactionType Type { get; set; } = StatementTransactionType.Charge;
    public decimal AmountMXN { get; set; }

    public bool IsMsi { get; set; }
    public int? MsiCurrent { get; set; }
    public int? MsiTotal { get; set; }
    public bool IsForeign { get; set; }
    public bool IsRecurring { get; set; }

    // Analysis results
    public bool IsAntExpense { get; set; }
    public bool IsSuspicious { get; set; }
    // Comma-separated reason codes (DUPLICATE, FOREIGN, NEW_MERCHANT, FEE_OR_INTEREST),
    // translated in the frontend so both languages work
    public string? SuspiciousReason { get; set; }
    public TransactionReviewStatus ReviewStatus { get; set; } = TransactionReviewStatus.None;
    public Guid? MatchedExpenseId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public CardStatement Statement { get; set; } = null!;
    public Expense? MatchedExpense { get; set; }
}
