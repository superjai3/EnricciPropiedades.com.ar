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
    public int Id { get; init; }
    public string Titulo { get; init; } = "";
    public string Direccion { get; init; } = "";
    public string Barrio { get; init; } = "";
    public Operacion Operacion { get; init; }
    public TipoPropiedad Tipo { get; init; }
    public EstadoPublicacion Estado { get; init; } = EstadoPublicacion.Disponible;

    /// <summary>Moneda de publicación: "USD" para venta, "ARS" para alquiler.</summary>
    public string Moneda { get; init; } = "USD";
    public decimal Precio { get; init; }
    public decimal Expensas { get; init; }

    public int Ambientes { get; init; }
    public int Dormitorios { get; init; }
    public int Banios { get; init; }
    public int SuperficieCubierta { get; init; }
    public int SuperficieTotal { get; init; }
    public int Antiguedad { get; init; }
    public bool Cochera { get; init; }
    public bool Balcon { get; init; }
    public bool AptoCredito { get; init; }
    public bool Destacada { get; init; }

    public string Descripcion { get; init; } = "";
    public IReadOnlyList<string> Comodidades { get; init; } = Array.Empty<string>();

    /// <summary>Rutas relativas a wwwroot. Si está vacío se dibuja una portada generada.</summary>
    public IReadOnlyList<string> Fotos { get; init; } = Array.Empty<string>();

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
}
