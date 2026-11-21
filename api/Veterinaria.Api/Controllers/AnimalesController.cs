using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Animales del cliente (A-012, A-013, RF-CLI-01). Solo con token de una cuenta con rol Cliente.
// La cuenta sale del token: el cliente solo ve y cambia sus animales (RN-18, RNF-SEG-05).
[ApiController]
[Route("api/v1/animales")]
[Authorize(Roles = "Cliente")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AnimalesController : ControllerBase
{
    private readonly IAnimalService _service;
    private readonly IConsultaService _consultas;

    public AnimalesController(IAnimalService service, IConsultaService consultas)
    {
        _service = service;
        _consultas = consultas;
    }

    private int CuentaId => int.TryParse(User.FindFirst(TokenService.ClaimId)?.Value, out var id) ? id : 0;

    // GET /api/v1/animales?page=1&pageSize=20
    [HttpGet]
    [ProducesResponseType(typeof(PaginaDto<AnimalReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Listar([FromQuery] int page = 1, [FromQuery] int pageSize = Paginacion.PageSizePorDefecto)
    {
        var error = Paginacion.Validar(page, pageSize);
        if (error != null)
        {
            return BadRequest(Problemas.Crear(400, "Paginación inválida", error, "PAGINACION_INVALIDA"));
        }

        return Ok(await _service.ListarAsync(CuentaId, page, pageSize));
    }

    // GET /api/v1/animales/5 → 404 también si es de otro cliente
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(AnimalReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Obtener(int id)
    {
        return Problemas.Desde(await _service.ObtenerAsync(id, CuentaId));
    }

    // POST /api/v1/animales → 201; 409 si el microchip ya existe
    [HttpPost]
    [ProducesResponseType(typeof(AnimalReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Crear(AnimalCreateDto datos)
    {
        return Problemas.Desde(await _service.CrearAsync(datos, CuentaId));
    }

    // PUT /api/v1/animales/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(AnimalReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Editar(int id, AnimalUpdateDto datos)
    {
        return Problemas.Desde(await _service.EditarAsync(id, datos, CuentaId));
    }

    // DELETE /api/v1/animales/5 → baja lógica; 409 si tiene turnos pendientes
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(MensajeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> DarDeBaja(int id)
    {
        return Problemas.Desde(await _service.DarDeBajaAsync(id, CuentaId));
    }

    // GET /api/v1/animales/5/historia → consultas firmadas con sus enmiendas y vacunas (solo lectura)
    [HttpGet("{id:int}/historia")]
    [ProducesResponseType(typeof(HistoriaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Historia(int id)
    {
        return Problemas.Desde(await _consultas.HistoriaParaClienteAsync(CuentaId, id));
    }
}
