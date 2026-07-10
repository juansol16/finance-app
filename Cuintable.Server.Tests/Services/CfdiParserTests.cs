using System.Text;
using System.Text.Json;
using Cuintable.Server.Services;
using Xunit;

namespace Cuintable.Server.Tests.Services;

public class CfdiParserTests
{
    private static Stream ToStream(string xml) => new MemoryStream(Encoding.UTF8.GetBytes(xml));

    private const string CfdiWithIvaAndIeps = """
        <?xml version="1.0" encoding="UTF-8"?>
        <cfdi:Comprobante xmlns:cfdi="http://www.sat.gob.mx/cfd/4" Version="4.0"
            Fecha="2026-05-20T10:00:00" SubTotal="12035.00" Total="13695.00" Moneda="MXN">
          <cfdi:Emisor Rfc="AMA123456789" Nombre="Amazon MX" RegimenFiscal="601" />
          <cfdi:Conceptos>
            <cfdi:Concepto Descripcion="Silla de oficina" Importe="12035.00" />
          </cfdi:Conceptos>
          <cfdi:Impuestos TotalImpuestosTrasladados="1710.00">
            <cfdi:Traslados>
              <cfdi:Traslado Base="10375.00" Impuesto="002" TipoFactor="Tasa" TasaOCuota="0.160000" Importe="1660.00" />
              <cfdi:Traslado Base="1000.00" Impuesto="003" TipoFactor="Tasa" TasaOCuota="0.050000" Importe="50.00" />
            </cfdi:Traslados>
          </cfdi:Impuestos>
        </cfdi:Comprobante>
        """;

    private const string CfdiWithoutTaxes = """
        <?xml version="1.0" encoding="UTF-8"?>
        <cfdi:Comprobante xmlns:cfdi="http://www.sat.gob.mx/cfd/4" Version="4.0"
            Fecha="2026-05-20T10:00:00" SubTotal="500.00" Total="500.00" Moneda="MXN">
          <cfdi:Emisor Rfc="XAXX010101000" Nombre="Proveedor Exento" RegimenFiscal="601" />
          <cfdi:Conceptos>
            <cfdi:Concepto Descripcion="Servicio exento" Importe="500.00" />
          </cfdi:Conceptos>
        </cfdi:Comprobante>
        """;

    [Fact]
    public void ParseDetailed_SumsOnlyIva002Traslados()
    {
        var result = CfdiParser.ParseDetailed(ToStream(CfdiWithIvaAndIeps));

        Assert.NotNull(result);
        // Only the 002 (IVA) traslado counts; the 003 (IEPS) one is excluded
        Assert.Equal(1660.00m, result!.IvaTrasladado);

        using var doc = JsonDocument.Parse(result.Json);
        Assert.Equal("AMA123456789", doc.RootElement.GetProperty("rfcEmisor").GetString());
        Assert.Equal(1660.00m, doc.RootElement.GetProperty("ivaTrasladado").GetDecimal());
    }

    [Fact]
    public void ParseDetailed_ReturnsNullIvaWhenNoTaxesNode()
    {
        var result = CfdiParser.ParseDetailed(ToStream(CfdiWithoutTaxes));

        Assert.NotNull(result);
        Assert.Null(result!.IvaTrasladado);
    }

    [Fact]
    public void ParseDetailed_ReturnsNullOnInvalidXml()
    {
        var result = CfdiParser.ParseDetailed(ToStream("not xml at all"));
        Assert.Null(result);
    }
}
