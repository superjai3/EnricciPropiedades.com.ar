using System.ComponentModel.DataAnnotations;
using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages;

public class ContactoModel : PageModel
{
    private readonly PropiedadesService _propiedades;
    private readonly ILogger<ContactoModel> _log;

    public ContactoModel(PropiedadesService propiedades, ILogger<ContactoModel> log)
    {
        _propiedades = propiedades;
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

    public IActionResult OnPost()
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

        _log.LogInformation(
            "Consulta web recibida de {Nombre} ({Email}, {Telefono}). Motivo: {Motivo}",
            Datos.Nombre, Datos.Email, Datos.Telefono ?? "sin teléfono", Datos.Motivo);

        var resumen =
            $"Consulta desde la web\n" +
            $"Nombre: {Datos.Nombre}\n" +
            $"Email: {Datos.Email}\n" +
            $"Teléfono: {Datos.Telefono ?? "-"}\n" +
            $"Motivo: {Datos.Motivo}\n\n{Datos.Mensaje}";

        MensajeWhatsapp = SitioInfo.Whatsapp(resumen);
        MensajeMail = $"{SitioInfo.MailA($"Consulta web: {Datos.Motivo}")}&body={Uri.EscapeDataString(resumen)}";
        Enviado = true;

        return Page();
    }
}
