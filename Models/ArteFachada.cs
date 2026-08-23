using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Html;

namespace Enricci_Propiedades.Models;

/// <summary>
/// Genera una portada vectorial determinística para las publicaciones que
/// todavía no tienen fotografía cargada. Evita el clásico "imagen rota" y
/// mantiene la identidad visual del sitio.
/// </summary>
public static class ArteFachada
{
    private static readonly string[][] Paletas =
    {
        new[] { "#F5333F", "#FFD9DB", "#241F24", "#FFF4F4" },
        new[] { "#1F3A5F", "#CBD9EC", "#141A24", "#EEF3FA" },
        new[] { "#B4700A", "#F3E0C0", "#231A0E", "#FBF4E8" },
        new[] { "#12A150", "#CDEBD9", "#0F1F16", "#EEF8F2" }
    };

    public static IHtmlContent Dibujar(Propiedad propiedad, string clase = "", int variante = 0)
    {
        var semilla = ((uint)propiedad.Id * 2654435761u) + ((uint)variante * 40503u);
        var paleta = Paletas[(int)(semilla % (uint)Paletas.Length)];
        var acento = paleta[0];
        var claro = paleta[1];
        var oscuro = paleta[2];
        var fondo = paleta[3];
        var gradiente = $"fachada-{propiedad.Id}-{variante}";

        var svg = new StringBuilder();
        svg.Append(CultureInfo.InvariantCulture,
            $"<svg class=\"{clase}\" viewBox=\"0 0 400 300\" role=\"img\" aria-label=\"Ilustración de fachada de {Escapar(propiedad.Direccion)}\" preserveAspectRatio=\"xMidYMid slice\" xmlns=\"http://www.w3.org/2000/svg\">");
        svg.Append(CultureInfo.InvariantCulture,
            $"<defs><linearGradient id=\"{gradiente}\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\">" +
            $"<stop offset=\"0\" stop-color=\"{fondo}\"/><stop offset=\"1\" stop-color=\"{claro}\"/></linearGradient></defs>");
        svg.Append(CultureInfo.InvariantCulture, $"<rect width=\"400\" height=\"300\" fill=\"url(#{gradiente})\"/>");
        svg.Append(CultureInfo.InvariantCulture,
            $"<circle cx=\"{312 + (semilla % 40)}\" cy=\"{58 + (semilla % 24)}\" r=\"46\" fill=\"{acento}\" opacity=\"0.16\"/>");

        // Torres: alturas y anchos derivados de la semilla, siempre dentro del lienzo.
        var x = 26;
        var torre = 0;
        while (x < 372 && torre < 5)
        {
            var mezcla = (semilla >> (torre * 5)) % 97;
            var ancho = 42 + (int)(mezcla % 34);
            if (x + ancho > 374) { ancho = 374 - x; }
            if (ancho < 30) { break; }

            var alto = 96 + (int)((mezcla * 7) % 132);
            var y = 262 - alto;
            var relleno = torre % 2 == 0 ? oscuro : acento;
            var opacidad = torre % 2 == 0 ? "0.92" : "0.85";

            svg.Append(CultureInfo.InvariantCulture,
                $"<rect x=\"{x}\" y=\"{y}\" width=\"{ancho}\" height=\"{alto}\" rx=\"4\" fill=\"{relleno}\" opacity=\"{opacidad}\"/>");

            // Ventanas
            var columnas = Math.Max(2, (ancho - 12) / 14);
            var filas = Math.Max(3, (alto - 18) / 18);
            for (var f = 0; f < filas; f++)
            {
                for (var c = 0; c < columnas; c++)
                {
                    var vx = x + 8 + (c * 14);
                    var vy = y + 12 + (f * 18);
                    if (vx + 8 > x + ancho - 4 || vy + 10 > 262 - 6) { continue; }
                    var encendida = ((semilla >> ((f + c + torre) % 24)) & 3u) == 0;
                    var color = encendida ? "#FFE9A8" : fondo;
                    svg.Append(CultureInfo.InvariantCulture,
                        $"<rect x=\"{vx}\" y=\"{vy}\" width=\"8\" height=\"10\" rx=\"1.5\" fill=\"{color}\" opacity=\"0.85\"/>");
                }
            }

            x += ancho + 8;
            torre++;
        }

        svg.Append(CultureInfo.InvariantCulture,
            $"<rect x=\"0\" y=\"262\" width=\"400\" height=\"38\" fill=\"{oscuro}\" opacity=\"0.12\"/>");
        svg.Append(CultureInfo.InvariantCulture,
            $"<rect x=\"0\" y=\"262\" width=\"400\" height=\"3\" fill=\"{acento}\"/>");
        svg.Append("</svg>");

        return new HtmlString(svg.ToString());
    }

    private static string Escapar(string texto) =>
        texto.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
}
