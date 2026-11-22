using Microsoft.AspNetCore.DataProtection;
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
builder.Services.Configure<OpcionesSeguridad>(builder.Configuration.GetSection("Seguridad"));
builder.Services.Configure<OpcionesTareas>(builder.Configuration.GetSection("TareasProgramadas"));
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
// Correo: si la sección "Correo" tiene servidor y remitente, se envía de verdad por SMTP; si no, envío de prueba.
builder.Services.Configure<OpcionesCorreo>(builder.Configuration.GetSection("Correo"));
var correo = builder.Configuration.GetSection("Correo").Get<OpcionesCorreo>() ?? new OpcionesCorreo();
if (correo.Configurado)
{
    builder.Services.AddScoped<IEnvioCorreo, CorreoSmtp>();
}
else
{
    builder.Services.AddScoped<IEnvioCorreo, EnvioCorreoDesarrollo>();
}
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
builder.Services.AddScoped<IAdministracionRepository, AdministracionRepository>();
builder.Services.AddScoped<IAdministracionService, AdministracionService>();
builder.Services.AddScoped<IInternacionRepository, InternacionRepository>();
builder.Services.AddScoped<IInternacionService, InternacionService>();
builder.Services.AddScoped<IComunicacionRepository, ComunicacionRepository>();
builder.Services.AddScoped<IComunicacionService, ComunicacionService>();
if (correo.Configurado)
{
    builder.Services.AddScoped<IEnvioComunicacion, CorreoSmtp>();
}
else
{
    builder.Services.AddScoped<IEnvioComunicacion, EnvioComunicacionDesarrollo>();
}
builder.Services.AddScoped<IAsistenciaRepository, AsistenciaRepository>();
builder.Services.AddScoped<IAsistenciaService, AsistenciaService>();
builder.Services.AddScoped<IDerechosRepository, DerechosRepository>();
builder.Services.AddScoped<IDerechosService, DerechosService>();

// Tareas automáticas: cola de correos y plazos de asistencia cada minuto; avisos de refuerzo, vencidos y
// cuentas sin verificar una vez por día. Se apagan con TareasProgramadas:Activas = false.
builder.Services.AddHostedService<ServicioTareas>();

// Avisos en tiempo real (SignalR, viene con ASP.NET Core): internación y asistencia.
builder.Services.AddSignalR();
builder.Services.AddSingleton<INotificador, NotificadorSignalR>();

// Verificación en dos pasos: el secreto se guarda cifrado con la protección de datos de ASP.NET Core.
builder.Services.AddDataProtection().SetApplicationName("Vitalia");
builder.Services.AddSingleton<ProteccionSecretos>();
builder.Services.AddScoped<ISegundoFactorService, SegundoFactorService>();

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

        // Los WebSockets del hub no mandan encabezados: el token llega en ?access_token= solo para /hubs.
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = contexto =>
            {
                var token = contexto.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) && contexto.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    contexto.Token = token;
                }

                return Task.CompletedTask;
            }
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

// RNF-SEG-04: el Administrador sin la verificación en dos pasos solo usa su cuenta y la activación.
builder.Services.AddControllers(o => o.Filters.Add<ExigirSegundoFactorFiltro>());
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
app.MapHub<AvisosHub>(AvisosHub.Ruta);

app.Run();
