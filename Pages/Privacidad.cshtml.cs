using Enricci_Propiedades.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Enricci_Propiedades.Pages;

/// <summary>
/// Política de privacidad (Ley 25.326 de Protección de los Datos Personales).
/// El plazo de conservación que declara sale de la misma configuración que usa
/// la purga automática, así lo que se promete y lo que se hace no se separan.
/// </summary>
public class PrivacidadModel : PageModel
{
    private readonly OpcionesConsultas _consultas;

    public PrivacidadModel(IOptions<OpcionesConsultas> consultas) => _consultas = consultas.Value;

    /// <summary>Meses que se conservan las consultas, o null si la purga está apagada.</summary>
    public int? MesesRetencion => _consultas.MesesRetencion > 0 ? _consultas.MesesRetencion : null;

    public void OnGet()
    {
    }
}
