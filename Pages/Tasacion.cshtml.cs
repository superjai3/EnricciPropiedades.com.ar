using System.ComponentModel.DataAnnotations;
using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Enricci_Propiedades.Pages;

public class TasacionModel : PageModel
{
    private readonly OpcionesSitio _sitio;
    private readonly ConsultasService _consultas;
    private readonly CorreoService _correo;
    private readonly LimiteEnvios _limite;
    private readonly ILogger<TasacionModel> _log;

    public TasacionModel(
        IOptions<OpcionesSitio> sitio,
        ConsultasService consultas,
        CorreoService correo,
        LimiteEnvios limite,
        ILogger<TasacionModel> log)
    {
        _sitio = sitio.Value;
        _consultas = consultas;
        _correo = correo;
        _limite = limite;
        _log = log;
    }

    public class Pedido
    {
        [Required(ErrorMessage = "Indicanos la dirección de la propiedad.")]
        [StringLength(120)]
        [Display(Name = "Dirección de la propiedad")]
        public string Direccion { get; set; } = "";

        [Required(ErrorMessage = "Elegí el barrio.")]
        [Display(Name = "Barrio")]
        public string Barrio { get; set; } = "";

        [Display(Name = "Tipo de propiedad")]
        public string Tipo { get; set; } = "Departamento";

        [Range(0, 20, ErrorMessage = "Ingresá una cantidad de ambientes válida.")]
        [Display(Name = "Ambientes")]
        public int? Ambientes { get; set; }

        [Range(0, 10000, ErrorMessage = "Ingresá una superficie válida en metros cuadrados.")]
        [Display(Name = "Superficie aproximada (m²)")]
        public int? Superficie { get; set; }

        [Display(Name = "¿Para qué la tasás?")]
        public string Objetivo { get; set; } = "Quiero venderla";

        [Required(ErrorMessage = "Necesitamos tu nombre.")]
        [StringLength(80, MinimumLength = 2)]
        [Display(Name = "Nombre y apellido")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "Dejanos un correo de contacto.")]
        [EmailAddress(ErrorMessage = "Revisá el correo: parece incompleto.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Dejanos un teléfono para coordinar la visita.")]
        [Phone(ErrorMessage = "Revisá el teléfono.")]
        [Display(Name = "Teléfono o WhatsApp")]
        public string Telefono { get; set; } = "";

        [StringLength(800)]
        [Display(Name = "Comentarios (opcional)")]
        public string? Comentario { get; set; }

        /// <summary>Campo trampa para robots.</summary>
        public string? Sitio { get; set; }
    }

    /// <summary>El pedido de tasación como servicio, para los buscadores.</summary>
    public string DatosEstructuradosJson => DatosEstructurados.Servicio(
        _sitio.UrlBase(Request),
        "Tasación de inmuebles",
        "Tasación gratuita y sin compromiso de departamentos, casas, PH y locales en la Ciudad " +
        "de Buenos Aires, con informe de valor por escrito.",
        _sitio.UrlBase(Request) + Request.Path);

    [BindProperty]
    public Pedido Datos { get; set; } = new();

    public bool Enviado { get; private set; }

    /// <summary>True si el pedido además llegó por correo a la inmobiliaria.</summary>
    public bool CorreoEnviado { get; private set; }
    public string MensajeWhatsapp { get; private set; } = SitioInfo.Whatsapp("Hola, quisiera pedir una tasación.");

    /// <summary>
    /// Los barrios donde la inmobiliaria trabaja, más una salida para el resto.
    /// Sale de SitioInfo y no de una lista propia: es el mismo dato que se les
    /// declara a los buscadores, y repetido se desincroniza.
    /// </summary>
    public static readonly string[] Barrios =
        SitioInfo.BarriosQueAtiende.Append("Otro barrio de CABA").ToArray();

    public static readonly string[] Tipos =
    {
        "Departamento", "Casa", "PH", "Local comercial", "Oficina", "Cochera", "Terreno"
    };

    public static readonly string[] Objetivos =
    {
        "Quiero venderla", "Quiero ponerla en alquiler", "Es una sucesión",
        "Divorcio o división de bienes", "Solo quiero saber cuánto vale"
    };

    public async Task<IActionResult> OnPostAsync()
    {
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
            _log.LogWarning("Se frenó un envío repetido desde {Ip}",
                HttpContext.Connection.RemoteIpAddress);

            ModelState.AddModelError(string.Empty,
                "Recibimos varios envíos seguidos desde tu conexión. " +
                "Esperá unos minutos o escribinos directamente por WhatsApp.");

            return Page();
        }

        // Los datos de la propiedad a tasar van juntos en el detalle: son propios
        // de este formulario y no tienen dónde caer en el resto de las consultas.
        var detalle =
            $"Propiedad: {Datos.Direccion}, {Datos.Barrio}\n" +
            $"Tipo: {Datos.Tipo}\n" +
            $"Ambientes: {Datos.Ambientes?.ToString() ?? "-"}\n" +
            $"Superficie: {(Datos.Superficie.HasValue ? Datos.Superficie + " m²" : "-")}";

        var resumen =
            $"Pedido de tasación desde la web\n" +
            $"{detalle}\n" +
            $"Objetivo: {Datos.Objetivo}\n\n" +
            $"Nombre: {Datos.Nombre}\n" +
            $"Email: {Datos.Email}\n" +
            $"Teléfono: {Datos.Telefono}\n" +
            $"Comentarios: {Datos.Comentario ?? "-"}";

        // Igual que en Contacto: primero queda registrado, después se avisa.
        var registro = await _consultas.RegistrarAsync(new Consulta
        {
            Origen = OrigenConsulta.Tasacion,
            Nombre = Datos.Nombre,
            Email = Datos.Email,
            Telefono = Datos.Telefono,
            Motivo = Datos.Objetivo,
            Mensaje = string.IsNullOrWhiteSpace(Datos.Comentario) ? "(sin comentarios)" : Datos.Comentario,
            Detalle = detalle
        });

        _log.LogInformation(
            "Pedido de tasación {Id} de {Nombre} para {Direccion}, {Barrio}",
            registro.Id, Datos.Nombre, Datos.Direccion, Datos.Barrio);

        MensajeWhatsapp = SitioInfo.Whatsapp(resumen);

        CorreoEnviado = await _correo.EnviarAsync(
            $"Pedido de tasación: {Datos.Direccion}, {Datos.Barrio}", resumen, Datos.Email);

        await _consultas.MarcarCorreoEnviadoAsync(registro.Id, CorreoEnviado);

        Enviado = true;

        return Page();
    }
}
