using Cuintable.Server.DTOs.FinancialAdvisor;

namespace Cuintable.Server.Services;

public interface IMonthlyAdviceService
{
    /// <summary>
    /// Generates (or regenerates) the consolidated AI advice for a month from every
    /// completed statement of the period and persists it (one row per tenant + period).
    /// Returns null when the month has no completed statements.
    /// </summary>
    Task<MonthlyAdviceResponse?> GenerateAsync(Guid tenantId, Guid userId, int year, int month);
}
