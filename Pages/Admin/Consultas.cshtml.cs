using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages.Admin;

/// <summary>
/// Las consultas que llegaron por los formularios del sitio: quién escribió,
/// por qué propiedad y si ya se le respondió.
/// </summary>
public class ConsultasModel : PageModel
{
    private readonly ConsultasService _consultas;
    private readonly ILogger<ConsultasModel> _log;

    public ConsultasModel(ConsultasService consultas, ILogger<ConsultasModel> log)
    {
        _consultas = consultas;
        _log = log;
    }

    public IReadOnlyList<Consulta> Listado { get; private set; } = Array.Empty<Consulta>();

    /// <summary>"pendientes" (lo primero que uno quiere ver), "atendidas" o "todas".</summary>
    [BindProperty(SupportsGet = true)]
    public string Ver { get; set; } = "pendientes";

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [TempData]
    public string? Mensaje { get; set; }

    public int TotalPendientes { get; private set; }
    public int TotalAtendidas { get; private set; }

    /// <summary>
    /// True si alguna consulta no se pudo avisar por correo. Es la señal de que
    /// el envío está apagado o mal configurado, y de que el panel es por ahora
    /// el único lugar donde se ven los contactos.
    /// </summary>
    public bool HayConsultasSinAvisar { get; private set; }

    public async Task OnGetAsync()
    {
        bool? filtro = Ver switch
        {
            "atendidas" => true,
            "todas" => null,
            _ => false
        };

        Listado = await _consultas.ListarAsync(filtro, Buscar);

        var todas = await _consultas.ListarAsync();
        TotalPendientes = todas.Count(c => !c.Atendida);
        TotalAtendidas = todas.Count(c => c.Atendida);
        HayConsultasSinAvisar = todas.Any(c => !c.CorreoEnviado);
    }

    public async Task<IActionResult> OnPostAtenderAsync(int id)
    {
        var consulta = await _consultas.AlternarAtendidaAsync(id);

        if (consulta is not null)
        {
            _log.LogInformation(
                "Consulta {Id} marcada como {Estado} por {Usuario}.",
                id, consulta.Atendida ? "atendida" : "pendiente", User.Identity?.Name);

            Mensaje = consulta.Atendida
                ? $"La consulta de {consulta.Nombre} quedó marcada como atendida."
                : $"La consulta de {consulta.Nombre} volvió a pendientes.";
        }

        return RedirectToPage(new { Ver, Buscar });
    }

    public async Task<IActionResult> OnPostNotasAsync(int id, string? notas)
    {
        if (await _consultas.GuardarNotasAsync(id, notas))
        {
            Mensaje = "Se guardaron las notas.";
        }

        return RedirectToPage(new { Ver, Buscar });
    }

    public async Task<IActionResult> OnPostEliminarAsync(int id)
    {
        if (await _consultas.EliminarAsync(id))
        {
            _log.LogInformation("Consulta {Id} eliminada por {Usuario}.", id, User.Identity?.Name);
            Mensaje = "Se eliminó la consulta.";
        }

        return RedirectToPage(new { Ver, Buscar });
    }
}
