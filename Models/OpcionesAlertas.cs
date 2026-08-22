namespace Enricci_Propiedades.Models;

/// <summary>
/// Los avisos por correo de propiedades nuevas. Se lee de la sección "Alertas"
/// de appsettings.json.
///
/// Aunque estén habilitadas, no hay alertas si el correo no está configurado:
/// ofrecer un alta que después no manda nada es peor que no ofrecerla.
/// </summary>
public class OpcionesAlertas
{
    public const string Seccion = "Alertas";

    public bool Habilitadas { get; set; } = true;

    /// <summary>
    /// Cada cuánto se busca si entró algo nuevo. No hace falta que sea seguido:
    /// una publicación no se carga cada cinco minutos, y avisar media hora más
    /// tarde no le cambia nada a nadie.
    /// </summary>
    public int MinutosEntreRevisiones { get; set; } = 30;

    /// <summary>
    /// Cuántas propiedades entran como mucho en un mismo aviso. Si entraron más,
    /// el correo dice cuántas quedaron y enlaza al listado: veinte fichas en un
    /// correo no las lee nadie.
    /// </summary>
    public int MaximoPorAviso { get; set; } = 5;
}
