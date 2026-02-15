using Cuintable.Server.DTOs.Tax;

namespace Cuintable.Server.Services;

public interface IResicoTaxService
{
    decimal CalculateResicoISR(decimal monthlyIncome);
    Task<TaxSummaryResponse> GetMonthlySummaryAsync(Guid userId, int month, int year);
    Task<AnnualTaxSummaryResponse> GetAnnualSummaryAsync(Guid userId, int year);
}
