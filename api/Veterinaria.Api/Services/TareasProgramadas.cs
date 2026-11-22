namespace Veterinaria.Api.Services;

// Configuración de las tareas automáticas (sección "TareasProgramadas" de appsettings).
public class OpcionesTareas
{
    // false = no corre nada solo (por ejemplo, en pruebas). Las tareas se pueden seguir disparando a mano.
    public bool Activas { get; set; } = true;

    // Cada cuánto se revisa qué toca hacer.
    public int SegundosEntreRevisiones { get; set; } = 60;

    // Hora local a partir de la que corren las tareas diarias (una vez por día).
    public int HoraTareasDiarias { get; set; } = 6;
}

// Decide qué tareas tocan en cada revisión. Separado del servicio para poder probarlo sin esperar.
public class PlanTareas
{
    public const string ColaComunicaciones = "Cola de comunicaciones";
    public const string PlazosAsistencia = "Plazos de asistencia";
    public const string AvisosRefuerzo = "Avisos de refuerzo";
    public const string LotesVencidos = "Lotes vencidos";
    public const string CuentasSinVerificar = "Cuentas sin verificar";

    // Cada minuto: lo que tiene plazos cortos (RNF-CON-04; rojo sin aceptar en 10 min, RN-55).
    public static readonly string[] Frecuentes = { ColaComunicaciones, PlazosAsistencia };

    // Una vez por día: lo que depende de la fecha.
    public static readonly string[] Diarias = { AvisosRefuerzo, LotesVencidos, CuentasSinVerificar };

    private readonly int _horaDiarias;
    private DateOnly? _ultimoDia;

    public PlanTareas(int horaDiarias)
    {
        _horaDiarias = Math.Clamp(horaDiarias, 0, 23);
    }

    public List<string> QueToca(DateTime ahora)
    {
        var tareas = new List<string>(Frecuentes);
        var hoy = DateOnly.FromDateTime(ahora);
        if (_ultimoDia != hoy && ahora.Hour >= _horaDiarias)
        {
            tareas.AddRange(Diarias);
            _ultimoDia = hoy;
        }

        return tareas;
    }
}

// Servicio en segundo plano de la API: cada minuto procesa la cola de correos y los plazos de asistencia;
// una vez por día genera los avisos de refuerzo, da de baja los lotes vencidos y borra las cuentas sin verificar.
// Cada tarea corre en su propio alcance y un error en una no frena a las demás (queda en el log, sin datos personales).
public class ServicioTareas : BackgroundService
{
    private readonly IServiceScopeFactory _alcances;
    private readonly OpcionesTareas _opciones;
    private readonly TimeProvider _reloj;
    private readonly ILogger<ServicioTareas> _logger;

    public ServicioTareas(IServiceScopeFactory alcances, Microsoft.Extensions.Options.IOptions<OpcionesTareas> opciones, TimeProvider reloj, ILogger<ServicioTareas> logger)
    {
        _alcances = alcances;
        _opciones = opciones.Value;
        _reloj = reloj;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken detener)
    {
        if (!_opciones.Activas)
        {
            _logger.LogInformation("Tareas programadas desactivadas por configuración.");
            return;
        }

        var plan = new PlanTareas(_opciones.HoraTareasDiarias);
        using var temporizador = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(10, _opciones.SegundosEntreRevisiones)), _reloj);
        do
        {
            foreach (var tarea in plan.QueToca(_reloj.GetLocalNow().DateTime))
            {
                if (detener.IsCancellationRequested)
                {
                    return;
                }

                await CorrerAsync(tarea);
            }
        }
        while (await temporizador.WaitForNextTickAsync(detener));
    }

    public async Task CorrerAsync(string tarea)
    {
        try
        {
            using var alcance = _alcances.CreateScope();
            var servicios = alcance.ServiceProvider;
            var detalle = tarea switch
            {
                PlanTareas.ColaComunicaciones => Contar(await servicios.GetRequiredService<IComunicacionService>().ProcesarColaAsync()),
                PlanTareas.PlazosAsistencia => Contar(await servicios.GetRequiredService<IAsistenciaService>().RevisarPlazosAsync()),
                PlanTareas.AvisosRefuerzo => Contar(await servicios.GetRequiredService<ICarnetService>().GenerarAvisosAsync()),
                PlanTareas.LotesVencidos => Contar(await servicios.GetRequiredService<IInventarioService>().ProcesarVencidosAsync()),
                PlanTareas.CuentasSinVerificar => $"{await servicios.GetRequiredService<ICuentaService>().EliminarNoVerificadasAsync()} eliminadas",
                _ => "tarea desconocida"
            };
            _logger.LogInformation("Tarea «{Tarea}»: {Detalle}.", tarea, detalle);
        }
        catch (Exception ex)
        {
            // Sin el mensaje de la excepción en el log: puede traer datos de la base (RNF-SEG-11).
            _logger.LogWarning("La tarea «{Tarea}» falló ({Tipo}); se vuelve a intentar en la próxima revisión.", tarea, ex.GetType().Name);
        }
    }

    private static string Contar(DTOs.ColaProcesadaDto r) => $"{r.Intentados} intentados, {r.Enviados} enviados, {r.Fallidos} fallidos";
    private static string Contar(DTOs.PlazosRevisadosDto r) => $"{r.RojosEscalados} rojos escalados, {r.ReevaluacionesPedidas} reevaluaciones";
    private static string Contar(DTOs.AvisosGeneradosDto r) => $"{r.Generados} generados, {r.YaExistian} ya existían, {r.SinRecibir} sin recibir";
    private static string Contar(DTOs.VencidosProcesadosDto r) => $"{r.LotesInhabilitados} lotes, {r.UnidadesDadasDeBaja} unidades";
}
