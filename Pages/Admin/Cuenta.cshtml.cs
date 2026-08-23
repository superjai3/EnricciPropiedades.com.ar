using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages.Admin;

public class CuentaModel : PageModel
{
    private readonly UsuariosService _usuarios;

    public CuentaModel(UsuariosService usuarios) => _usuarios = usuarios;

    public class CambioDeClave
    {
        [Required(ErrorMessage = "Escribí tu contraseña actual.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña actual")]
        public string Actual { get; set; } = "";

        [Required(ErrorMessage = "Escribí la contraseña nueva.")]
        [StringLength(72, MinimumLength = 10, ErrorMessage = "La contraseña nueva necesita al menos 10 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña nueva")]
        public string Nueva { get; set; } = "";

        [Required(ErrorMessage = "Repetí la contraseña nueva.")]
        [Compare(nameof(Nueva), ErrorMessage = "Las dos contraseñas no coinciden.")]
        [DataType(DataType.Password)]
        [Display(Name = "Repetir la contraseña nueva")]
        public string Repetida { get; set; } = "";
    }

    [BindProperty]
    public CambioDeClave Datos { get; set; } = new();

    /// <summary>Llega en true justo después del primer ingreso, con la clave provisoria.</summary>
    [BindProperty(SupportsGet = true)]
    public bool Primera { get; set; }

    public Usuario? Usuario { get; private set; }
    public string? Error { get; private set; }

    [TempData]
    public string? Mensaje { get; set; }

    private int UsuarioId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public async Task<IActionResult> OnGetAsync()
    {
        Usuario = await _usuarios.PorIdAsync(UsuarioId);
        return Usuario is null ? RedirectToPage("/Admin/Salir") : Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Usuario = await _usuarios.PorIdAsync(UsuarioId);

        if (Usuario is null)
        {
            return RedirectToPage("/Admin/Salir");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Datos.Actual == Datos.Nueva)
        {
            Error = "La contraseña nueva tiene que ser distinta de la actual.";
            return Page();
        }

        var cambiada = await _usuarios.CambiarClaveAsync(UsuarioId, Datos.Actual, Datos.Nueva);

        if (!cambiada)
        {
            Error = "La contraseña actual no es correcta.";
            return Page();
        }

        Mensaje = "Listo, tu contraseña quedó actualizada.";
        return RedirectToPage("/Admin/Index");
    }
}
