namespace Enricci_Propiedades.Services;

/// <summary>
/// Guarda en disco las fotos que se suben desde el panel y devuelve la ruta
/// pública para referenciarlas en la publicación.
/// </summary>
public class FotosService
{
    // Una foto de teléfono actual pasa los 8 MB sin ninguna dificultad, así que
    // el tope tiene que dar lugar a lo que la gente sube de verdad.
    private const long TamanioMaximo = 20 * 1024 * 1024;
    private const int MaximoPorPropiedad = 12;

    /// <summary>
    /// Formatos que el navegador sabe mostrar. ".jfif" es un JPEG con otro
    /// nombre: así los guarda Windows al bajarlos desde el navegador.
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

            // Nombre propio: nunca se usa el del archivo subido, que podría
            // traer rutas o caracteres inesperados. La extensión se normaliza
            // para que el archivo se sirva con el tipo correcto.
            var nombre = $"{Guid.NewGuid():N}{Normalizar(extension)}";
            var destino = Path.Combine(carpetaFisica, nombre);

            await using (var salida = File.Create(destino))
            {
                await archivo.CopyToAsync(salida);
            }

            guardadas.Add($"/imagenes/propiedades/{propiedadId}/{nombre}");
            _log.LogInformation("Foto cargada para la propiedad {Id}: {Nombre}", propiedadId, nombre);
        }

        return guardadas;
    }

    /// <summary>
    /// ".jfif" y ".jpeg" son JPEG con otro nombre. Se guardan como ".jpg" para
    /// que el servidor los entregue como image/jpeg y no como el heredado
    /// image/pjpeg, que es lo que devuelve para .jfif.
    /// </summary>
    private static string Normalizar(string extension) => extension switch
    {
        ".jfif" or ".jpeg" => ".jpg",
        _ => extension
    };

    /// <summary>Borra el archivo físico de una foto. Ignora las que no existan.</summary>
    public void Borrar(string rutaPublica)
    {
        // Sólo se borran archivos dentro de la carpeta de fotos del sitio.
        if (!rutaPublica.StartsWith("/imagenes/propiedades/", StringComparison.Ordinal))
        {
            return;
        }

        var relativa = rutaPublica.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fisica = Path.GetFullPath(Path.Combine(RaizWeb, relativa));
        var raizFotos = Path.GetFullPath(Path.Combine(RaizWeb, "imagenes", "propiedades"));

        if (!fisica.StartsWith(raizFotos, StringComparison.Ordinal) || !File.Exists(fisica))
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
