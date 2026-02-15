using Cuintable.Server.DTOs.Tax;

namespace Cuintable.Server.Services;

public interface IResicoTaxService
{
    decimal CalculateResicoISR(decimal monthlyIncome);
    Task<TaxSummaryResponse> GetMonthlySummaryAsync(Guid tenantId, int month, int year);
    Task<AnnualTaxSummaryResponse> GetAnnualSummaryAsync(Guid tenantId, int year);
}
