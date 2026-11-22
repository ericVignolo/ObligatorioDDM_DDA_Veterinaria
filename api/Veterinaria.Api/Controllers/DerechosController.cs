using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Derechos del titular (A-028, RF-CLI-19, RNF-LEG-01/02/06). Cada cliente ve y pide solo lo suyo (RN-18).
[ApiController]
[Route("api/v1/privacidad")]
[Authorize(Roles = "Cliente")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class PrivacidadController : ControllerBase
{
    private readonly IDerechosService _service;

    public PrivacidadController(IDerechosService service)
    {
        _service = service;
    }

    // GET /api/v1/privacidad/politicas → qué se guarda, para qué, quién accede y cuánto se conserva (pública)
    [HttpGet("politicas")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<PoliticaConservacionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Politicas() => Ok(await _service.PoliticasAsync());

    // GET /api/v1/privacidad/exportacion → archivo JSON con mis datos (queda registrado)
    [HttpGet("exportacion")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Exportar()
    {
        var resultado = await _service.ExportarAsync(User.CuentaId());
        if (!resultado.Exito)
        {
            return Problemas.Desde(resultado);
        }

        return File(Encoding.UTF8.GetBytes(resultado.Valor!), "application/json", "mis-datos-vitalia.json");
    }

    // GET /api/v1/privacidad/solicitudes
    [HttpGet("solicitudes")]
    [ProducesResponseType(typeof(List<SolicitudDerechoReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Solicitudes() => Ok(await _service.SolicitudesAsync(User.CuentaId()));

    // GET /api/v1/privacidad/obstaculos → qué falta resolver antes de pedir la eliminación
    [HttpGet("obstaculos")]
    [ProducesResponseType(typeof(ObstaculosDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> Obstaculos() => Ok(await _service.ObstaculosAsync(User.CuentaId()));

    // POST /api/v1/privacidad/eliminacion { confirmado: true } → 201; la resuelve el Administrador
    [HttpPost("eliminacion")]
    [ProducesResponseType(typeof(SolicitudDerechoReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> PedirEliminacion(PedidoEliminacionDto datos) => Problemas.Desde(await _service.PedirEliminacionAsync(User.CuentaId(), datos.Confirmado));

    // POST /api/v1/privacidad/solicitudes/3/cancelacion
    [HttpPost("solicitudes/{id:int}/cancelacion")]
    [ProducesResponseType(typeof(SolicitudDerechoReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Cancelar(int id) => Problemas.Desde(await _service.CancelarAsync(User.CuentaId(), id));
}

// Pedidos de eliminación para el Administrador.
[ApiController]
[Route("api/v1/admin/derechos")]
[Authorize(Roles = "Administrador")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AdminDerechosController : ControllerBase
{
    private readonly IDerechosService _service;

    public AdminDerechosController(IDerechosService service)
    {
        _service = service;
    }

    // GET /api/v1/admin/derechos
    [HttpGet]
    [ProducesResponseType(typeof(List<SolicitudDerechoReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Todas() => Ok(await _service.TodasAsync());

    // POST /api/v1/admin/derechos/3/resolucion { aprobar, motivo } → aprobar anonimiza la cuenta
    [HttpPost("{id:int}/resolucion")]
    [ProducesResponseType(typeof(SolicitudDerechoReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Resolver(int id, ResolucionDerechoDto datos) => Problemas.Desde(await _service.ResolverAsync(User.CuentaId(), id, datos));
}
