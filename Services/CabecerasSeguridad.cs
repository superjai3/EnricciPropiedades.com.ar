using System.Security.Cryptography;

namespace Enricci_Propiedades.Services;

/// <summary>
/// Cabeceras de seguridad para todas las respuestas, incluida una política de
/// contenido (CSP) estricta. Los pocos bloques de script en línea del sitio se
/// autorizan con un nonce distinto en cada pedido, así que un texto inyectado
/// por un tercero no se ejecuta aunque llegue a la página.
/// </summary>
public static class CabecerasSeguridad
{
    private const string ClaveNonce = "csp-nonce";

    /// <summary>Nonce del pedido en curso, para los &lt;script&gt; en línea de las vistas.</summary>
    public static string Nonce(this HttpContext contexto) =>
        contexto.Items[ClaveNonce] as string ?? "";

    public static IApplicationBuilder UseCabecerasSeguridad(this WebApplication app)
    {
        var enProduccion = !app.Environment.IsDevelopment();

        return app.Use(async (contexto, siguiente) =>
        {
            // Hexadecimal y no Base64: el "+" y el "/" de Base64 salen escapados
            // como &#x2B; dentro del atributo HTML, lo que hace que la marca y la
            // cabecera no se lean iguales. Con hexadecimal el valor viaja tal cual.
            var nonce = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
            contexto.Items[ClaveNonce] = nonce;

            var cabeceras = contexto.Response.Headers;

            // El navegador respeta el Content-Type declarado y no adivina otro.
            cabeceras["X-Content-Type-Options"] = "nosniff";

            // No se filtra la ruta completa al salir del sitio.
            cabeceras["Referrer-Policy"] = "strict-origin-when-cross-origin";

            // El sitio no se puede embeber en un marco ajeno (clickjacking).
            cabeceras["X-Frame-Options"] = "DENY";

            // No hace falta ninguna de estas capacidades del dispositivo.
            cabeceras["Permissions-Policy"] =
                "camera=(), microphone=(), geolocation=(), payment=(), usb=(), interest-cohort=()";

            var politica = new List<string>
            {
                "default-src 'self'",
                "base-uri 'self'",
                "object-src 'none'",
                "frame-ancestors 'none'",
                "form-action 'self'",
                "img-src 'self' data:",
                "font-src 'self'",
                "connect-src 'self'",
                // Los estilos en línea son atributos style de las vistas: no
                // ejecutan código, y el riesgo es muy menor al de un script.
                "style-src 'self' 'unsafe-inline'",
                $"script-src 'self' 'nonce-{nonce}'"
            };

            if (enProduccion)
            {
                politica.Add("upgrade-insecure-requests");
            }

            cabeceras["Content-Security-Policy"] = string.Join("; ", politica);

            await siguiente();
        });
    }
}
