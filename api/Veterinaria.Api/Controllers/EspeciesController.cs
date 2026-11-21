using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Especies configuradas, para los formularios de animales y de asistencia. Lectura anónima.
[ApiController]
[Route("api/v1/especies")]
public class EspeciesController : ControllerBase
{
    private readonly IAnimalService _service;

    public EspeciesController(IAnimalService service)
    {
        _service = service;
    }

    // GET /api/v1/especies
    [HttpGet]
    [ProducesResponseType(typeof(List<EspecieReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EspecieReadDto>>> Listar()
    {
        return Ok(await _service.ListarEspeciesAsync());
    }
}
