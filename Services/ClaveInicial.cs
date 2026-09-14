namespace Enricci_Propiedades.Services;

/// <summary>
/// Dónde queda la contraseña inicial del panel cuando se genera al azar.
///
/// Antes se escribía en el log de arranque, y un log se copia, se envía por
/// correo para pedir ayuda y se guarda durante meses: no es lugar para una
/// contraseña. Ahora va a un archivo con permisos 600 en la carpeta de datos
/// —la misma de la base—, que se borra solo en el primer ingreso correcto.
/// </summary>
public static class ClaveInicial
{
    public const string NombreArchivo = "clave-inicial.txt";

    /// <summary>Ruta del archivo: al lado del .db, fuera del alcance de un despliegue.</summary>
    public static string Ruta(IConfiguration configuracion, string carpetaDeLaAplicacion)
    {
        var archivoBase = RutaBaseDeDatos.Archivo(configuracion, carpetaDeLaAplicacion);
        var carpeta = Path.GetDirectoryName(archivoBase);

        return Path.Combine(
            string.IsNullOrEmpty(carpeta) ? carpetaDeLaAplicacion : carpeta,
            NombreArchivo);
    }

    /// <summary>
    /// Agrega una línea "correo: contraseña" al archivo, creándolo con permisos
    /// 600 si no existe. Se agrega y no se pisa porque en un mismo arranque
    /// pueden crearse varios usuarios (el principal y los adicionales).
    /// </summary>
    public static void Escribir(string ruta, string email, string clave)
    {
        var carpeta = Path.GetDirectoryName(ruta);
        if (!string.IsNullOrEmpty(carpeta))
        {
            Directory.CreateDirectory(carpeta);
        }

        var esNuevo = !File.Exists(ruta);

        File.AppendAllText(ruta, $"{email}: {clave}{Environment.NewLine}");

        // Sólo el usuario del servicio puede leerlo. En Windows no aplica: ahí
        // manda la ACL de la carpeta.
        if (esNuevo && !OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(ruta, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    /// <summary>Borra el archivo si existe. Devuelve true si había algo que borrar.</summary>
    public static bool Borrar(string ruta)
    {
        if (!File.Exists(ruta))
        {
            return false;
        }

        File.Delete(ruta);
        return true;
    }
}
