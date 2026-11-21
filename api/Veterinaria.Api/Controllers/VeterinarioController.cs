using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Agenda, consulta clínica e historia del veterinario (RF-VET-01 a 06, RF-VET-13, CU-VET-01).
// Solo el autor edita y firma su consulta; la cuenta sale del token.
[ApiController]
[Route("api/v1/veterinario")]
[Authorize(Roles = "Veterinario")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class VeterinarioController : ControllerBase
{
    private readonly ITurnoService _turnos;
    private readonly IConsultaService _consultas;

    public VeterinarioController(ITurnoService turnos, IConsultaService consultas)
    {
        _turnos = turnos;
        _consultas = consultas;
    }

    // GET /api/v1/veterinario/agenda?fecha=2026-11-25 → los turnos del profesional de la cuenta
    [HttpGet("agenda")]
    [ProducesResponseType(typeof(List<TurnoReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Agenda([FromQuery] DateOnly? fecha)
    {
        return Problemas.Desde(await _turnos.AgendaDelVeterinarioAsync(User.CuentaId(), fecha ?? DateOnly.FromDateTime(DateTime.Now)));
    }

    // POST /api/v1/veterinario/turnos/5/consulta → inicia (En espera → En curso) o retoma la consulta del turno
    [HttpPost("turnos/{turnoId:int}/consulta")]
    [ProducesResponseType(typeof(ConsultaReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ConsultaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> IniciarConsulta(int turnoId)
    {
        return Problemas.Desde(await _consultas.IniciarAsync(User.CuentaId(), turnoId));
    }

    // GET /api/v1/veterinario/consultas/abiertas → borradores con el turno En curso (P-RC-04)
    [HttpGet("consultas/abiertas")]
    [ProducesResponseType(typeof(List<ConsultaReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ConsultaReadDto>>> Abiertas()
    {
        return Ok(await _consultas.AbiertasAsync(User.CuentaId()));
    }

    // GET /api/v1/veterinario/consultas/7
    [HttpGet("consultas/{id:int}")]
    [ProducesResponseType(typeof(ConsultaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ObtenerConsulta(int id)
    {
        return Problemas.Desde(await _consultas.ObtenerAsync(User.CuentaId(), id));
    }

    // PUT /api/v1/veterinario/consultas/7 → guarda el borrador con la versión esperada
    [HttpPut("consultas/{id:int}")]
    [ProducesResponseType(typeof(ConsultaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> GuardarConsulta(int id, ConsultaGuardarDto datos)
    {
        return Problemas.Desde(await _consultas.GuardarAsync(User.CuentaId(), id, datos));
    }

    // POST /api/v1/veterinario/consultas/7/insumos → aparta stock por FEFO hasta la firma
    [HttpPost("consultas/{id:int}/insumos")]
    [ProducesResponseType(typeof(ConsultaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> AgregarInsumo(int id, InsumoDto datos)
    {
        return Problemas.Desde(await _consultas.AgregarInsumoAsync(User.CuentaId(), id, datos));
    }

    // DELETE /api/v1/veterinario/consultas/7/insumos/8
    [HttpDelete("consultas/{id:int}/insumos/{articuloId:int}")]
    [ProducesResponseType(typeof(ConsultaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> QuitarInsumo(int id, int articuloId)
    {
        return Problemas.Desde(await _consultas.QuitarInsumoAsync(User.CuentaId(), id, articuloId));
    }

    // POST /api/v1/veterinario/consultas/7/vacunas → vacuna o antiparasitario (RN-15, RN-16)
    [HttpPost("consultas/{id:int}/vacunas")]
    [ProducesResponseType(typeof(ConsultaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> AgregarVacuna(int id, AplicacionDto datos)
    {
        return Problemas.Desde(await _consultas.AgregarVacunaAsync(User.CuentaId(), id, datos));
    }

    // DELETE /api/v1/veterinario/consultas/7/vacunas/3
    [HttpDelete("consultas/{id:int}/vacunas/{aplicacionId:int}")]
    [ProducesResponseType(typeof(ConsultaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> QuitarVacuna(int id, int aplicacionId)
    {
        return Problemas.Desde(await _consultas.QuitarVacunaAsync(User.CuentaId(), id, aplicacionId));
    }

    // POST /api/v1/veterinario/consultas/7/firma → con la contraseña; descuenta stock, genera el cargo y finaliza el turno
    [HttpPost("consultas/{id:int}/firma")]
    [ProducesResponseType(typeof(FirmaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Firmar(int id, ConsultaFirmaDto datos)
    {
        return Problemas.Desde(await _consultas.FirmarAsync(User.CuentaId(), id, datos));
    }

    // POST /api/v1/veterinario/consultas/7/cancelacion → sin atención ni consumos (P-RC-04)
    [HttpPost("consultas/{id:int}/cancelacion")]
    [ProducesResponseType(typeof(MensajeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> CancelarSinAtencion(int id, CancelacionSinAtencionDto datos)
    {
        return Problemas.Desde(await _consultas.CancelarSinAtencionAsync(User.CuentaId(), id, datos.Motivo));
    }

    // POST /api/v1/veterinario/consultas/7/enmiendas → solo el autor, con la contraseña (RN-12)
    [HttpPost("consultas/{id:int}/enmiendas")]
    [ProducesResponseType(typeof(ConsultaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Enmendar(int id, EnmiendaDto datos)
    {
        return Problemas.Desde(await _consultas.EnmendarAsync(User.CuentaId(), id, datos));
    }

    // GET /api/v1/veterinario/animales/3/historia?motivo=... → la lectura queda registrada (RN-40)
    [HttpGet("animales/{animalId:int}/historia")]
    [ProducesResponseType(typeof(HistoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Historia(int animalId, [FromQuery] string motivo = "")
    {
        return Problemas.Desde(await _consultas.HistoriaParaPersonalAsync(User.CuentaId(), animalId, motivo));
    }
}
