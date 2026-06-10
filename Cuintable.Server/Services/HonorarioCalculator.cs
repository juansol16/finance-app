namespace Cuintable.Server.Services;

/// <summary>
/// Reverse-calculates the honorario breakdown from the net amount deposited in the bank.
/// The client (persona moral) withholds ISR (1.25%) and two-thirds of the IVA
/// (10.666%, matching the accountant's spreadsheet rounding, not the exact 2/3 = 10.6667%).
///
/// Forward:  Neto = Honorario + IVA(16%) - ISRret(1.25%) - IVAret(10.666%)
/// Reverse:  Honorario = Neto / (1 + 0.16 - 0.0125 - 0.10666) = Neto / 1.04084
/// </summary>
public static class HonorarioCalculator
{
    public const decimal IvaRate = 0.16m;
    public const decimal IsrRetentionRate = 0.0125m;
    public const decimal IvaRetentionRate = 0.10666m;
    public const decimal NetFactor = 1m + IvaRate - IsrRetentionRate - IvaRetentionRate; // 1.04084

    public static HonorarioBreakdown FromNetAmount(decimal netAmountMxn, decimal exchangeRate)
    {
        var honorario = Round(netAmountMxn / NetFactor);
        var iva = Round(honorario * IvaRate);
        var subtotal = honorario + iva;
        var isrWithheld = Round(honorario * IsrRetentionRate);
        var ivaWithheld = Round(honorario * IvaRetentionRate);
        var takeHomePayUsd = Round(subtotal / exchangeRate);

        return new HonorarioBreakdown(honorario, iva, subtotal, isrWithheld, ivaWithheld, takeHomePayUsd);
    }

    // PostgreSQL round(numeric, 2) rounds half away from zero; keep C# consistent
    // so the migration backfill and runtime calculation produce identical values.
    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}

public record HonorarioBreakdown(
    decimal HonorarioMXN,
    decimal IvaMXN,
    decimal SubtotalMXN,
    decimal IsrWithheldMXN,
    decimal IvaWithheldMXN,
    decimal TakeHomePayUSD);
