using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MySql.EntityFrameworkCore.Extensions;
using Veterinaria.Api.Controllers;
using Veterinaria.Api.Data;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Única conexión a MySQL del sistema (DT-BD-01). Blazor y React Native consumen esta API.
// La contraseña real va en appsettings.Development.json, que no se sube a GitHub.
var conexion = builder.Configuration.GetConnectionString("Vitalia")
    ?? throw new InvalidOperationException("Falta la cadena de conexión 'Vitalia'.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseMySQL(conexion));

// Reloj del sistema (las pruebas usan uno falso).
builder.Services.AddSingleton(TimeProvider.System);

// Opciones de tokens y cuentas (secciones "Jwt" y "Cuentas" de appsettings).
builder.Services.Configure<OpcionesJwt>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<OpcionesCuentas>(o =>
{
    builder.Configuration.GetSection("Cuentas").Bind(o);
    // SOLO DESARROLLO: el código de verificación vuelve en la respuesta porque todavía no se envían correos.
    o.MostrarCodigoEnRespuesta = builder.Environment.IsDevelopment();
});

// Repositories y Services (Controller → Service → Repository → AppDbContext).
builder.Services.AddScoped<IPrestacionRepository, PrestacionRepository>();
builder.Services.AddScoped<IPrestacionService, PrestacionService>();
builder.Services.AddScoped<IProfesionalRepository, ProfesionalRepository>();
builder.Services.AddScoped<IProfesionalService, ProfesionalService>();
builder.Services.AddScoped<IClinicaRepository, ClinicaRepository>();
builder.Services.AddScoped<IClinicaService, ClinicaService>();
builder.Services.AddScoped<ICuentaRepository, CuentaRepository>();
builder.Services.AddScoped<ICuentaService, CuentaService>();
builder.Services.AddScoped<ISesionService, SesionService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEnvioCorreo, EnvioCorreoDesarrollo>();
builder.Services.AddScoped<IAnimalRepository, AnimalRepository>();
builder.Services.AddScoped<IAnimalService, AnimalService>();
builder.Services.AddScoped<ITurnoRepository, TurnoRepository>();
builder.Services.AddScoped<ITurnoService, TurnoService>();
builder.Services.AddScoped<IConsultaRepository, ConsultaRepository>();
builder.Services.AddScoped<IConsultaService, ConsultaService>();
builder.Services.AddScoped<ISaludRepository, SaludRepository>();
builder.Services.AddScoped<ICarnetService, CarnetService>();
builder.Services.AddScoped<IInventarioService, InventarioService>();
builder.Services.AddScoped<IConsentimientoService, ConsentimientoService>();
builder.Services.AddScoped<ICobroRepository, CobroRepository>();
builder.Services.AddScoped<ICobroService, CobroService>();

// Autenticación con token de acceso corto (RNF-SEG-03). La autorización se verifica en cada pedido (RNF-SEG-05).
var jwt = builder.Configuration.GetSection("Jwt").Get<OpcionesJwt>() ?? new OpcionesJwt();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Emisor,
            ValidAudience = jwt.Audiencia,
            IssuerSigningKey = TokenService.Clave(jwt),
            NameClaimType = TokenService.ClaimNombre,
            RoleClaimType = TokenService.ClaimRol,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// Límite de pedidos por dirección de origen para registro, ingreso y recuperación (RNF-SEG-09).
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(Problemas.PoliticaCuentas, contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
    o.OnRejected = async (contexto, cancelacion) =>
    {
        await contexto.HttpContext.Response.WriteAsJsonAsync(
            Problemas.Crear(429, "Demasiados pedidos", "Hiciste muchos pedidos seguidos. Esperá un minuto y volvé a intentar.", "DEMASIADOS_PEDIDOS"),
            cancelacion);
    };
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Errores no controlados en formato Problem Details, sin detalles internos (D-CAT-03).
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorConcurrencia>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
