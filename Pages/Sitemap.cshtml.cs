using System.Text;
using System.Xml;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages;

/// <summary>
/// Mapa del sitio para buscadores. Se arma con las páginas fijas más una
/// entrada por cada propiedad publicada, así el catálogo se indexa solo.
/// </summary>
public class SitemapModel : PageModel
{
    private static readonly (string Ruta, string Prioridad, string Frecuencia)[] PaginasFijas =
    {
        ("/", "1.0", "weekly"),
        ("/Propiedades", "0.9", "daily"),
        ("/Servicios", "0.8", "monthly"),
        ("/Tasacion", "0.8", "monthly"),
        ("/Cobranza", "0.7", "monthly"),
        ("/Asesoria_Legal", "0.7", "monthly"),
        ("/Quienes_Somos", "0.6", "yearly"),
        ("/Mision", "0.4", "yearly"),
        ("/Vision", "0.4", "yearly"),
        ("/Contacto", "0.7", "monthly")
    };

    private readonly PropiedadesService _propiedades;

    public SitemapModel(PropiedadesService propiedades) => _propiedades = propiedades;

    public IActionResult OnGet()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var hoy = DateTime.UtcNow.ToString("yyyy-MM-dd");

        var texto = new StringBuilder();
        using (var escritor = XmlWriter.Create(new EscritorUtf8(texto), new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8,
            OmitXmlDeclaration = false
        }))
        {
            escritor.WriteStartDocument();
            escritor.WriteStartElement("urlset", "http://www.sitemaps.org/schemas/sitemap/0.9");

            foreach (var (ruta, prioridad, frecuencia) in PaginasFijas)
            {
                EscribirUrl(escritor, baseUrl + ruta, hoy, frecuencia, prioridad);
            }

            foreach (var propiedad in _propiedades.Todas)
            {
                EscribirUrl(escritor, $"{baseUrl}/propiedad/{propiedad.Id}", hoy, "weekly", "0.8");
            }

            escritor.WriteEndElement();
            escritor.WriteEndDocument();
        }

        return Content(texto.ToString(), "application/xml", Encoding.UTF8);
    }

    /// <summary>StringWriter que declara UTF-8 (el de la BCL declara UTF-16).</summary>
    private sealed class EscritorUtf8 : StringWriter
    {
        public EscritorUtf8(StringBuilder destino) : base(destino) { }

        public override Encoding Encoding => Encoding.UTF8;
    }

    private static void EscribirUrl(
        XmlWriter escritor, string url, string fecha, string frecuencia, string prioridad)
    {
        escritor.WriteStartElement("url");
        escritor.WriteElementString("loc", url);
        escritor.WriteElementString("lastmod", fecha);
        escritor.WriteElementString("changefreq", frecuencia);
        escritor.WriteElementString("priority", prioridad);
        escritor.WriteEndElement();
    }
}
