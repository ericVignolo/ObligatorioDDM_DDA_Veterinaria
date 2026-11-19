using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Catálogo público: lectura anónima (ESPECIFICACION_CATALOGO_SERVICIOS_API.md).
[ApiController]
[Route("api/v1/servicios")]
public class ServiciosController : ControllerBase
{
    private readonly IPrestacionService _service;

    public ServiciosController(IPrestacionService service)
    {
        _service = service;
    }

    // GET /api/v1/servicios?page=1&pageSize=20
    [HttpGet]
    public async Task<ActionResult<PaginaDto<ServicioReadDto>>> Listar(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paginacion.PageSizePorDefecto)
    {
        var error = Paginacion.Validar(page, pageSize);
        if (error != null)
        {
            return BadRequest(Problema(400, "Paginación inválida", error, "PAGINACION_INVALIDA"));
        }

        return Ok(await _service.ListarPublicasAsync(page, pageSize));
    }

    // GET /api/v1/servicios/5
    [HttpGet("{id:int}")]
    public async Task<ActionResult<ServicioReadDto>> Obtener(int id)
    {
        var servicio = await _service.ObtenerPublicaAsync(id);
        if (servicio == null)
        {
            return NotFound(Problema(404, "Servicio no disponible", "El servicio no existe o no está disponible.", "SERVICIO_NO_ENCONTRADO"));
        }

        return Ok(servicio);
    }

    // Errores en formato Problem Details con un código propio estable (D-CAT-03).
    private static ProblemDetails Problema(int status, string titulo, string detalle, string codigo)
    {
        var problema = new ProblemDetails
        {
            Status = status,
            Title = titulo,
            Detail = detalle,
            Type = $"https://vitalia.uy/problemas/{codigo.ToLowerInvariant().Replace('_', '-')}"
        };
        problema.Extensions["code"] = codigo;
        return problema;
    }
}
