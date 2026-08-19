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

    /// <summary>
    /// Devuelve el usuario si el correo y la contraseña son correctos, o null.
    /// El motivo del rechazo no se le informa al visitante: un mensaje único
    /// evita que se pueda averiguar qué correos existen.
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

        if (!ClaveHash.Verificar(clave ?? "", usuario.HashClave, usuario.Sal))
        {
            _log.LogWarning("Intento de ingreso al panel con contraseña incorrecta para {Email}.", usuario.Email);
            return null;
        }

        usuario.UltimoIngreso = DateTime.UtcNow;
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
