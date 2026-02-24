namespace Cuintable.Server.DTOs.Tax;

public class CashFlowItem
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal TotalTaxPayments { get; set; }
    public decimal TotalOutflow => TotalExpenses + TotalTaxPayments;
}

public class VolatilityItem
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal AverageExchangeRate { get; set; }
}

public class OperationsItem
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal Income { get; set; }
    public decimal DeductibleExpenses { get; set; }
    public decimal ISR { get; set; }
    public decimal IVANet { get; set; }
    public decimal Profit { get; set; }
}

public class VolatilitySummary
{
    public decimal CurrentRate { get; set; }
    public decimal PreviousRate { get; set; }
    public decimal ChangePercent { get; set; }
    public string Trend { get; set; } = "neutral"; // "up", "down", "neutral"
}

public class DashboardChartsResponse
{
    public List<CashFlowItem> CashFlow { get; set; } = [];
    public List<VolatilityItem> Volatility { get; set; } = [];
    public List<OperationsItem> Operations { get; set; } = [];
    public VolatilitySummary VolatilitySummary { get; set; } = new();
}
