using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Equipo profesional del área pública: lectura anónima (RF-VIS-02).
[ApiController]
[Route("api/v1/profesionales")]
public class ProfesionalesController : ControllerBase
{
    private readonly IProfesionalService _service;

    public ProfesionalesController(IProfesionalService service)
    {
        _service = service;
    }

    // GET /api/v1/profesionales?page=1&pageSize=20
    [HttpGet]
    [ProducesResponseType(typeof(PaginaDto<ProfesionalReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PaginaDto<ProfesionalReadDto>>> Listar(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paginacion.PageSizePorDefecto)
    {
        var error = Paginacion.Validar(page, pageSize);
        if (error != null)
        {
            return BadRequest(Problemas.Crear(400, "Paginación inválida", error, "PAGINACION_INVALIDA"));
        }

        return Ok(await _service.ListarPublicosAsync(page, pageSize));
    }

    // GET /api/v1/profesionales/1 → con el horario semanal por día
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProfesionalDetalleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProfesionalDetalleDto>> Obtener(int id)
    {
        var profesional = await _service.ObtenerPublicoAsync(id);
        if (profesional == null)
        {
            return NotFound(Problemas.Crear(404, "Profesional no disponible", "El profesional no existe o no está disponible.", "PROFESIONAL_NO_ENCONTRADO"));
        }

        return Ok(profesional);
    }
}
