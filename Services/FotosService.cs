using System.Collections.Concurrent;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Enricci_Propiedades.Services;

/// <summary>
/// Guarda en disco las fotos que se suben desde el panel y devuelve la ruta
/// pública para referenciarlas en la publicación.
///
/// Ninguna foto se guarda como vino: se la reduce, se la convierte a WEBP y se
/// le genera una miniatura. Una foto de teléfono sin tocar pesa varios MB y el
/// listado carga seis de una; publicada tal cual, el sitio se vuelve inusable
/// con datos móviles.
/// </summary>
public class FotosService
{
    // Tope de lo que se acepta subir. Una foto de teléfono actual pasa los 8 MB
    // sin ninguna dificultad, así que tiene que dar lugar a lo que la gente sube
    // de verdad; lo que se guarda después pesa una fracción de eso.
    private const long TamanioMaximo = 20 * 1024 * 1024;
    private const int MaximoPorPropiedad = 12;

    /// <summary>
    /// Lado mayor de la foto publicada. 1600 px cubre una pantalla grande con el
    /// visor abierto; más allá de eso el peso sube y no se nota.
    /// </summary>
    private const int LadoMaximo = 1600;

    /// <summary>Lado mayor de la miniatura: la que va en las tarjetas del listado.</summary>
    private const int LadoMiniatura = 600;

    /// <summary>
    /// Calidad del WEBP. En 78 la diferencia con el original no se ve en una
    /// fotografía y el archivo queda varias veces más liviano.
    /// </summary>
    private const int CalidadWebp = 78;

    /// <summary>Sufijo del archivo de miniatura, que queda junto al de la foto grande.</summary>
    private const string SufijoMiniatura = "-min";

    /// <summary>Carpeta pública donde viven todas las fotos del catálogo.</summary>
    private const string RaizPublicaFotos = "/imagenes/propiedades/";

    /// <summary>
    /// Formatos que se aceptan subir. ".jfif" es un JPEG con otro nombre: así
    /// los guarda Windows al bajarlos desde el navegador.
    /// </summary>
    private static readonly string[] ExtensionesValidas =
        { ".jpg", ".jpeg", ".jfif", ".png", ".webp" };

    /// <summary>
    /// Formatos de foto que el teléfono genera pero que los navegadores no
    /// muestran. Se rechazan con una explicación de qué hacer, en vez del
    /// mensaje genérico que no le dice nada a nadie.
    /// </summary>
    private static readonly Dictionary<string, string> ExtensionesConocidasNoSoportadas = new()
    {
        [".heic"] = "las fotos del iPhone vienen en formato HEIC, que los navegadores no muestran",
        [".heif"] = "las fotos del iPhone vienen en formato HEIF, que los navegadores no muestran",
        [".avif"] = "AVIF todavía no lo muestran todos los navegadores",
        [".tif"] = "TIFF no se puede publicar en una web",
        [".tiff"] = "TIFF no se puede publicar en una web",
        [".bmp"] = "BMP pesa muchísimo para una web",
        [".gif"] = "GIF pierde calidad en una fotografía"
    };

    private readonly IWebHostEnvironment _entorno;
    private readonly ILogger<FotosService> _log;

    /// <summary>
    /// Qué fotos tienen miniatura en disco. Las publicaciones cargadas antes de
    /// que existieran las miniaturas no la tienen, y preguntárselo al disco en
    /// cada tarjeta sería un acceso por imagen y por visita.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _miniaturas = new();

    public FotosService(IWebHostEnvironment entorno, ILogger<FotosService> log)
    {
        _entorno = entorno;
        _log = log;
    }

    public static int Maximo => MaximoPorPropiedad;

    public static string TamanioMaximoTexto => $"{TamanioMaximo / 1024 / 1024} MB";

    /// <summary>
    /// Carpeta pública del sitio. <c>WebRootPath</c> queda en null cuando la
    /// carpeta todavía no existe en disco (según cómo se arranque el proceso),
    /// así que se arma la ruta a mano en ese caso.
    /// </summary>
    private string RaizWeb => string.IsNullOrEmpty(_entorno.WebRootPath)
        ? Path.Combine(_entorno.ContentRootPath, "wwwroot")
        : _entorno.WebRootPath;

    /// <summary>
    /// Versión chica de una foto, para las tarjetas del listado y del panel. Si
    /// la publicación es anterior a las miniaturas devuelve la foto original,
    /// que se sigue viendo igual: sólo pesa más.
    /// </summary>
    public string Miniatura(string rutaPublica)
    {
        if (string.IsNullOrEmpty(rutaPublica) ||
            !rutaPublica.StartsWith(RaizPublicaFotos, StringComparison.Ordinal))
        {
            return rutaPublica;
        }

        return _miniaturas.GetOrAdd(rutaPublica, ruta =>
        {
            var candidata = ConSufijoMiniatura(ruta);
            var fisica = ARutaFisica(candidata);

            return fisica is not null && File.Exists(fisica) ? candidata : ruta;
        });
    }

    /// <summary>Convierte "/…/abc.webp" en "/…/abc-min.webp".</summary>
    private static string ConSufijoMiniatura(string rutaPublica)
    {
        var punto = rutaPublica.LastIndexOf('.');

        return punto < 0
            ? rutaPublica + SufijoMiniatura
            : rutaPublica[..punto] + SufijoMiniatura + rutaPublica[punto..];
    }

    /// <summary>
    /// Guarda los archivos válidos y devuelve las rutas públicas. Los que no
    /// pasan la validación se descartan y se informan en <paramref name="errores"/>.
    /// </summary>
    public async Task<List<string>> GuardarAsync(
        int propiedadId,
        IEnumerable<IFormFile> archivos,
        int yaCargadas,
        List<string> errores)
    {
        var guardadas = new List<string>();
        var carpetaRelativa = Path.Combine("imagenes", "propiedades", propiedadId.ToString());
        var carpetaFisica = Path.Combine(RaizWeb, carpetaRelativa);

        foreach (var archivo in archivos)
        {
            if (archivo.Length == 0)
            {
                continue;
            }

            if (yaCargadas + guardadas.Count >= MaximoPorPropiedad)
            {
                errores.Add($"Se alcanzó el máximo de {MaximoPorPropiedad} fotos por propiedad.");
                break;
            }

            var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();

            if (!ExtensionesValidas.Contains(extension))
            {
                // Cada rechazo queda en el log con el detalle del archivo: sin esto
                // no hay forma de averiguar después por qué una foto no entró.
                _log.LogWarning(
                    "Foto rechazada por formato: {Nombre} ({Tipo}, {Bytes} bytes).",
                    archivo.FileName, archivo.ContentType, archivo.Length);

                errores.Add(ExtensionesConocidasNoSoportadas.TryGetValue(extension, out var motivo)
                    ? $"«{archivo.FileName}»: {motivo}. Convertila a JPG y volvé a subirla."
                    : $"«{archivo.FileName}»: sólo se aceptan JPG, PNG o WEBP.");
                continue;
            }

            if (archivo.Length > TamanioMaximo)
            {
                _log.LogWarning(
                    "Foto rechazada por tamaño: {Nombre} ({Bytes} bytes).",
                    archivo.FileName, archivo.Length);

                errores.Add(
                    $"«{archivo.FileName}» pesa {archivo.Length / 1024d / 1024d:N1} MB y el máximo " +
                    $"es {TamanioMaximoTexto}. Reducile el tamaño y volvé a subirla.");
                continue;
            }

            if (!await EsImagenDeVerdadAsync(archivo))
            {
                _log.LogWarning(
                    "Foto rechazada: el contenido de {Nombre} no coincide con una imagen ({Tipo}).",
                    archivo.FileName, archivo.ContentType);

                errores.Add($"«{archivo.FileName}» no parece ser una imagen válida.");
                continue;
            }

            Directory.CreateDirectory(carpetaFisica);

            // Nombre propio: nunca se usa el del archivo subido, que podría traer
            // rutas o caracteres inesperados. Todo sale en WEBP, sin importar con
            // qué formato haya entrado.
            var nombre = $"{Guid.NewGuid():N}.webp";

            try
            {
                await ProcesarYGuardarAsync(archivo, carpetaFisica, nombre);
            }
            catch (Exception ex) when (ex is ImageFormatException or InvalidImageContentException)
            {
                // La firma decía que era una imagen, pero el archivo está cortado
                // o dañado y el decodificador no pudo con él.
                _log.LogWarning(ex, "No se pudo procesar la foto {Nombre}.", archivo.FileName);
                errores.Add($"«{archivo.FileName}» está dañada y no se pudo procesar.");
                continue;
            }

            guardadas.Add($"{RaizPublicaFotos}{propiedadId}/{nombre}");
            _log.LogInformation("Foto cargada para la propiedad {Id}: {Nombre}", propiedadId, nombre);
        }

        return guardadas;
    }

    /// <summary>
    /// Reduce la foto, le saca los metadatos y escribe la versión grande y la
    /// miniatura, las dos en WEBP.
    /// </summary>
    private async Task ProcesarYGuardarAsync(IFormFile archivo, string carpetaFisica, string nombre)
    {
        await using var entrada = archivo.OpenReadStream();
        using var imagen = await Image.LoadAsync(entrada);

        var anchoOriginal = imagen.Width;
        var altoOriginal = imagen.Height;

        // La cámara del teléfono no rota la foto: deja la orientación anotada en
        // los metadatos EXIF. Hay que aplicarla antes de tocar nada, o las fotos
        // sacadas de costado quedan acostadas.
        imagen.Mutate(x => x.AutoOrient());

        // Fuera los metadatos. Además de pesar, las fotos de teléfono suelen
        // traer las coordenadas GPS de dónde fueron sacadas: publicarlas sería
        // dar la ubicación exacta de la propiedad sin haberlo decidido.
        imagen.Metadata.ExifProfile = null;
        imagen.Metadata.IptcProfile = null;
        imagen.Metadata.XmpProfile = null;

        var codificador = new WebpEncoder
        {
            Quality = CalidadWebp,
            FileFormat = WebpFileFormatType.Lossy
        };

        var rutaGrande = Path.Combine(carpetaFisica, nombre);
        var rutaMiniatura = Path.Combine(
            carpetaFisica, Path.GetFileNameWithoutExtension(nombre) + SufijoMiniatura + ".webp");

        using (var grande = Redimensionar(imagen, LadoMaximo))
        {
            await grande.SaveAsWebpAsync(rutaGrande, codificador);
        }

        using (var chica = Redimensionar(imagen, LadoMiniatura))
        {
            await chica.SaveAsWebpAsync(rutaMiniatura, codificador);
        }

        _log.LogInformation(
            "Foto procesada: {Nombre}, {Ancho}x{Alto} y {MbOriginal:N1} MB, quedó en {Kb:N0} KB " +
            "más {KbMin:N0} KB de miniatura.",
            archivo.FileName, anchoOriginal, altoOriginal, archivo.Length / 1024d / 1024d,
            new FileInfo(rutaGrande).Length / 1024d, new FileInfo(rutaMiniatura).Length / 1024d);
    }

    /// <summary>
    /// Copia reducida para que el lado mayor no pase de <paramref name="lado"/>.
    /// Las fotos que ya son más chicas no se agrandan: sólo se verían borrosas.
    /// </summary>
    private static Image Redimensionar(Image original, int lado) =>
        original.Clone(x => x.Resize(new ResizeOptions
        {
            Size = new Size(lado, lado),
            Mode = ResizeMode.Max,
            Sampler = KnownResamplers.Lanczos3
        }));

    /// <summary>
    /// Borra el archivo físico de una foto y el de su miniatura. Ignora las que
    /// no existan.
    /// </summary>
    public void Borrar(string rutaPublica)
    {
        // Sólo se borran archivos dentro de la carpeta de fotos del sitio.
        if (!rutaPublica.StartsWith(RaizPublicaFotos, StringComparison.Ordinal))
        {
            return;
        }

        BorrarArchivo(rutaPublica);
        BorrarArchivo(ConSufijoMiniatura(rutaPublica));
        _miniaturas.TryRemove(rutaPublica, out _);
    }

    private void BorrarArchivo(string rutaPublica)
    {
        var fisica = ARutaFisica(rutaPublica);

        if (fisica is null || !File.Exists(fisica))
        {
            return;
        }

        try
        {
            File.Delete(fisica);
            _log.LogInformation("Foto eliminada: {Ruta}", rutaPublica);
        }
        catch (IOException ex)
        {
            _log.LogWarning(ex, "No se pudo borrar la foto {Ruta}.", rutaPublica);
        }
    }

    /// <summary>
    /// Ruta en disco de una foto del sitio, o null si la ruta pública apunta
    /// fuera de la carpeta de fotos.
    /// </summary>
    private string? ARutaFisica(string rutaPublica)
    {
        if (!rutaPublica.StartsWith(RaizPublicaFotos, StringComparison.Ordinal))
        {
            return null;
        }

        var relativa = rutaPublica.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fisica = Path.GetFullPath(Path.Combine(RaizWeb, relativa));
        var raizFotos = Path.GetFullPath(Path.Combine(RaizWeb, "imagenes", "propiedades"));

        return fisica.StartsWith(raizFotos, StringComparison.Ordinal) ? fisica : null;
    }

    /// <summary>
    /// Comprueba la firma del archivo: la extensión sola se puede falsificar,
    /// así que se leen los primeros bytes para confirmar que es una imagen.
    /// </summary>
    private static async Task<bool> EsImagenDeVerdadAsync(IFormFile archivo)
    {
        var cabecera = new byte[12];
        await using var entrada = archivo.OpenReadStream();

        // ReadAsync puede devolver menos bytes de los pedidos aunque queden más:
        // ReadAtLeastAsync insiste hasta completar la cabecera.
        var leidos = await entrada.ReadAtLeastAsync(cabecera, cabecera.Length, throwOnEndOfStream: false);

        if (leidos < cabecera.Length)
        {
            return false;
        }

        // JPEG: FF D8 FF
        if (cabecera[0] == 0xFF && cabecera[1] == 0xD8 && cabecera[2] == 0xFF)
        {
            return true;
        }

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (cabecera[0] == 0x89 && cabecera[1] == 0x50 && cabecera[2] == 0x4E && cabecera[3] == 0x47 &&
            cabecera[4] == 0x0D && cabecera[5] == 0x0A && cabecera[6] == 0x1A && cabecera[7] == 0x0A)
        {
            return true;
        }

        // WEBP: "RIFF" .... "WEBP"
        if (cabecera[0] == 'R' && cabecera[1] == 'I' && cabecera[2] == 'F' && cabecera[3] == 'F' &&
            cabecera[8] == 'W' && cabecera[9] == 'E' && cabecera[10] == 'B' && cabecera[11] == 'P')
        {
            return true;
        }

        return false;
    }
}
