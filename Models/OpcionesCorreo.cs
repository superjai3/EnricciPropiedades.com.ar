namespace Enricci_Propiedades.Models;

/// <summary>
/// Configuración del envío de correo. Se lee de la sección "Correo" de
/// appsettings.json (o de variables de entorno / user-secrets, que es donde
/// debe ir la contraseña).
/// </summary>
public class OpcionesCorreo
{
    public const string Seccion = "Correo";

    /// <summary>Si está en false, las consultas solo se registran en el log.</summary>
    public bool Habilitado { get; set; }

    public string Servidor { get; set; } = "";
    public int Puerto { get; set; } = 587;
    public bool UsarSsl { get; set; } = true;
    public string Usuario { get; set; } = "";
    public string Clave { get; set; } = "";

    /// <summary>Casilla desde la que sale el aviso (suele ser la misma que Usuario).</summary>
    public string Remitente { get; set; } = "";

    /// <summary>Casilla que recibe las consultas de la web.</summary>
    public string Destinatario { get; set; } = SitioInfo.Email;

    public bool EstaConfigurado =>
        Habilitado &&
        !string.IsNullOrWhiteSpace(Servidor) &&
        !string.IsNullOrWhiteSpace(Remitente) &&
        !string.IsNullOrWhiteSpace(Destinatario);
}
