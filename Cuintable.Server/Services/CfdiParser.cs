using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;

namespace Cuintable.Server.Services;

public record CfdiParseResult(string Json, decimal? IvaTrasladado);

public static class CfdiParser
{
    private static readonly XNamespace CfdiNs = "http://www.sat.gob.mx/cfd/4";
    private static readonly XNamespace TfdNs = "http://www.sat.gob.mx/TimbreFiscalDigital";

    public static string? Parse(Stream xmlStream) => ParseDetailed(xmlStream)?.Json;

    public static CfdiParseResult? ParseDetailed(Stream xmlStream)
    {
        try
        {
            var doc = XDocument.Load(xmlStream);
            var comprobante = doc.Root;
            if (comprobante is null) return null;

            var emisor = comprobante.Element(CfdiNs + "Emisor");
            var timbre = comprobante
                .Element(CfdiNs + "Complemento")?
                .Element(TfdNs + "TimbreFiscalDigital");

            var conceptos = comprobante
                .Element(CfdiNs + "Conceptos")?
                .Elements(CfdiNs + "Concepto")
                .Select(c => new
                {
                    Descripcion = c.Attribute("Descripcion")?.Value,
                    Importe = c.Attribute("Importe")?.Value
                })
                .ToList();

            var iva = ExtractIvaTrasladado(comprobante);

            var metadata = new
            {
                RfcEmisor = emisor?.Attribute("Rfc")?.Value,
                NombreEmisor = emisor?.Attribute("Nombre")?.Value,
                Total = comprobante.Attribute("Total")?.Value,
                Moneda = comprobante.Attribute("Moneda")?.Value,
                Fecha = comprobante.Attribute("Fecha")?.Value,
                UUID = timbre?.Attribute("UUID")?.Value,
                FechaTimbrado = timbre?.Attribute("FechaTimbrado")?.Value,
                IvaTrasladado = iva,
                Conceptos = conceptos
            };

            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            return new CfdiParseResult(json, iva);
        }
        catch
        {
            return null;
        }
    }

    // IVA is the "002" tax in the comprobante-level Impuestos/Traslados nodes;
    // summing only 002 keeps IEPS and other taxes out of the creditable amount.
    private static decimal? ExtractIvaTrasladado(XElement comprobante)
    {
        var traslados = comprobante
            .Element(CfdiNs + "Impuestos")?
            .Element(CfdiNs + "Traslados")?
            .Elements(CfdiNs + "Traslado")
            .Where(t => t.Attribute("Impuesto")?.Value == "002")
            .Select(t => t.Attribute("Importe")?.Value)
            .Where(v => v is not null)
            .ToList();

        if (traslados is null || traslados.Count == 0) return null;

        decimal sum = 0;
        foreach (var value in traslados)
        {
            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var importe))
                return null;
            sum += importe;
        }

        return sum;
    }
}
