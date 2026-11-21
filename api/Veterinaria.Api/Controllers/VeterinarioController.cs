using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Agenda del veterinario (RF-VET-01). La consulta clínica se agrega en su propio módulo.
[ApiController]
[Route("api/v1/veterinario")]
[Authorize(Roles = "Veterinario")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class VeterinarioController : ControllerBase
{
    private readonly ITurnoService _turnos;

    public VeterinarioController(ITurnoService turnos)
    {
        _turnos = turnos;
    }

    // GET /api/v1/veterinario/agenda?fecha=2026-11-25 → los turnos del profesional de la cuenta
    [HttpGet("agenda")]
    [ProducesResponseType(typeof(List<TurnoReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Agenda([FromQuery] DateOnly? fecha)
    {
        return Problemas.Desde(await _turnos.AgendaDelVeterinarioAsync(User.CuentaId(), fecha ?? DateOnly.FromDateTime(DateTime.Now)));
    }
}
