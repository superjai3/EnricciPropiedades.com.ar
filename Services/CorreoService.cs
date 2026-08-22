using System.Net;
using System.Net.Mail;
using Enricci_Propiedades.Models;
using Microsoft.Extensions.Options;

namespace Enricci_Propiedades.Services;

/// <summary>
/// Envía por correo las consultas que llegan desde los formularios.
/// Si el envío no está configurado, la consulta igual queda registrada en el
/// log y la página le ofrece al visitante mandarla por WhatsApp o correo.
/// </summary>
public class CorreoService
{
    private readonly OpcionesCorreo _opciones;
    private readonly ILogger<CorreoService> _log;

    public CorreoService(IOptions<OpcionesCorreo> opciones, ILogger<CorreoService> log)
    {
        _opciones = opciones.Value;
        _log = log;
    }

    public bool Disponible => _opciones.EstaConfigurado;

    /// <summary>
    /// Avisa a la inmobiliaria. Devuelve true solo si el correo salió
    /// efectivamente. Nunca lanza: un problema con el servidor de correo no
    /// puede tumbar el formulario.
    /// </summary>
    public Task<bool> EnviarAsync(
        string asunto,
        string cuerpo,
        string? responderA = null,
        CancellationToken cancelacion = default) =>
        EnviarAAsync(_opciones.Destinatario, asunto, cuerpo, responderA, cancelacion);

    /// <summary>
    /// Le escribe a alguien de afuera: la confirmación de un alta en las alertas
    /// o el aviso de una propiedad nueva. Va aparte del envío a la oficina
    /// porque el destinatario no es el mismo y equivocarse acá significa mandarle
    /// a un visitante algo que no era para él.
    /// </summary>
    public async Task<bool> EnviarAAsync(
        string destinatario,
        string asunto,
        string cuerpo,
        string? responderA = null,
        CancellationToken cancelacion = default)
    {
        if (string.IsNullOrWhiteSpace(destinatario))
        {
            return false;
        }

        if (!_opciones.EstaConfigurado)
        {
            if (_opciones.Habilitado)
            {
                _log.LogWarning(
                    "El envío de correo está habilitado pero incompleto: faltan Servidor, Remitente o Destinatario.");
            }

            return false;
        }

        try
        {
            using var mensaje = new MailMessage
            {
                From = new MailAddress(_opciones.Remitente, SitioInfo.Nombre),
                Subject = asunto,
                Body = cuerpo,
                IsBodyHtml = false
            };
            mensaje.To.Add(destinatario);

            if (!string.IsNullOrWhiteSpace(responderA) && MailAddress.TryCreate(responderA, out var direccion))
            {
                mensaje.ReplyToList.Add(direccion);
            }

            using var cliente = new SmtpClient(_opciones.Servidor, _opciones.Puerto)
            {
                EnableSsl = _opciones.UsarSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 15000
            };

            if (!string.IsNullOrWhiteSpace(_opciones.Usuario))
            {
                cliente.Credentials = new NetworkCredential(_opciones.Usuario, _opciones.Clave);
            }

            await cliente.SendMailAsync(mensaje, cancelacion);
            _log.LogInformation("Correo enviado a {Destinatario}: {Asunto}", destinatario, asunto);
            return true;
        }
        catch (Exception ex)
        {
            // Quien llama decide qué hacer: las consultas ya quedaron guardadas
            // en la base y las alertas se reintentan en la pasada siguiente.
            _log.LogError(ex, "No se pudo enviar el correo a {Destinatario}.", destinatario);
            return false;
        }
    }
}
