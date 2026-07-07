using Cuintable.Server.Models;

namespace Cuintable.Server.DTOs.FinancialAdvisor;

public class StatementDetailResponse : StatementSummaryResponse
{
    public DateOnly? PeriodStart { get; set; }
    public decimal? PreviousBalance { get; set; }
    public decimal? TotalPayments { get; set; }
    public decimal? InterestCharged { get; set; }
    public decimal? FeesCharged { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal? AvailableCredit { get; set; }
    public DateTime? ProcessedAt { get; set; }
    /// <summary>Raw advice JSON ({ summary, suggestions[] }) as produced by the model; parsed client-side.</summary>
    public string? AdviceJson { get; set; }
    public List<StatementTransactionResponse> Transactions { get; set; } = [];
    public List<AntExpenseClusterResponse> AntClusters { get; set; } = [];
    public ReconciliationSummaryResponse? Reconciliation { get; set; }
}

public class AntExpenseClusterResponse
{
    public string Merchant { get; set; } = string.Empty;
    public StatementCategory Category { get; set; }
    public int Count { get; set; }
    public decimal TotalMXN { get; set; }
    public decimal AnnualProjectionMXN { get; set; }
}

public class ReconciliationSummaryResponse
{
    public int MatchedPayments { get; set; }
    public int UnmatchedStatementPayments { get; set; }
    public int UnmatchedPlatformPayments { get; set; }
    public decimal MatchedAmountMXN { get; set; }
    public decimal UnmatchedStatementAmountMXN { get; set; }
    public decimal UnmatchedPlatformAmountMXN { get; set; }
}
