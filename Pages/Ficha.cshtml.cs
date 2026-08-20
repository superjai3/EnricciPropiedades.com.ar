using System.Text.Json;
using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Enricci_Propiedades.Pages;

public class FichaModel : PageModel
{
    private readonly PropiedadesService _propiedades;
    private readonly OpcionesSitio _sitio;

    public FichaModel(PropiedadesService propiedades, IOptions<OpcionesSitio> sitio)
    {
        _propiedades = propiedades;
        _sitio = sitio.Value;
    }

    public Propiedad Ficha { get; private set; } = default!;
    public IReadOnlyList<Propiedad> Similares { get; private set; } = Array.Empty<Propiedad>();

    /// <summary>
    /// La publicación descrita en el vocabulario de schema.org, para que el
    /// buscador entienda que esto es un aviso inmobiliario y no una página
    /// cualquiera: así puede mostrar precio, ambientes y superficie en el
    /// resultado en vez de dos líneas de texto suelto.
    /// </summary>
    public string DatosEstructurados { get; private set; } = "";

    /// <summary>Foto de portada en URL absoluta, para Open Graph. Null si no tiene.</summary>
    public string? FotoDePortada { get; private set; }

    public IActionResult OnGet(int id)
    {
        var propiedad = _propiedades.PorId(id);
        if (propiedad is null)
        {
            return RedirectToPage("/Propiedades");
        }

        Ficha = propiedad;
        Similares = _propiedades.Similares(propiedad).ToList();
        FotoDePortada = propiedad.Fotos.Count > 0 ? propiedad.Fotos[0] : null;
        DatosEstructurados = ArmarDatosEstructurados(propiedad);

        return Page();
    }

    private string ArmarDatosEstructurados(Propiedad p)
    {
        var urlBase = _sitio.UrlBase(Request);
        var urlFicha = $"{urlBase}/propiedad/{p.Id}";

        var aviso = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "RealEstateListing",
            ["name"] = p.Titulo,
            ["description"] = p.Descripcion,
            ["url"] = urlFicha,
            ["datePosted"] = p.FechaAlta.ToString("yyyy-MM-dd"),
            ["about"] = AcercaDe(p)
        };

        if (p.Fotos.Count > 0)
        {
            aviso["image"] = p.Fotos.Select(f => urlBase + f).ToArray();
        }

        // Un precio en 0 significa "Consultar": publicarlo como cero sería
        // declararle al buscador que la propiedad no cuesta nada.
        if (p.Precio > 0)
        {
            aviso["offers"] = new Dictionary<string, object?>
            {
                ["@type"] = "Offer",
                ["price"] = p.Precio,
                ["priceCurrency"] = p.Moneda,
                ["availability"] = p.Estado == EstadoPublicacion.Reservada
                    ? "https://schema.org/LimitedAvailability"
                    : "https://schema.org/InStock",
                ["url"] = urlFicha
            };
        }

        var migas = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = new object[]
            {
                Miga(1, "Inicio", urlBase),
                Miga(2, "Propiedades", $"{urlBase}/Propiedades"),
                // La ruta pasa por la página del barrio, no por el listado
                // filtrado: es la que queremos que el buscador siga y valore.
                Miga(3, p.Barrio, $"{urlBase}/propiedades/{Slug.De(p.Barrio)}"),
                Miga(4, p.Direccion, urlFicha)
            }
        };

        // El serializador escapa "<" y ">", así que un título con una etiqueta
        // adentro no puede cerrar el <script> que contiene este bloque.
        return JsonSerializer.Serialize(new object[] { aviso, migas });
    }

    private static Dictionary<string, object?> AcercaDe(Propiedad p)
    {
        var cosa = new Dictionary<string, object?>
        {
            ["@type"] = TipoSchema(p.Tipo),
            ["name"] = p.Titulo,
            ["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["streetAddress"] = p.Direccion,
                ["addressLocality"] = p.Barrio,
                // La ciudad y el país salen de la publicación: el catálogo es de
                // CABA salvo excepciones, y declarar mal dónde queda una
                // propiedad es peor que no declararlo.
                ["addressRegion"] = p.Region,
                ["addressCountry"] = p.Pais
            }
        };

        // Los datos que faltan van en 0 en la base y significan "no figura".
        // Declararlos como cero sería afirmar que la propiedad no tiene ninguno.
        if (p.Ambientes > 0)
        {
            cosa["numberOfRooms"] = p.Ambientes;
        }

        if (p.Dormitorios > 0)
        {
            cosa["numberOfBedrooms"] = p.Dormitorios;
        }

        if (p.Banios > 0)
        {
            cosa["numberOfBathroomsTotal"] = p.Banios;
        }

        var superficie = p.SuperficieTotal > 0 ? p.SuperficieTotal : p.SuperficieCubierta;

        if (superficie > 0)
        {
            cosa["floorSize"] = new Dictionary<string, object?>
            {
                ["@type"] = "QuantitativeValue",
                ["value"] = superficie,
                // MTK es el código de "metro cuadrado" en la lista UN/CEFACT,
                // que es la que schema.org espera acá.
                ["unitCode"] = "MTK"
            };
        }

        if (p.Comodidades.Count > 0)
        {
            cosa["amenityFeature"] = p.Comodidades
                .Select(c => new Dictionary<string, object?>
                {
                    ["@type"] = "LocationFeatureSpecification",
                    ["name"] = c,
                    ["value"] = true
                })
                .ToArray();
        }

        return cosa;
    }

    /// <summary>Tipo de schema.org que le corresponde a cada tipo de propiedad.</summary>
    private static string TipoSchema(TipoPropiedad tipo) => tipo switch
    {
        TipoPropiedad.Departamento => "Apartment",
        TipoPropiedad.Casa => "House",
        TipoPropiedad.PH => "House",
        TipoPropiedad.LocalComercial => "LocalBusiness",
        TipoPropiedad.Oficina => "Place",
        TipoPropiedad.FondoDeComercio => "LocalBusiness",
        TipoPropiedad.Cochera => "Place",
        TipoPropiedad.Terreno => "LandForm",
        _ => "Residence"
    };

    private static Dictionary<string, object?> Miga(int posicion, string nombre, string url) => new()
    {
        ["@type"] = "ListItem",
        ["position"] = posicion,
        ["name"] = nombre,
        ["item"] = url
    };
}
