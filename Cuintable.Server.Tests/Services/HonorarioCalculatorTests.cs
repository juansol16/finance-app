using Cuintable.Server.Services;
using Xunit;

namespace Cuintable.Server.Tests.Services;

public class HonorarioCalculatorTests
{
    // Reference values from the accountant's spreadsheet (tipo de cambio 16.78):
    // Take home USD 5,000.00 | Take home MXN 83,900.00 | Honorario 72,327.59
    // IVA 11,572.41 | Subtotal 83,900.00 | ISR ret 904.09 | IVA ret 7,714.46 | Neto 75,281.44
    [Fact]
    public void FromNetAmount_SpreadsheetRow_MatchesAccountantValues()
    {
        var result = HonorarioCalculator.FromNetAmount(75_281.45m, 16.78m);

        Assert.Equal(72_327.59m, result.HonorarioMXN);
        Assert.Equal(11_572.41m, result.IvaMXN);
        Assert.Equal(83_900.00m, result.SubtotalMXN);
        Assert.Equal(904.09m, result.IsrWithheldMXN);
        Assert.Equal(7_714.46m, result.IvaWithheldMXN);
        Assert.Equal(5_000.00m, result.TakeHomePayUSD);
    }

    // The spreadsheet's own net (75,281.44) differs by one cent from the exact
    // forward calculation (75,281.45) due to rounding; the reverse calculation
    // must still be internally consistent: subtotal - retentions == net.
    [Theory]
    [InlineData(75_281.44, 16.78)]
    [InlineData(75_281.45, 16.78)]
    [InlineData(4_486.38, 17.50)]
    [InlineData(100_000.00, 18.1234)]
    [InlineData(0.01, 16.78)]
    public void FromNetAmount_IsInternallyConsistent(decimal net, decimal rate)
    {
        var r = HonorarioCalculator.FromNetAmount(net, rate);

        // Subtotal = honorario + IVA
        Assert.Equal(r.HonorarioMXN + r.IvaMXN, r.SubtotalMXN);

        // Reconstructed net matches the input within rounding tolerance (1 cent)
        var reconstructedNet = r.SubtotalMXN - r.IsrWithheldMXN - r.IvaWithheldMXN;
        Assert.True(Math.Abs(reconstructedNet - net) <= 0.01m,
            $"Reconstructed net {reconstructedNet} differs from input {net} by more than 0.01");
    }

    [Fact]
    public void FromNetAmount_AppliesAccountantRates()
    {
        // Honorario of exactly 10,000 implies net of 10,408.40 (x 1.04084)
        var r = HonorarioCalculator.FromNetAmount(10_408.40m, 16.00m);

        Assert.Equal(10_000.00m, r.HonorarioMXN);
        Assert.Equal(1_600.00m, r.IvaMXN);           // 16%
        Assert.Equal(11_600.00m, r.SubtotalMXN);
        Assert.Equal(125.00m, r.IsrWithheldMXN);     // 1.25%
        Assert.Equal(1_066.60m, r.IvaWithheldMXN);   // 10.666% (accountant's rounding)
        Assert.Equal(725.00m, r.TakeHomePayUSD);     // 11,600 / 16.00
    }
}
