using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Veterinaria.Api.Data;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Única conexión a MySQL del sistema (DT-BD-01). Blazor y React Native consumen esta API.
// La contraseña real va en appsettings.Development.json, que no se sube a GitHub.
var conexion = builder.Configuration.GetConnectionString("Vitalia")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Vitalia'.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseMySQL(conexion));

// Repositories y Services (Controller → Service → Repository → AppDbContext).
builder.Services.AddScoped<IPrestacionRepository, PrestacionRepository>();
builder.Services.AddScoped<IPrestacionService, PrestacionService>();

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Errores no controlados en formato Problem Details, sin detalles internos (D-CAT-03).
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
