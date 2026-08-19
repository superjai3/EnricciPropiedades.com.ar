using Enricci_Propiedades.Data;
using Enricci_Propiedades.Models;
using Microsoft.EntityFrameworkCore;

namespace Enricci_Propiedades.Services;

/// <summary>
/// Catálogo de propiedades. El sitio público sólo ve las publicaciones que no
/// están marcadas como vendidas; el panel de administración las ve todas.
/// </summary>
public class PropiedadesService
{
    private readonly EnricciContexto _bd;

    public PropiedadesService(EnricciContexto bd) => _bd = bd;

    /// <summary>Publicaciones visibles en el sitio: todas menos las dadas de baja.</summary>
    private IQueryable<Propiedad> Publicadas =>
        _bd.Propiedades.Where(p => p.Estado != EstadoPublicacion.Vendida);

    // ---------------------------------------------------------------- consulta

    public IReadOnlyList<Propiedad> Todas => Publicadas.OrderBy(p => p.Id).ToList();

    public Propiedad? PorId(int id) => Publicadas.FirstOrDefault(p => p.Id == id);

    /// <summary>
    /// Destacadas de la portada, las más recientes primero. El orden importa:
    /// si se ordenara por Id, una propiedad recién destacada quedaría siempre
    /// última, y en cuanto hubiera más de <paramref name="cantidad"/> no
    /// llegaría a mostrarse nunca.
    ///
    /// Si no hay ninguna marcada, se muestran las últimas publicadas: la portada
    /// nunca puede quedar con la sección vacía por una cuestión de curaduría.
    /// </summary>
    public IEnumerable<Propiedad> Destacadas(int cantidad = 6)
    {
        var marcadas = Publicadas
            .Where(p => p.Destacada)
            .OrderByDescending(p => p.FechaActualizacion)
            .ThenByDescending(p => p.Id)
            .Take(cantidad)
            .ToList();

        if (marcadas.Count > 0)
        {
            return marcadas;
        }

        return Publicadas
            .OrderByDescending(p => p.FechaActualizacion)
            .ThenByDescending(p => p.Id)
            .Take(cantidad)
            .ToList();
    }

    /// <summary>True si la portada está mostrando el respaldo por falta de destacadas.</summary>
    public bool HayDestacadas => Publicadas.Any(p => p.Destacada);

    public IEnumerable<string> Barrios =>
        Publicadas.Select(p => p.Barrio).Distinct().OrderBy(b => b).ToList();

    public IEnumerable<Propiedad> Similares(Propiedad propiedad, int cantidad = 3) =>
        Publicadas
            .Where(p => p.Id != propiedad.Id && p.Operacion == propiedad.Operacion)
            .ToList()
            .OrderByDescending(p => p.Barrio == propiedad.Barrio)
            .ThenBy(p => Math.Abs(p.Ambientes - propiedad.Ambientes))
            .Take(cantidad);

    public IEnumerable<Propiedad> Buscar(
        string? operacion = null,
        string? tipo = null,
        string? barrio = null,
        int? ambientesMinimos = null,
        decimal? precioMaximo = null,
        string? orden = null)
    {
        var consulta = Publicadas;

        if (!string.IsNullOrWhiteSpace(operacion) &&
            Enum.TryParse<Operacion>(operacion, true, out var op))
        {
            consulta = consulta.Where(p => p.Operacion == op);
        }

        if (!string.IsNullOrWhiteSpace(tipo) &&
            Enum.TryParse<TipoPropiedad>(tipo, true, out var tp))
        {
            consulta = consulta.Where(p => p.Tipo == tp);
        }

        if (!string.IsNullOrWhiteSpace(barrio))
        {
            consulta = consulta.Where(p => p.Barrio.ToLower() == barrio.ToLower());
        }

        if (ambientesMinimos is > 0)
        {
            consulta = consulta.Where(p => p.Ambientes >= ambientesMinimos);
        }

        if (precioMaximo is > 0)
        {
            consulta = consulta.Where(p => p.Precio > 0 && p.Precio <= precioMaximo);
        }

        return orden switch
        {
            "precio-asc" => consulta.OrderBy(p => p.Precio == 0).ThenBy(p => p.Precio).ToList(),
            "precio-desc" => consulta.OrderByDescending(p => p.Precio).ToList(),
            "superficie" => consulta.OrderByDescending(p => p.SuperficieTotal).ToList(),
            _ => consulta.OrderByDescending(p => p.Destacada).ThenBy(p => p.Id).ToList()
        };
    }

    // ------------------------------------------------------ panel de administración

    /// <summary>Todas las publicaciones, incluidas las dadas de baja.</summary>
    public Task<List<Propiedad>> TodasParaPanelAsync(string? buscar = null)
    {
        var consulta = _bd.Propiedades.AsQueryable();

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim().ToLower();
            consulta = consulta.Where(p =>
                p.Titulo.ToLower().Contains(texto) ||
                p.Direccion.ToLower().Contains(texto) ||
                p.Barrio.ToLower().Contains(texto));
        }

        return consulta
            .OrderByDescending(p => p.FechaActualizacion)
            .ToListAsync();
    }

    /// <summary>Busca por Id sin filtrar por estado: el panel edita también las dadas de baja.</summary>
    public Task<Propiedad?> PorIdParaPanelAsync(int id) =>
        _bd.Propiedades.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<Propiedad> CrearAsync(Propiedad propiedad)
    {
        propiedad.FechaAlta = DateTime.UtcNow;
        propiedad.FechaActualizacion = DateTime.UtcNow;

        _bd.Propiedades.Add(propiedad);
        await _bd.SaveChangesAsync();
        return propiedad;
    }

    public async Task ActualizarAsync(Propiedad propiedad)
    {
        propiedad.FechaActualizacion = DateTime.UtcNow;
        await _bd.SaveChangesAsync();
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var propiedad = await PorIdParaPanelAsync(id);
        if (propiedad is null)
        {
            return false;
        }

        _bd.Propiedades.Remove(propiedad);
        await _bd.SaveChangesAsync();
        return true;
    }

    public async Task<int> ContarAsync() => await _bd.Propiedades.CountAsync();

    public async Task<int> ContarPublicadasAsync() => await Publicadas.CountAsync();

    /// <summary>Barrios ya cargados, para sugerirlos en el formulario del panel.</summary>
    public Task<List<string>> BarriosCargadosAsync() =>
        _bd.Propiedades.Select(p => p.Barrio).Distinct().OrderBy(b => b).ToListAsync();
}
