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

        var totalIncome = incomes.Sum(i => i.AmountMXN);
        var totalDeductible = taxableExpenses.Sum(te => te.AmountMXN);
        var taxableBase = totalIncome - totalDeductible;

        // In RESICO, ISR is calculated on Total Income, not Taxable Base (Profit)
        var estimatedISR = CalculateResicoISR(totalIncome);
        var effectiveRate = totalIncome > 0 ? (estimatedISR / totalIncome) : 0;

        // Previous month income for % change
        var prevDate = startDate.AddMonths(-1);
        var prevStart = new DateOnly(prevDate.Year, prevDate.Month, 1);
        var prevEnd = prevStart.AddMonths(1).AddDays(-1);
        var prevIncome = await _context.Incomes
            .Where(i => i.TenantId == tenantId && i.Date >= prevStart && i.Date <= prevEnd)
            .SumAsync(i => i.AmountMXN);
        var incomeChangePercent = prevIncome > 0 ? ((totalIncome - prevIncome) / prevIncome) : 0;

        // Deductible as % of income
        var deductiblePercent = totalIncome > 0 ? (totalDeductible / totalIncome) : 0;

        // Profit margin
        var profitMargin = totalIncome > 0 ? (taxableBase / totalIncome) : 0;

        // IVA: RESICO charges 16% on total income
        var estimatedIVA = totalIncome * 0.16m;

        // Annual accumulated income (Jan 1 to end of selected month)
        var yearStart = new DateOnly(year, 1, 1);
        var annualAccumulated = await _context.Incomes
            .Where(i => i.TenantId == tenantId && i.Date >= yearStart && i.Date <= endDate)
            .SumAsync(i => i.AmountMXN);

        return new TaxSummaryResponse
        {
            Month = month,
            Year = year,
            TotalIncome = totalIncome,
            TotalDeductibleExpenses = totalDeductible,
            TaxableBase = taxableBase,
            EstimatedISR = estimatedISR,
            EffectiveTaxRate = effectiveRate,
            IncomeChangePercent = incomeChangePercent,
            DeductiblePercent = deductiblePercent,
            ProfitMargin = profitMargin,
            EstimatedIVA = estimatedIVA,
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

        response.AverageEffectiveTaxRate = response.TotalAnnualIncome > 0
            ? (response.TotalAnnualISR / response.TotalAnnualIncome)
            : 0;

        return response;
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

            // Income
            var incomes = await _context.Incomes
                .Where(x => x.TenantId == tenantId && x.Date >= startDate && x.Date <= endDate)
                .ToListAsync();

            var totalIncome = incomes.Sum(x => x.AmountMXN);
            
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
                TotalIncome = totalIncome,
                TotalExpenses = expenses,
                TotalTaxPayments = taxPayments
            });

            // Operations breakdown
            var deductibleExpenses = await _context.TaxableExpenses
                .Where(x => x.TenantId == tenantId && x.Date >= startDate && x.Date <= endDate)
                .SumAsync(x => x.AmountMXN);

            var isr = CalculateResicoISR(totalIncome);
            var iva = totalIncome * 0.16m;

            response.Operations.Add(new OperationsItem
            {
                Month = month,
                Year = year,
                Income = totalIncome,
                DeductibleExpenses = deductibleExpenses,
                ISR = isr,
                IVANet = iva,
                Profit = totalIncome - deductibleExpenses
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
