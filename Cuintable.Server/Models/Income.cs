namespace Cuintable.Server.Models;

public class Income
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public IncomeType Type { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal AmountMXN { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal? AmountUSD { get; set; }
    public string? Description { get; set; }
    public string? InvoicePdfUrl { get; set; }
    public string? InvoiceXmlUrl { get; set; }
    public string? XmlMetadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public User User { get; set; } = null!;
}
