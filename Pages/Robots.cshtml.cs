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
    private readonly OpcionesSitio _sitio;

    public RobotsModel(IOptions<OpcionesSitio> sitio) => _sitio = sitio.Value;

    public IActionResult OnGet()
    {
        var texto = new StringBuilder()
            .AppendLine("User-agent: *")
            .AppendLine("Allow: /")
            .AppendLine()
            // El panel no tiene nada que hacer en un buscador. Las páginas ya
            // van con noindex; esto le ahorra al robot el paseo.
            .AppendLine("Disallow: /Admin/")
            .AppendLine()
            .AppendLine($"Sitemap: {_sitio.UrlBase(Request)}/sitemap.xml")
            .ToString();

        return Content(texto, "text/plain", Encoding.UTF8);
    }
}
