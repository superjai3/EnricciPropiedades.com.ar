using System.Collections.Concurrent;

namespace Enricci_Propiedades.Services;

/// <summary>
/// Freno por dirección IP para los formularios públicos.
///
/// Va aparte del limitador de ASP.NET, que se aplica a la página entera: acá
/// sólo se frena el envío. Una ficha de propiedad tiene que poder abrirse mil
/// veces —buscadores incluidos— y aun así no aceptar mil consultas por minuto.
///
/// El campo trampa frena al robot tonto; esto frena al que además lo completa
/// bien. Ninguno de los dos reemplaza al otro.
/// </summary>
public class LimiteEnvios
{
    private readonly ConcurrentDictionary<string, List<DateTimeOffset>> _envios = new();

    /// <summary>
    /// Cuántos envíos se aceptan por IP dentro de la ventana. El número es
    /// holgado a propósito: muchos visitantes entran desde redes móviles donde
    /// cientos de personas comparten una misma IP, y frenar a alguien que
    /// quiere consultar cuesta más caro que dejar pasar un par de spams.
    /// </summary>
    public int Maximo { get; init; } = 10;

    public TimeSpan Ventana { get; init; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Anota el intento y dice si se puede seguir. Devuelve false recién cuando
    /// se pasó del máximo, así el visitante que se equivoca y reenvía no queda
    /// afuera.
    /// </summary>
    public bool Permite(string? ip)
    {
        var clave = string.IsNullOrWhiteSpace(ip) ? "sin-ip" : ip;
        var ahora = DateTimeOffset.UtcNow;

        // Se limpian las IPs viejas de vez en cuando para que el diccionario no
        // crezca sin techo en un proceso que vive meses.
        if (_envios.Count > 500)
        {
            Limpiar(ahora);
        }

        var marcas = _envios.GetOrAdd(clave, _ => new List<DateTimeOffset>());

        lock (marcas)
        {
            marcas.RemoveAll(m => ahora - m > Ventana);

            if (marcas.Count >= Maximo)
            {
                return false;
            }

            marcas.Add(ahora);
            return true;
        }
    }

    private void Limpiar(DateTimeOffset ahora)
    {
        foreach (var (clave, marcas) in _envios.ToArray())
        {
            lock (marcas)
            {
                marcas.RemoveAll(m => ahora - m > Ventana);

                if (marcas.Count == 0)
                {
                    _envios.TryRemove(clave, out _);
                }
            }
        }
    }
}
