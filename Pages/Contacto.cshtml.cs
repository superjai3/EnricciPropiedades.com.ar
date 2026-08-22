using System.ComponentModel.DataAnnotations;
using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages;

public class ContactoModel : PageModel
{
    private readonly PropiedadesService _propiedades;
    private readonly ConsultasService _consultas;
    private readonly CorreoService _correo;
    private readonly LimiteEnvios _limite;
    private readonly ILogger<ContactoModel> _log;

    public ContactoModel(
        PropiedadesService propiedades,
        ConsultasService consultas,
        CorreoService correo,
        LimiteEnvios limite,
        ILogger<ContactoModel> log)
    {
        _propiedades = propiedades;
        _consultas = consultas;
        _correo = correo;
        _limite = limite;
        _log = log;
    }

    public class Consulta
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

        [Display(Name = "Motivo de la consulta")]
        public string Motivo { get; set; } = "Quiero comprar";

        [Required(ErrorMessage = "Contanos brevemente en qué podemos ayudarte.")]
        [StringLength(1200, MinimumLength = 10, ErrorMessage = "Escribí al menos una o dos líneas.")]
        [Display(Name = "Consulta")]
        public string Mensaje { get; set; } = "";

        /// <summary>Campo trampa para robots: si viene completo, se descarta el envío.</summary>
        public string? Sitio { get; set; }
    }

    [BindProperty]
    public Consulta Datos { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? Propiedad { get; set; }

    public bool Enviado { get; private set; }

    /// <summary>True si la consulta además llegó por correo a la inmobiliaria.</summary>
    public bool CorreoEnviado { get; private set; }
    public string MensajeWhatsapp { get; private set; } = SitioInfo.WhatsappGeneral;
    public string MensajeMail { get; private set; } = SitioInfo.MailA("Consulta desde la web");

    public static readonly string[] Motivos =
    {
        "Quiero comprar",
        "Quiero vender",
        "Quiero alquilar",
        "Quiero poner en alquiler",
        "Necesito una tasación",
        "Administración y cobranza",
        "Asesoría legal",
        "Otra consulta"
    };

    public void OnGet()
    {
        if (Propiedad is not > 0)
        {
            return;
        }

        var ficha = _propiedades.PorId(Propiedad.Value);
        if (ficha is null)
        {
            return;
        }

        Datos.Motivo = ficha.Operacion == Models.Operacion.Venta ? "Quiero comprar" : "Quiero alquilar";
        Datos.Mensaje = $"Hola, me interesa la propiedad de {ficha.Direccion} ({ficha.Barrio}), " +
                        $"publicación {ficha.Slug}. Quisiera coordinar una visita.";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Trampa anti spam: los robots completan todos los campos del formulario.
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

        var resumen =
            $"Consulta desde la web\n" +
            $"Nombre: {Datos.Nombre}\n" +
            $"Email: {Datos.Email}\n" +
            $"Teléfono: {Datos.Telefono ?? "-"}\n" +
            $"Motivo: {Datos.Motivo}\n\n{Datos.Mensaje}";

        var ficha = Propiedad is > 0 ? _propiedades.PorId(Propiedad.Value) : null;

        // Primero se guarda y después se intenta el correo. Al revés, una casilla
        // mal configurada o un servidor SMTP caído harían desaparecer el contacto.
        var registro = await _consultas.RegistrarAsync(new Models.Consulta
        {
            Origen = OrigenConsulta.Contacto,
            Nombre = Datos.Nombre,
            Email = Datos.Email,
            Telefono = Datos.Telefono,
            Motivo = Datos.Motivo,
            Mensaje = Datos.Mensaje,
            PropiedadId = ficha?.Id,
            PropiedadTitulo = ficha?.Titulo
        });

        _log.LogInformation(
            "Consulta web {Id} recibida de {Nombre} ({Email}, {Telefono}). Motivo: {Motivo}",
            registro.Id, Datos.Nombre, Datos.Email, Datos.Telefono ?? "sin teléfono", Datos.Motivo);

        MensajeWhatsapp = SitioInfo.Whatsapp(resumen);
        MensajeMail = $"{SitioInfo.MailA($"Consulta web: {Datos.Motivo}")}&body={Uri.EscapeDataString(resumen)}";

        CorreoEnviado = await _correo.EnviarAsync(
            $"Consulta web: {Datos.Motivo} — {Datos.Nombre}", resumen, Datos.Email);

        await _consultas.MarcarCorreoEnviadoAsync(registro.Id, CorreoEnviado);

        Enviado = true;

        return Page();
    }
}
