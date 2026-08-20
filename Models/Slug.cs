using System.Globalization;
using System.Text;

namespace Enricci_Propiedades.Models;

/// <summary>
/// Convierte un nombre en un trozo de URL legible: "Constitución" → "constitucion".
///
/// Hace falta porque las páginas por barrio viven en /propiedades/{barrio}. Una
/// URL con tildes o espacios se puede escribir, pero viaja escapada
/// (%C3%B3), se ve mal cuando alguien la comparte y los buscadores prefieren la
/// forma limpia.
/// </summary>
public static class Slug
{
    public static string De(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto))
        {
            return "";
        }

        // FormD separa la letra de su tilde y así se puede descartar la tilde
        // sola, en vez de tener que listar cada vocal acentuada a mano.
        var descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var salida = new StringBuilder(descompuesto.Length);
        var guionPendiente = false;

        foreach (var caracter in descompuesto)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(caracter) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(caracter) && caracter < 128)
            {
                if (guionPendiente && salida.Length > 0)
                {
                    salida.Append('-');
                }

                salida.Append(caracter);
                guionPendiente = false;
            }
            else
            {
                // El guión se agrega recién cuando venga otra letra: así no
                // quedan guiones al final ni dos seguidos.
                guionPendiente = true;
            }
        }

        return salida.ToString();
    }

    /// <summary>
    /// Busca en <paramref name="candidatos"/> el que corresponde a un slug. Se
    /// compara por slug y no por nombre para que /propiedades/constitucion
    /// encuentre "Constitución".
    /// </summary>
    public static string? Resolver(IEnumerable<string> candidatos, string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var buscado = De(slug);

        return candidatos.FirstOrDefault(c => De(c) == buscado);
    }
}
