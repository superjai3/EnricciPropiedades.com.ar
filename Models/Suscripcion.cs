using System.ComponentModel.DataAnnotations;

namespace Enricci_Propiedades.Models;

/// <summary>
/// Alguien que pidió que le avisemos cuando entre una propiedad que le sirva.
///
/// Dos decisiones que valen la pena explicar:
///
/// El alta no vale hasta que la persona confirma desde su casilla. Cualquiera
/// puede escribir la dirección de otro en un formulario; mandarle avisos a quien
/// no los pidió es spam, y el costo no lo paga quien lo escribió sino la
/// inmobiliaria, que termina en la carpeta de correo no deseado.
///
/// Cada suscripción recuerda hasta qué publicación ya avisó. Sin eso, un
/// reinicio del servicio o dos pasadas seguidas mandarían el mismo aviso otra
/// vez, y nada hace que alguien se dé de baja más rápido.
/// </summary>
public class Suscripcion
{
    public int Id { get; set; }

    [Required]
    [StringLength(160)]
    public string Email { get; set; } = "";

    /// <summary>Qué operación le interesa. Null es "las dos".</summary>
    public Operacion? Operacion { get; set; }

    /// <summary>Barrio que le interesa. Vacío es "cualquiera".</summary>
    [StringLength(60)]
    public string? Barrio { get; set; }

    /// <summary>Tope de precio en dólares. Cero es "sin tope".</summary>
    public decimal PrecioMaximo { get; set; }

    /// <summary>
    /// Secreto que viaja en los enlaces de confirmación y de baja. Es lo único
    /// que hace falta para darse de baja: pedirle a alguien que inicie sesión
    /// para dejar de recibir correos es una forma de retenerlo a la fuerza.
    /// </summary>
    [Required]
    [StringLength(64)]
    public string Token { get; set; } = "";

    public bool Confirmada { get; set; }

    /// <summary>False cuando la persona se dio de baja. No se borra la fila: si
    /// vuelve a anotarse con la misma dirección, se reutiliza.</summary>
    public bool Activa { get; set; } = true;

    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public DateTime? FechaConfirmada { get; set; }
    public DateTime? FechaBaja { get; set; }

    /// <summary>
    /// Id de la última publicación por la que ya se avisó. Al confirmar se pone
    /// en la más nueva del momento: quien se suscribe hoy quiere enterarse de lo
    /// que entre a partir de hoy, no de todo el catálogo de golpe.
    /// </summary>
    public int UltimaPropiedadAvisada { get; set; }

    public DateTime? FechaUltimoAviso { get; set; }

    /// <summary>Cuántos avisos se le mandaron. Sirve para ver si la cosa funciona.</summary>
    public int AvisosEnviados { get; set; }

    /// <summary>Sólo estas reciben avisos.</summary>
    public bool RecibeAvisos => Confirmada && Activa;

    /// <summary>Cómo se describe lo que pidió, para el panel y para el correo.</summary>
    public string CriterioTexto
    {
        get
        {
            var partes = new List<string>
            {
                Operacion switch
                {
                    Models.Operacion.Venta => "en venta",
                    Models.Operacion.Alquiler => "en alquiler",
                    Models.Operacion.AlquilerTemporario => "en alquiler temporario",
                    _ => "en venta o alquiler"
                }
            };

            if (!string.IsNullOrWhiteSpace(Barrio))
            {
                partes.Add($"en {Barrio}");
            }

            if (PrecioMaximo > 0)
            {
                partes.Add($"hasta USD {PrecioMaximo:N0}");
            }

            return string.Join(", ", partes);
        }
    }

    /// <summary>True si esta publicación es de las que la persona pidió.</summary>
    public bool LeSirve(Propiedad propiedad)
    {
        // Una propiedad vendida o reservada no es una novedad para nadie.
        if (propiedad.Estado != EstadoPublicacion.Disponible)
        {
            return false;
        }

        if (Operacion.HasValue && propiedad.Operacion != Operacion.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(Barrio) &&
            !string.Equals(Slug.De(propiedad.Barrio), Slug.De(Barrio), StringComparison.Ordinal))
        {
            return false;
        }

        if (PrecioMaximo > 0)
        {
            // Precio en cero es "Consultar": no se puede afirmar que esté por
            // encima del tope, y dejarlo pasar es preferible a esconderlo.
            if (propiedad.Precio > 0 &&
                propiedad.Moneda == "USD" &&
                propiedad.Precio > PrecioMaximo)
            {
                return false;
            }
        }

        return true;
    }
}
