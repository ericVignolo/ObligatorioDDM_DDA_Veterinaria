using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Inventario del Administrador (A-022, RF-ADM-05, RN-26 a RN-31): stock por artículo y lote, compras, ajustes y
// bajas con motivo, historial de movimientos, alertas de stock mínimo y baja de vencidos.
[ApiController]
[Route("api/v1/admin/inventario")]
[Authorize(Roles = "Administrador")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class InventarioController : ControllerBase
{
    private readonly IInventarioService _service;

    public InventarioController(IInventarioService service)
    {
        _service = service;
    }

    // GET /api/v1/admin/inventario/articulos
    [HttpGet("articulos")]
    [ProducesResponseType(typeof(List<ArticuloStockDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Articulos()
    {
        return Ok(await _service.ArticulosAsync());
    }

    // GET /api/v1/admin/inventario/articulos/1/lotes
    [HttpGet("articulos/{id:int}/lotes")]
    [ProducesResponseType(typeof(List<LoteReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Lotes(int id)
    {
        return Problemas.Desde(await _service.LotesAsync(id));
    }

    // GET /api/v1/admin/inventario/movimientos?articuloId=1&page=1
    [HttpGet("movimientos")]
    [ProducesResponseType(typeof(PaginaDto<MovimientoReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Movimientos([FromQuery] int? articuloId, [FromQuery] int page = 1, [FromQuery] int pageSize = Paginacion.PageSizePorDefecto)
    {
        var error = Paginacion.Validar(page, pageSize);
        if (error != null)
        {
            return BadRequest(Problemas.Crear(400, "Paginación inválida", error, "PAGINACION_INVALIDA"));
        }

        return Ok(await _service.MovimientosAsync(articuloId, page, pageSize));
    }

    // POST /api/v1/admin/inventario/compras → 201 con el lote nuevo
    [HttpPost("compras")]
    [ProducesResponseType(typeof(LoteReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Compra(CompraDto datos)
    {
        return Problemas.Desde(await _service.RegistrarCompraAsync(User.CuentaId(), datos));
    }

    // POST /api/v1/admin/inventario/ajustes { loteId, cantidad (+/-), motivo }
    [HttpPost("ajustes")]
    [ProducesResponseType(typeof(LoteReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Ajuste(AjusteDto datos)
    {
        return Problemas.Desde(await _service.AjustarAsync(User.CuentaId(), datos));
    }

    // POST /api/v1/admin/inventario/bajas { loteId, cantidad (> 0), motivo }
    [HttpPost("bajas")]
    [ProducesResponseType(typeof(LoteReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Baja(AjusteDto datos)
    {
        return Problemas.Desde(await _service.DarDeBajaAsync(User.CuentaId(), datos));
    }

    // POST /api/v1/admin/inventario/vencidos → inhabilita y da de baja los lotes vencidos (RN-29)
    [HttpPost("vencidos")]
    [ProducesResponseType(typeof(VencidosProcesadosDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> Vencidos()
    {
        return Ok(await _service.ProcesarVencidosAsync());
    }

    // GET /api/v1/admin/inventario/alertas?pendientes=true
    [HttpGet("alertas")]
    [ProducesResponseType(typeof(List<AlertaStockReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Alertas([FromQuery] bool pendientes = true)
    {
        return Ok(await _service.AlertasAsync(pendientes));
    }

    // POST /api/v1/admin/inventario/alertas/4/atendida
    [HttpPost("alertas/{id:int}/atendida")]
    [ProducesResponseType(typeof(AlertaStockReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Atendida(int id)
    {
        return Problemas.Desde(await _service.MarcarAlertaAtendidaAsync(id));
    }
}
