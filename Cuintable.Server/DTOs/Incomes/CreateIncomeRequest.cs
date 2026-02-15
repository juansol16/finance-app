using Cuintable.Server.Models;

namespace Cuintable.Server.DTOs.Incomes;

public class CreateIncomeRequest
{
    public IncomeType Type { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public decimal AmountMXN { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal? AmountUSD { get; set; }
    public string? Description { get; set; }
}
