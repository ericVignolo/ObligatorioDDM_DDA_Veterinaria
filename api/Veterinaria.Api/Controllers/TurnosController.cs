using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Turnos del cliente (CU-CLI-02, A-014 a A-016). Solo con token de Cliente; cada cliente ve y cambia los suyos.
[ApiController]
[Route("api/v1/turnos")]
[Authorize(Roles = "Cliente")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class TurnosController : ControllerBase
{
    private readonly ITurnoService _service;

    public TurnosController(ITurnoService service)
    {
        _service = service;
    }

    // GET /api/v1/turnos/disponibilidad?profesionalId=1&prestacionId=1&animalId=3&fecha=2026-11-25
    [HttpGet("disponibilidad")]
    [ProducesResponseType(typeof(DisponibilidadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Disponibilidad([FromQuery] int profesionalId, [FromQuery] int prestacionId, [FromQuery] int animalId, [FromQuery] DateOnly fecha)
    {
        return Problemas.Desde(await _service.DisponibilidadClienteAsync(User.CuentaId(), profesionalId, prestacionId, animalId, fecha));
    }

    // POST /api/v1/turnos → 201; 409 si el horario se ocupó; 403 si debe reservar por Recepción (RN-08)
    [HttpPost]
    [ProducesResponseType(typeof(TurnoReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Reservar(ReservaCreateDto datos)
    {
        return Problemas.Desde(await _service.ReservarAsync(User.CuentaId(), datos));
    }

    // GET /api/v1/turnos?vista=proximos (o historial)
    [HttpGet]
    [ProducesResponseType(typeof(PaginaDto<TurnoReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> MisTurnos([FromQuery] string vista = "proximos", [FromQuery] int page = 1, [FromQuery] int pageSize = Paginacion.PageSizePorDefecto)
    {
        var error = Paginacion.Validar(page, pageSize);
        if (error != null)
        {
            return BadRequest(Problemas.Crear(400, "Paginación inválida", error, "PAGINACION_INVALIDA"));
        }

        return Ok(await _service.MisTurnosAsync(User.CuentaId(), vista != "historial", page, pageSize));
    }

    // GET /api/v1/turnos/5
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TurnoReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Obtener(int id)
    {
        return Problemas.Desde(await _service.ObtenerMioAsync(User.CuentaId(), id));
    }

    // POST /api/v1/turnos/5/cancelacion → hasta 24 h antes (RN-06)
    [HttpPost("{id:int}/cancelacion")]
    [ProducesResponseType(typeof(TurnoReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Cancelar(int id, CancelacionDto datos)
    {
        return Problemas.Desde(await _service.CancelarAsync(User.CuentaId(), id, datos.Motivo));
    }

    // PUT /api/v1/turnos/5/horario → mismo turno con otro horario (A-016)
    [HttpPut("{id:int}/horario")]
    [ProducesResponseType(typeof(TurnoReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Reprogramar(int id, ReprogramacionDto datos)
    {
        return Problemas.Desde(await _service.ReprogramarAsync(User.CuentaId(), id, datos.Inicio!.Value));
    }
}
