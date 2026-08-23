using Enricci_Propiedades.Data;
using Enricci_Propiedades.Models;
using Microsoft.EntityFrameworkCore;

namespace Enricci_Propiedades.Services;

/// <summary>
/// Consultas recibidas por los formularios del sitio.
///
/// Existe para que ningún contacto dependa de que el correo salga: el registro
/// queda en la base y el panel es el lugar donde se trabajan.
/// </summary>
public class ConsultasService
{
    private readonly EnricciContexto _bd;

    public ConsultasService(EnricciContexto bd) => _bd = bd;

    /// <summary>Guarda la consulta y devuelve la fila con su Id ya asignado.</summary>
    public async Task<Consulta> RegistrarAsync(Consulta consulta)
    {
        consulta.Fecha = DateTime.UtcNow;
        _bd.Consultas.Add(consulta);
        await _bd.SaveChangesAsync();
        return consulta;
    }

    /// <summary>
    /// Anota si el aviso por correo llegó a salir. Va aparte del alta porque el
    /// envío se intenta después de guardar, y un fallo del servidor de correo no
    /// tiene que impedir que la consulta quede registrada.
    /// </summary>
    public async Task MarcarCorreoEnviadoAsync(int id, bool enviado)
    {
        if (!enviado)
        {
            return;
        }

        var consulta = await _bd.Consultas.FirstOrDefaultAsync(c => c.Id == id);
        if (consulta is null)
        {
            return;
        }

        consulta.CorreoEnviado = true;
        await _bd.SaveChangesAsync();
    }

    public Task<List<Consulta>> ListarAsync(bool? atendidas = null, string? buscar = null)
    {
        var consulta = _bd.Consultas.AsQueryable();

        if (atendidas.HasValue)
        {
            consulta = consulta.Where(c => c.Atendida == atendidas.Value);
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var texto = buscar.Trim().ToLower();
            consulta = consulta.Where(c =>
                c.Nombre.ToLower().Contains(texto) ||
                c.Email.ToLower().Contains(texto) ||
                (c.Telefono != null && c.Telefono.Contains(texto)) ||
                c.Mensaje.ToLower().Contains(texto));
        }

        return consulta.OrderByDescending(c => c.Fecha).ToListAsync();
    }

    public Task<Consulta?> PorIdAsync(int id) =>
        _bd.Consultas.FirstOrDefaultAsync(c => c.Id == id);

    public Task<int> ContarPendientesAsync() =>
        _bd.Consultas.CountAsync(c => !c.Atendida);

    /// <summary>Marca o desmarca como atendida, y guarda cuándo se hizo.</summary>
    public async Task<Consulta?> AlternarAtendidaAsync(int id)
    {
        var consulta = await PorIdAsync(id);
        if (consulta is null)
        {
            return null;
        }

        consulta.Atendida = !consulta.Atendida;
        consulta.FechaAtendida = consulta.Atendida ? DateTime.UtcNow : null;
        await _bd.SaveChangesAsync();
        return consulta;
    }

    public async Task<bool> GuardarNotasAsync(int id, string? notas)
    {
        var consulta = await PorIdAsync(id);
        if (consulta is null)
        {
            return false;
        }

        consulta.Notas = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim();
        await _bd.SaveChangesAsync();
        return true;
    }

    public async Task<bool> EliminarAsync(int id)
    {
        var consulta = await PorIdAsync(id);
        if (consulta is null)
        {
            return false;
        }

        _bd.Consultas.Remove(consulta);
        await _bd.SaveChangesAsync();
        return true;
    }
}
