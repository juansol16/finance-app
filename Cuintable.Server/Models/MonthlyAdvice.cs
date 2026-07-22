namespace Cuintable.Server.Models;

/// <summary>
/// AI advice for a whole calendar month, generated on demand from every
/// completed statement of the period. One row per tenant + period.
/// </summary>
public class MonthlyAdvice
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public int PeriodYear { get; set; }
    public int PeriodMonth { get; set; }

    public string AdviceJson { get; set; } = string.Empty;

    /// <summary>Completed statements included when the advice was generated (staleness check).</summary>
    public int StatementCount { get; set; }
    public DateTime GeneratedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
