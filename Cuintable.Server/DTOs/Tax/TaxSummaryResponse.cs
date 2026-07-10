namespace Cuintable.Server.DTOs.Tax;

public class TaxSummaryResponse
{
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalIncome { get; set; }
    public decimal TotalDeductibleExpenses { get; set; }
    public decimal TaxableBase { get; set; }
    public decimal EstimatedISR { get; set; }
    public decimal IsrWithheld { get; set; }
    public decimal IsrNetDue { get; set; }
    public decimal EffectiveTaxRate { get; set; }
    public decimal IncomeChangePercent { get; set; }
    public decimal DeductiblePercent { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal EstimatedIVA { get; set; }
    public decimal IvaWithheld { get; set; }
    public decimal IvaCreditable { get; set; }
    public decimal IvaNetDue { get; set; }
    // Excess IVA credit left after this month; rolls into the next month
    public decimal IvaFavorBalance { get; set; }
    // ISR + IVA due this month: the accountant's "total de impuestos por pagar"
    public decimal TotalDue { get; set; }
    public decimal AnnualAccumulatedIncome { get; set; }
}
