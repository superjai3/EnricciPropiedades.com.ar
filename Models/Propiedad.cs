using System.ComponentModel.DataAnnotations;

namespace Enricci_Propiedades.Models;

public enum Operacion
{
    Venta,
    Alquiler,
    AlquilerTemporario
}

public enum TipoPropiedad
{
    Departamento,
    Casa,
    PH,
    LocalComercial,
    Oficina,
    FondoDeComercio,
    Cochera,
    Terreno
}

public enum EstadoPublicacion
{
    Disponible,
    Reservada,
    Vendida
}

public class Propiedad
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Poné un título para la publicación.")]
    [StringLength(140, MinimumLength = 8, ErrorMessage = "El título va entre 8 y 140 caracteres.")]
    [Display(Name = "Título")]
    public string Titulo { get; set; } = "";

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [StringLength(120)]
    [Display(Name = "Dirección")]
    public string Direccion { get; set; } = "";

    [Required(ErrorMessage = "Indicá el barrio.")]
    [StringLength(60)]
    public string Barrio { get; set; } = "";

    public Operacion Operacion { get; set; }
    public TipoPropiedad Tipo { get; set; }
    public EstadoPublicacion Estado { get; set; } = EstadoPublicacion.Disponible;

    /// <summary>Moneda de publicación: "USD" para venta, "ARS" para alquiler.</summary>
    [Required]
    [StringLength(3)]
    public string Moneda { get; set; } = "USD";

    [Range(0, 99_999_999, ErrorMessage = "Revisá el precio. Dejalo en 0 para publicar \"Consultar\".")]
    public decimal Precio { get; set; }

    [Range(0, 99_999_999, ErrorMessage = "Revisá el monto de expensas.")]
    public decimal Expensas { get; set; }

    [Range(0, 40)] public int Ambientes { get; set; }
    [Range(0, 40)] public int Dormitorios { get; set; }
    [Range(0, 40)] public int Banios { get; set; }

    [Range(0, 100_000)]
    [Display(Name = "Superficie cubierta")]
    public int SuperficieCubierta { get; set; }

    [Range(0, 100_000)]
    [Display(Name = "Superficie total")]
    public int SuperficieTotal { get; set; }

    [Range(0, 300)] public int Antiguedad { get; set; }

    public bool Cochera { get; set; }
    public bool Balcon { get; set; }

    [Display(Name = "Apto crédito")]
    public bool AptoCredito { get; set; }

    public bool Destacada { get; set; }

    [Required(ErrorMessage = "Escribí una descripción de la propiedad.")]
    [StringLength(4000, MinimumLength = 40, ErrorMessage = "La descripción necesita al menos 40 caracteres.")]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = "";

    public List<string> Comodidades { get; set; } = new();

    /// <summary>Rutas relativas a wwwroot. Si está vacío se dibuja una portada generada.</summary>
    public List<string> Fotos { get; set; } = new();

    public DateTime FechaAlta { get; set; } = DateTime.UtcNow;
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

    public string Slug => $"{Id}";

    public string TipoTexto => Tipo switch
    {
        TipoPropiedad.PH => "PH",
        TipoPropiedad.LocalComercial => "Local comercial",
        TipoPropiedad.FondoDeComercio => "Fondo de comercio",
        _ => Tipo.ToString()
    };

    public string OperacionTexto => Operacion switch
    {
        Operacion.AlquilerTemporario => "Alquiler temporario",
        _ => Operacion.ToString()
    };

    public string EstadoTexto => Estado switch
    {
        EstadoPublicacion.Vendida => Operacion == Operacion.Venta ? "Vendida" : "Alquilada",
        _ => Estado.ToString()
    };

    public string PrecioTexto => Precio <= 0
        ? "Consultar"
        : Moneda == "USD"
            ? $"USD {Precio:N0}"
            : $"$ {Precio:N0}";

    public string PrecioSufijo => Precio <= 0
        ? ""
        : Operacion == Operacion.Venta ? "" : "por mes";

    public string ResumenSuperficie => SuperficieTotal > 0
        ? $"{SuperficieTotal} m² totales"
        : $"{SuperficieCubierta} m² cubiertos";

    /// <summary>Visible en el sitio público: las vendidas se dan de baja del catálogo.</summary>
    public bool EstaPublicada => Estado != EstadoPublicacion.Vendida;
}
