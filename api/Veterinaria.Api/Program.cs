using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Veterinaria.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Única conexión a MySQL del sistema (DT-BD-01). Blazor y React Native consumen esta API.
// La contraseña real va en appsettings.Development.json, que no se sube a GitHub.
var conexion = builder.Configuration.GetConnectionString("Vitalia")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Vitalia'.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseMySQL(conexion));

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
