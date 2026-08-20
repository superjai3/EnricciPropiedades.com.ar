namespace Enricci_Propiedades.Services;

/// <summary>
/// Corre el respaldo una vez por día, a la hora configurada.
///
/// Va adentro de la aplicación y no como tarea programada del sistema para que
/// el respaldo viaje con el sitio: si mañana se muda de servidor, sigue
/// funcionando sin que nadie tenga que acordarse de volver a configurarlo.
/// </summary>
public class RespaldoProgramado : BackgroundService
{
    private readonly RespaldoService _respaldo;
    private readonly ILogger<RespaldoProgramado> _log;

    public RespaldoProgramado(RespaldoService respaldo, ILogger<RespaldoProgramado> log)
    {
        _respaldo = respaldo;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken cancelacion)
    {
        if (!_respaldo.Habilitado)
        {
            _log.LogInformation("El respaldo automático está apagado.");
            return;
        }

        _log.LogInformation(
            "Respaldo automático activo: todos los días a las {Hora}:00, conservando {Conservar} " +
            "copias en {Carpeta}.",
            _respaldo.HoraDiaria, _respaldo.Conservar, _respaldo.Carpeta);

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
