using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace Enricci_Propiedades.Pages;

/// <summary>
/// Estimador de los gastos que se pagan al escriturar, además del precio.
///
/// Es la pregunta que todo el mundo hace en la primera visita y casi nadie
/// contesta en su sitio. Contestarla acá tiene dos efectos: la persona llega a
/// la consulta sabiendo cuánto necesita en total, y la inmobiliaria deja de
/// perder media hora explicando lo mismo cada vez.
///
/// La página existe sólo si los porcentajes están cargados y confirmados: ver
/// <see cref="OpcionesEscritura"/>.
/// </summary>
public class EscrituracionModel : PageModel
{
    private readonly OpcionesEscritura _opciones;
    private readonly PropiedadesService _propiedades;
    private readonly CotizacionService _cotizacion;

    public EscrituracionModel(
        IOptions<OpcionesEscritura> opciones,
        PropiedadesService propiedades,
        CotizacionService cotizacion)
    {
        _opciones = opciones.Value;
        _propiedades = propiedades;
        _cotizacion = cotizacion;
    }

    /// <summary>Precio de la operación sobre el que se calcula, en dólares.</summary>
    [BindProperty(SupportsGet = true)]
    public decimal Precio { get; set; }

    /// <summary>Publicación desde la que se llegó, si se llegó desde una ficha.</summary>
    [BindProperty(SupportsGet = true)]
    public int? Propiedad { get; set; }

    public Propiedad? Origen { get; private set; }

    public IReadOnlyList<ConceptoEscritura> Conceptos => _opciones.Conceptos;
    public string Vigencia => _opciones.Vigencia;
    public string Nota => _opciones.Nota;

    /// <summary>Los conceptos ya calculados para el precio pedido.</summary>
    public IReadOnlyList<(ConceptoEscritura Concepto, decimal Importe)> Detalle { get; private set; } =
        Array.Empty<(ConceptoEscritura, decimal)>();

    public decimal TotalComprador { get; private set; }
    public decimal TotalVendedor { get; private set; }
    public decimal Total => TotalComprador + TotalVendedor;

    /// <summary>Precio más los gastos que paga quien compra: lo que hay que tener.</summary>
    public decimal NecesitaElComprador => Precio + TotalComprador;

    /// <summary>Cuánto es en pesos lo que necesita el comprador. Null si no hay cotización.</summary>
    public decimal? EnPesos => _cotizacion.APesos(NecesitaElComprador);

    public string FuenteCotizacion => _cotizacion.Fuente;

    /// <summary>Pesos por dólar, para que el navegador rehaga la cuenta al vuelo. 0 si no hay.</summary>
    public decimal ValorDolar => _cotizacion.Valor ?? 0m;
    public Cotizacion? TipoDeCambio => _cotizacion.Actual;

    /// <summary>Valor con el que se llena el formulario si no vino ninguno.</summary>
    private const decimal PrecioSugerido = 100_000m;

    public IActionResult OnGet()
    {
        // Sin porcentajes confirmados la página no existe. Redirigir en vez de
        // devolver un 404 deja a quien llegue por un enlace viejo en la página
        // de servicios, que es donde iba a terminar preguntando lo mismo.
        if (!_opciones.Lista)
        {
            return RedirectToPage("/Servicios");
        }

        if (Propiedad is > 0)
        {
            Origen = _propiedades.PorId(Propiedad.Value);

            // El precio de la ficha manda sólo si no vino uno escrito a mano:
            // el visitante tiene que poder probar con otro número.
            if (Precio <= 0 && Origen is { Moneda: "USD", Precio: > 0 })
            {
                Precio = Origen.Precio;
            }
        }

        if (Precio <= 0)
        {
            Precio = PrecioSugerido;
        }

        // Un precio disparatado no rompe nada, pero llena la pantalla de ceros
        // y no ayuda a nadie. Se acota en silencio.
        Precio = Math.Clamp(Math.Round(Precio, 0), 1_000m, 50_000_000m);

        Calcular();

        return Page();
    }

    private void Calcular()
    {
        var detalle = _opciones.Conceptos
            .Select(c => (Concepto: c, Importe: c.Calcular(Precio)))
            .ToList();

        Detalle = detalle;

        // "Ambos" se reparte por mitades: es como se pacta habitualmente y es
        // más honesto que cargárselo entero a una de las dos partes.
        TotalComprador = detalle.Sum(d => d.Concepto.Paga switch
        {
            "Vendedor" => 0m,
            "Ambos" => d.Importe / 2m,
            _ => d.Importe
        });

        TotalVendedor = detalle.Sum(d => d.Concepto.Paga switch
        {
            "Vendedor" => d.Importe,
            "Ambos" => d.Importe / 2m,
            _ => 0m
        });

        TotalComprador = Math.Round(TotalComprador, 2);
        TotalVendedor = Math.Round(TotalVendedor, 2);
    }
}
