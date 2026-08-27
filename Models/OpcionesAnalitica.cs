namespace Enricci_Propiedades.Models;

/// <summary>
/// Medición del sitio. Se lee de la sección "Analitica" de appsettings.json.
///
/// El identificador no está escrito en el código a propósito: lo da el dueño del
/// sitio desde su cuenta de Google, y hasta que no esté cargado el sitio no mide
/// nada ni deja ninguna cookie de terceros.
///
/// Mientras <see cref="Id"/> esté vacío:
/// no se carga el script de Google, no aparece el aviso de cookies y la política
/// de contenido sigue tan cerrada como estaba. Es un interruptor único: al pegar
/// el identificador se encienden a la vez la medición y el aviso que la habilita,
/// que es justo lo que exige la ley — nunca uno sin el otro.
/// </summary>
public class OpcionesAnalitica
{
    public const string Seccion = "Analitica";

    /// <summary>
    /// Identificador de propiedad de Google Analytics 4, con el formato
    /// "G-XXXXXXXXXX". Vacío mientras el cliente no lo entregue.
    /// </summary>
    public string Id { get; set; } = "";

    public bool Habilitada => !string.IsNullOrWhiteSpace(Id);

    /// <summary>
    /// El identificador ya saneado para escribirlo dentro de una URL. Google usa
    /// letras, números y guiones; cualquier otra cosa es un error de carga y no
    /// tiene por qué llegar al HTML.
    /// </summary>
    public string IdSeguro =>
        new(Id.Trim().Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());
}
