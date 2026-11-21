using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Asistencia del cliente desde la app (RF-CLI-21 a 25, B-013, B-014): crear, seguir e informar empeoramiento.
[ApiController]
[Route("api/v1/asistencias")]
[Authorize(Roles = "Cliente")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AsistenciasController : ControllerBase
{
    private readonly IAsistenciaService _service;

    public AsistenciasController(IAsistenciaService service)
    {
        _service = service;
    }

    // POST /api/v1/asistencias → 201; con la misma claveIdempotencia devuelve la que ya existía
    [HttpPost]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Crear(SolicitudAppDto datos) => Problemas.Desde(await _service.CrearDesdeAppAsync(User.CuentaId(), datos));

    // GET /api/v1/asistencias
    [HttpGet]
    [ProducesResponseType(typeof(List<SolicitudAsistenciaReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Mias() => Ok(await _service.MiasAsync(User.CuentaId()));

    // GET /api/v1/asistencias/4 → estado, veterinario y llegada estimada (solo después de aceptar, RN-53)
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Mia(int id) => Problemas.Desde(await _service.MiaAsync(User.CuentaId(), id));

    // POST /api/v1/asistencias/4/empeoramiento { nivel, motivo } → solo puede subir (RN-51)
    [HttpPost("{id:int}/empeoramiento")]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Empeoramiento(int id, CambioNivelDto datos) => Problemas.Desde(await _service.EmpeoramientoClienteAsync(User.CuentaId(), id, datos));
}

// Tablero de asistencia del personal (RF-REC-08/09). Mirar: Recepción y Administración; actuar: solo Recepción.
[ApiController]
[Route("api/v1/recepcion/asistencias")]
[Authorize(Roles = "Recepcionista,Administrador")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AsistenciaRecepcionController : ControllerBase
{
    private readonly IAsistenciaService _service;

    public AsistenciaRecepcionController(IAsistenciaService service)
    {
        _service = service;
    }

    // GET /api/v1/recepcion/asistencias → cola activa por prioridad (revisa los plazos antes)
    [HttpGet]
    [ProducesResponseType(typeof(List<SolicitudAsistenciaReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Cola() => Ok(await _service.ColaAsync());

    // GET /api/v1/recepcion/asistencias/cerradas → las últimas 50
    [HttpGet("cerradas")]
    [ProducesResponseType(typeof(List<SolicitudAsistenciaReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Cerradas() => Ok(await _service.CerradasAsync());

    // GET /api/v1/recepcion/asistencias/4 → con el historial
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Detalle(int id) => Problemas.Desde(await _service.DetalleAsync(id));

    // GET /api/v1/recepcion/asistencias/4/elegibles
    [HttpGet("{id:int}/elegibles")]
    [ProducesResponseType(typeof(List<VeterinarioElegibleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Elegibles(int id) => Problemas.Desde(await _service.ElegiblesAsync(id));

    // POST /api/v1/recepcion/asistencias → solicitud telefónica
    [HttpPost]
    [Authorize(Roles = "Recepcionista")]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Crear(SolicitudTelefonicaDto datos) => Problemas.Desde(await _service.CrearTelefonicaAsync(User.CuentaId(), datos));

    // POST /api/v1/recepcion/asistencias/4/asignacion { veterinarioId } (cuenta del veterinario)
    [HttpPost("{id:int}/asignacion")]
    [Authorize(Roles = "Recepcionista")]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Asignar(int id, AsignacionDto datos) => Problemas.Desde(await _service.AsignarAsync(User.CuentaId(), id, datos.VeterinarioId));

    // POST /api/v1/recepcion/asistencias/4/empeoramiento { nivel, motivo } → lo que informó el cliente por teléfono
    [HttpPost("{id:int}/empeoramiento")]
    [Authorize(Roles = "Recepcionista")]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Empeoramiento(int id, CambioNivelDto datos) => Problemas.Desde(await _service.EmpeoramientoTelefonicoAsync(User.CuentaId(), id, datos));

    // POST /api/v1/recepcion/asistencias/4/cierre { estado: "Cancelada por la clínica"|"No concretada", motivo }
    [HttpPost("{id:int}/cierre")]
    [Authorize(Roles = "Recepcionista")]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Cerrar(int id, CierreAsistenciaDto datos) => Problemas.Desde(await _service.CerrarAsync(User.CuentaId(), id, datos));

    // POST /api/v1/recepcion/asistencias/plazos → rojos sin aceptar y reevaluaciones de 24 h
    [HttpPost("plazos")]
    [ProducesResponseType(typeof(PlazosRevisadosDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> Plazos() => Ok(await _service.RevisarPlazosAsync());
}

// Asistencias del veterinario (RF-VET-16/17): aceptar, rechazar, avanzar, finalizar y cambiar el nivel (RN-52).
[ApiController]
[Route("api/v1/veterinario/asistencias")]
[Authorize(Roles = "Veterinario")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AsistenciaVeterinarioController : ControllerBase
{
    private readonly IAsistenciaService _service;

    public AsistenciaVeterinarioController(IAsistenciaService service)
    {
        _service = service;
    }

    // GET /api/v1/veterinario/asistencias → ofrecidas a mí y las que acepté
    [HttpGet]
    [ProducesResponseType(typeof(List<SolicitudAsistenciaReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Mias() => Ok(await _service.DeVeterinarioAsync(User.CuentaId()));

    // GET /api/v1/veterinario/asistencias/4
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Detalle(int id) => Problemas.Desde(await _service.DetalleAsync(id));

    [HttpPost("{id:int}/aceptacion")]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Aceptar(int id) => Problemas.Desde(await _service.AceptarAsync(User.CuentaId(), id));

    // POST /api/v1/veterinario/asistencias/4/rechazo { motivo }
    [HttpPost("{id:int}/rechazo")]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Rechazar(int id, MotivoAsistenciaDto datos) => Problemas.Desde(await _service.RechazarAsync(User.CuentaId(), id, datos.Motivo));

    // POST /api/v1/veterinario/asistencias/4/avance → Aceptada → En camino → En el lugar → En atención
    [HttpPost("{id:int}/avance")]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Avanzar(int id) => Problemas.Desde(await _service.AvanzarAsync(User.CuentaId(), id));

    // POST /api/v1/veterinario/asistencias/4/finalizacion { resultado } → genera el cargo de la visita (RN-58)
    [HttpPost("{id:int}/finalizacion")]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Finalizar(int id, ResultadoAtencionDto datos) => Problemas.Desde(await _service.FinalizarAsync(User.CuentaId(), id, datos.Resultado));

    // POST /api/v1/veterinario/asistencias/4/nivel { nivel, motivo } → confirmar, subir o bajar (RN-52)
    [HttpPost("{id:int}/nivel")]
    [ProducesResponseType(typeof(SolicitudAsistenciaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Nivel(int id, CambioNivelDto datos) => Problemas.Desde(await _service.CambiarNivelAsync(User.CuentaId(), id, datos));
}

// Guardia de asistencia (RF-ADM-13): solo el Administrador.
[ApiController]
[Route("api/v1/admin/guardia")]
[Authorize(Roles = "Administrador")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class GuardiaController : ControllerBase
{
    private readonly IAsistenciaService _service;

    public GuardiaController(IAsistenciaService service)
    {
        _service = service;
    }

    // PUT /api/v1/admin/guardia { cuentaId }
    [HttpPut]
    [ProducesResponseType(typeof(VeterinarioElegibleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Cambiar(GuardiaDto datos) => Problemas.Desde(await _service.CambiarGuardiaAsync(User.CuentaId(), datos.CuentaId));
}
