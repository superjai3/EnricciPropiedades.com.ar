using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Enricci_Propiedades.Pages;

public class CobranzaModel : PageModel
{
    private readonly OpcionesSitio _sitio;

    public CobranzaModel(IOptions<OpcionesSitio> sitio) => _sitio = sitio.Value;

    /// <summary>
    /// El servicio declarado en el vocabulario de schema.org, enlazado a la
    /// inmobiliaria que lo presta. Va como propiedad calculada y no como campo
    /// que se llena en OnGet para que valga también si la página se vuelve a
    /// dibujar por otro camino.
    /// </summary>
    public string DatosEstructuradosJson => DatosEstructurados.Servicio(
        _sitio.UrlBase(Request),
        "Administración y cobranza de alquileres",
        "Administración de alquileres en la Ciudad de Buenos Aires: cobranza mensual, " +
        "actualización por índice, control de expensas e impuestos y rendición en tiempo y forma.",
        _sitio.UrlBase(Request) + Request.Path);

    public void OnGet()
    {
    }
}
