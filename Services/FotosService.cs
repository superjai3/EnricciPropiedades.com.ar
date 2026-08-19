namespace Enricci_Propiedades.Services;

/// <summary>
/// Guarda en disco las fotos que se suben desde el panel y devuelve la ruta
/// pública para referenciarlas en la publicación.
/// </summary>
public class FotosService
{
    private const long TamanioMaximo = 8 * 1024 * 1024;
    private const int MaximoPorPropiedad = 12;

    private static readonly string[] ExtensionesValidas = { ".jpg", ".jpeg", ".png", ".webp" };

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
                errores.Add($"«{archivo.FileName}»: sólo se aceptan JPG, PNG o WEBP.");
                continue;
            }

            if (archivo.Length > TamanioMaximo)
            {
                errores.Add($"«{archivo.FileName}» pesa más de {TamanioMaximoTexto}.");
                continue;
            }

            if (!await EsImagenDeVerdadAsync(archivo))
            {
                errores.Add($"«{archivo.FileName}» no parece ser una imagen válida.");
                continue;
            }

            Directory.CreateDirectory(carpetaFisica);

            // Nombre propio: nunca se usa el del archivo subido, que podría
            // traer rutas o caracteres inesperados.
            var nombre = $"{Guid.NewGuid():N}{extension}";
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
        var leidos = await entrada.ReadAsync(cabecera);

        if (leidos < 12)
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
