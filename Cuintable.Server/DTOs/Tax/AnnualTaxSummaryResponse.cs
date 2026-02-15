namespace Cuintable.Server.DTOs.Tax;

public class AnnualTaxSummaryResponse
{
    public int Year { get; set; }
    public List<TaxSummaryResponse> MonthlySummaries { get; set; } = new();
    public decimal TotalAnnualIncome { get; set; }
    public decimal TotalAnnualDeductible { get; set; }
    public decimal TotalAnnualISR { get; set; }
    public decimal AverageEffectiveTaxRate { get; set; }
}
