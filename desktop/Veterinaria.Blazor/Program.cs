using System.Globalization;
using Veterinaria.Blazor.Components;
using Veterinaria.Blazor.Datos;
using Veterinaria.Blazor.Datos.Demo;

var builder = WebApplication.CreateBuilder(args);

// Español rioplatense, fechas y moneda locales (RNF-USA-05).
var cultura = new CultureInfo("es-UY");
CultureInfo.DefaultThreadCurrentCulture = cultura;
CultureInfo.DefaultThreadCurrentUICulture = cultura;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton(TimeProvider.System);

// Sección A: adaptador con datos de demostración. En la sección C se reemplaza
// por un cliente HTTP de la API compartida; las pantallas no cambian.
builder.Services.AddSingleton<ICatalogoServicios, CatalogoServiciosDemo>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
