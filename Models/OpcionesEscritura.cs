namespace Enricci_Propiedades.Models;

/// <summary>
/// Con qué números se estiman los gastos de una escritura. Se lee de la sección
/// "Escritura" de appsettings.json.
///
/// Está en configuración y no escrito en el código por una razón de fondo: los
/// porcentajes reales —sellos, honorarios, certificaciones— los fija la
/// provincia, el colegio de escribanos y cada operación, cambian con el tiempo
/// y admiten excepciones (vivienda única, montos exentos). Publicar un número
/// equivocado en el sitio de una inmobiliaria no es un error de cálculo: es una
/// promesa que después hay que sostener frente a un cliente.
///
/// Por eso nace apagada. Se enciende recién cuando los valores están confirmados.
/// </summary>
public class OpcionesEscritura
{
    public const string Seccion = "Escritura";

    /// <summary>
    /// En false la calculadora no existe para el visitante: la página redirige
    /// y no aparece ningún enlace hacia ella. Es lo que corresponde mientras los
    /// porcentajes no estén confirmados por la inmobiliaria.
    /// </summary>
    public bool Habilitada { get; set; }

    /// <summary>
    /// Cuándo se revisaron por última vez estos valores, como texto ("agosto de
    /// 2026"). Se muestra al pie: una estimación sin fecha envejece sin avisar.
    /// </summary>
    public string Vigencia { get; set; } = "";

    /// <summary>Aclaración que se muestra debajo del resultado, además de la advertencia fija.</summary>
    public string Nota { get; set; } = "";

    /// <summary>Los conceptos que se suman. Sin conceptos cargados, no hay nada que calcular.</summary>
    public List<ConceptoEscritura> Conceptos { get; set; } = new();

    /// <summary>True si además de estar encendida tiene con qué calcular.</summary>
    public bool Lista => Habilitada && Conceptos.Count > 0;
}

/// <summary>Un gasto de la escritura: cuánto es, quién lo paga y por qué.</summary>
public class ConceptoEscritura
{
    public string Nombre { get; set; } = "";

    /// <summary>Porcentaje sobre el precio de la operación. Cero si es un importe fijo.</summary>
    public decimal Porcentaje { get; set; }

    /// <summary>
    /// Importe fijo, en la misma moneda que el precio, para los gastos que no
    /// dependen del valor de la propiedad (certificaciones, informes, sellados).
    /// </summary>
    public decimal Fijo { get; set; }

    /// <summary>"Comprador", "Vendedor" o "Ambos": los gastos no los paga siempre el mismo.</summary>
    public string Paga { get; set; } = "Comprador";

    /// <summary>Explicación breve de qué es el concepto, para quien nunca escrituró.</summary>
    public string Detalle { get; set; } = "";

    /// <summary>Cuánto sale este concepto para una operación de este precio.</summary>
    public decimal Calcular(decimal precio) => Math.Round(precio * Porcentaje / 100m + Fijo, 2);

    /// <summary>Cómo se explica el cálculo al lado del importe.</summary>
    public string FormulaTexto
    {
        get
        {
            var partes = new List<string>();

            if (Porcentaje > 0)
            {
                partes.Add($"{Porcentaje.ToString("0.##")} %");
            }

            if (Fijo > 0)
            {
                partes.Add($"+ {Fijo.ToString("N0")} fijos");
            }

            return string.Join(" ", partes);
        }
    }
}
