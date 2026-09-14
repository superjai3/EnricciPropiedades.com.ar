using Enricci_Propiedades.Models;
using Microsoft.Extensions.Options;

namespace Enricci_Propiedades.Services;

/// <summary>
/// Tareas de una vez por día, a la hora configurada: el respaldo y la purga de
/// consultas viejas.
///
/// Van adentro de la aplicación y no como tarea programada del sistema para
/// que viajen con el sitio: si mañana se muda de servidor, siguen funcionando
/// sin que nadie tenga que acordarse de volver a configurarlas.
/// </summary>
public class RespaldoProgramado : BackgroundService
{
    private readonly RespaldoService _respaldo;
    private readonly IServiceScopeFactory _alcances;
    private readonly OpcionesConsultas _consultas;
    private readonly ILogger<RespaldoProgramado> _log;

    public RespaldoProgramado(
        RespaldoService respaldo,
        IServiceScopeFactory alcances,
        IOptions<OpcionesConsultas> consultas,
        ILogger<RespaldoProgramado> log)
    {
        _respaldo = respaldo;
        _alcances = alcances;
        _consultas = consultas.Value;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken cancelacion)
    {
        var purga = _consultas.MesesRetencion > 0;

        if (!_respaldo.Habilitado)
        {
            _log.LogInformation("El respaldo automático está apagado.");
        }
        else
        {
            _log.LogInformation(
                "Respaldo automático activo: todos los días a las {Hora}:00, conservando {Conservar} " +
                "copias en {Carpeta}.",
                _respaldo.HoraDiaria, _respaldo.Conservar, _respaldo.Carpeta);
        }

        if (purga)
        {
            _log.LogInformation(
                "Las consultas con más de {Meses} meses se borran todos los días a las {Hora}:00.",
                _consultas.MesesRetencion, _respaldo.HoraDiaria);
        }

        if (!_respaldo.Habilitado && !purga)
        {
            return;
        }

        while (!cancelacion.IsCancellationRequested)
        {
            var espera = HastaLaProximaCorrida();

            try
            {
                await Task.Delay(espera, cancelacion);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (_respaldo.Habilitado)
            {
                try
                {
                    await _respaldo.RespaldarAsync(cancelacion);
                }
                catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    // Un respaldo que falla no puede tumbar el sitio: queda anotado
                    // y se vuelve a intentar al día siguiente.
                    _log.LogError(ex, "Falló el respaldo automático.");
                }
            }

            // La purga va después del respaldo a propósito: lo que se borra hoy
            // todavía está en la copia de hoy, por si hiciera falta.
            if (purga)
            {
                await PurgarConsultasAsync(cancelacion);
            }
        }
    }

    /// <summary>
    /// Borra las consultas que superaron el plazo de conservación. Las
    /// consultas guardan datos personales, y guardarlos más de lo que hace
    /// falta es un riesgo sin ningún beneficio.
    /// </summary>
    private async Task PurgarConsultasAsync(CancellationToken cancelacion)
    {
        try
        {
            // ConsultasService es scoped (lleva el DbContext): hay que abrir un
            // alcance propio, porque este servicio vive tanto como la aplicación.
            using var alcance = _alcances.CreateScope();
            var consultas = alcance.ServiceProvider.GetRequiredService<ConsultasService>();
            var borradas = await consultas.PurgarAntiguasAsync(_consultas.MesesRetencion, cancelacion);

            if (borradas > 0)
            {
                _log.LogInformation(
                    "Se borraron {Cantidad} consultas con más de {Meses} meses.",
                    borradas, _consultas.MesesRetencion);
            }
        }
        catch (OperationCanceledException) when (cancelacion.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Falló la purga de consultas viejas.");
        }
    }

    /// <summary>
    /// Cuánto falta para la próxima corrida, en hora de Buenos Aires. Si la hora
    /// de hoy ya pasó, apunta a la de mañana.
    /// </summary>
    private TimeSpan HastaLaProximaCorrida()
    {
        var ahora = Models.FechaHelper.AHoraArgentina(DateTime.UtcNow);
        var hoy = ahora.Date.AddHours(Math.Clamp(_respaldo.HoraDiaria, 0, 23));
        var proxima = hoy > ahora ? hoy : hoy.AddDays(1);

        return proxima - ahora;
    }
}
