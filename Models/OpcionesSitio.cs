namespace Enricci_Propiedades.Models;

/// <summary>
/// Cómo se publica el sitio. Se lee de la sección "Sitio" de appsettings.json.
///
/// El dominio vive acá y no escrito dentro del código: se usa para las URL
/// canónicas, las de Open Graph, el sitemap y robots.txt, de modo que un cambio
/// de dominio sea una línea de configuración y no una búsqueda por el proyecto.
/// </summary>
public class OpcionesSitio
{
    public const string Seccion = "Sitio";

    /// <summary>
    /// Dominio definitivo, sin esquema ni barra final (por ejemplo
    /// "www.enricci-propiedades.com.ar"). Vacío mientras no esté dado de alta:
    /// en ese caso las URL absolutas salen del host del pedido, que es lo
    /// correcto en desarrollo y con un túnel de pruebas.
    /// </summary>
    public string Dominio { get; set; } = "";

    public bool HayDominio => !string.IsNullOrWhiteSpace(Dominio);

    /// <summary>
    /// URL absoluta del sitio. Con el dominio configurado se usa siempre ese,
    /// aunque el visitante haya entrado por la IP o por un túnel: si no, los
    /// buscadores verían la misma página publicada en dos direcciones distintas
    /// y repartirían el posicionamiento entre las dos.
    /// </summary>
    public string UrlBase(HttpRequest pedido) => HayDominio
        ? $"https://{Dominio.Trim().TrimEnd('/')}"
        : $"{pedido.Scheme}://{pedido.Host}";
}
