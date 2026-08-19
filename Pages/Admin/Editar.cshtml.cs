using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Enricci_Propiedades.Pages.Admin;

/// <summary>
/// Alta y edición de una publicación. Sin <c>id</c> en la ruta da de alta; con
/// <c>id</c> edita la existente.
/// </summary>
public class EditarModel : PageModel
{
    private readonly PropiedadesService _propiedades;
    private readonly FotosService _fotos;
    private readonly ILogger<EditarModel> _log;

    public EditarModel(PropiedadesService propiedades, FotosService fotos, ILogger<EditarModel> log)
    {
        _propiedades = propiedades;
        _fotos = fotos;
        _log = log;
    }

    [BindProperty]
    public Propiedad Datos { get; set; } = new();

    /// <summary>Las comodidades se escriben una por línea, que es lo más cómodo de editar.</summary>
    [BindProperty]
    public string ComodidadesTexto { get; set; } = "";

    [BindProperty]
    public List<IFormFile> Archivos { get; set; } = new();

    /// <summary>Rutas de fotos que el usuario marcó para quitar antes de guardar.</summary>
    [BindProperty]
    public List<string> FotosAQuitar { get; set; } = new();

    [TempData]
    public string? Mensaje { get; set; }

    public bool EsAlta => Datos.Id == 0;
    public List<string> ErroresDeFotos { get; } = new();
    public List<string> BarriosSugeridos { get; private set; } = new();

    public SelectList Operaciones => new(
        new[]
        {
            new { Valor = nameof(Operacion.Venta), Texto = "Venta" },
            new { Valor = nameof(Operacion.Alquiler), Texto = "Alquiler" },
            new { Valor = nameof(Operacion.AlquilerTemporario), Texto = "Alquiler temporario" }
        },
        "Valor", "Texto", Datos.Operacion.ToString());

    public SelectList Tipos => new(
        Enum.GetValues<TipoPropiedad>().Select(t => new
        {
            Valor = t.ToString(),
            Texto = t switch
            {
                TipoPropiedad.PH => "PH",
                TipoPropiedad.LocalComercial => "Local comercial",
                TipoPropiedad.FondoDeComercio => "Fondo de comercio",
                _ => t.ToString()
            }
        }),
        "Valor", "Texto", Datos.Tipo.ToString());

    public SelectList Estados => new(
        new[]
        {
            new { Valor = nameof(EstadoPublicacion.Disponible), Texto = "Disponible — se ve en el sitio" },
            new { Valor = nameof(EstadoPublicacion.Reservada), Texto = "Reservada — se ve, con el cartel puesto" },
            new { Valor = nameof(EstadoPublicacion.Vendida), Texto = "Dada de baja — no se ve en el sitio" }
        },
        "Valor", "Texto", Datos.Estado.ToString());

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        BarriosSugeridos = await _propiedades.BarriosCargadosAsync();

        if (id is null or 0)
        {
            // Valores de partida razonables para una publicación nueva.
            Datos = new Propiedad { Moneda = "USD", Estado = EstadoPublicacion.Disponible };
            ComodidadesTexto = "";
            return Page();
        }

        var propiedad = await _propiedades.PorIdParaPanelAsync(id.Value);
        if (propiedad is null)
        {
            Mensaje = "Esa publicación no existe.";
            return RedirectToPage("/Admin/Index");
        }

        Datos = propiedad;
        ComodidadesTexto = string.Join(Environment.NewLine, propiedad.Comodidades);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        BarriosSugeridos = await _propiedades.BarriosCargadosAsync();

        // Las listas no llegan del formulario tal cual: se rearman acá.
        ModelState.Remove("Datos.Comodidades");
        ModelState.Remove("Datos.Fotos");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var comodidades = ComodidadesTexto
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(c => c.Length > 0)
            .Distinct()
            .ToList();

        if (EsAlta)
        {
            var nueva = new Propiedad
            {
                Comodidades = comodidades,
                Fotos = new List<string>()
            };

            Copiar(Datos, nueva);
            await _propiedades.CrearAsync(nueva);

            // Las fotos necesitan el Id, así que se guardan después del alta.
            if (Archivos.Count > 0)
            {
                nueva.Fotos = await _fotos.GuardarAsync(nueva.Id, Archivos, 0, ErroresDeFotos);
                await _propiedades.ActualizarAsync(nueva);
            }

            _log.LogInformation(
                "Publicación {Id} creada por {Usuario}: {Titulo}", nueva.Id, User.Identity?.Name, nueva.Titulo);

            Mensaje = ErroresDeFotos.Count > 0
                ? $"Se publicó «{nueva.Titulo}», pero algunas fotos no se pudieron cargar: {string.Join(" ", ErroresDeFotos)}"
                : $"Se publicó «{nueva.Titulo}».";

            return RedirectToPage("/Admin/Index");
        }

        var existente = await _propiedades.PorIdParaPanelAsync(Datos.Id);
        if (existente is null)
        {
            Mensaje = "Esa publicación ya no existe.";
            return RedirectToPage("/Admin/Index");
        }

        Copiar(Datos, existente);
        existente.Comodidades = comodidades;

        // Primero se quitan las marcadas, después se agregan las nuevas.
        foreach (var ruta in FotosAQuitar.Where(existente.Fotos.Contains))
        {
            existente.Fotos.Remove(ruta);
            _fotos.Borrar(ruta);
        }

        if (Archivos.Count > 0)
        {
            var nuevas = await _fotos.GuardarAsync(
                existente.Id, Archivos, existente.Fotos.Count, ErroresDeFotos);
            existente.Fotos.AddRange(nuevas);
        }

        await _propiedades.ActualizarAsync(existente);

        _log.LogInformation(
            "Publicación {Id} actualizada por {Usuario}.", existente.Id, User.Identity?.Name);

        Mensaje = ErroresDeFotos.Count > 0
            ? $"Se guardó «{existente.Titulo}», pero algunas fotos no se pudieron cargar: {string.Join(" ", ErroresDeFotos)}"
            : $"Se guardaron los cambios de «{existente.Titulo}».";

        return RedirectToPage("/Admin/Index");
    }

    /// <summary>
    /// Vuelca los campos editables del formulario sobre la entidad. Se copian de
    /// a uno a propósito: así un campo agregado al formulario por fuera del
    /// panel no puede tocar Id, fechas ni fotos.
    /// </summary>
    private static void Copiar(Propiedad origen, Propiedad destino)
    {
        destino.Titulo = origen.Titulo.Trim();
        destino.Direccion = origen.Direccion.Trim();
        destino.Barrio = origen.Barrio.Trim();
        destino.Operacion = origen.Operacion;
        destino.Tipo = origen.Tipo;
        destino.Estado = origen.Estado;
        destino.Moneda = origen.Moneda;
        destino.Precio = origen.Precio;
        destino.Expensas = origen.Expensas;
        destino.Ambientes = origen.Ambientes;
        destino.Dormitorios = origen.Dormitorios;
        destino.Banios = origen.Banios;
        destino.SuperficieCubierta = origen.SuperficieCubierta;
        destino.SuperficieTotal = origen.SuperficieTotal;
        destino.Antiguedad = origen.Antiguedad;
        destino.Cochera = origen.Cochera;
        destino.Balcon = origen.Balcon;
        destino.AptoCredito = origen.AptoCredito;
        destino.Destacada = origen.Destacada;
        destino.Descripcion = origen.Descripcion.Trim();
    }
}
