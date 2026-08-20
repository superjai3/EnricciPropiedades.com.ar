using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages.Admin;

/// <summary>
/// Los respaldos de la base y de las fotos: qué hay guardado, hacer uno ahora y
/// bajarse el archivo.
///
/// Bajarlo importa: un respaldo que vive en el mismo servidor no sirve de nada
/// el día que el servidor se pierde.
/// </summary>
public class RespaldosModel : PageModel
{
    private readonly RespaldoService _respaldo;
    private readonly ILogger<RespaldosModel> _log;

    public RespaldosModel(RespaldoService respaldo, ILogger<RespaldosModel> log)
    {
        _respaldo = respaldo;
        _log = log;
    }

    public IReadOnlyList<ArchivoDeRespaldo> Archivos { get; private set; } = Array.Empty<ArchivoDeRespaldo>();

    [TempData]
    public string? Mensaje { get; set; }

    [TempData]
    public string? Advertencia { get; set; }

    public bool Automatico => _respaldo.Habilitado;
    public int HoraDiaria => _respaldo.HoraDiaria;
    public int Conservar => _respaldo.Conservar;
    public string Carpeta => _respaldo.Carpeta;

    public void OnGet() => Archivos = _respaldo.Listar();

    public async Task<IActionResult> OnPostRespaldarAsync()
    {
        try
        {
            var archivo = await _respaldo.RespaldarAsync(HttpContext.RequestAborted);
            _log.LogInformation("Respaldo pedido a mano por {Usuario}.", User.Identity?.Name);
            Mensaje = $"Respaldo hecho: {archivo.Nombre} ({archivo.TamanioTexto}).";
        }
        catch (Exception ex)
        {
            // Que falle el respaldo no puede tumbar el panel: se informa y se
            // deja el detalle en el log para poder averiguar qué pasó.
            _log.LogError(ex, "Falló el respaldo pedido desde el panel.");
            Advertencia = "No se pudo hacer el respaldo. El detalle quedó en el registro del servidor.";
        }

        return RedirectToPage();
    }

    public IActionResult OnGetDescargar(string nombre)
    {
        var ruta = _respaldo.RutaDe(nombre);

        if (ruta is null)
        {
            return NotFound();
        }

        _log.LogInformation("Respaldo {Nombre} descargado por {Usuario}.", nombre, User.Identity?.Name);

        // Se abre el archivo en lugar de leerlo entero en memoria: con muchas
        // fotos el zip puede pesar bastante.
        var flujo = System.IO.File.OpenRead(ruta);
        return File(flujo, "application/zip", nombre);
    }

    public IActionResult OnPostEliminar(string nombre)
    {
        if (_respaldo.Eliminar(nombre))
        {
            Mensaje = $"Se eliminó {nombre}.";
        }
        else
        {
            Advertencia = "No se pudo eliminar ese respaldo.";
        }

        return RedirectToPage();
    }
}
