namespace Cuintable.Server.DTOs.FinancialAdvisor;

public class MonthlyAdviceResponse
{
    public string AdviceJson { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public int StatementCount { get; set; }

    /// <summary>True when statements were added, reprocessed or removed after generation.</summary>
    public bool IsStale { get; set; }
}
