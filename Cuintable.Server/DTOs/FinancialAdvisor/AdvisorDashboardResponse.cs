using Cuintable.Server.Models;

namespace Cuintable.Server.DTOs.FinancialAdvisor;

public class AdvisorDashboardResponse
{
    public int Year { get; set; }
    public int Month { get; set; }
    public int StatementCount { get; set; }
    public decimal TotalChargesMXN { get; set; }
    public decimal AntTotalMXN { get; set; }
    public decimal AntAnnualProjectionMXN { get; set; }
    public int SuspiciousCount { get; set; }
    public int SuspiciousPendingCount { get; set; }
    public decimal SubscriptionsMXN { get; set; }
    public decimal MsiLoadMXN { get; set; }
    public decimal InterestAndFeesMXN { get; set; }
    public List<CategoryTotalItem> CategoryTotals { get; set; } = [];
    public List<TrendPointItem> Trend { get; set; } = [];
    public List<AntExpenseClusterResponse> AntClusters { get; set; } = [];
    public ReconciliationSummaryResponse? Reconciliation { get; set; }
    public MonthlyAdviceResponse? MonthlyAdvice { get; set; }
}

public class CategoryTotalItem
{
    public StatementCategory Category { get; set; }
    public decimal TotalMXN { get; set; }
    public int Count { get; set; }
}

public class TrendPointItem
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalChargesMXN { get; set; }
}
