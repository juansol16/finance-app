using System.Text.Json;
using System.Xml.Linq;

namespace Cuintable.Server.Services;

public static class CfdiParser
{
    private static readonly XNamespace CfdiNs = "http://www.sat.gob.mx/cfd/4";
    private static readonly XNamespace TfdNs = "http://www.sat.gob.mx/TimbreFiscalDigital";

    public static string? Parse(Stream xmlStream)
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

            var metadata = new
            {
                RfcEmisor = emisor?.Attribute("Rfc")?.Value,
                NombreEmisor = emisor?.Attribute("Nombre")?.Value,
                Total = comprobante.Attribute("Total")?.Value,
                Moneda = comprobante.Attribute("Moneda")?.Value,
                Fecha = comprobante.Attribute("Fecha")?.Value,
                UUID = timbre?.Attribute("UUID")?.Value,
                FechaTimbrado = timbre?.Attribute("FechaTimbrado")?.Value,
                Conceptos = conceptos
            };

            return JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }
        catch
        {
            return null;
        }
    }
}
