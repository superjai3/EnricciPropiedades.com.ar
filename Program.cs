using System.Globalization;
using System.Text.Unicode;
using Microsoft.AspNetCore.Authentication.Cookies;
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
        opciones.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

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

// Canalización HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // Valor por defecto de HSTS: 30 días. Ver https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error", "?codigo={0}");
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

// Aplica migraciones, carga el catálogo de ejemplo y crea el usuario del panel
// la primera vez que arranca.
await SembradorInicial.PrepararAsync(app);

app.Run();
