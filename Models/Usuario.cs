using System.ComponentModel.DataAnnotations;

namespace Enricci_Propiedades.Models;

/// <summary>
/// Usuario del panel de administración. La contraseña nunca se guarda: se
/// almacena el hash PBKDF2 junto con la sal que se usó para calcularlo.
/// </summary>
public class Usuario
{
    public int Id { get; set; }

    [Required]
    [StringLength(80)]
    public string Nombre { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(160)]
    public string Email { get; set; } = "";

    [Required]
    [StringLength(200)]
    public string HashClave { get; set; } = "";

    [Required]
    [StringLength(120)]
    public string Sal { get; set; } = "";

    public bool Activo { get; set; } = true;

    /// <summary>
    /// Obliga a cambiar la contraseña al ingresar. Queda en true para el usuario
    /// que se crea en el primer arranque, porque su clave inicial es conocida.
    /// </summary>
    public bool DebeCambiarClave { get; set; }

    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoIngreso { get; set; }
}
