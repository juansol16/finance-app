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
        // However, we still return TaxableBase for user's information
        var estimatedISR = CalculateResicoISR(totalIncome);
        var effectiveRate = totalIncome > 0 ? (estimatedISR / totalIncome) : 0;

        return new TaxSummaryResponse
        {
            Month = month,
            Year = year,
            TotalIncome = totalIncome,
            TotalDeductibleExpenses = totalDeductible,
            TaxableBase = taxableBase,
            EstimatedISR = estimatedISR,
            EffectiveTaxRate = effectiveRate
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

    public async Task<DashboardChartsResponse> GetDashboardChartsAsync(Guid tenantId)
    {
        var response = new DashboardChartsResponse();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        // Last 6 months
        for (int i = 5; i >= 0; i--)
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

        return response;
    }
}
