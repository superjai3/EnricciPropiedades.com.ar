using System.Globalization;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.WebEncoders;
using Enricci_Propiedades.Data;
using Enricci_Propiedades.Models;
using Enricci_Propiedades.Services;

// El sitio se publica en español de Argentina: fija formatos de número y fecha.
var culturaArgentina = new CultureInfo("es-AR");
CultureInfo.DefaultThreadCurrentCulture = culturaArgentina;
CultureInfo.DefaultThreadCurrentUICulture = culturaArgentina;

var builder = WebApplication.CreateBuilder(args);

// No anunciar el servidor: es información gratis para quien busca vulnerabilidades.
builder.WebHost.ConfigureKestrel(opciones => opciones.AddServerHeader = false);

// Base de datos: un archivo SQLite junto a la aplicación. La ruta se resuelve
// contra la carpeta del sitio y no contra el directorio de trabajo, que cambia
// según cómo se lo arranque (consola, servicio de Windows, IIS).
var cadena = builder.Configuration.GetConnectionString("Enricci") ?? "Data Source=enricci.db";
var constructor = new SqliteConnectionStringBuilder(cadena);

if (!string.IsNullOrWhiteSpace(constructor.DataSource) && !Path.IsPathRooted(constructor.DataSource))
{
    constructor.DataSource = Path.Combine(builder.Environment.ContentRootPath, constructor.DataSource);
}

builder.Services.AddDbContext<EnricciContexto>(opciones => opciones.UseSqlite(constructor.ConnectionString));

// Servicios del contenedor.
builder.Services.AddScoped<PropiedadesService>();
builder.Services.AddScoped<UsuariosService>();
builder.Services.AddSingleton<FotosService>();

// Envío de correo de los formularios (ver sección "Correo" de appsettings.json).
builder.Services.Configure<OpcionesCorreo>(builder.Configuration.GetSection(OpcionesCorreo.Seccion));
builder.Services.AddSingleton<CorreoService>();

// Tope al tamaño de lo que se sube: el máximo por foto lo controla FotosService,
// esto acota el pedido completo para que nadie llene el disco de una.
builder.Services.Configure<FormOptions>(opciones =>
{
    // Tiene que entrar una tanda entera de fotos de teléfono de una sola vez.
    opciones.MultipartBodyLengthLimit = 150 * 1024 * 1024;
    opciones.ValueCountLimit = 256;
});

var enProduccion = !builder.Environment.IsDevelopment();

// Detrás de un proxy (ngrok, Cloudflare, IIS, un balanceador) el sitio recibe
// los pedidos por HTTP aunque el visitante entre por HTTPS. Sin esto, las URL
// canónicas y las de Open Graph saldrían con "http://", y el freno por IP de la
// pantalla de ingreso agruparía a todo el mundo bajo la IP del proxy, con lo que
// un solo visitante podría dejar afuera a los demás.
var detrasDeProxy = builder.Configuration.GetValue("Hosting:DetrasDeProxy", false);

if (detrasDeProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(opciones =>
    {
        opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                                  | ForwardedHeaders.XForwardedProto
                                  | ForwardedHeaders.XForwardedHost;

        // El proxy no tiene una IP fija conocida. Se activa a propósito y sólo
        // cuando el sitio está efectivamente publicado detrás de uno.
        opciones.KnownNetworks.Clear();
        opciones.KnownProxies.Clear();
    });
}

// Las cookies se marcan Secure cuando el pedido llega por HTTPS. Con las
// cabeceras reenviadas activadas, detrás de un proxy eso es siempre, así que en
// producción salen igual de protegidas que con "Always".
//
// No se usa Always: el sistema antiforgery lanza una excepción si está en
// Always y llega un pedido por HTTP, de modo que cualquier acceso directo al
// origen —una comprobación de estado, alguien entrando sin pasar por el proxy—
// devolvería error 500 en todas las páginas con formulario.
var politicaCookie = CookieSecurePolicy.SameAsRequest;

// Ingreso al panel: cookie de sesión propia, sin dependencias externas.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opciones =>
    {
        opciones.LoginPath = "/Admin/Ingresar";
        opciones.LogoutPath = "/Admin/Salir";
        opciones.AccessDeniedPath = "/Admin/Ingresar";
        opciones.ExpireTimeSpan = TimeSpan.FromHours(8);
        opciones.SlidingExpiration = true;
        opciones.Cookie.Name = "enricci.panel";
        opciones.Cookie.HttpOnly = true;
        opciones.Cookie.SameSite = SameSiteMode.Lax;
        opciones.Cookie.SecurePolicy = politicaCookie;
    });

builder.Services.AddAntiforgery(opciones =>
{
    opciones.Cookie.HttpOnly = true;
    opciones.Cookie.SameSite = SameSiteMode.Strict;
    opciones.Cookie.SecurePolicy = politicaCookie;
});

builder.Services.AddAuthorization();

// Freno por dirección IP en la pantalla de ingreso. Se suma al bloqueo de la
// cuenta: uno corta la fuerza bruta contra una cuenta, el otro contra muchas.
builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // 20 pedidos por minuto: alcanza de sobra para quien se equivoca de
    // contraseña un par de veces, y corta en seco a quien prueba en serie.
    opciones.AddPolicy("ingreso", contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: contexto.Connection.RemoteIpAddress?.ToString() ?? "sin-ip",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    opciones.OnRejected = async (contexto, cancelacion) =>
    {
        contexto.HttpContext.Response.Headers.RetryAfter = "60";
        contexto.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
        await contexto.HttpContext.Response.WriteAsync(
            "Demasiados intentos seguidos. Esperá un minuto y volvé a probar.", cancelacion);
    };
});

// Todo lo que cuelga de /Admin exige sesión iniciada, salvo la propia pantalla
// de ingreso. Así una página nueva del panel nace protegida por omisión.
builder.Services
    .AddRazorPages()
    .AddRazorPagesOptions(opciones =>
    {
        opciones.Conventions.AuthorizeFolder("/Admin");
        opciones.Conventions.AllowAnonymousToPage("/Admin/Ingresar");
    });

// Permite que las tildes y la eñe se emitan tal cual (evita &#xF3; dentro de <script>).
builder.Services.Configure<WebEncoderOptions>(opciones =>
    opciones.TextEncoderSettings = new System.Text.Encodings.Web.TextEncoderSettings(UnicodeRanges.All));

var app = builder.Build();

// Lo primero de todo: el resto de la canalización tiene que ver el esquema y la
// IP reales del visitante, no los del proxy.
if (detrasDeProxy)
{
    app.UseForwardedHeaders();
}

// Canalización HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// Antes que nada, para que alcancen también a los archivos estáticos.
app.UseCabecerasSeguridad();

app.UseStatusCodePagesWithReExecute("/Error", "?codigo={0}");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Aplica migraciones, carga el catálogo de ejemplo y crea el usuario del panel
// la primera vez que arranca.
await SembradorInicial.PrepararAsync(app);

app.Run();
