using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Inicio, renovación y cierre de sesión para clientes y personal (A-008, RNF-SEG-03).
[ApiController]
[Route("api/v1/sesiones")]
[EnableRateLimiting(Problemas.PoliticaCuentas)]
public class SesionesController : ControllerBase
{
    private readonly ISesionService _service;

    public SesionesController(ISesionService service)
    {
        _service = service;
    }

    private string Origen => HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "";

    // POST /api/v1/sesiones → tokens; 401 credenciales, 403 sin verificar, 423 bloqueada
    [HttpPost]
    [ProducesResponseType(typeof(SesionReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status423Locked)]
    public async Task<ActionResult> Iniciar(InicioSesionDto datos)
    {
        return Problemas.Desde(await _service.IniciarAsync(datos, Origen));
    }

    // POST /api/v1/sesiones/refresco → tokens nuevos; el de refresco usado deja de servir
    [HttpPost("refresco")]
    [ProducesResponseType(typeof(SesionReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Refrescar(RefrescoDto datos)
    {
        return Problemas.Desde(await _service.RefrescarAsync(datos.TokenRefresco, Origen));
    }

    // DELETE /api/v1/sesiones (con el token de refresco en el cuerpo) → 204
    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> Cerrar([FromBody] RefrescoDto datos)
    {
        await _service.CerrarAsync(datos.TokenRefresco);
        return NoContent();
    }
}
