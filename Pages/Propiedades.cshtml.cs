using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Enricci_Propiedades.Pages;

public class PropiedadesModel : PageModel
{
    private readonly PropiedadesService _propiedades;

    public PropiedadesModel(PropiedadesService propiedades) => _propiedades = propiedades;

    /// <summary>Búsqueda libre: dirección, barrio, tipo o cualquier palabra del aviso.</summary>
    [BindProperty(SupportsGet = true)]
    public string? Texto { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Operacion { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Tipo { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Barrio { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Ambientes { get; set; }

    [BindProperty(SupportsGet = true)]
    public decimal? Precio { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Orden { get; set; }

    public IReadOnlyList<Propiedad> Resultados { get; private set; } = Array.Empty<Propiedad>();
    public IReadOnlyList<string> Barrios { get; private set; } = Array.Empty<string>();

    public bool HayFiltros =>
        !string.IsNullOrWhiteSpace(Texto) ||
        !string.IsNullOrWhiteSpace(Operacion) ||
        !string.IsNullOrWhiteSpace(Tipo) ||
        !string.IsNullOrWhiteSpace(Barrio) ||
        Ambientes is > 0 ||
        Precio is > 0;

    public string Encabezado => Operacion switch
    {
        "Venta" => "Propiedades en venta",
        "Alquiler" => "Propiedades en alquiler",
        "AlquilerTemporario" => "Alquiler temporario",
        _ => Tipo switch
        {
            "LocalComercial" => "Locales comerciales",
            "FondoDeComercio" => "Fondos de comercio",
            "Cochera" => "Cocheras",
            "Oficina" => "Oficinas",
            _ => "Todas las propiedades"
        }
    };

    public void OnGet()
    {
        Barrios = _propiedades.Barrios.ToList();
        Resultados = _propiedades
            .Buscar(
                texto: Texto,
                operacion: Operacion,
                tipo: Tipo,
                barrio: Barrio,
                ambientesMinimos: Ambientes,
                precioMaximo: Precio,
                orden: Orden)
            .ToList();
    }
}
