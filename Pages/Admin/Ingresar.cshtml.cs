using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages.Admin;

public class IngresarModel : PageModel
{
    private readonly UsuariosService _usuarios;

    public IngresarModel(UsuariosService usuarios) => _usuarios = usuarios;

    public class Credenciales
    {
        [Required(ErrorMessage = "Escribí tu correo.")]
        [EmailAddress(ErrorMessage = "Revisá el correo.")]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Escribí tu contraseña.")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Clave { get; set; } = "";
    }

    [BindProperty]
    public Credenciales Datos { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public string? Error { get; private set; }

    public IActionResult OnGet()
    {
        // Si ya hay sesión abierta no tiene sentido volver a pedir los datos.
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToPage("/Admin/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var usuario = await _usuarios.AutenticarAsync(Datos.Email, Datos.Clave);

        if (usuario is null)
        {
            // Un único mensaje para cualquier motivo: no se revela si el correo existe.
            Error = "El correo o la contraseña no son correctos.";
            return Page();
        }

        var identidad = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email)
            },
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identidad),
            new AuthenticationProperties { IsPersistent = false });

        if (usuario.DebeCambiarClave)
        {
            return RedirectToPage("/Admin/Cuenta", new { primera = true });
        }

        // Sólo se acepta volver a una ruta interna: evita el salto a otro sitio.
        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Admin/Index");
    }
}
