namespace Enricci_Propiedades.Models;

/// <summary>
/// Cuánto tiempo se conservan las consultas de los formularios. Sección
/// "Consultas" de appsettings.json.
///
/// Los datos personales de una consulta (nombre, correo, teléfono, dirección
/// de la propiedad a tasar) no tienen por qué quedarse para siempre: pasado el
/// plazo, la consulta se borra sola. Es el plazo que declara la Política de
/// privacidad, así que si se cambia acá hay que cambiarlo también allá.
/// </summary>
public class OpcionesConsultas
{
    public const string Seccion = "Consultas";

    /// <summary>Meses que se guarda una consulta antes de borrarla. Con 0 no se purga nada.</summary>
    public int MesesRetencion { get; set; } = 24;
}
