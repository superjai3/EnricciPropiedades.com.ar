using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages;

public class FichaModel : PageModel
{
    private readonly PropiedadesService _propiedades;

    public FichaModel(PropiedadesService propiedades) => _propiedades = propiedades;

    public Propiedad Ficha { get; private set; } = default!;
    public IReadOnlyList<Propiedad> Similares { get; private set; } = Array.Empty<Propiedad>();

    public IActionResult OnGet(int id)
    {
        var propiedad = _propiedades.PorId(id);
        if (propiedad is null)
        {
            return RedirectToPage("/Propiedades");
        }

        Ficha = propiedad;
        Similares = _propiedades.Similares(propiedad).ToList();
        return Page();
    }
}
