namespace Enricci_Propiedades.Models;

/// <summary>
/// Datos de contacto y de la empresa en un solo lugar, para que no queden
/// repetidos (ni desactualizados) a lo largo de las páginas.
/// </summary>
public static class SitioInfo
{
    public const string Nombre = "R. H. Enricci Propiedades";
    public const string NombreCorto = "Enricci Propiedades";
    public const string Bajada = "Inmobiliaria en CABA desde 1932";
    public const string AnioFundacion = "1932";

    public const string Direccion = "Solís 581";
    public const string Localidad = "Monserrat, Ciudad Autónoma de Buenos Aires";
    public const string CodigoPostal = "C1078AAK";

    public const string Telefono = "011 4383-1516";
    public const string TelefonoLink = "tel:+541143831516";
    public const string Celular = "011 15-3298-6133";
    public const string WhatsappNumero = "5491132986133";
    public const string Email = "horacioenricci@gmail.com";

    public const string Horario = "Lunes a viernes de 10 a 18 h · Sábados de 10 a 13 h";
    public const string Matricula = "Matrícula CUCICBA · Corredor Inmobiliario";

    public const string Instagram = "https://www.instagram.com/enricci_propiedades/";

    /// <summary>Vacío mientras no haya página: el ícono no se muestra si no hay adónde ir.</summary>
    public const string Facebook = "";

    public static string Whatsapp(string mensaje) =>
        $"https://wa.me/{WhatsappNumero}?text={Uri.EscapeDataString(mensaje)}";

    public static string WhatsappGeneral =>
        Whatsapp("¡Hola! Escribo desde la web de Enricci Propiedades. Quisiera hacer una consulta.");

    public static string MailA(string asunto) =>
        $"mailto:{Email}?subject={Uri.EscapeDataString(asunto)}";
}
