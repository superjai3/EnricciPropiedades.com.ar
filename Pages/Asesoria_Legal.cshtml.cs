using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Enricci_Propiedades.Pages;

public class AsesoriaLegalModel : PageModel
{
    private readonly OpcionesSitio _sitio;

    public AsesoriaLegalModel(IOptions<OpcionesSitio> sitio) => _sitio = sitio.Value;

    public string DatosEstructuradosJson => DatosEstructurados.Servicio(
        _sitio.UrlBase(Request),
        "Asesoría legal inmobiliaria",
        "Contratos, boletos de compraventa, sucesiones, regularización de títulos y desalojos, " +
        "con estudio jurídico y escribanía de confianza en la Ciudad de Buenos Aires.",
        _sitio.UrlBase(Request) + Request.Path);

    public void OnGet()
    {
    }
}
