using Cuintable.Server.Models;

namespace Cuintable.Server.DTOs.Incomes;

public class IncomeResponse
{
    public Guid Id { get; set; }
    public IncomeType Type { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal AmountMXN { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal? AmountUSD { get; set; }
    public decimal? HonorarioMXN { get; set; }
    public decimal? IvaMXN { get; set; }
    public decimal? SubtotalMXN { get; set; }
    public decimal? IsrWithheldMXN { get; set; }
    public decimal? IvaWithheldMXN { get; set; }
    public decimal? TakeHomePayUSD { get; set; }
    public string? Description { get; set; }
    public string? InvoicePdfUrl { get; set; }
    public string? InvoiceXmlUrl { get; set; }
    public string? XmlMetadata { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
