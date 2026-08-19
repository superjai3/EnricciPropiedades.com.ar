using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages.Admin;

public class IndexModel : PageModel
{
    private readonly PropiedadesService _propiedades;
    private readonly FotosService _fotos;
    private readonly ILogger<IndexModel> _log;

    public IndexModel(PropiedadesService propiedades, FotosService fotos, ILogger<IndexModel> log)
    {
        _propiedades = propiedades;
        _fotos = fotos;
        _log = log;
    }

    public IReadOnlyList<Propiedad> Publicaciones { get; private set; } = Array.Empty<Propiedad>();

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [TempData]
    public string? Mensaje { get; set; }

    [TempData]
    public string? Advertencia { get; set; }

    public int TotalPublicadas { get; private set; }
    public int TotalDestacadas { get; private set; }
    public int TotalDadasDeBaja { get; private set; }

    /// <summary>Cuántas destacadas entran en la portada. Debe coincidir con Index.cshtml.cs del sitio.</summary>
    public const int CupoDePortada = 6;

    /// <summary>True cuando hay más destacadas que lugares en la portada.</summary>
    public bool SobranDestacadas => TotalDestacadas > CupoDePortada;

    public async Task OnGetAsync()
    {
        Publicaciones = await _propiedades.TodasParaPanelAsync(Buscar);

        TotalPublicadas = Publicaciones.Count(p => p.Estado != EstadoPublicacion.Vendida);
        TotalDestacadas = Publicaciones.Count(p => p.Destacada && p.Estado != EstadoPublicacion.Vendida);
        TotalDadasDeBaja = Publicaciones.Count(p => p.Estado == EstadoPublicacion.Vendida);
    }

    /// <summary>Elimina la publicación y, con ella, las fotos que se subieron desde el panel.</summary>
    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        var propiedad = await _propiedades.PorIdParaPanelAsync(id);

        if (propiedad is null)
        {
            Mensaje = "La publicación ya no existe.";
            return RedirectToPage();
        }

        foreach (var foto in propiedad.Fotos)
        {
            _fotos.Borrar(foto);
        }

        await _propiedades.EliminarAsync(id);
        _log.LogInformation("Publicación {Id} eliminada por {Usuario}.", id, User.Identity?.Name);

        Mensaje = $"Se eliminó «{propiedad.Titulo}».";
        return RedirectToPage();
    }

    /// <summary>Marca o desmarca la publicación como destacada, sin salir del listado.</summary>
    public async Task<IActionResult> OnPostDestacarAsync(int id)
    {
        var propiedad = await _propiedades.PorIdParaPanelAsync(id);

        if (propiedad is not null)
        {
            propiedad.Destacada = !propiedad.Destacada;
            await _propiedades.ActualizarAsync(propiedad);
            Mensaje = propiedad.Destacada
                ? $"«{propiedad.Titulo}» ahora aparece en la portada."
                : $"«{propiedad.Titulo}» se quitó de la portada.";
        }

        return RedirectToPage(new { Buscar });
    }
}
