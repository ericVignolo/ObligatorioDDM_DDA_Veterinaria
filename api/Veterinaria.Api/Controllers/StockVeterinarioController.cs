using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Lo que el veterinario puede registrar en la consulta: insumos y productos sanitarios con stock (RN-26, RN-27).
[ApiController]
[Route("api/v1/veterinario")]
[Authorize(Roles = "Veterinario")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class StockVeterinarioController : ControllerBase
{
    private readonly IInventarioService _service;

    public StockVeterinarioController(IInventarioService service)
    {
        _service = service;
    }

    // GET /api/v1/veterinario/insumos
    [HttpGet("insumos")]
    [ProducesResponseType(typeof(List<InsumoDisponibleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Insumos()
    {
        return Ok(await _service.InsumosParaConsultaAsync());
    }

    // GET /api/v1/veterinario/productos-sanitarios?animalId=3 → los de su especie, con el lote que se usaría (FEFO)
    [HttpGet("productos-sanitarios")]
    [ProducesResponseType(typeof(List<ProductoDisponibleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Productos([FromQuery] int animalId)
    {
        return Problemas.Desde(await _service.ProductosParaAnimalAsync(animalId));
    }
}
