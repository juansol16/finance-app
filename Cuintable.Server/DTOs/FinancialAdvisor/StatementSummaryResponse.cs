using Cuintable.Server.Models;

namespace Cuintable.Server.DTOs.FinancialAdvisor;

public class StatementSummaryResponse
{
    public Guid Id { get; set; }
    public StatementAccountType AccountType { get; set; }
    public Guid? CreditCardId { get; set; }
    public string? CardNickname { get; set; }
    public string? BankName { get; set; }
    public string? CardLastFour { get; set; }
    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }
    public DateOnly? PeriodEnd { get; set; }
    public DateOnly? PaymentDueDate { get; set; }
    public decimal? TotalCharges { get; set; }
    public decimal? NewBalance { get; set; }
    public decimal? MinimumPayment { get; set; }
    public decimal? NoInterestPayment { get; set; }
    public StatementStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public int TransactionCount { get; set; }
    public int SuspiciousCount { get; set; }
    public int AntExpenseCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
