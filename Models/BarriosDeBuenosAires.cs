namespace Enricci_Propiedades.Models;

/// <summary>Dónde queda un barrio: cambia la provincia que se declara.</summary>
public enum Jurisdiccion
{
    /// <summary>Ciudad Autónoma de Buenos Aires.</summary>
    Ciudad,

    /// <summary>Partidos del Gran Buenos Aires, que son provincia de Buenos Aires.</summary>
    GranBuenosAires
}

/// <summary>Una entrada de la lista de barrios y partidos donde se puede publicar.</summary>
public record Zona(string Nombre, Jurisdiccion Jurisdiccion, string Grupo)
{
    /// <summary>Lo que va como addressRegion en los datos estructurados.</summary>
    public string Region => Jurisdiccion == Jurisdiccion.Ciudad
        ? "Ciudad Autónoma de Buenos Aires"
        : "Provincia de Buenos Aires";
}

/// <summary>
/// La lista cerrada de barrios y partidos donde la inmobiliaria publica.
///
/// Existe porque el campo era texto libre y así se coló un «Río de Janeiro»
/// —que en la Ciudad es una avenida, no un barrio— y con él una publicación que
/// figuraba fuera del país. Con una lista, el barrio de una publicación siempre
/// es un lugar que existe, las páginas por barrio no se multiplican por errores
/// de tipeo y los datos estructurados declaran la provincia correcta.
///
/// Todo lo de acá es Argentina: la Ciudad y los 24 partidos del Gran Buenos
/// Aires.
/// </summary>
public static class BarriosDeBuenosAires
{
    public const string GrupoCiudad = "Ciudad de Buenos Aires";
    public const string GrupoZonas = "Zonas de la Ciudad";
    public const string GrupoGba = "Gran Buenos Aires";

    /// <summary>Los 48 barrios oficiales de la Ciudad Autónoma de Buenos Aires.</summary>
    private static readonly string[] BarriosOficiales =
    {
        "Agronomía", "Almagro", "Balvanera", "Barracas", "Belgrano", "Boedo",
        "Caballito", "Chacarita", "Coghlan", "Colegiales", "Constitución",
        "Flores", "Floresta", "La Boca", "La Paternal", "Liniers", "Mataderos",
        "Monserrat", "Monte Castro", "Nueva Pompeya", "Núñez", "Palermo",
        "Parque Avellaneda", "Parque Chacabuco", "Parque Chas", "Parque Patricios",
        "Puerto Madero", "Recoleta", "Retiro", "Saavedra", "San Cristóbal",
        "San Nicolás", "San Telmo", "Vélez Sársfield", "Versalles", "Villa Crespo",
        "Villa del Parque", "Villa Devoto", "Villa General Mitre", "Villa Lugano",
        "Villa Luro", "Villa Ortúzar", "Villa Pueyrredón", "Villa Real",
        "Villa Riachuelo", "Villa Santa Rita", "Villa Soldati", "Villa Urquiza"
    };

    /// <summary>
    /// Nombres que no son barrios oficiales pero que el mercado inmobiliario usa
    /// todos los días —y que el propio catálogo ya usa, como Congreso—. Van en un
    /// grupo aparte para que se vea que no son de la lista oficial: si se
    /// prefiere obligar a usar sólo los 48, se borra esta lista y listo.
    /// </summary>
    private static readonly string[] ZonasDeUsoCorriente =
    {
        "Abasto", "Barrio Norte", "Catalinas", "Congreso", "Las Cañitas",
        "Microcentro", "Once", "Palermo Chico", "Palermo Hollywood",
        "Palermo Soho", "Tribunales"
    };

    /// <summary>Los 24 partidos del Gran Buenos Aires.</summary>
    private static readonly string[] PartidosDelGba =
    {
        "Almirante Brown", "Avellaneda", "Berazategui", "Esteban Echeverría",
        "Ezeiza", "Florencio Varela", "General San Martín", "Hurlingham",
        "Ituzaingó", "José C. Paz", "La Matanza", "Lanús", "Lomas de Zamora",
        "Malvinas Argentinas", "Merlo", "Moreno", "Morón", "Quilmes",
        "San Fernando", "San Isidro", "San Miguel", "Tigre", "Tres de Febrero",
        "Vicente López"
    };

    /// <summary>Todas las zonas donde se puede publicar, en el orden en que se ofrecen.</summary>
    public static readonly IReadOnlyList<Zona> Todas =
        BarriosOficiales.Select(n => new Zona(n, Jurisdiccion.Ciudad, GrupoCiudad))
            .Concat(ZonasDeUsoCorriente.Select(n => new Zona(n, Jurisdiccion.Ciudad, GrupoZonas)))
            .Concat(PartidosDelGba.Select(n => new Zona(n, Jurisdiccion.GranBuenosAires, GrupoGba)))
            .ToList();

    /// <summary>Las zonas agrupadas, para armar los &lt;optgroup&gt; del formulario.</summary>
    public static IEnumerable<IGrouping<string, Zona>> PorGrupo =>
        Todas.GroupBy(z => z.Grupo);

    public const string Pais = "AR";

    /// <summary>Busca una zona por nombre. Devuelve null si el nombre no está en la lista.</summary>
    public static Zona? Buscar(string? nombre) =>
        string.IsNullOrWhiteSpace(nombre)
            ? null
            : Todas.FirstOrDefault(z =>
                string.Equals(z.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase));

    public static bool EsValido(string? nombre) => Buscar(nombre) is not null;

    /// <summary>
    /// Provincia que le corresponde a un barrio. Las publicaciones viejas pueden
    /// tener un barrio que no está en la lista —el catálogo importado trae al
    /// menos uno—; para esas se asume la Ciudad, que es donde está todo lo demás.
    /// </summary>
    public static string RegionDe(string? barrio) =>
        Buscar(barrio)?.Region ?? "Ciudad Autónoma de Buenos Aires";
}
