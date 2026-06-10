using Cuintable.Server.Data;
using Cuintable.Server.DTOs.Tax;
using Cuintable.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Cuintable.Server.Services;

public class ResicoTaxService : IResicoTaxService
{
    private readonly AppDbContext _context;

    public ResicoTaxService(AppDbContext context)
    {
        _context = context;
    }

    // AmountMXN stores the NET amount deposited. For incomes with a withholding
    // client the invoiced amount lives in HonorarioMXN; RESICO ISR and IVA are
    // calculated on that fiscal income, falling back to AmountMXN for incomes
    // without a breakdown (MXN clients with no retentions).
    private static decimal FiscalIncome(Income i) => i.HonorarioMXN ?? i.AmountMXN;

    private static decimal IvaCharged(Income i) =>
        i.IvaMXN ?? Math.Round(FiscalIncome(i) * 0.16m, 2, MidpointRounding.AwayFromZero);

    public decimal CalculateResicoISR(decimal monthlyIncome)
    {
        return monthlyIncome switch
        {
            <= 25_000.00m   => monthlyIncome * 0.0100m,
            <= 50_000.00m   => monthlyIncome * 0.0110m,
            <= 83_333.33m   => monthlyIncome * 0.0150m,
            <= 208_333.33m  => monthlyIncome * 0.0200m,
            _               => monthlyIncome * 0.0250m
        };
    }

    public async Task<TaxSummaryResponse> GetMonthlySummaryAsync(Guid tenantId, int month, int year)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var incomes = await _context.Incomes
            .Where(i => i.TenantId == tenantId && i.Date >= startDate && i.Date <= endDate)
            .ToListAsync();

        var taxableExpenses = await _context.TaxableExpenses
            .Where(te => te.TenantId == tenantId && te.Date >= startDate && te.Date <= endDate)
            .ToListAsync();

        var totalIncome = incomes.Sum(FiscalIncome);
        var totalDeductible = taxableExpenses.Sum(te => te.AmountMXN);
        var taxableBase = totalIncome - totalDeductible;

        // In RESICO, ISR is calculated on Total Income, not Taxable Base (Profit).
        // The 1.25% ISR withheld by personas morales is creditable against it.
        var estimatedISR = CalculateResicoISR(totalIncome);
        var isrWithheld = incomes.Sum(i => i.IsrWithheldMXN ?? 0m);
        var isrNetDue = Math.Max(estimatedISR - isrWithheld, 0m);
        var effectiveRate = totalIncome > 0 ? (estimatedISR / totalIncome) : 0;

        // Previous month income for % change
        var prevDate = startDate.AddMonths(-1);
        var prevStart = new DateOnly(prevDate.Year, prevDate.Month, 1);
        var prevEnd = prevStart.AddMonths(1).AddDays(-1);
        var prevIncome = await _context.Incomes
            .Where(i => i.TenantId == tenantId && i.Date >= prevStart && i.Date <= prevEnd)
            .SumAsync(i => i.HonorarioMXN ?? i.AmountMXN);
        var incomeChangePercent = prevIncome > 0 ? ((totalIncome - prevIncome) / prevIncome) : 0;

        // Deductible as % of income
        var deductiblePercent = totalIncome > 0 ? (totalDeductible / totalIncome) : 0;

        // Profit margin
        var profitMargin = totalIncome > 0 ? (taxableBase / totalIncome) : 0;

        // IVA: 16% charged on the invoiced honorario; the 10.666% withheld
        // by personas morales is subtracted from what is owed to SAT.
        var estimatedIVA = incomes.Sum(IvaCharged);
        var ivaWithheld = incomes.Sum(i => i.IvaWithheldMXN ?? 0m);
        var ivaNetDue = Math.Max(estimatedIVA - ivaWithheld, 0m);

        // Annual accumulated income (Jan 1 to end of selected month);
        // the RESICO 3.5M limit applies to invoiced income
        var yearStart = new DateOnly(year, 1, 1);
        var annualAccumulated = await _context.Incomes
            .Where(i => i.TenantId == tenantId && i.Date >= yearStart && i.Date <= endDate)
            .SumAsync(i => i.HonorarioMXN ?? i.AmountMXN);

        return new TaxSummaryResponse
        {
            Month = month,
            Year = year,
            TotalIncome = totalIncome,
            TotalDeductibleExpenses = totalDeductible,
            TaxableBase = taxableBase,
            EstimatedISR = estimatedISR,
            IsrWithheld = isrWithheld,
            IsrNetDue = isrNetDue,
            EffectiveTaxRate = effectiveRate,
            IncomeChangePercent = incomeChangePercent,
            DeductiblePercent = deductiblePercent,
            ProfitMargin = profitMargin,
            EstimatedIVA = estimatedIVA,
            IvaWithheld = ivaWithheld,
            IvaNetDue = ivaNetDue,
            AnnualAccumulatedIncome = annualAccumulated
        };
    }

    public async Task<AnnualTaxSummaryResponse> GetAnnualSummaryAsync(Guid tenantId, int year)
    {
        var response = new AnnualTaxSummaryResponse
        {
            Year = year
        };

        for (int month = 1; month <= 12; month++)
        {
            var summary = await GetMonthlySummaryAsync(tenantId, month, year);
            response.MonthlySummaries.Add(summary);
        }

        response.TotalAnnualIncome = response.MonthlySummaries.Sum(s => s.TotalIncome);
        response.TotalAnnualDeductible = response.MonthlySummaries.Sum(s => s.TotalDeductibleExpenses);
        response.TotalAnnualISR = response.MonthlySummaries.Sum(s => s.EstimatedISR);
        response.TotalAnnualIsrWithheld = response.MonthlySummaries.Sum(s => s.IsrWithheld);
        response.TotalAnnualIsrNetDue = response.MonthlySummaries.Sum(s => s.IsrNetDue);

        response.AverageEffectiveTaxRate = response.TotalAnnualIncome > 0
            ? (response.TotalAnnualISR / response.TotalAnnualIncome)
            : 0;

        return response;
    }

    public async Task<LastUsdIncomeResponse?> GetLastUsdIncomeAsync(Guid tenantId)
    {
        var income = await _context.Incomes
            .Where(i => i.TenantId == tenantId && i.TakeHomePayUSD != null && i.ExchangeRate != null)
            .OrderByDescending(i => i.Date)
            .ThenByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync();

        if (income is null) return null;

        return new LastUsdIncomeResponse
        {
            Date = income.Date,
            Source = income.Source,
            TakeHomePayUSD = income.TakeHomePayUSD!.Value,
            ExchangeRate = income.ExchangeRate!.Value,
            NetReceivedMXN = income.AmountMXN
        };
    }

    public async Task<DashboardChartsResponse> GetDashboardChartsAsync(Guid tenantId, int months = 12)
    {
        var response = new DashboardChartsResponse();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        for (int i = months - 1; i >= 0; i--)
        {
            var date = today.AddMonths(-i);
            var month = date.Month;
            var year = date.Year;
            
            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            // Income: cash flow uses the real money deposited (AmountMXN = net),
            // fiscal calculations use the invoiced honorario.
            var incomes = await _context.Incomes
                .Where(x => x.TenantId == tenantId && x.Date >= startDate && x.Date <= endDate)
                .ToListAsync();

            var cashIncome = incomes.Sum(x => x.AmountMXN);
            var fiscalIncome = incomes.Sum(FiscalIncome);

            // Expenses (Only Expenses, NOT TaxableExpenses as per user request)
            var expenses = await _context.Expenses
                .Where(x => x.TenantId == tenantId && x.Date >= startDate && x.Date <= endDate)
                .SumAsync(x => x.AmountMXN);

            // SAT Payments (Paid in this month)
            var taxPayments = await _context.TaxPayments
                .Where(x => x.TenantId == tenantId && 
                            x.Status == TaxPaymentStatus.Pagado && 
                            x.PaymentDate >= startDate && 
                            x.PaymentDate <= endDate)
                .SumAsync(x => x.AmountDue);

            response.CashFlow.Add(new CashFlowItem
            {
                Month = month,
                Year = year,
                TotalIncome = cashIncome,
                TotalExpenses = expenses,
                TotalTaxPayments = taxPayments
            });

            // Operations breakdown: taxes net of what the client already withheld
            var deductibleExpenses = await _context.TaxableExpenses
                .Where(x => x.TenantId == tenantId && x.Date >= startDate && x.Date <= endDate)
                .SumAsync(x => x.AmountMXN);

            var isr = Math.Max(CalculateResicoISR(fiscalIncome) - incomes.Sum(x => x.IsrWithheldMXN ?? 0m), 0m);
            var iva = Math.Max(incomes.Sum(IvaCharged) - incomes.Sum(x => x.IvaWithheldMXN ?? 0m), 0m);

            response.Operations.Add(new OperationsItem
            {
                Month = month,
                Year = year,
                Income = fiscalIncome,
                DeductibleExpenses = deductibleExpenses,
                ISR = isr,
                IVANet = iva,
                Profit = fiscalIncome - deductibleExpenses
            });

            // Volatility (Average Exchange Rate)
            var rates = incomes
                .Where(x => x.ExchangeRate.HasValue && x.ExchangeRate > 0)
                .Select(x => x.ExchangeRate!.Value)
                .ToList();

            if (rates.Any())
            {
                response.Volatility.Add(new VolatilityItem
                {
                    Month = month,
                    Year = year,
                    AverageExchangeRate = rates.Average()
                });
            }
            else
            {
                 response.Volatility.Add(new VolatilityItem
                {
                    Month = month,
                    Year = year,
                    AverageExchangeRate = 0 // Or handle appropriately on frontend
                });
            }
        }

        // Volatility Summary
        var recentRates = response.Volatility
            .Where(v => v.AverageExchangeRate > 0)
            .TakeLast(2)
            .ToList();

        if (recentRates.Count >= 2)
        {
            var current = recentRates[^1].AverageExchangeRate;
            var previous = recentRates[^2].AverageExchangeRate;
            var change = previous > 0 ? ((current - previous) / previous) : 0;
            response.VolatilitySummary = new VolatilitySummary
            {
                CurrentRate = current,
                PreviousRate = previous,
                ChangePercent = change,
                Trend = change > 0 ? "up" : change < 0 ? "down" : "neutral"
            };
        }
        else if (recentRates.Count == 1)
        {
            response.VolatilitySummary = new VolatilitySummary
            {
                CurrentRate = recentRates[0].AverageExchangeRate,
                Trend = "neutral"
            };
        }

        return response;
    }
}
