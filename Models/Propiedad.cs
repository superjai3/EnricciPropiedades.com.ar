using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

    [Required(ErrorMessage = "Elegí el barrio o partido.")]
    [StringLength(60)]
    [BarrioValido]
    [Display(Name = "Barrio o partido")]
    public string Barrio { get; set; } = "";

    /// <summary>
    /// Provincia que le corresponde al barrio. No se guarda: se deduce de la
    /// lista de barrios, porque un dato que se puede derivar y además se guarda
    /// termina, tarde o temprano, diciendo algo distinto del que lo origina.
    /// </summary>
    [NotMapped]
    public string Region => BarriosDeBuenosAires.RegionDe(Barrio);

    /// <summary>Siempre Argentina: el sitio publica en la Ciudad y el Gran Buenos Aires.</summary>
    [NotMapped]
    public string Pais => BarriosDeBuenosAires.Pais;

    /// <summary>True si el barrio guardado no está en la lista de zonas válidas.</summary>
    [NotMapped]
    public bool BarrioFueraDeLista => !BarriosDeBuenosAires.EsValido(Barrio);

    /// <summary>"Monserrat, Ciudad Autónoma de Buenos Aires".</summary>
    [NotMapped]
    public string UbicacionTexto => $"{Barrio}, {Region}";

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

    /// <summary>
    /// "A estrenar" cuando no tiene años cargados, que es lo que diría un aviso
    /// de verdad. "0 años" no lo escribe nadie.
    /// </summary>
    public string AntiguedadTexto => Antiguedad <= 0 ? "A estrenar" : $"{Antiguedad} años";

    /// <summary>
    /// Resumen para la descripción que ven los buscadores, armado sólo con los
    /// datos que están cargados. Sin esto, una cochera aparece en Google
    /// descrita como "0 ambientes, 0 m²".
    /// </summary>
    public string ResumenParaBuscadores
    {
        get
        {
            var partes = new List<string>();

            if (Ambientes > 0) { partes.Add($"{Ambientes} ambientes"); }
            if (Dormitorios > 0) { partes.Add($"{Dormitorios} dormitorios"); }
            if (Banios > 0) { partes.Add(Banios == 1 ? "1 baño" : $"{Banios} baños"); }

            if (SuperficieTotal > 0) { partes.Add($"{SuperficieTotal} m²"); }
            else if (SuperficieCubierta > 0) { partes.Add($"{SuperficieCubierta} m² cubiertos"); }

            return string.Join(", ", partes);
        }
    }
}
