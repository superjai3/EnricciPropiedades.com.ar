namespace Enricci_Propiedades.Models;

/// <summary>
/// Respaldo automático de la base y de las fotos. Sección "Respaldo" de
/// appsettings.json.
/// </summary>
public class OpcionesRespaldo
{
    public const string Seccion = "Respaldo";

    public bool Habilitado { get; set; } = true;

    /// <summary>
    /// Carpeta donde se dejan los archivos. Si es relativa se resuelve contra la
    /// carpeta del sitio. Conviene apuntarla a otro disco o a una carpeta
    /// sincronizada con la nube: un respaldo en el mismo disco no protege de
    /// que el disco se rompa.
    /// </summary>
    public string Carpeta { get; set; } = "respaldos";

    /// <summary>Hora local a la que corre el respaldo diario (0 a 23).</summary>
    public int HoraDiaria { get; set; } = 3;

    /// <summary>Cuántos respaldos se conservan; los más viejos se van borrando.</summary>
    public int Conservar { get; set; } = 14;
}
