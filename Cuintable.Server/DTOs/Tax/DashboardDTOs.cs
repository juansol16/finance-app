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

public class DashboardChartsResponse
{
    public List<CashFlowItem> CashFlow { get; set; } = [];
    public List<VolatilityItem> Volatility { get; set; } = [];
}
