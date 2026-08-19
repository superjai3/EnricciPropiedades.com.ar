using System.Text.Json;
using Enricci_Propiedades.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Enricci_Propiedades.Data;

/// <summary>
/// Base de datos del sitio: el catálogo de propiedades y los usuarios del panel.
/// Corre sobre SQLite (un único archivo junto a la aplicación).
/// </summary>
public class EnricciContexto : DbContext
{
    public EnricciContexto(DbContextOptions<EnricciContexto> opciones) : base(opciones) { }

    public DbSet<Propiedad> Propiedades => Set<Propiedad>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        // Las listas de textos cortos se guardan como JSON en una sola columna:
        // son parte de la publicación y nunca se consultan por separado.
        var aJson = new ValueConverter<List<string>, string>(
            lista => JsonSerializer.Serialize(lista, (JsonSerializerOptions?)null),
            texto => JsonSerializer.Deserialize<List<string>>(texto, (JsonSerializerOptions?)null) ?? new List<string>());

        var comparador = new ValueComparer<List<string>>(
            (a, b) => a != null && b != null && a.SequenceEqual(b),
            lista => lista.Aggregate(0, (acumulado, texto) => HashCode.Combine(acumulado, texto.GetHashCode())),
            lista => lista.ToList());

        var propiedad = modelo.Entity<Propiedad>();

        propiedad.Property(p => p.Comodidades).HasConversion(aJson).Metadata.SetValueComparer(comparador);
        propiedad.Property(p => p.Fotos).HasConversion(aJson).Metadata.SetValueComparer(comparador);

        // Los enums se guardan como texto: el archivo .db queda legible y una
        // reordenación futura de los valores no corrompe los datos.
        propiedad.Property(p => p.Operacion).HasConversion<string>().HasMaxLength(24);
        propiedad.Property(p => p.Tipo).HasConversion<string>().HasMaxLength(24);
        propiedad.Property(p => p.Estado).HasConversion<string>().HasMaxLength(24);

        // SQLite no tiene decimal nativo; se guarda como texto para no perder precisión.
        propiedad.Property(p => p.Precio).HasConversion<double>();
        propiedad.Property(p => p.Expensas).HasConversion<double>();

        propiedad.HasIndex(p => p.Estado);
        propiedad.HasIndex(p => p.Operacion);
        propiedad.HasIndex(p => p.Barrio);

        modelo.Entity<Usuario>().HasIndex(u => u.Email).IsUnique();
    }
}
