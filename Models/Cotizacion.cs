namespace Enricci_Propiedades.Models;

/// <summary>
/// Un tipo de cambio ya obtenido, con la fecha que informó la fuente. La fecha
/// se muestra siempre: un valor de referencia sin fecha no sirve para decidir.
/// </summary>
public record Cotizacion(decimal Compra, decimal Venta, DateTimeOffset Actualizado)
{
    /// <summary>El valor con el que se convierten los importes en dólares.</summary>
    public decimal Valor(string punta) =>
        punta.Equals("compra", StringComparison.OrdinalIgnoreCase) ? Compra : Venta;

    /// <summary>"21/08/2026", en hora de Buenos Aires.</summary>
    public string FechaTexto
    {
        get
        {
            try
            {
                var zona = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
                return TimeZoneInfo.ConvertTime(Actualizado, zona).ToString("dd/MM/yyyy");
            }
            catch (TimeZoneNotFoundException)
            {
                // Un contenedor sin la base de zonas horarias no puede tumbar la página.
                return Actualizado.ToString("dd/MM/yyyy");
            }
        }
    }
}
