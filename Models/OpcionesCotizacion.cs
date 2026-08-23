namespace Enricci_Propiedades.Models;

/// <summary>
/// De dónde sale el tipo de cambio con el que se muestra el equivalente en
/// pesos. Se lee de la sección "Cotizacion" de appsettings.json.
/// </summary>
public class OpcionesCotizacion
{
    public const string Seccion = "Cotizacion";

    /// <summary>Con esto en false el sitio no consulta nada y sólo muestra el precio publicado.</summary>
    public bool Habilitada { get; set; } = true;

    /// <summary>
    /// Fuente del dato. La de fábrica devuelve el dólar oficial, que es el que
    /// publica el Banco de la Nación Argentina.
    /// </summary>
    public string Url { get; set; } = "https://dolarapi.com/v1/dolares/oficial";

    /// <summary>Cada cuánto se vuelve a consultar. El BNA actualiza una vez por día hábil.</summary>
    public int MinutosEntreConsultas { get; set; } = 60;

    /// <summary>Cómo se nombra la fuente en el sitio, al pie del importe convertido.</summary>
    public string Fuente { get; set; } = "Banco Nación";

    /// <summary>
    /// Qué punta se usa para convertir. "venta" es la que paga quien compra
    /// dólares, que es la situación de quien está por comprar una propiedad.
    /// </summary>
    public string Punta { get; set; } = "venta";
}
