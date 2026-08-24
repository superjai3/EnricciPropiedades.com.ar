namespace Enricci_Propiedades.Services;

/// <summary>
/// La foto de Horacio, que es quien atiende. Existe como servicio y no como una
/// ruta escrita en las páginas por una razón concreta: el archivo lo pone una
/// persona en el servidor, no el repositorio, y una etiqueta img apuntando a un
/// archivo que no está deja un ícono roto en la portada.
///
/// Con esto, si la foto no está, las páginas muestran las iniciales y nadie se
/// entera de que faltaba algo.
/// </summary>
public class RetratoTitular
{
    /// <summary>Dónde va el archivo, dentro de wwwroot.</summary>
    private const string RutaPublica = "/imagenes/horacio.jpg";

    private readonly bool _existe;

    public RetratoTitular(IWebHostEnvironment entorno)
    {
        // Se resuelve una sola vez, al arrancar: es un archivo que no cambia
        // durante la vida del proceso y preguntarle al disco en cada visita
        // sería pagar una consulta de sistema de archivos por página.
        var ruta = Path.Combine(entorno.WebRootPath, "imagenes", "horacio.jpg");
        _existe = File.Exists(ruta);
    }

    /// <summary>True si la foto está puesta y se puede mostrar.</summary>
    public bool Hay => _existe;

    /// <summary>Ruta pública de la foto, o null si no está.</summary>
    public string? Url => _existe ? RutaPublica : null;

    /// <summary>Iniciales para el círculo de respaldo cuando no hay foto.</summary>
    public const string Iniciales = "HE";

    /// <summary>
    /// Texto alternativo. Describe a la persona y su cargo: quien no ve la
    /// imagen tiene que recibir la misma información que quien la ve.
    /// </summary>
    public const string Alt = "Raúl Horacio Enricci, corredor inmobiliario, en su oficina";
}
