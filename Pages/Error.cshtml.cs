using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages;

[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? Codigo { get; set; }

    public string Titulo { get; private set; } = "Algo salió mal";
    public string Detalle { get; private set; } =
        "Tuvimos un problema al procesar la página. Podés volver al inicio o escribirnos y lo resolvemos.";

    public void OnGet()
    {
        if (Codigo == 404)
        {
            Titulo = "No encontramos esta página";
            Detalle = "Puede que la publicación haya sido dada de baja o que el enlace esté incompleto. " +
                      "Probá con el catálogo de propiedades o escribinos y te ayudamos.";
        }
        else if (Codigo == 403)
        {
            Titulo = "Esta sección no está disponible";
            Detalle = "No tenés permiso para ver esta página.";
        }
    }
}
