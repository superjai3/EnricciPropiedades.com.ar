using System.ComponentModel.DataAnnotations;
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
    private readonly ConsultasService _consultas;
    private readonly CorreoService _correo;
    private readonly LimiteEnvios _limite;
    private readonly ILogger<FichaModel> _log;
    private readonly OpcionesSitio _sitio;

    public FichaModel(
        PropiedadesService propiedades,
        ConsultasService consultas,
        CorreoService correo,
        LimiteEnvios limite,
        ILogger<FichaModel> log,
        IOptions<OpcionesSitio> sitio)
    {
        _propiedades = propiedades;
        _consultas = consultas;
        _correo = correo;
        _limite = limite;
        _log = log;
        _sitio = sitio.Value;
    }

    /// <summary>
    /// Lo que se pregunta desde la ficha misma. Es más corto que el formulario
    /// de contacto a propósito: acá ya sabemos por qué propiedad preguntan, y
    /// cada campo de más es una consulta menos que llega.
    /// </summary>
    public class ConsultaFicha
    {
        [Required(ErrorMessage = "Necesitamos tu nombre para responderte.")]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Escribí tu nombre y apellido.")]
        [Display(Name = "Nombre y apellido")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "Dejanos un correo de contacto.")]
        [EmailAddress(ErrorMessage = "Revisá el correo: parece incompleto.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = "";

        [Phone(ErrorMessage = "Revisá el teléfono.")]
        [Display(Name = "Teléfono o WhatsApp")]
        public string? Telefono { get; set; }

        [Required(ErrorMessage = "Contanos brevemente qué querés saber.")]
        [StringLength(1200, MinimumLength = 10, ErrorMessage = "Escribí al menos una línea.")]
        [Display(Name = "Tu consulta")]
        public string Mensaje { get; set; } = "";

        /// <summary>Campo trampa para robots: si viene completo, se descarta el envío.</summary>
        public string? Sitio { get; set; }
    }

    [BindProperty]
    public ConsultaFicha Datos { get; set; } = new();

    /// <summary>True cuando la consulta quedó registrada y hay que mostrar el gracias.</summary>
    public bool Enviado { get; private set; }

    /// <summary>True si además se pudo avisar por correo a la inmobiliaria.</summary>
    public bool CorreoEnviado { get; private set; }

    /// <summary>Enlace de WhatsApp con la consulta ya escrita, para el después del envío.</summary>
    public string MensajeWhatsapp { get; private set; } = SitioInfo.WhatsappGeneral;

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

    /// <summary>
    /// Dirección absoluta de la publicación. Se arma con el dominio configurado
    /// y no con lo que muestra el navegador, para que el enlace compartido
    /// siga funcionando aunque alguien esté entrando por la IP.
    /// </summary>
    public string UrlPublica { get; private set; } = "";

    public IActionResult OnGet(int id)
    {
        if (!Cargar(id))
        {
            return RedirectToPage("/Propiedades");
        }

        Datos.Mensaje = MensajeSugerido(Ficha);

        // Vuelta del envío: la confirmación viaja en TempData porque después de
        // guardar se redirige, y así refrescar la página no reenvía la consulta.
        if (TempData["ConsultaEnviada"] is true)
        {
            Enviado = true;
            CorreoEnviado = TempData["ConsultaCorreo"] is true;

            if (TempData["ConsultaWhatsapp"] is string wsp)
            {
                MensajeWhatsapp = wsp;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        // La ficha se vuelve a cargar sí o sí: si el formulario falla hay que
        // repintar la página entera, no un formulario suelto.
        if (!Cargar(id))
        {
            return RedirectToPage("/Propiedades");
        }

        // Trampa anti spam: los robots completan todos los campos. Se les
        // contesta lo mismo que a una persona para no enseñarles cuál falló.
        if (!string.IsNullOrWhiteSpace(Datos.Sitio))
        {
            Enviado = true;
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (!_limite.Permite(HttpContext.Connection.RemoteIpAddress?.ToString()))
        {
            _log.LogWarning("Se frenó un envío repetido desde {Ip} en la ficha {Id}",
                HttpContext.Connection.RemoteIpAddress, id);

            ModelState.AddModelError(string.Empty,
                "Recibimos varias consultas seguidas desde tu conexión. " +
                "Esperá unos minutos o escribinos directamente por WhatsApp.");

            return Page();
        }

        var resumen =
            $"Consulta por una propiedad publicada\n" +
            $"Propiedad: {Ficha.Titulo} — {Ficha.Direccion}, {Ficha.Barrio}\n" +
            $"Publicación: {Ficha.Slug} ({UrlPublica})\n" +
            $"Precio publicado: {Ficha.PrecioTexto}\n\n" +
            $"Nombre: {Datos.Nombre}\n" +
            $"Email: {Datos.Email}\n" +
            $"Teléfono: {Datos.Telefono ?? "-"}\n\n{Datos.Mensaje}";

        // Primero se guarda y después se intenta el correo: una casilla mal
        // configurada no puede hacer desaparecer un contacto.
        var registro = await _consultas.RegistrarAsync(new Models.Consulta
        {
            Origen = OrigenConsulta.Contacto,
            Nombre = Datos.Nombre,
            Email = Datos.Email,
            Telefono = Datos.Telefono,
            Motivo = Ficha.Operacion == Operacion.Venta ? "Quiero comprar" : "Quiero alquilar",
            Mensaje = Datos.Mensaje,
            PropiedadId = Ficha.Id,
            PropiedadTitulo = Ficha.Titulo
        });

        _log.LogInformation(
            "Consulta {Id} desde la ficha {Propiedad} de {Nombre} ({Email}, {Telefono})",
            registro.Id, Ficha.Slug, Datos.Nombre, Datos.Email, Datos.Telefono ?? "sin teléfono");

        MensajeWhatsapp = SitioInfo.Whatsapp(resumen);

        CorreoEnviado = await _correo.EnviarAsync(
            $"Consulta web: {Ficha.Direccion}, {Ficha.Barrio} — {Datos.Nombre}", resumen, Datos.Email);

        await _consultas.MarcarCorreoEnviadoAsync(registro.Id, CorreoEnviado);

        TempData["ConsultaEnviada"] = true;
        TempData["ConsultaCorreo"] = CorreoEnviado;
        TempData["ConsultaWhatsapp"] = MensajeWhatsapp;

        // Se redirige en vez de pintar acá mismo: el ancla deja al visitante
        // frente a la confirmación en vez de arriba de todo, y F5 no vuelve a
        // enviar la consulta.
        return Redirect($"{Url.Page("/Ficha", new { id })}#consultar");
    }

    /// <summary>
    /// Deja la página lista para pintarse. Devuelve false si la publicación no
    /// existe, que es lo único que obliga a irse a otro lado.
    /// </summary>
    private bool Cargar(int id)
    {
        var propiedad = _propiedades.PorId(id);
        if (propiedad is null)
        {
            return false;
        }

        Ficha = propiedad;
        Similares = _propiedades.Similares(propiedad).ToList();
        FotoDePortada = propiedad.Fotos.Count > 0 ? propiedad.Fotos[0] : null;
        UrlPublica = $"{_sitio.UrlBase(Request)}/propiedad/{propiedad.Id}";
        DatosEstructurados = ArmarDatosEstructurados(propiedad);
        MensajeWhatsapp = SitioInfo.Whatsapp(MensajeSugerido(propiedad));

        return true;
    }

    /// <summary>
    /// Texto con el que arranca el formulario. Escrito, la consulta cuesta un
    /// clic; en blanco, cuesta redactarla, y ahí se pierde la mitad.
    /// </summary>
    private static string MensajeSugerido(Propiedad p) =>
        $"Hola, me interesa la propiedad de {p.Direccion} ({p.Barrio}), publicación {p.Slug}. " +
        "Quisiera coordinar una visita.";

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
