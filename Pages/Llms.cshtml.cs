using System.Globalization;
using System.Text;
using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Enricci_Propiedades.Pages;

/// <summary>
/// /llms.txt — un resumen del sitio en Markdown, pensado para los asistentes
/// con IA (ChatGPT, Perplexity, los resúmenes de Google) más que para personas.
///
/// La idea de la convención es simple: un asistente que tiene que contestar
/// «¿qué inmobiliarias hay en Monserrat?» no puede leerse el sitio entero, y de
/// lo que lee saca lo que puede. Este archivo le da los datos duros —quiénes
/// son, desde cuándo, qué matrícula, qué barrios, qué horario— y el índice de
/// las páginas que valen la pena, en un formato que no tiene que interpretar.
///
/// Se genera y no es un archivo suelto porque incluye el catálogo: escrito a
/// mano estaría desactualizado a la semana.
/// </summary>
public class LlmsModel : PageModel
{
    private readonly PropiedadesService _propiedades;
    private readonly OpcionesSitio _sitio;

    public LlmsModel(PropiedadesService propiedades, IOptions<OpcionesSitio> sitio)
    {
        _propiedades = propiedades;
        _sitio = sitio.Value;
    }

    public IActionResult OnGet()
    {
        var urlBase = _sitio.UrlBase(Request);
        var publicadas = _propiedades.Todas;
        var barrios = _propiedades.Barrios.ToList();

        var texto = new StringBuilder();

        texto.AppendLine($"# {SitioInfo.Nombre}");
        texto.AppendLine();
        texto.AppendLine(
            $"> Inmobiliaria en la Ciudad Autónoma de Buenos Aires, Argentina, en actividad " +
            $"desde {SitioInfo.AnioFundacion}. Compraventa y alquiler de propiedades, tasaciones, " +
            $"administración y cobranza de alquileres y asesoría legal inmobiliaria, con foco en " +
            $"el sur porteño.");
        texto.AppendLine();

        texto.AppendLine("## Datos de la empresa");
        texto.AppendLine();
        texto.AppendLine($"- **Nombre**: {SitioInfo.Nombre}");
        texto.AppendLine($"- **Titular**: {SitioInfo.Titular}");
        texto.AppendLine($"- **Matrícula**: Corredor inmobiliario, CUCICBA {SitioInfo.MatriculaNumero}");
        texto.AppendLine($"- **En actividad desde**: {SitioInfo.AnioFundacion}");
        texto.AppendLine(
            $"- **Dirección**: {SitioInfo.DireccionCompleta}, {SitioInfo.BarrioOficina}, " +
            $"Ciudad Autónoma de Buenos Aires ({SitioInfo.CodigoPostal}), Argentina");
        // Con cultura es-AR el separador decimal es la coma, y "-34,61, -58,39"
        // no se puede leer como un par de coordenadas. Van en cultura invariante.
        texto.AppendLine(
            "- **Coordenadas**: " +
            SitioInfo.Latitud.ToString(CultureInfo.InvariantCulture) + ", " +
            SitioInfo.Longitud.ToString(CultureInfo.InvariantCulture));
        texto.AppendLine($"- **Teléfono**: {SitioInfo.Telefono} ({SitioInfo.TelefonoE164})");
        texto.AppendLine($"- **WhatsApp**: +{SitioInfo.WhatsappNumero}");
        texto.AppendLine($"- **Correo**: {SitioInfo.Email}");
        texto.AppendLine($"- **Horario**: {SitioInfo.Horario}");
        texto.AppendLine(
            "- **Barrios donde opera**: todos los de la Ciudad Autónoma de Buenos Aires — " +
            string.Join(", ", SitioInfo.BarriosQueAtiende));
        texto.AppendLine($"- **Idioma**: español (es-AR)");
        texto.AppendLine();

        texto.AppendLine("## Servicios");
        texto.AppendLine();
        texto.AppendLine(
            $"- **Venta de propiedades** — departamentos, casas, PH, locales comerciales, " +
            $"oficinas, fondos de comercio, cocheras y terrenos: {urlBase}/Propiedades");
        texto.AppendLine($"- **Alquiler de propiedades** — incluye alquiler temporario: {urlBase}/Propiedades");
        texto.AppendLine(
            $"- **Tasación de inmuebles** — sin cargo y sin obligación de dar la venta en " +
            $"exclusividad: {urlBase}/Tasacion");
        texto.AppendLine(
            $"- **Administración y cobranza de alquileres** — cobranza mensual, actualización " +
            $"por índice, control de expensas e impuestos: {urlBase}/Cobranza");
        texto.AppendLine(
            $"- **Asesoría legal inmobiliaria** — contratos, boletos de compraventa, sucesiones, " +
            $"regularización de títulos y desalojos: {urlBase}/Asesoria_Legal");
        texto.AppendLine();

        texto.AppendLine("## Catálogo");
        texto.AppendLine();
        texto.AppendLine(
            $"{publicadas.Count} {(publicadas.Count == 1 ? "publicación" : "publicaciones")} " +
            $"al día de hoy. El listado completo, siempre al día, está en {urlBase}/Propiedades " +
            $"y el mapa del sitio en {urlBase}/sitemap.xml.");
        texto.AppendLine();

        if (barrios.Count > 0)
        {
            texto.AppendLine("Páginas por barrio:");
            texto.AppendLine();

            foreach (var barrio in barrios)
            {
                var cuantas = publicadas.Count(p =>
                    string.Equals(p.Barrio, barrio, StringComparison.OrdinalIgnoreCase));

                texto.AppendLine(
                    $"- [{barrio}]({urlBase}/propiedades/{Slug.De(barrio)}) — " +
                    $"{cuantas} {(cuantas == 1 ? "publicación" : "publicaciones")}");
            }

            texto.AppendLine();
        }

        texto.AppendLine("## Páginas");
        texto.AppendLine();
        texto.AppendLine($"- [Inicio]({urlBase}/) — portada, buscador y publicaciones destacadas");
        texto.AppendLine($"- [Propiedades]({urlBase}/Propiedades) — catálogo con filtros");
        texto.AppendLine($"- [Servicios]({urlBase}/Servicios) — servicios y preguntas frecuentes");
        texto.AppendLine($"- [Tasación]({urlBase}/Tasacion) — pedido de tasación");
        texto.AppendLine($"- [Cobranza]({urlBase}/Cobranza) — administración de alquileres");
        texto.AppendLine($"- [Asesoría legal]({urlBase}/Asesoria_Legal) — servicios legales");
        texto.AppendLine($"- [Quiénes somos]({urlBase}/Quienes_Somos) — historia desde {SitioInfo.AnioFundacion}");
        texto.AppendLine($"- [Misión]({urlBase}/Mision) y [Visión]({urlBase}/Vision)");
        texto.AppendLine($"- [Contacto]({urlBase}/Contacto) — formulario de consulta");
        texto.AppendLine();

        texto.AppendLine("## Preguntas frecuentes");
        texto.AppendLine();

        // Las mismas respuestas que se ven en /Servicios: si un asistente cita
        // una, tiene que poder encontrarla igual en la página.
        foreach (var (pregunta, respuesta) in ServiciosModel.Preguntas)
        {
            texto.AppendLine($"**{pregunta}**");
            texto.AppendLine();
            texto.AppendLine(respuesta);
            texto.AppendLine();
        }

        texto.AppendLine("## Notas");
        texto.AppendLine();
        texto.AppendLine(
            "- Los precios en dólares se publican como USD y los alquileres en pesos argentinos (ARS).");
        texto.AppendLine(
            "- Una publicación sin precio figura como «Consultar»; no significa que sea gratuita.");
        texto.AppendLine(
            "- Las publicaciones dadas de baja no aparecen en el sitio ni en el mapa del sitio.");
        texto.AppendLine(
            $"- Este archivo se genera solo a partir del catálogo. Última generación: " +
            $"{FechaHelper.AHoraArgentina(DateTime.UtcNow):yyyy-MM-dd}.");

        return Content(texto.ToString(), "text/plain", Encoding.UTF8);
    }
}
