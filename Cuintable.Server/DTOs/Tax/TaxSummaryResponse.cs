namespace Cuintable.Server.DTOs.Tax;

public class TaxSummaryResponse
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalDeductibleExpenses { get; set; }
    public decimal TaxableBase { get; set; }
    public decimal EstimatedISR { get; set; }
    public decimal EffectiveTaxRate { get; set; }
    public decimal IncomeChangePercent { get; set; }
    public decimal DeductiblePercent { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal EstimatedIVA { get; set; }
    public decimal AnnualAccumulatedIncome { get; set; }
}
