using Enricci_Propiedades.Data;
using Enricci_Propiedades.Models;
using Microsoft.EntityFrameworkCore;

namespace Enricci_Propiedades.Services;

/// <summary>
/// Alta y autenticación de los usuarios del panel de administración.
/// </summary>
public class UsuariosService
{
    private readonly EnricciContexto _bd;
    private readonly ILogger<UsuariosService> _log;

    public UsuariosService(EnricciContexto bd, ILogger<UsuariosService> log)
    {
        _bd = bd;
        _log = log;
    }

    public Task<List<Usuario>> TodosAsync() =>
        _bd.Usuarios.OrderBy(u => u.Nombre).ToListAsync();

    public Task<Usuario?> PorIdAsync(int id) =>
        _bd.Usuarios.FirstOrDefaultAsync(u => u.Id == id);

    /// <summary>Fallos seguidos que hacen falta para bloquear la cuenta un rato.</summary>
    private const int IntentosAntesDeBloquear = 5;

    /// <summary>
    /// Devuelve el usuario si el correo y la contraseña son correctos, o null.
    /// El motivo del rechazo no se le informa al visitante: un mensaje único
    /// evita que se pueda averiguar qué correos existen.
    ///
    /// A los <see cref="IntentosAntesDeBloquear"/> fallos seguidos la cuenta
    /// queda bloqueada un rato que crece con cada tanda, así que probar
    /// contraseñas al voleo deja de ser viable.
    /// </summary>
    public async Task<Usuario?> AutenticarAsync(string email, string clave)
    {
        var normalizado = (email ?? "").Trim().ToLowerInvariant();
        var usuario = await _bd.Usuarios.FirstOrDefaultAsync(u => u.Email == normalizado);

        if (usuario is null || !usuario.Activo)
        {
            _log.LogWarning("Intento de ingreso al panel con un correo inexistente o inactivo.");
            return null;
        }

        if (usuario.EstaBloqueado)
        {
            _log.LogWarning(
                "Ingreso rechazado: la cuenta {Email} está bloqueada hasta {Hasta:u}.",
                usuario.Email, usuario.BloqueadoHasta);
            return null;
        }

        if (!ClaveHash.Verificar(clave ?? "", usuario.HashClave, usuario.Sal))
        {
            usuario.IntentosFallidos++;

            if (usuario.IntentosFallidos >= IntentosAntesDeBloquear)
            {
                // 5 minutos la primera tanda, 10 la segunda, 20 la tercera… hasta 2 horas.
                var tandas = usuario.IntentosFallidos / IntentosAntesDeBloquear;
                var minutos = Math.Min(5 * Math.Pow(2, tandas - 1), 120);
                usuario.BloqueadoHasta = DateTime.UtcNow.AddMinutes(minutos);

                _log.LogWarning(
                    "Cuenta {Email} bloqueada {Minutos} minutos tras {Intentos} intentos fallidos.",
                    usuario.Email, minutos, usuario.IntentosFallidos);
            }
            else
            {
                _log.LogWarning(
                    "Contraseña incorrecta para {Email} ({Intentos} de {Tope}).",
                    usuario.Email, usuario.IntentosFallidos, IntentosAntesDeBloquear);
            }

            await _bd.SaveChangesAsync();
            return null;
        }

        usuario.UltimoIngreso = DateTime.UtcNow;
        usuario.IntentosFallidos = 0;
        usuario.BloqueadoHasta = null;
        await _bd.SaveChangesAsync();

        _log.LogInformation("Ingreso al panel de {Email}.", usuario.Email);
        return usuario;
    }

    public async Task<Usuario> CrearAsync(string nombre, string email, string clave, bool debeCambiarClave = false)
    {
        var (hash, sal) = ClaveHash.Calcular(clave);

        var usuario = new Usuario
        {
            Nombre = nombre.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            HashClave = hash,
            Sal = sal,
            Activo = true,
            DebeCambiarClave = debeCambiarClave
        };

        _bd.Usuarios.Add(usuario);
        await _bd.SaveChangesAsync();
        return usuario;
    }

    /// <summary>Cambia la contraseña validando primero la actual.</summary>
    public async Task<bool> CambiarClaveAsync(int usuarioId, string claveActual, string claveNueva)
    {
        var usuario = await PorIdAsync(usuarioId);
        if (usuario is null || !ClaveHash.Verificar(claveActual, usuario.HashClave, usuario.Sal))
        {
            return false;
        }

        var (hash, sal) = ClaveHash.Calcular(claveNueva);
        usuario.HashClave = hash;
        usuario.Sal = sal;
        usuario.DebeCambiarClave = false;

        await _bd.SaveChangesAsync();
        _log.LogInformation("El usuario {Email} cambió su contraseña.", usuario.Email);
        return true;
    }

    public Task<bool> HayAlgunoAsync() => _bd.Usuarios.AnyAsync();
}
