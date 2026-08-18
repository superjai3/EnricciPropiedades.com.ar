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
    /// Devuelve true solo si el correo salió efectivamente. Nunca lanza: un
    /// problema con el servidor de correo no puede tumbar el formulario.
    /// </summary>
    public async Task<bool> EnviarAsync(
        string asunto,
        string cuerpo,
        string? responderA = null,
        CancellationToken cancelacion = default)
    {
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
            mensaje.To.Add(_opciones.Destinatario);

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
            _log.LogInformation("Consulta enviada por correo a {Destinatario}.", _opciones.Destinatario);
            return true;
        }
        catch (Exception ex)
        {
            // La consulta ya quedó en el log; el visitante recibe el aviso igual.
            _log.LogError(ex, "No se pudo enviar la consulta por correo.");
            return false;
        }
    }
}
