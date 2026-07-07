namespace Cuintable.Server.DTOs.FinancialAdvisor;

public class ReviewTransactionRequest
{
    /// <summary>"Recognized" or "NotMine"</summary>
    public string Status { get; set; } = string.Empty;
}
