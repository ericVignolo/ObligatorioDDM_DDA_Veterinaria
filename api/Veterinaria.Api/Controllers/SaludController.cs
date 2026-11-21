using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Carnet sanitario, alertas de refuerzo y consentimiento informado del cliente (A-021, A-025, RF-CLI-06, RF-CLI-11).
// Cada cliente ve solo sus animales y sus turnos (RN-18).
[ApiController]
[Route("api/v1")]
[Authorize(Roles = "Cliente")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class SaludController : ControllerBase
{
    private readonly ICarnetService _carnet;
    private readonly IConsentimientoService _consentimientos;

    public SaludController(ICarnetService carnet, IConsentimientoService consentimientos)
    {
        _carnet = carnet;
        _consentimientos = consentimientos;
    }

    // GET /api/v1/animales/3/carnet
    [HttpGet("animales/{id:int}/carnet")]
    [ProducesResponseType(typeof(List<ItemCarnetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Carnet(int id)
    {
        return Problemas.Desde(await _carnet.CarnetAsync(User.CuentaId(), id));
    }

    // GET /api/v1/alertas-sanitarias → refuerzos próximos o vencidos de todos mis animales
    [HttpGet("alertas-sanitarias")]
    [ProducesResponseType(typeof(List<ItemCarnetDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Alertas()
    {
        return Ok(await _carnet.AlertasAsync(User.CuentaId()));
    }

    // GET /api/v1/turnos/consentimientos-pendientes
    [HttpGet("turnos/consentimientos-pendientes")]
    [ProducesResponseType(typeof(List<ConsentimientoEstadoDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Pendientes()
    {
        return Ok(await _consentimientos.PendientesAsync(User.CuentaId()));
    }

    // GET /api/v1/turnos/5/consentimiento → estado y texto de la versión vigente
    [HttpGet("turnos/{id:int}/consentimiento")]
    [ProducesResponseType(typeof(ConsentimientoEstadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Consentimiento(int id)
    {
        return Problemas.Desde(await _consentimientos.EstadoAsync(User.CuentaId(), id));
    }

    // POST /api/v1/turnos/5/consentimiento { "acepta": true, "nombreCompleto": "Ana Pérez" }
    [HttpPost("turnos/{id:int}/consentimiento")]
    [ProducesResponseType(typeof(ConsentimientoEstadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Responder(int id, RespuestaConsentimientoDto datos)
    {
        return Problemas.Desde(await _consentimientos.ResponderAsync(User.CuentaId(), id, datos));
    }
}
