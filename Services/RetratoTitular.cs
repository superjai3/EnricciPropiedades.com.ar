namespace Enricci_Propiedades.Services;

/// <summary>
/// La foto de Horacio, que es quien atiende. Existe como servicio y no como una
/// ruta escrita en las páginas para que el sitio no dependa de que el archivo
/// esté: una etiqueta img apuntando a una foto que falta deja un ícono roto en
/// la portada, y quien clona el proyecto no tiene por qué enterarse así.
///
/// Si la foto no está, las páginas muestran las iniciales y no se rompe nada.
/// </summary>
public class RetratoTitular
{
    /// <summary>La foto para donde se ve grande: portada y Quiénes somos.</summary>
    public const string RutaGrande = "/imagenes/horacio.webp";

    /// <summary>
    /// La misma foto en chico, para el círculo de 46 px de la firma. Existe
    /// aparte porque bajarse la grande para mostrarla del tamaño de una moneda
    /// es gastar 54 KB en lugar de 3.
    /// </summary>
    public const string RutaChica = "/imagenes/horacio-min.webp";

    private readonly bool _existe;

    public RetratoTitular(IWebHostEnvironment entorno)
    {
        // Se resuelve una sola vez, al arrancar: es un archivo que no cambia
        // durante la vida del proceso y preguntarle al disco en cada visita
        // sería pagar una consulta de sistema de archivos por página.
        var ruta = Path.Combine(entorno.WebRootPath, "imagenes", "horacio.webp");
        _existe = File.Exists(ruta);
    }

    /// <summary>True si la foto está puesta y se puede mostrar.</summary>
    public bool Hay => _existe;

    /// <summary>Ruta pública de la foto grande, o null si no está.</summary>
    public string? Url => _existe ? RutaGrande : null;

    /// <summary>Ruta pública de la foto chica, o null si no está.</summary>
    public string? UrlChica => _existe ? RutaChica : null;

    /// <summary>Iniciales para el círculo de respaldo cuando no hay foto.</summary>
    public const string Iniciales = "HE";

    /// <summary>
    /// Texto alternativo. Describe a la persona y su cargo: quien no ve la
    /// imagen tiene que recibir la misma información que quien la ve.
    /// </summary>
    public const string Alt = "Raúl Horacio Enricci, corredor inmobiliario, en su oficina";
}
