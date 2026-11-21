using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Mostrador (A-017, RF-REC-01 a 04). La agenda y los turnos del mostrador los usan Recepción y el Administrador (RN-09);
// llegada, ausencia y cancelación solo Recepción (matriz de permisos, FLUJO_RESERVA_CONSULTA §5).
[ApiController]
[Route("api/v1/recepcion")]
[Authorize(Roles = "Recepcionista,Administrador")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class RecepcionController : ControllerBase
{
    private readonly ITurnoService _service;

    public RecepcionController(ITurnoService service)
    {
        _service = service;
    }

    // GET /api/v1/recepcion/clientes?buscar=perez
    [HttpGet("clientes")]
    [ProducesResponseType(typeof(PaginaDto<ClienteBusquedaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> BuscarClientes([FromQuery] string buscar = "", [FromQuery] int page = 1, [FromQuery] int pageSize = Paginacion.PageSizePorDefecto)
    {
        var error = Paginacion.Validar(page, pageSize);
        if (error != null)
        {
            return BadRequest(Problemas.Crear(400, "Paginación inválida", error, "PAGINACION_INVALIDA"));
        }

        return Ok(await _service.BuscarClientesAsync(buscar, page, pageSize));
    }

    // GET /api/v1/recepcion/disponibilidad?clienteId=12&animalId=3&profesionalId=1&prestacionId=1&fecha=2026-11-25
    [HttpGet("disponibilidad")]
    [ProducesResponseType(typeof(DisponibilidadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Disponibilidad([FromQuery] int clienteId, [FromQuery] int animalId, [FromQuery] int profesionalId, [FromQuery] int prestacionId, [FromQuery] DateOnly fecha)
    {
        return Problemas.Desde(await _service.DisponibilidadMostradorAsync(clienteId, profesionalId, prestacionId, animalId, fecha));
    }

    // GET /api/v1/recepcion/turnos?fecha=2026-11-25&profesionalId=1&estado=Confirmado
    [HttpGet("turnos")]
    [ProducesResponseType(typeof(List<TurnoReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<TurnoReadDto>>> AgendaDelDia([FromQuery] DateOnly? fecha, [FromQuery] int? profesionalId, [FromQuery] string? estado)
    {
        var dia = fecha ?? DateOnly.FromDateTime(DateTime.Now);
        return Ok(await _service.AgendaDelDiaAsync(dia, profesionalId, estado));
    }

    // POST /api/v1/recepcion/turnos → turno o sobreturno en nombre del cliente
    [HttpPost("turnos")]
    [ProducesResponseType(typeof(TurnoReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> ReservarPorMostrador(ReservaMostradorDto datos)
    {
        var rol = User.RolEntre("Recepcionista", "Administrador");
        return Problemas.Desde(await _service.ReservarPorMostradorAsync(User.CuentaId(), rol, datos));
    }

    // POST /api/v1/recepcion/turnos/5/llegada → Confirmado → En espera
    [HttpPost("turnos/{id:int}/llegada")]
    [Authorize(Roles = "Recepcionista")]
    [ProducesResponseType(typeof(TurnoReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> RegistrarLlegada(int id)
    {
        return Problemas.Desde(await _service.RegistrarLlegadaAsync(id));
    }

    // POST /api/v1/recepcion/turnos/5/ausencia → Confirmado → Ausente (15 min después del inicio)
    [HttpPost("turnos/{id:int}/ausencia")]
    [Authorize(Roles = "Recepcionista")]
    [ProducesResponseType(typeof(TurnoReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> RegistrarAusencia(int id)
    {
        return Problemas.Desde(await _service.RegistrarAusenciaAsync(id));
    }

    // POST /api/v1/recepcion/turnos/5/cancelacion → en cualquier momento, con motivo
    [HttpPost("turnos/{id:int}/cancelacion")]
    [Authorize(Roles = "Recepcionista")]
    [ProducesResponseType(typeof(TurnoReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Cancelar(int id, CancelacionDto datos)
    {
        return Problemas.Desde(await _service.CancelarPorRecepcionAsync(id, datos.Motivo));
    }
}
