using System.Globalization;
using Veterinaria.Blazor.Components;
using Veterinaria.Blazor.Services;

// Cultura uruguaya para números, moneda y fechas.
var culturaUruguay = new CultureInfo("es-UY");
CultureInfo.DefaultThreadCurrentCulture = culturaUruguay;
CultureInfo.DefaultThreadCurrentUICulture = culturaUruguay;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<PrestacionService>();
builder.Services.AddScoped<ClinicaService>();
builder.Services.AddScoped<ProfesionalService>();
builder.Services.AddScoped<DocumentoLegalService>();
builder.Services.AddScoped<CuentaService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode(); 

app.Run();
