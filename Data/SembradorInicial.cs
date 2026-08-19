using System.Security.Cryptography;
using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;
using Microsoft.EntityFrameworkCore;

namespace Enricci_Propiedades.Data;

/// <summary>
/// Prepara la base en el primer arranque: aplica las migraciones pendientes,
/// carga el catálogo de ejemplo si todavía no hay ninguna propiedad y crea el
/// usuario administrador si no existe ninguno.
/// </summary>
public static class SembradorInicial
{
    /// <summary>Título de la primera publicación de ejemplo, para saber si ya están cargadas.</summary>
    private const string TituloTestigoDeEjemplo = "Piso alto con vista abierta sobre Av. Entre Ríos";

    public static async Task PrepararAsync(WebApplication app)
    {
        using var alcance = app.Services.CreateScope();
        var servicios = alcance.ServiceProvider;
        var bd = servicios.GetRequiredService<EnricciContexto>();
        var log = servicios.GetRequiredService<ILoggerFactory>().CreateLogger("Sembrador");

        await bd.Database.MigrateAsync();

        // El catálogo real de la inmobiliaria entra por migración, así que una
        // base recién creada ya viene con esas publicaciones. Las de ejemplo son
        // ficticias y sólo sirven para probar el sitio con el catálogo poblado:
        // se cargan únicamente si se piden.
        var configuracion = servicios.GetRequiredService<IConfiguration>();
        var quiereEjemplos = configuracion.GetValue("Admin:CargarCatalogoDeEjemplo", false);

        // No alcanza con preguntar si la tabla está vacía: después de la
        // migración nunca lo está. Se busca una de las publicaciones de ejemplo.
        if (quiereEjemplos &&
            !await bd.Propiedades.AnyAsync(p => p.Titulo == TituloTestigoDeEjemplo))
        {
            bd.Propiedades.AddRange(CatalogoDeEjemplo());
            await bd.SaveChangesAsync();
            log.LogInformation("Catálogo de ejemplo cargado con 12 propiedades ficticias.");
        }

        await CrearAdministradorAsync(servicios, bd, log);
    }

    /// <summary>
    /// Crea el usuario del panel si la tabla está vacía. La contraseña sale de
    /// la configuración (sección "Admin"); si no está definida se genera una al
    /// azar y se escribe una única vez en el log, con la obligación de cambiarla
    /// en el primer ingreso. Así el repositorio nunca contiene una clave.
    /// </summary>
    private static async Task CrearAdministradorAsync(
        IServiceProvider servicios, EnricciContexto bd, ILogger log)
    {
        if (await bd.Usuarios.AnyAsync())
        {
            return;
        }

        var configuracion = servicios.GetRequiredService<IConfiguration>();
        var usuarios = servicios.GetRequiredService<UsuariosService>();

        var nombre = configuracion["Admin:Nombre"] ?? "Horacio Enricci";
        var email = configuracion["Admin:Email"] ?? SitioInfo.Email;
        var claveConfigurada = configuracion["Admin:ClaveInicial"];

        var clave = string.IsNullOrWhiteSpace(claveConfigurada) ? GenerarClave() : claveConfigurada;
        var generada = string.IsNullOrWhiteSpace(claveConfigurada);

        await usuarios.CrearAsync(nombre, email, clave, debeCambiarClave: true);

        if (generada)
        {
            log.LogWarning(
                "Usuario del panel creado: {Email} · contraseña inicial: {Clave} — " +
                "anotala ahora, no se vuelve a mostrar. Hay que cambiarla al ingresar.",
                email, clave);
        }
        else
        {
            log.LogInformation(
                "Usuario del panel creado: {Email}, con la contraseña de la configuración.", email);
        }
    }

    /// <summary>Contraseña legible pero impredecible, para entregarle a Horacio.</summary>
    private static string GenerarClave()
    {
        const string alfabeto = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var letras = new char[14];
        for (var i = 0; i < letras.Length; i++)
        {
            letras[i] = alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)];
        }

        return new string(letras);
    }

    private static List<Propiedad> CatalogoDeEjemplo() => new()
    {
        new Propiedad
        {
            Titulo = "Piso alto con vista abierta sobre Av. Entre Ríos",
            Direccion = "Av. Entre Ríos 500",
            Barrio = "Monserrat",
            Operacion = Operacion.Venta,
            Tipo = TipoPropiedad.Departamento,
            Moneda = "USD",
            Precio = 148000,
            Expensas = 92000,
            Ambientes = 3,
            Dormitorios = 2,
            Banios = 1,
            SuperficieCubierta = 68,
            SuperficieTotal = 74,
            Antiguedad = 45,
            Balcon = true,
            AptoCredito = true,
            Destacada = true,
            Descripcion = "Departamento de tres ambientes en piso alto sobre una de las avenidas " +
                          "más conectadas del centro porteño. Living comedor con salida a balcón " +
                          "corrido, dos dormitorios con placard y cocina independiente. Excelente " +
                          "luminosidad durante todo el día y frente despejado hacia el este.",
            Comodidades = new() { "Balcón corrido", "Piso alto", "Luminoso", "Cocina independiente", "Portero", "Apto crédito" },
            Fotos = new() { "/imagenes/Av%20Entre%20Rios%20500/Frente.jpg" }
        },
        new Propiedad
        {
            Titulo = "Dos ambientes ideal primera vivienda",
            Direccion = "Cochabamba 1700",
            Barrio = "Constitución",
            Operacion = Operacion.Venta,
            Tipo = TipoPropiedad.Departamento,
            Moneda = "USD",
            Precio = 79000,
            Expensas = 61000,
            Ambientes = 2,
            Dormitorios = 1,
            Banios = 1,
            SuperficieCubierta = 42,
            SuperficieTotal = 45,
            Antiguedad = 38,
            AptoCredito = true,
            Destacada = true,
            Descripcion = "Dos ambientes en muy buen estado de conservación, pensado para quien " +
                          "compra su primera vivienda o busca una renta estable. Living comedor, " +
                          "dormitorio con placard, cocina completa y baño con bañera. Edificio " +
                          "con expensas contenidas y a cuadras de la estación Constitución.",
            Comodidades = new() { "Apto crédito", "Expensas bajas", "Cerca del subte", "Placard", "Contrafrente" },
            Fotos = new() { "/imagenes/Cochabamba%201700/Frente.jpg" }
        },
        new Propiedad
        {
            Titulo = "Casa reciclada a nuevo con patio y entrepiso",
            Direccion = "Solís 700",
            Barrio = "Constitución",
            Operacion = Operacion.Venta,
            Tipo = TipoPropiedad.PH,
            Moneda = "USD",
            Precio = 132000,
            Expensas = 0,
            Ambientes = 3,
            Dormitorios = 2,
            Banios = 2,
            SuperficieCubierta = 78,
            SuperficieTotal = 96,
            Antiguedad = 70,
            AptoCredito = true,
            Destacada = true,
            Descripcion = "PH reciclado íntegramente: instalaciones nuevas, aberturas restauradas " +
                          "y terminaciones de primera. Entrepiso con altura, patio propio con " +
                          "parrilla y sin expensas. Una tipología difícil de conseguir en la zona.",
            Comodidades = new() { "Sin expensas", "Patio propio", "Parrilla", "Entrepiso", "Reciclado a nuevo", "Apto crédito" },
            Fotos = new() { "/imagenes/Solis%20700/Frente.jpg" }
        },
        new Propiedad
        {
            Titulo = "Monoambiente amoblado con amenities",
            Direccion = "Av. San Juan 1200",
            Barrio = "San Cristóbal",
            Operacion = Operacion.Alquiler,
            Tipo = TipoPropiedad.Departamento,
            Moneda = "ARS",
            Precio = 480000,
            Expensas = 78000,
            Ambientes = 1,
            Dormitorios = 0,
            Banios = 1,
            SuperficieCubierta = 34,
            SuperficieTotal = 34,
            Antiguedad = 8,
            Balcon = true,
            Descripcion = "Monoambiente divisible totalmente amoblado, listo para habitar. " +
                          "Edificio con laundry, SUM y terraza con solárium. Contrato de " +
                          "alquiler por 36 meses con garantía propietaria o seguro de caución.",
            Comodidades = new() { "Amoblado", "Amenities", "Laundry", "Balcón", "Apto profesional" }
        },
        new Propiedad
        {
            Titulo = "Local a la calle con vidriera sobre avenida",
            Direccion = "Av. Independencia 1900",
            Barrio = "San Cristóbal",
            Operacion = Operacion.Alquiler,
            Tipo = TipoPropiedad.LocalComercial,
            Moneda = "ARS",
            Precio = 950000,
            Expensas = 0,
            Ambientes = 2,
            Dormitorios = 0,
            Banios = 1,
            SuperficieCubierta = 62,
            SuperficieTotal = 70,
            Antiguedad = 55,
            Destacada = true,
            Descripcion = "Local a la calle con 5 metros de vidriera sobre avenida de alto " +
                          "tránsito peatonal. Salón principal, depósito, baño y entrepiso " +
                          "de guardado. Habilitación comercial vigente para rubros varios.",
            Comodidades = new() { "Vidriera a la calle", "Depósito", "Entrepiso", "Alto tránsito", "Habilitación vigente" }
        },
        new Propiedad
        {
            Titulo = "Cuatro ambientes con dependencia en edificio de categoría",
            Direccion = "Belgrano 2100",
            Barrio = "Balvanera",
            Operacion = Operacion.Venta,
            Tipo = TipoPropiedad.Departamento,
            Moneda = "USD",
            Precio = 189000,
            Expensas = 145000,
            Ambientes = 4,
            Dormitorios = 3,
            Banios = 2,
            SuperficieCubierta = 104,
            SuperficieTotal = 112,
            Antiguedad = 50,
            Cochera = true,
            Balcon = true,
            AptoCredito = true,
            Descripcion = "Amplio cuatro ambientes al frente con living comedor de gran " +
                          "superficie, tres dormitorios, dos baños y dependencia de servicio " +
                          "con baño. Incluye cochera cubierta en el mismo edificio.",
            Comodidades = new() { "Cochera cubierta", "Dependencia", "Balcón al frente", "Dos baños", "Portero permanente" }
        },
        new Propiedad
        {
            Titulo = "Oficina en planta libre apta profesional",
            Direccion = "Bernardo de Irigoyen 600",
            Barrio = "Monserrat",
            Operacion = Operacion.Alquiler,
            Tipo = TipoPropiedad.Oficina,
            Moneda = "ARS",
            Precio = 720000,
            Expensas = 110000,
            Ambientes = 2,
            Dormitorios = 0,
            Banios = 1,
            SuperficieCubierta = 58,
            SuperficieTotal = 58,
            Antiguedad = 30,
            Descripcion = "Oficina en planta libre con cableado estructurado, aire " +
                          "acondicionado central y baño privado. Edificio con recepción, " +
                          "ascensores modernizados y seguridad las 24 horas.",
            Comodidades = new() { "Planta libre", "Aire acondicionado", "Seguridad 24 h", "Baño privado", "Apto profesional" }
        },
        new Propiedad
        {
            Titulo = "Cochera fija cubierta en playa con acceso 24 h",
            Direccion = "Estados Unidos 1500",
            Barrio = "Constitución",
            Operacion = Operacion.Alquiler,
            Tipo = TipoPropiedad.Cochera,
            Moneda = "ARS",
            Precio = 145000,
            Expensas = 0,
            Ambientes = 0,
            Dormitorios = 0,
            Banios = 0,
            SuperficieCubierta = 12,
            SuperficieTotal = 12,
            Antiguedad = 25,
            Cochera = true,
            Descripcion = "Cochera fija cubierta para auto mediano, en playa con acceso " +
                          "las 24 horas y vigilancia. Maniobra cómoda y sin bloqueos.",
            Comodidades = new() { "Fija", "Cubierta", "Acceso 24 h", "Vigilancia" }
        },
        new Propiedad
        {
            Titulo = "Fondo de comercio: café de especialidad en funcionamiento",
            Direccion = "Chile 1300",
            Barrio = "San Telmo",
            Operacion = Operacion.Venta,
            Tipo = TipoPropiedad.FondoDeComercio,
            Moneda = "USD",
            Precio = 65000,
            Expensas = 0,
            Ambientes = 3,
            Dormitorios = 0,
            Banios = 2,
            SuperficieCubierta = 88,
            SuperficieTotal = 95,
            Antiguedad = 6,
            Destacada = true,
            Descripcion = "Se transfiere fondo de comercio de cafetería de especialidad en " +
                          "pleno funcionamiento, con clientela formada, equipamiento completo " +
                          "y contrato de alquiler vigente. Se entregan libros y proveedores.",
            Comodidades = new() { "En funcionamiento", "Equipamiento incluido", "Contrato vigente", "Clientela formada" }
        },
        new Propiedad
        {
            Titulo = "Dos ambientes temporario con vista al Parque Lezama",
            Direccion = "Defensa 1400",
            Barrio = "San Telmo",
            Operacion = Operacion.AlquilerTemporario,
            Tipo = TipoPropiedad.Departamento,
            Moneda = "ARS",
            Precio = 890000,
            Expensas = 0,
            Ambientes = 2,
            Dormitorios = 1,
            Banios = 1,
            SuperficieCubierta = 46,
            SuperficieTotal = 50,
            Antiguedad = 90,
            Balcon = true,
            Descripcion = "Departamento amoblado con estilo, en edificio antiguo restaurado " +
                          "frente al Parque Lezama. Alquiler temporario desde un mes, con " +
                          "servicios y wifi incluidos. Ideal para estadías de trabajo.",
            Comodidades = new() { "Amoblado", "Servicios incluidos", "Wifi", "Vista al parque", "Desde 1 mes" }
        },
        new Propiedad
        {
            Titulo = "Casa de tres dormitorios con terraza propia",
            Direccion = "Pavón 2400",
            Barrio = "Boedo",
            Operacion = Operacion.Venta,
            Tipo = TipoPropiedad.Casa,
            Moneda = "USD",
            Precio = 215000,
            Expensas = 0,
            Ambientes = 5,
            Dormitorios = 3,
            Banios = 2,
            SuperficieCubierta = 128,
            SuperficieTotal = 172,
            Antiguedad = 60,
            Cochera = true,
            AptoCredito = true,
            Descripcion = "Casa en lote propio sobre dos plantas: living comedor, cocina " +
                          "office, tres dormitorios, dos baños completos y terraza con " +
                          "parrilla. Garaje pasante para un auto y patio con jardín.",
            Comodidades = new() { "Lote propio", "Terraza con parrilla", "Garaje", "Patio", "Sin expensas", "Apto crédito" }
        },
        new Propiedad
        {
            Titulo = "Tres ambientes al frente, reciclado y con balcón",
            Direccion = "Carlos Calvo 1800",
            Barrio = "San Cristóbal",
            Operacion = Operacion.Alquiler,
            Tipo = TipoPropiedad.Departamento,
            Moneda = "ARS",
            Precio = 640000,
            Expensas = 95000,
            Ambientes = 3,
            Dormitorios = 2,
            Banios = 1,
            SuperficieCubierta = 64,
            SuperficieTotal = 68,
            Antiguedad = 42,
            Balcon = true,
            Estado = EstadoPublicacion.Reservada,
            Descripcion = "Tres ambientes al frente completamente reciclado: pisos nuevos, " +
                          "cocina integrada y baño renovado. Balcón con vista despejada. " +
                          "Contrato de 36 meses con actualización según normativa vigente.",
            Comodidades = new() { "Reciclado", "Balcón al frente", "Cocina integrada", "Luminoso" }
        }
    };
}
