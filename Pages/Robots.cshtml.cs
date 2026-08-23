using System.Text;
using Enricci_Propiedades.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Enricci_Propiedades.Pages;

/// <summary>
/// robots.txt generado, y no un archivo suelto en wwwroot, para que la línea del
/// sitemap salga siempre del dominio configurado. Escrito a mano se desactualiza
/// en cuanto el sitio cambia de dirección, y apuntar el sitemap a un dominio que
/// ya no es queda como un error en las herramientas de los buscadores.
/// </summary>
public class RobotsModel : PageModel
{
    /// <summary>
    /// Rastreadores de los asistentes con IA, autorizados uno por uno.
    ///
    /// Con "User-agent: *" ya estarían permitidos, pero varios de estos buscan
    /// su propio nombre y algunos —Google-Extended entre ellos— sólo se
    /// gobiernan con una regla propia. Nombrarlos deja además por escrito la
    /// decisión: a una inmobiliaria le conviene que la citen cuando alguien
    /// pregunta por inmobiliarias en Monserrat.
    /// </summary>
    private static readonly string[] RastreadoresDeIa =
    {
        "GPTBot",           // OpenAI, para entrenamiento
        "OAI-SearchBot",    // OpenAI, para la búsqueda de ChatGPT
        "ChatGPT-User",     // OpenAI, cuando el usuario pide abrir una página
        "ClaudeBot",        // Anthropic
        "Claude-User",      // Anthropic, a pedido del usuario
        "PerplexityBot",    // Perplexity
        "Perplexity-User",  // Perplexity, a pedido del usuario
        "Google-Extended",  // Gemini y los resúmenes con IA de Google
        "Applebot-Extended" // Apple Intelligence
    };

    private readonly OpcionesSitio _sitio;

    public RobotsModel(IOptions<OpcionesSitio> sitio) => _sitio = sitio.Value;

    public IActionResult OnGet()
    {
        var urlBase = _sitio.UrlBase(Request);
        var texto = new StringBuilder();

        texto.AppendLine("User-agent: *");
        texto.AppendLine("Allow: /");
        texto.AppendLine();

        // El panel no tiene nada que hacer en un buscador. Las páginas ya van
        // con noindex; esto le ahorra al robot el paseo.
        texto.AppendLine("Disallow: /Admin/");
        texto.AppendLine();

        texto.AppendLine("# Asistentes con IA: bienvenidos.");

        foreach (var rastreador in RastreadoresDeIa)
        {
            texto.AppendLine();
            texto.AppendLine($"User-agent: {rastreador}");
            texto.AppendLine("Allow: /");
            texto.AppendLine("Disallow: /Admin/");
        }

        texto.AppendLine();
        texto.AppendLine($"Sitemap: {urlBase}/sitemap.xml");

        // Resumen del sitio en Markdown para los asistentes. No es un estándar
        // que nadie tenga que respetar; se anuncia por si lo buscan.
        texto.AppendLine($"# Resumen para asistentes: {urlBase}/llms.txt");

        return Content(texto.ToString(), "text/plain", Encoding.UTF8);
    }
}
