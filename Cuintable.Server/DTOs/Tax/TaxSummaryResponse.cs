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
}
