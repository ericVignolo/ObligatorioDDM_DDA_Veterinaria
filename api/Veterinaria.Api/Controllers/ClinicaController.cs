using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Datos públicos de la clínica: lectura anónima (RF-VIS-03, RF-VIS-06).
[ApiController]
[Route("api/v1/clinica")]
public class ClinicaController : ControllerBase
{
    private readonly IClinicaService _service;

    public ClinicaController(IClinicaService service)
    {
        _service = service;
    }

    // GET /api/v1/clinica
    [HttpGet]
    [ProducesResponseType(typeof(ClinicaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClinicaReadDto>> Obtener()
    {
        var clinica = await _service.ObtenerAsync();
        if (clinica == null)
        {
            return NotFound(Problemas.Crear(404, "Datos no disponibles", "Todavía no se cargaron los datos de la clínica.", "CLINICA_NO_CONFIGURADA"));
        }

        return Ok(clinica);
    }
}
