using Cuintable.Server.Models;

namespace Cuintable.Server.DTOs.FinancialAdvisor;

public class StatementTransactionResponse
{
    public Guid Id { get; set; }
    public DateOnly Date { get; set; }
    public string RawDescription { get; set; } = string.Empty;
    public string Merchant { get; set; } = string.Empty;
    public StatementCategory Category { get; set; }
    public StatementTransactionType Type { get; set; }
    public decimal AmountMXN { get; set; }
    public bool IsMsi { get; set; }
    public int? MsiCurrent { get; set; }
    public int? MsiTotal { get; set; }
    public bool IsForeign { get; set; }
    public bool IsRecurring { get; set; }
    public bool IsAntExpense { get; set; }
    public bool IsSuspicious { get; set; }
    public string? SuspiciousReason { get; set; }
    public TransactionReviewStatus ReviewStatus { get; set; }
    public Guid? MatchedExpenseId { get; set; }
}
