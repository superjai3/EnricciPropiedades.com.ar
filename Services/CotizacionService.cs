using System.Globalization;
using System.Text.Json;
using Enricci_Propiedades.Models;
using Microsoft.Extensions.Options;

namespace Enricci_Propiedades.Services;

/// <summary>
/// Tipo de cambio del Banco Nación, para mostrar cuánto es en pesos una
/// propiedad publicada en dólares.
///
/// El valor se refresca en segundo plano y las páginas lo leen ya resuelto: una
/// consulta a otro servidor en medio del armado de la página agregaría espera al
/// visitante y lo dejaría a merced de que la fuente responda.
/// </summary>
public class CotizacionService
{
    private readonly IHttpClientFactory _fabrica;
    private readonly OpcionesCotizacion _opciones;
    private readonly ILogger<CotizacionService> _log;

    public CotizacionService(
        IHttpClientFactory fabrica,
        IOptions<OpcionesCotizacion> opciones,
        ILogger<CotizacionService> log)
    {
        _fabrica = fabrica;
        _opciones = opciones.Value;
        _log = log;
    }

    /// <summary>
    /// Última cotización conocida, o null si nunca se pudo obtener. Se conserva
    /// aunque una consulta posterior falle: un valor de ayer sirve; ninguno, no.
    /// </summary>
    public Cotizacion? Actual { get; private set; }

    public bool Habilitada => _opciones.Habilitada;
    public string Fuente => _opciones.Fuente;

    /// <summary>
    /// Pesos por dólar con los que se está convirtiendo, o null si no hay
    /// cotización. Lo usa la calculadora para rehacer la cuenta en el navegador
    /// sin volver a pedirle la página al servidor.
    /// </summary>
    public decimal? Valor
    {
        get
        {
            if (!_opciones.Habilitada || Actual is null)
            {
                return null;
            }

            var valor = Actual.Valor(_opciones.Punta);

            return valor > 0 ? valor : null;
        }
    }

    /// <summary>Cuánto es en pesos un importe en dólares. Null si no hay cotización.</summary>
    public decimal? APesos(decimal dolares)
    {
        if (!_opciones.Habilitada || Actual is null || dolares <= 0)
        {
            return null;
        }

        var valor = Actual.Valor(_opciones.Punta);

        return valor > 0 ? Math.Round(dolares * valor, 0) : null;
    }

    public async Task RefrescarAsync(CancellationToken cancelacion = default)
    {
        if (!_opciones.Habilitada || string.IsNullOrWhiteSpace(_opciones.Url))
        {
            return;
        }

        try
        {
            var cliente = _fabrica.CreateClient(nameof(CotizacionService));
            cliente.Timeout = TimeSpan.FromSeconds(10);

            var json = await cliente.GetStringAsync(_opciones.Url, cancelacion);
            var cotizacion = Interpretar(json);

            if (cotizacion is null)
            {
                _log.LogWarning(
                    "La cotización llegó con un formato que no se pudo interpretar desde {Url}.",
                    _opciones.Url);
                return;
            }

            Actual = cotizacion;
            _log.LogInformation(
                "Cotización actualizada: compra {Compra}, venta {Venta}, informada el {Fecha}.",
                cotizacion.Compra, cotizacion.Venta, cotizacion.FechaTexto);
        }
        catch (Exception ex)
        {
            // Que no haya cotización es una molestia; que se caiga el sitio, no.
            // Se conserva la última buena y se reintenta en el próximo ciclo.
            _log.LogWarning(ex, "No se pudo consultar la cotización en {Url}.", _opciones.Url);
        }
    }

    /// <summary>
    /// Lee la respuesta sin atarse a un proveedor: acepta el objeto suelto o el
    /// primero de una lista, y los importes tanto en número como en texto.
    /// </summary>
    internal static Cotizacion? Interpretar(string json)
    {
        try
        {
            using var documento = JsonDocument.Parse(json);
            var raiz = documento.RootElement;

            if (raiz.ValueKind == JsonValueKind.Array)
            {
                if (raiz.GetArrayLength() == 0)
                {
                    return null;
                }

                raiz = raiz[0];
            }

            if (raiz.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var venta = Numero(raiz, "venta") ?? Numero(raiz, "value_sell") ?? Numero(raiz, "sell");
            var compra = Numero(raiz, "compra") ?? Numero(raiz, "value_buy") ?? Numero(raiz, "buy") ?? venta;

            if (venta is null or <= 0)
            {
                return null;
            }

            var fecha = Fecha(raiz, "fechaActualizacion")
                        ?? Fecha(raiz, "fecha")
                        ?? Fecha(raiz, "last_update")
                        ?? DateTimeOffset.UtcNow;

            return new Cotizacion(compra!.Value, venta.Value, fecha);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static decimal? Numero(JsonElement objeto, string propiedad)
    {
        if (!objeto.TryGetProperty(propiedad, out var valor))
        {
            return null;
        }

        return valor.ValueKind switch
        {
            JsonValueKind.Number => valor.GetDecimal(),
            JsonValueKind.String when decimal.TryParse(
                valor.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var n) => n,
            _ => null
        };
    }

    private static DateTimeOffset? Fecha(JsonElement objeto, string propiedad)
    {
        if (!objeto.TryGetProperty(propiedad, out var valor) || valor.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTimeOffset.TryParse(
            valor.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var f)
            ? f
            : null;
    }
}

/// <summary>Mantiene la cotización al día sin que ninguna visita tenga que esperar.</summary>
public class CotizacionProgramada : BackgroundService
{
    private readonly CotizacionService _cotizacion;
    private readonly OpcionesCotizacion _opciones;

    public CotizacionProgramada(CotizacionService cotizacion, IOptions<OpcionesCotizacion> opciones)
    {
        _cotizacion = cotizacion;
        _opciones = opciones.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken cancelacion)
    {
        if (!_opciones.Habilitada)
        {
            return;
        }

        var espera = TimeSpan.FromMinutes(Math.Max(5, _opciones.MinutosEntreConsultas));

        while (!cancelacion.IsCancellationRequested)
        {
            await _cotizacion.RefrescarAsync(cancelacion);

            try
            {
                await Task.Delay(espera, cancelacion);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
