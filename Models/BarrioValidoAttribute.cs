using System.ComponentModel.DataAnnotations;

namespace Enricci_Propiedades.Models;

/// <summary>
/// Exige que el barrio sea uno de la lista de <see cref="BarriosDeBuenosAires"/>.
///
/// La validación va del lado del servidor y no sólo en el desplegable: el
/// formulario se puede enviar sin pasar por el navegador, y un barrio inventado
/// se traduce en una página por barrio que no le sirve a nadie y en datos
/// estructurados que declaran un lugar que no existe.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class BarrioValidoAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? valor, ValidationContext contexto)
    {
        var barrio = valor as string;

        // Que sea obligatorio lo dice [Required]; acá sólo interesa si el valor
        // que vino está en la lista.
        if (string.IsNullOrWhiteSpace(barrio) || BarriosDeBuenosAires.EsValido(barrio))
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            $"«{barrio}» no está en la lista de barrios y partidos. Elegí uno del desplegable.",
            new[] { contexto.MemberName ?? nameof(Propiedad.Barrio) });
    }
}
