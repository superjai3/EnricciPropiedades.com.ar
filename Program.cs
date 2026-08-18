using System.Globalization;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using Enricci_Propiedades.Services;

// El sitio se publica en español de Argentina: fija formatos de número y fecha.
var culturaArgentina = new CultureInfo("es-AR");
CultureInfo.DefaultThreadCurrentCulture = culturaArgentina;
CultureInfo.DefaultThreadCurrentUICulture = culturaArgentina;

var builder = WebApplication.CreateBuilder(args);

// Servicios del contenedor.
builder.Services.AddRazorPages();
builder.Services.AddSingleton<PropiedadesService>();

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

app.UseAuthorization();

app.MapRazorPages();

app.Run();
