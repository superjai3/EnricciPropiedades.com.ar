using System.ComponentModel.DataAnnotations;

namespace Enricci_Propiedades.Models;

/// <summary>De qué formulario del sitio vino la consulta.</summary>
public enum OrigenConsulta
{
    Contacto,
    Tasacion
}

/// <summary>
/// Una consulta recibida por los formularios del sitio.
///
/// Se guarda en la base <b>antes</b> de intentar el envío por correo: el correo
/// puede estar apagado o fallar el servidor SMTP, y un contacto perdido es un
/// negocio perdido. El correo es un aviso, no el registro.
/// </summary>
public class Consulta
{
    public int Id { get; set; }

    public OrigenConsulta Origen { get; set; }

    [Required]
    [StringLength(80)]
    public string Nombre { get; set; } = "";

    [Required]
    [StringLength(160)]
    public string Email { get; set; } = "";

    [StringLength(60)]
    public string? Telefono { get; set; }

    /// <summary>Motivo en Contacto, objetivo de la tasación en Tasación.</summary>
    [StringLength(80)]
    public string Motivo { get; set; } = "";

    [StringLength(1200)]
    public string Mensaje { get; set; } = "";

    /// <summary>
    /// Datos propios del pedido de tasación (dirección, tipo, superficie…) ya
    /// armados como texto. Vacío en las consultas de contacto.
    /// </summary>
    [StringLength(1000)]
    public string? Detalle { get; set; }

    /// <summary>Propiedad por la que se consultó, si la consulta salió de una ficha.</summary>
    public int? PropiedadId { get; set; }

    /// <summary>
    /// Título de la propiedad en el momento de la consulta. Se guarda copiado y
    /// no por relación: si después se elimina la publicación, la consulta tiene
    /// que seguir diciendo por qué propiedad preguntaron.
    /// </summary>
    [StringLength(140)]
    public string? PropiedadTitulo { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// <summary>True si además se pudo avisar por correo a la inmobiliaria.</summary>
    public bool CorreoEnviado { get; set; }

    public bool Atendida { get; set; }

    public DateTime? FechaAtendida { get; set; }

    /// <summary>Anotaciones internas: qué se respondió, cómo siguió.</summary>
    [StringLength(2000)]
    public string? Notas { get; set; }

    public string OrigenTexto => Origen switch
    {
        OrigenConsulta.Tasacion => "Pedido de tasación",
        _ => "Consulta de contacto"
    };

    /// <summary>Fecha en hora de Buenos Aires: en la base se guarda siempre en UTC.</summary>
    public DateTime FechaLocal => FechaHelper.AHoraArgentina(Fecha);
}

/// <summary>
/// La base guarda las fechas en UTC —así no hay ambigüedad ni saltos de horario—
/// pero el panel las tiene que mostrar en la hora de acá.
/// </summary>
public static class FechaHelper
{
    private static readonly TimeZoneInfo Argentina = Buscar();

    private static TimeZoneInfo Buscar()
    {
        // El identificador cambia según el sistema: Windows usa el nombre largo
        // y Linux el de la base IANA. Si no está ninguno, se cae a UTC-3 fijo,
        // que es la hora de Argentina todo el año (no hay horario de verano).
        foreach (var id in new[] { "Argentina Standard Time", "America/Argentina/Buenos_Aires" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        return TimeZoneInfo.CreateCustomTimeZone("ART", TimeSpan.FromHours(-3), "Argentina", "Argentina");
    }

    public static DateTime AHoraArgentina(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Argentina);
}
