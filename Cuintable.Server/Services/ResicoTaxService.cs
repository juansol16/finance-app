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

    public async Task<TaxSummaryResponse> GetMonthlySummaryAsync(Guid userId, int month, int year)
    {
        var startDate = new DateOnly(year, month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);

        var incomes = await _context.Incomes
            .Where(i => i.UserId == userId && i.Date >= startDate && i.Date <= endDate)
            .ToListAsync();

        var taxableExpenses = await _context.TaxableExpenses
            .Where(te => te.UserId == userId && te.Date >= startDate && te.Date <= endDate)
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

    public async Task<AnnualTaxSummaryResponse> GetAnnualSummaryAsync(Guid userId, int year)
    {
        var response = new AnnualTaxSummaryResponse
        {
            Year = year
        };

        for (int month = 1; month <= 12; month++)
        {
            var summary = await GetMonthlySummaryAsync(userId, month, year);
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
}
