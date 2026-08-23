using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages;

public class ServiciosModel : PageModel
{
    /// <summary>
    /// Las preguntas frecuentes, como datos y no escritas en la vista.
    ///
    /// De acá salen las dos cosas: el acordeón que se ve y el bloque FAQPage que
    /// leen los buscadores y los asistentes con IA. Si estuvieran escritas dos
    /// veces se separarían al primer cambio de texto, y declarar una respuesta
    /// distinta de la que se muestra es motivo de penalización.
    ///
    /// Son además el contenido más citable del sitio: una respuesta corta y
    /// completa a una pregunta concreta es exactamente lo que un asistente toma
    /// para contestar «¿cuánto cobra de comisión una inmobiliaria en CABA?».
    /// </summary>
    public static readonly (string Pregunta, string Respuesta)[] Preguntas =
    {
        ("¿Cuánto cobran de comisión?",
         "Depende de la operación y del tipo de propiedad. Los honorarios se informan por " +
         "escrito antes de firmar cualquier autorización, junto con el detalle de sellos, " +
         "gastos de escrituración y certificaciones. Nunca vas a encontrarte con un costo " +
         "que no te hayamos anticipado."),

        ("¿La tasación tiene costo?",
         "No. La visita y el informe de valor son sin cargo y no te obligan a darnos la venta " +
         "ni a firmar exclusividad. Si necesitás una tasación formal para una sucesión o un " +
         "juicio, te avisamos porque ese trámite sí tiene un arancel específico."),

        ("¿Qué garantías aceptan para alquilar?",
         "Garantía propietaria en CABA o GBA, recibo de sueldo con antigüedad comprobable, o " +
         "seguro de caución de compañías habilitadas. Si no tenés garantía, escribinos igual: " +
         "en la mayoría de los casos hay solución."),

        ("¿Trabajan con propiedades fuera de esos barrios?",
         "Nuestra zona de trabajo diaria es el sur porteño, donde conocemos el valor cuadra " +
         "por cuadra. Fuera de ese radio evaluamos caso por caso, y si no somos los indicados " +
         "te lo decimos de entrada."),

        ("¿Cuánto tarda una venta?",
         "Con el precio bien puesto y los papeles en orden, la mayoría de las operaciones de " +
         "la zona se cierran entre 60 y 120 días. El informe de tasación incluye una " +
         "estimación realista para tu caso puntual.")
    };

    public string DatosEstructuradosJson { get; private set; } = "";

    public void OnGet() =>
        DatosEstructuradosJson = DatosEstructurados.PreguntasFrecuentes(Preguntas);
}
