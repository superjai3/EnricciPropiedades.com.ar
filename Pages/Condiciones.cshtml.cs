using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages;

/// <summary>
/// Términos y condiciones de uso del sitio. Es texto fijo: todo lo que muestra
/// (titular, matrícula, correo, enlace a Defensa del Consumidor) sale de
/// SitioInfo, así no hay datos repetidos que se desactualicen por separado.
/// </summary>
public class CondicionesModel : PageModel
{
    public void OnGet()
    {
    }
}
