using Microsoft.Data.Sqlite;

namespace Enricci_Propiedades.Services;

/// <summary>
/// Dónde está el archivo SQLite del sitio.
///
/// La ruta se resuelve contra la carpeta de la aplicación y no contra el
/// directorio de trabajo, que cambia según cómo se arranque el proceso
/// (consola, servicio de Windows, IIS). Vive acá y no suelta en Program.cs
/// porque el respaldo necesita exactamente la misma ruta que el contexto.
/// </summary>
public static class RutaBaseDeDatos
{
    public const string Predeterminada = "Data Source=enricci.db";

    /// <summary>Cadena de conexión con la ruta ya vuelta absoluta.</summary>
    public static string CadenaDeConexion(IConfiguration configuracion, string carpetaDeLaAplicacion)
    {
        var cadena = configuracion.GetConnectionString("Enricci") ?? Predeterminada;
        var constructor = new SqliteConnectionStringBuilder(cadena);

        if (!string.IsNullOrWhiteSpace(constructor.DataSource) &&
            !Path.IsPathRooted(constructor.DataSource))
        {
            constructor.DataSource = Path.Combine(carpetaDeLaAplicacion, constructor.DataSource);
        }

        return constructor.ConnectionString;
    }

    /// <summary>Ruta del archivo .db en disco.</summary>
    public static string Archivo(IConfiguration configuracion, string carpetaDeLaAplicacion) =>
        new SqliteConnectionStringBuilder(CadenaDeConexion(configuracion, carpetaDeLaAplicacion))
            .DataSource;
}
