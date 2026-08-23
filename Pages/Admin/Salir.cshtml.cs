using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages.Admin;

public class SalirModel : PageModel
{
    /// <summary>Cerrar sesión sólo por POST: un enlace ajeno no puede desconectar a Horacio.</summary>
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Admin/Ingresar");
    }

    public IActionResult OnGet() => RedirectToPage("/Admin/Index");
}
