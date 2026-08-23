namespace Enricci_Propiedades.Models;

/// <summary>
/// Datos de contacto y de la empresa en un solo lugar, para que no queden
/// repetidos (ni desactualizados) a lo largo de las páginas.
///
/// De acá salen también los datos estructurados que leen los buscadores y los
/// asistentes con IA: si el teléfono cambia en un solo archivo, cambia en la
/// página, en el pie y en lo que le declaramos a Google.
/// </summary>
public static class SitioInfo
{
    public const string Nombre = "R. H. Enricci Propiedades";
    public const string NombreCorto = "Enricci Propiedades";
    public const string Bajada = "Inmobiliaria en CABA desde 1932";
    public const string AnioFundacion = "1932";

    public const string Direccion = "Solís 642";
    public const string Unidad = "Piso 1º D";
    public const string Localidad = "Monserrat, Ciudad Autónoma de Buenos Aires";
    public const string CodigoPostal = "C1078AAK";

    /// <summary>Barrio donde está la oficina. Es el que más peso tiene en las búsquedas locales.</summary>
    public const string BarrioOficina = "Monserrat";

    /// <summary>Comuna de la Ciudad a la que pertenece Monserrat.</summary>
    public const string Comuna = "Comuna 1";

    // Coordenadas de la puerta de la oficina, tomadas de OpenStreetMap.
    // Van en los datos estructurados: sin ellas, el buscador tiene que deducir
    // dónde queda el local a partir del texto de la dirección, y a veces le erra.
    public const double Latitud = -34.6161007;
    public const double Longitud = -58.3903063;

    public const string Telefono = "011 4383-1516";
    public const string TelefonoLink = "tel:+541143831516";

    /// <summary>Teléfono en formato internacional, que es el que piden los datos estructurados.</summary>
    public const string TelefonoE164 = "+541143831516";

    public const string Celular = "011 15-3298-6133";
    public const string WhatsappNumero = "5491132986133";
    public const string Email = "horacioenricci@gmail.com";

    public const string Horario = "Lunes a viernes de 11 a 18.30 h, con turno previo";

    // El horario, otra vez, pero en piezas: los datos estructurados lo piden así
    // y de este modo la frase de arriba y lo que lee el buscador no se separan.
    public static readonly string[] DiasDeAtencion =
        { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };

    public const string HoraApertura = "11:00";
    public const string HoraCierre = "18:30";

    public const string Titular = "Raúl Horacio Enricci";
    public const string MatriculaNumero = "2377";
    public const string Matricula = $"{Titular} · Corredor inmobiliario · CUCICBA {MatriculaNumero}";

    /// <summary>
    /// Barrios donde la inmobiliaria trabaja. Es el área que se declara en los
    /// datos estructurados y la lista que ofrece el formulario de tasación: una
    /// sola fuente para las dos cosas, que antes estaban repetidas.
    /// </summary>
    public static readonly string[] BarriosQueAtiende =
    {
        "Monserrat",
        "Constitución",
        "San Cristóbal",
        "San Telmo",
        "Balvanera",
        "Boedo",
        "Almagro",
        "Parque Patricios"
    };

    /// <summary>Dirección con la unidad, para la página de contacto y el pie.</summary>
    public const string DireccionCompleta = $"{Direccion}, {Unidad}";

    /// <summary>Búsqueda en el mapa: va sin la unidad, que no ayuda a ubicar el lugar.</summary>
    public static string MapaUrl =>
        $"https://www.google.com/maps/search/?api=1&query={Uri.EscapeDataString($"{Direccion}, CABA")}";

    public const string Instagram = "https://www.instagram.com/enricci_propiedades/";

    /// <summary>Vacío mientras no haya página: el ícono no se muestra si no hay adónde ir.</summary>
    public const string Facebook = "";

    /// <summary>
    /// Perfiles de la inmobiliaria en otros lados. Los buscadores los usan para
    /// confirmar que la empresa de la web y la del perfil son la misma.
    /// </summary>
    public static IEnumerable<string> Perfiles =>
        new[] { Instagram, Facebook }.Where(p => !string.IsNullOrWhiteSpace(p));

    public static string Whatsapp(string mensaje) =>
        $"https://wa.me/{WhatsappNumero}?text={Uri.EscapeDataString(mensaje)}";

    public static string WhatsappGeneral =>
        Whatsapp("¡Hola! Escribo desde la web de Enricci Propiedades. Quisiera hacer una consulta.");

    public static string MailA(string asunto) =>
        $"mailto:{Email}?subject={Uri.EscapeDataString(asunto)}";
}
