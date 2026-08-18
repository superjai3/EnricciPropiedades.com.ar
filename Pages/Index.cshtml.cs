using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages;

public class IndexModel : PageModel
{
    private readonly PropiedadesService _propiedades;

    public IndexModel(PropiedadesService propiedades) => _propiedades = propiedades;

    public IReadOnlyList<Propiedad> Destacadas { get; private set; } = Array.Empty<Propiedad>();
    public IReadOnlyList<string> Barrios { get; private set; } = Array.Empty<string>();
    public int TotalPublicadas { get; private set; }

    public void OnGet()
    {
        Destacadas = _propiedades.Destacadas(6).ToList();
        Barrios = _propiedades.Barrios.ToList();
        TotalPublicadas = _propiedades.Todas.Count;
    }
}
