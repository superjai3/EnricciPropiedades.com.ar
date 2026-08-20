using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Enricci_Propiedades.Pages;

/// <summary>
/// Página propia por barrio, en /propiedades/{barrio}.
///
/// Existe por el posicionamiento local: lo que la gente busca no es "propiedades"
/// sino "departamentos en venta en Monserrat". Antes eso sólo se podía ver como
/// /Propiedades?barrio=Monserrat, y una página que se distingue de otra sólo por
/// la cadena de consulta no compite: los buscadores la tratan como la misma
/// página filtrada, no como una página sobre el barrio.
/// </summary>
public class BarrioModel : PageModel
{
    private readonly PropiedadesService _propiedades;
    private readonly OpcionesSitio _sitio;

    public BarrioModel(PropiedadesService propiedades, IOptions<OpcionesSitio> sitio)
    {
        _propiedades = propiedades;
        _sitio = sitio.Value;
    }

    public string Barrio { get; private set; } = "";
    public IReadOnlyList<Propiedad> Resultados { get; private set; } = Array.Empty<Propiedad>();

    /// <summary>Otros barrios con publicaciones, para poder saltar de uno a otro.</summary>
    public IReadOnlyList<string> OtrosBarrios { get; private set; } = Array.Empty<string>();

    public int EnVenta { get; private set; }
    public int EnAlquiler { get; private set; }

    /// <summary>Tipos de propiedad que hay publicados acá, en texto.</summary>
    public IReadOnlyList<string> Tipos { get; private set; } = Array.Empty<string>();

    /// <summary>Precio de venta más bajo publicado, o null si están todos a consultar.</summary>
    public decimal? DesdeVenta { get; private set; }

    /// <summary>
    /// Ciudad del barrio, tomada de las propias publicaciones. No se da por
    /// sentado que sea Buenos Aires: el catálogo tiene al menos una propiedad
    /// en Río de Janeiro, y una página que dijera "Copacabana, Ciudad de
    /// Buenos Aires" sería falsa a la vista y en los datos estructurados.
    /// </summary>
    public string Ciudad { get; private set; } = Propiedad.CiudadPredeterminada;

    /// <summary>Rótulo del barrio con su ciudad, sin repetir cuando coinciden.</summary>
    public string UbicacionTexto =>
        string.Equals(Barrio, Ciudad, StringComparison.OrdinalIgnoreCase)
            ? Ciudad
            : $"{Barrio} · {Ciudad}";

    /// <summary>
    /// True si la oficina de la inmobiliaria queda en este barrio. Exige que
    /// además sea el mismo país: hay un "Río de Janeiro" en el catálogo.
    /// </summary>
    public bool EsElBarrioDeLaOficina =>
        Slug.De(Barrio) == Slug.De(SitioInfo.BarrioOficina) &&
        Ciudad == Propiedad.CiudadPredeterminada;

    public string DatosEstructuradosJson { get; private set; } = "";

    /// <summary>Descripción para el &lt;title&gt; y el meta description.</summary>
    public string Resumen { get; private set; } = "";

    public IActionResult OnGet(string barrio)
    {
        var nombre = Slug.Resolver(_propiedades.Barrios, barrio);

        if (nombre is null)
        {
            // Un barrio sin publicaciones no tiene página. Devolver 404 y no un
            // desvío al listado es lo correcto: si no hay contenido, decirlo.
            return NotFound();
        }

        Barrio = nombre;
        Resultados = _propiedades.Buscar(barrio: nombre).ToList();

        Ciudad = Resultados.Count > 0 ? Resultados[0].Ciudad : Propiedad.CiudadPredeterminada;

        EnVenta = Resultados.Count(p => p.Operacion == Operacion.Venta);
        EnAlquiler = Resultados.Count(p => p.Operacion != Operacion.Venta);

        Tipos = Resultados
            .Select(p => p.TipoTexto)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        var preciosDeVenta = Resultados
            .Where(p => p.Operacion == Operacion.Venta && p.Precio > 0)
            .Select(p => p.Precio)
            .ToList();

        DesdeVenta = preciosDeVenta.Count > 0 ? preciosDeVenta.Min() : null;

        OtrosBarrios = _propiedades.Barrios
            .Where(b => !string.Equals(b, nombre, StringComparison.OrdinalIgnoreCase))
            .ToList();

        Resumen = ArmarResumen();

        DatosEstructuradosJson = DatosEstructurados.ListadoDeBarrio(
            _sitio.UrlBase(Request), Barrio, Ciudad, Resumen, Resultados);

        return Page();
    }

    /// <summary>
    /// Una descripción armada con lo que hay de verdad publicado: cuántas
    /// propiedades, de qué tipo y desde qué precio. Sale del catálogo y no de un
    /// texto fijo para que diga algo distinto en cada barrio —si todas las
    /// páginas dijeran lo mismo, el buscador las trataría como contenido
    /// repetido— y para que no quede desactualizada sola.
    /// </summary>
    private string ArmarResumen()
    {
        var partes = new List<string>();

        if (EnVenta > 0 && EnAlquiler > 0)
        {
            partes.Add($"{EnVenta} {Plural(EnVenta, "propiedad", "propiedades")} en venta " +
                       $"y {EnAlquiler} en alquiler en {Barrio}");
        }
        else if (EnVenta > 0)
        {
            partes.Add($"{EnVenta} {Plural(EnVenta, "propiedad", "propiedades")} en venta en {Barrio}");
        }
        else if (EnAlquiler > 0)
        {
            partes.Add($"{EnAlquiler} {Plural(EnAlquiler, "propiedad", "propiedades")} " +
                       $"en alquiler en {Barrio}");
        }
        else
        {
            partes.Add($"Propiedades en {Barrio}");
        }

        if (Tipos.Count > 0)
        {
            partes.Add(Enumerar(Tipos.Select(t => t.ToLowerInvariant())));
        }

        if (DesdeVenta is > 0)
        {
            partes.Add($"desde USD {DesdeVenta:N0}");
        }

        return string.Join(". ", partes) +
               $". Inmobiliaria con oficina en {SitioInfo.Direccion}, {SitioInfo.BarrioOficina}, " +
               $"desde {SitioInfo.AnioFundacion}.";
    }

    private static string Plural(int cantidad, string singular, string plural) =>
        cantidad == 1 ? singular : plural;

    /// <summary>"a, b y c" — la enumeración como se escribe en castellano.</summary>
    private static string Enumerar(IEnumerable<string> textos)
    {
        var lista = textos.ToList();

        return lista.Count switch
        {
            0 => "",
            1 => lista[0],
            _ => string.Join(", ", lista.Take(lista.Count - 1)) + " y " + lista[^1]
        };
    }
}
