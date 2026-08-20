using System.IO.Compression;
using Enricci_Propiedades.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Enricci_Propiedades.Services;

/// <summary>Un archivo de respaldo ya hecho, para listarlo en el panel.</summary>
public record ArchivoDeRespaldo(string Nombre, DateTime Fecha, long Bytes)
{
    public string TamanioTexto => Bytes >= 1024 * 1024
        ? $"{Bytes / 1024d / 1024d:N1} MB"
        : $"{Bytes / 1024d:N0} KB";

    public DateTime FechaLocal => FechaHelper.AHoraArgentina(Fecha);
}

/// <summary>
/// Respaldo de todo lo que no está en el repositorio: la base de datos y las
/// fotos que se subieron desde el panel. Deja un único .zip con las dos cosas.
///
/// La base no se copia con File.Copy: mientras el sitio está andando, el
/// archivo .db puede tener escrituras a medio confirmar en el diario (-wal) y
/// la copia saldría inconsistente. Se usa "VACUUM INTO", que es la forma que
/// tiene SQLite de sacar una foto entera y coherente de la base sin frenarla.
/// </summary>
public class RespaldoService
{
    private const string PrefijoArchivo = "respaldo-";
    private const string NombreBaseEnZip = "enricci.db";
    private const string CarpetaFotosEnZip = "imagenes/propiedades";

    private readonly OpcionesRespaldo _opciones;
    private readonly ILogger<RespaldoService> _log;
    private readonly string _rutaBase;
    private readonly string _carpetaFotos;

    /// <summary>
    /// Un respaldo por vez. Si el diario se cruza con el botón «Respaldar
    /// ahora», el segundo espera en lugar de escribir sobre el primero.
    /// </summary>
    private readonly SemaphoreSlim _turno = new(1, 1);

    public RespaldoService(
        IOptions<OpcionesRespaldo> opciones,
        IConfiguration configuracion,
        IWebHostEnvironment entorno,
        ILogger<RespaldoService> log)
    {
        _opciones = opciones.Value;
        _log = log;
        _rutaBase = RutaBaseDeDatos.Archivo(configuracion, entorno.ContentRootPath);

        var raizWeb = string.IsNullOrEmpty(entorno.WebRootPath)
            ? Path.Combine(entorno.ContentRootPath, "wwwroot")
            : entorno.WebRootPath;

        _carpetaFotos = Path.Combine(raizWeb, "imagenes", "propiedades");

        Carpeta = Path.IsPathRooted(_opciones.Carpeta)
            ? _opciones.Carpeta
            : Path.Combine(entorno.ContentRootPath, _opciones.Carpeta);
    }

    /// <summary>Carpeta donde quedan los .zip, ya vuelta absoluta.</summary>
    public string Carpeta { get; }

    public int Conservar => _opciones.Conservar;

    public int HoraDiaria => _opciones.HoraDiaria;

    public bool Habilitado => _opciones.Habilitado;

    /// <summary>
    /// Hace un respaldo y devuelve el archivo generado. Las excepciones se
    /// propagan: el panel las muestra y el respaldo diario las registra.
    /// </summary>
    public async Task<ArchivoDeRespaldo> RespaldarAsync(CancellationToken cancelacion = default)
    {
        await _turno.WaitAsync(cancelacion);

        try
        {
            Directory.CreateDirectory(Carpeta);

            // Nombre ordenable: listar la carpeta por nombre las deja por fecha.
            var sello = FechaHelper.AHoraArgentina(DateTime.UtcNow).ToString("yyyyMMdd-HHmmss");
            var rutaZip = Path.Combine(Carpeta, $"{PrefijoArchivo}{sello}.zip");
            var rutaCopiaBase = Path.Combine(Carpeta, $"{PrefijoArchivo}{sello}.db.tmp");

            try
            {
                await CopiarBaseAsync(rutaCopiaBase, cancelacion);

                using (var zip = ZipFile.Open(rutaZip, ZipArchiveMode.Create))
                {
                    zip.CreateEntryFromFile(rutaCopiaBase, NombreBaseEnZip, CompressionLevel.Optimal);
                    AgregarFotos(zip);
                }
            }
            finally
            {
                // La copia suelta no queda: lo que vale es el .zip.
                if (File.Exists(rutaCopiaBase))
                {
                    File.Delete(rutaCopiaBase);
                }
            }

            var info = new FileInfo(rutaZip);
            _log.LogInformation(
                "Respaldo hecho: {Nombre} ({Kb:N0} KB).", info.Name, info.Length / 1024d);

            Rotar();

            return new ArchivoDeRespaldo(info.Name, info.CreationTimeUtc, info.Length);
        }
        finally
        {
            _turno.Release();
        }
    }

    /// <summary>
    /// Foto consistente de la base con el sitio andando. VACUUM INTO falla si el
    /// destino ya existe, así que se escribe siempre sobre un nombre nuevo.
    /// </summary>
    private async Task CopiarBaseAsync(string destino, CancellationToken cancelacion)
    {
        if (File.Exists(destino))
        {
            File.Delete(destino);
        }

        await using var conexion = new SqliteConnection($"Data Source={_rutaBase};Mode=ReadOnly");
        await conexion.OpenAsync(cancelacion);

        await using var comando = conexion.CreateCommand();

        // El destino va como parámetro y no concatenado: si la carpeta de
        // respaldos tuviera comillas en el nombre, la sentencia se rompería.
        comando.CommandText = "VACUUM INTO $destino";
        comando.Parameters.AddWithValue("$destino", destino);
        await comando.ExecuteNonQueryAsync(cancelacion);
    }

    /// <summary>Mete la carpeta de fotos del catálogo dentro del zip.</summary>
    private void AgregarFotos(ZipArchive zip)
    {
        if (!Directory.Exists(_carpetaFotos))
        {
            return;
        }

        foreach (var archivo in Directory.EnumerateFiles(_carpetaFotos, "*", SearchOption.AllDirectories))
        {
            var relativa = Path.GetRelativePath(_carpetaFotos, archivo).Replace('\\', '/');

            // Las fotos ya vienen comprimidas (WEBP): volver a comprimirlas sólo
            // gasta tiempo y no achica nada.
            zip.CreateEntryFromFile(archivo, $"{CarpetaFotosEnZip}/{relativa}", CompressionLevel.NoCompression);
        }
    }

    /// <summary>Borra los respaldos más viejos y deja los <see cref="Conservar"/> últimos.</summary>
    private void Rotar()
    {
        if (_opciones.Conservar <= 0)
        {
            return;
        }

        foreach (var viejo in Listar().Skip(_opciones.Conservar))
        {
            try
            {
                File.Delete(Path.Combine(Carpeta, viejo.Nombre));
                _log.LogInformation("Respaldo viejo eliminado: {Nombre}", viejo.Nombre);
            }
            catch (IOException ex)
            {
                _log.LogWarning(ex, "No se pudo borrar el respaldo {Nombre}.", viejo.Nombre);
            }
        }
    }

    /// <summary>Respaldos que hay en la carpeta, del más nuevo al más viejo.</summary>
    public IReadOnlyList<ArchivoDeRespaldo> Listar()
    {
        if (!Directory.Exists(Carpeta))
        {
            return Array.Empty<ArchivoDeRespaldo>();
        }

        return new DirectoryInfo(Carpeta)
            .GetFiles($"{PrefijoArchivo}*.zip")
            .OrderByDescending(a => a.Name, StringComparer.Ordinal)
            .Select(a => new ArchivoDeRespaldo(a.Name, a.CreationTimeUtc, a.Length))
            .ToList();
    }

    /// <summary>
    /// Ruta en disco de un respaldo, o null si el nombre no corresponde a uno.
    /// Se valida el nombre entero contra el listado real: así una ruta armada a
    /// mano no puede salirse de la carpeta de respaldos.
    /// </summary>
    public string? RutaDe(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre) ||
            !Listar().Any(a => a.Nombre.Equals(nombre, StringComparison.Ordinal)))
        {
            return null;
        }

        return Path.Combine(Carpeta, nombre);
    }

    public bool Eliminar(string nombre)
    {
        var ruta = RutaDe(nombre);

        if (ruta is null)
        {
            return false;
        }

        try
        {
            File.Delete(ruta);
            _log.LogInformation("Respaldo eliminado a mano: {Nombre}", nombre);
            return true;
        }
        catch (IOException ex)
        {
            _log.LogWarning(ex, "No se pudo borrar el respaldo {Nombre}.", nombre);
            return false;
        }
    }
}
