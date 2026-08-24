using System.Text.Json;
using Enricci_Propiedades.Models;

namespace Enricci_Propiedades.Services;

/// <summary>
/// Arma los bloques de schema.org que el sitio le declara a los buscadores y a
/// los asistentes con IA.
///
/// Está centralizado a propósito. Los datos estructurados repartidos por las
/// vistas se desincronizan del contenido visible en cuanto alguien cambia un
/// texto, y un buscador que encuentra que lo declarado no coincide con lo que se
/// ve directamente deja de confiar en el resto.
/// </summary>
public static class DatosEstructurados
{
    /// <summary>
    /// Identificadores estables de las entidades del sitio. Sirven para que la
    /// inmobiliaria sea <b>una</b> entidad referenciada desde todas las páginas
    /// y no una empresa distinta por página, que es lo que pasa cuando cada
    /// bloque se declara suelto.
    /// </summary>
    public static string IdInmobiliaria(string urlBase) => $"{urlBase}/#inmobiliaria";

    public static string IdSitio(string urlBase) => $"{urlBase}/#sitio";

    /// <summary>
    /// El bloque que va en todas las páginas: quién es la inmobiliaria y qué es
    /// este sitio. Se emite como @graph —dos entidades enlazadas— en vez de dos
    /// bloques sueltos.
    /// </summary>
    public static string Grafo(string urlBase, string descripcion)
    {
        var grafo = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = new object[]
            {
                Inmobiliaria(urlBase, descripcion),
                Sitio(urlBase, descripcion)
            }
        };

        return Serializar(grafo);
    }

    private static Dictionary<string, object?> Inmobiliaria(string urlBase, string descripcion)
    {
        var entidad = new Dictionary<string, object?>
        {
            ["@type"] = "RealEstateAgent",
            ["@id"] = IdInmobiliaria(urlBase),
            ["name"] = SitioInfo.Nombre,
            ["alternateName"] = SitioInfo.NombreCorto,
            ["description"] = descripcion,
            ["url"] = urlBase + "/",
            ["logo"] = urlBase + "/imagenes/isologo.png",
            ["image"] = urlBase + "/imagenes/isologo.png",
            ["email"] = SitioInfo.Email,
            ["telephone"] = SitioInfo.TelefonoE164,
            ["foundingDate"] = SitioInfo.AnioFundacion,
            ["knowsLanguage"] = "es-AR",
            ["currenciesAccepted"] = "USD, ARS",

            // El titular es quien tiene la matrícula: en una inmobiliaria eso no
            // es un dato de color, es lo que la habilita a operar.
            ["founder"] = new Dictionary<string, object?>
            {
                ["@type"] = "Person",
                ["name"] = SitioInfo.Titular
            },
            ["hasCredential"] = new Dictionary<string, object?>
            {
                ["@type"] = "EducationalOccupationalCredential",
                ["credentialCategory"] = "Matrícula profesional",
                ["name"] = $"Corredor inmobiliario CUCICBA {SitioInfo.MatriculaNumero}",
                ["recognizedBy"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Organization",
                    ["name"] = "CUCICBA — Colegio Único de Corredores Inmobiliarios de la Ciudad de Buenos Aires"
                }
            },

            ["address"] = new Dictionary<string, object?>
            {
                ["@type"] = "PostalAddress",
                ["streetAddress"] = SitioInfo.DireccionCompleta,
                ["addressLocality"] = SitioInfo.BarrioOficina,
                ["addressRegion"] = "Ciudad Autónoma de Buenos Aires",
                ["postalCode"] = SitioInfo.CodigoPostal,
                ["addressCountry"] = "AR"
            },

            // Sin coordenadas, el buscador tiene que deducir dónde queda el local
            // a partir del texto de la dirección, y a veces le erra de barrio.
            ["geo"] = new Dictionary<string, object?>
            {
                ["@type"] = "GeoCoordinates",
                ["latitude"] = SitioInfo.Latitud,
                ["longitude"] = SitioInfo.Longitud
            },
            ["hasMap"] = SitioInfo.MapaUrl,

            ["openingHoursSpecification"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["@type"] = "OpeningHoursSpecification",
                    ["dayOfWeek"] = SitioInfo.DiasDeAtencion,
                    ["opens"] = SitioInfo.HoraApertura,
                    ["closes"] = SitioInfo.HoraCierre
                }
            },

            // Los barrios donde trabaja: es lo que contesta "¿atienden en San
            // Cristóbal?" sin que nadie tenga que leer la página entera.
            ["areaServed"] = SitioInfo.BarriosQueAtiende
                .Select(barrio => new Dictionary<string, object?>
                {
                    ["@type"] = "Place",
                    ["name"] = $"{barrio}, Ciudad Autónoma de Buenos Aires"
                })
                .ToArray(),

            ["serviceType"] = new[]
            {
                "Venta de propiedades",
                "Alquiler de propiedades",
                "Tasación de inmuebles",
                "Administración y cobranza de alquileres",
                "Asesoría legal inmobiliaria"
            }
        };

        var perfiles = SitioInfo.Perfiles.ToArray();

        if (perfiles.Length > 0)
        {
            entidad["sameAs"] = perfiles;
        }

        return entidad;
    }

    private static Dictionary<string, object?> Sitio(string urlBase, string descripcion) => new()
    {
        ["@type"] = "WebSite",
        ["@id"] = IdSitio(urlBase),
        ["url"] = urlBase + "/",
        ["name"] = SitioInfo.Nombre,
        ["description"] = descripcion,
        ["inLanguage"] = "es-AR",
        ["publisher"] = new Dictionary<string, object?>
        {
            ["@id"] = IdInmobiliaria(urlBase)
        },

        // El buscador del sitio, declarado para que Google pueda ofrecer una
        // caja de búsqueda propia en el resultado. Se declara recién ahora
        // porque hasta que existió la búsqueda por texto habría sido anunciar
        // algo que no funcionaba, que es peor que no anunciar nada.
        ["potentialAction"] = new Dictionary<string, object?>
        {
            ["@type"] = "SearchAction",
            ["target"] = new Dictionary<string, object?>
            {
                ["@type"] = "EntryPoint",
                ["urlTemplate"] = urlBase + "/Propiedades?texto={search_term_string}"
            },
            // schema.org lo pide como un texto suelto y no como un arreglo:
            // nombra el hueco de la plantilla de arriba.
            ["query-input"] = "required name=search_term_string"
        }
    };

    /// <summary>
    /// Preguntas frecuentes. Se arma con las mismas preguntas y respuestas que
    /// se ven en la página —no con un texto paralelo— porque declarar una cosa y
    /// mostrar otra es motivo de penalización.
    /// </summary>
    public static string PreguntasFrecuentes(IEnumerable<(string Pregunta, string Respuesta)> preguntas)
    {
        var bloque = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "FAQPage",
            ["mainEntity"] = preguntas
                .Select(p => new Dictionary<string, object?>
                {
                    ["@type"] = "Question",
                    ["name"] = p.Pregunta,
                    ["acceptedAnswer"] = new Dictionary<string, object?>
                    {
                        ["@type"] = "Answer",
                        ["text"] = p.Respuesta
                    }
                })
                .ToArray()
        };

        return Serializar(bloque);
    }

    /// <summary>Un servicio de la inmobiliaria, enlazado a la entidad que lo presta.</summary>
    public static string Servicio(
        string urlBase, string nombre, string descripcion, string urlPagina)
    {
        var bloque = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Service",
            ["name"] = nombre,
            ["description"] = descripcion,
            ["url"] = urlPagina,
            ["serviceType"] = nombre,
            ["provider"] = new Dictionary<string, object?>
            {
                ["@id"] = IdInmobiliaria(urlBase)
            },
            ["areaServed"] = SitioInfo.BarriosQueAtiende
                .Select(barrio => new Dictionary<string, object?>
                {
                    ["@type"] = "Place",
                    ["name"] = $"{barrio}, Ciudad Autónoma de Buenos Aires"
                })
                .ToArray(),
            ["availableChannel"] = new Dictionary<string, object?>
            {
                ["@type"] = "ServiceChannel",
                ["serviceUrl"] = urlPagina,
                ["servicePhone"] = SitioInfo.TelefonoE164
            }
        };

        return Serializar(bloque);
    }

    /// <summary>
    /// Listado de propiedades de un barrio, con la ruta de navegación. El
    /// ItemList le dice al buscador qué publicaciones hay y en qué orden, que es
    /// lo que necesita para entender que la página es un listado y no un aviso.
    /// </summary>
    public static string ListadoDeBarrio(
        string urlBase,
        string barrio,
        string region,
        string descripcion,
        IReadOnlyList<Propiedad> propiedades)
    {
        var lugar = $"{barrio}, {region}";

        var urlBarrio = $"{urlBase}/propiedades/{Slug.De(barrio)}";

        var listado = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "CollectionPage",
            ["name"] = $"Propiedades en {barrio}",
            ["description"] = descripcion,
            ["url"] = urlBarrio,
            ["inLanguage"] = "es-AR",
            ["isPartOf"] = new Dictionary<string, object?> { ["@id"] = IdSitio(urlBase) },
            ["about"] = new Dictionary<string, object?>
            {
                ["@type"] = "Place",
                ["name"] = lugar
            },
            ["mainEntity"] = new Dictionary<string, object?>
            {
                ["@type"] = "ItemList",
                ["numberOfItems"] = propiedades.Count,
                ["itemListElement"] = propiedades
                    .Select((p, indice) => new Dictionary<string, object?>
                    {
                        ["@type"] = "ListItem",
                        ["position"] = indice + 1,
                        ["url"] = $"{urlBase}/propiedad/{p.Id}",
                        ["name"] = p.Titulo
                    })
                    .ToArray()
            }
        };

        var migas = Migas(urlBase, new[]
        {
            ("Inicio", urlBase + "/"),
            ("Propiedades", $"{urlBase}/Propiedades"),
            (barrio, urlBarrio)
        });

        return Serializar(new object[] { listado, migas });
    }

    /// <summary>Ruta de navegación, para que el buscador la muestre en el resultado.</summary>
    public static Dictionary<string, object?> Migas(
        string urlBase, IReadOnlyList<(string Nombre, string Url)> pasos) => new()
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = pasos
            .Select((paso, indice) => new Dictionary<string, object?>
            {
                ["@type"] = "ListItem",
                ["position"] = indice + 1,
                ["name"] = paso.Nombre,
                ["item"] = paso.Url
            })
            .ToArray()
    };

    /// <summary>
    /// El serializador escapa "&lt;" y "&gt;", así que un texto con una etiqueta
    /// adentro no puede cerrar el &lt;script&gt; que contiene el bloque.
    /// </summary>
    private static string Serializar(object valor) => JsonSerializer.Serialize(valor);
}
