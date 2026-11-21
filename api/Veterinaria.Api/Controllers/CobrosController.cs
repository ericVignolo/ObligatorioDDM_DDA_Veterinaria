using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Cargos del cliente (A-023, RN-18): lo que debe, con su estado. Se paga en Recepción (DT-ALC-02).
[ApiController]
[Route("api/v1/cobros")]
[Authorize(Roles = "Cliente")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class CobrosController : ControllerBase
{
    private readonly ICobroService _service;

    public CobrosController(ICobroService service)
    {
        _service = service;
    }

    // GET /api/v1/cobros?page=1
    [HttpGet]
    [ProducesResponseType(typeof(PaginaDto<CargoReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> MisCargos([FromQuery] int page = 1, [FromQuery] int pageSize = Paginacion.PageSizePorDefecto)
    {
        var error = Paginacion.Validar(page, pageSize);
        if (error != null)
        {
            return BadRequest(Problemas.Crear(400, "Paginación inválida", error, "PAGINACION_INVALIDA"));
        }

        return Ok(await _service.MisCargosAsync(User.CuentaId(), page, pageSize));
    }
}

// Caja de Recepción (RF-REC-05): cargos, cobro con comprobante numerado, comprobantes del día y deuda (RN-36).
// Ver es de Recepción y Administración; cobrar, solo de Recepción.
[ApiController]
[Route("api/v1/recepcion/cobros")]
[Authorize(Roles = "Recepcionista,Administrador")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class CajaController : ControllerBase
{
    private readonly ICobroService _service;

    public CajaController(ICobroService service)
    {
        _service = service;
    }

    // GET /api/v1/recepcion/cobros/cargos?clienteId=&pendientes=true&page=1
    [HttpGet("cargos")]
    [ProducesResponseType(typeof(PaginaDto<CargoReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Cargos([FromQuery] int? clienteId, [FromQuery] bool pendientes = true, [FromQuery] int page = 1, [FromQuery] int pageSize = Paginacion.PageSizePorDefecto)
    {
        var error = Paginacion.Validar(page, pageSize);
        if (error != null)
        {
            return BadRequest(Problemas.Crear(400, "Paginación inválida", error, "PAGINACION_INVALIDA"));
        }

        return Ok(await _service.CargosAsync(clienteId, pendientes, page, pageSize));
    }

    // GET /api/v1/recepcion/cobros/cargos/7 → con sus comprobantes
    [HttpGet("cargos/{id:int}")]
    [ProducesResponseType(typeof(CargoReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Cargo(int id)
    {
        return Problemas.Desde(await _service.CargoAsync(id));
    }

    // POST /api/v1/recepcion/cobros/pagos { cargoId, monto, medio, referencia } → 201 con el comprobante
    [HttpPost("pagos")]
    [Authorize(Roles = "Recepcionista")]
    [ProducesResponseType(typeof(CobroRegistradoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Cobrar(CobroCreateDto datos)
    {
        return Problemas.Desde(await _service.CobrarAsync(User.CuentaId(), datos));
    }

    // GET /api/v1/recepcion/cobros/pagos?fecha=2026-11-23 → comprobantes del día
    [HttpGet("pagos")]
    [ProducesResponseType(typeof(List<PagoReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Comprobantes([FromQuery] DateOnly? fecha)
    {
        return Ok(await _service.ComprobantesDelDiaAsync(fecha ?? DateOnly.FromDateTime(DateTime.Now)));
    }

    // GET /api/v1/recepcion/cobros/pagos/3
    [HttpGet("pagos/{id:int}")]
    [ProducesResponseType(typeof(PagoReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Comprobante(int id)
    {
        return Problemas.Desde(await _service.ComprobanteAsync(id));
    }

    // GET /api/v1/recepcion/cobros/resumen?fecha=
    [HttpGet("resumen")]
    [ProducesResponseType(typeof(ResumenCajaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> Resumen([FromQuery] DateOnly? fecha)
    {
        return Ok(await _service.ResumenAsync(fecha ?? DateOnly.FromDateTime(DateTime.Now)));
    }

    // GET /api/v1/recepcion/cobros/deuda/10 → aviso al agendar (RN-36), nunca bloquea
    [HttpGet("deuda/{clienteId:int}")]
    [ProducesResponseType(typeof(DeudaClienteDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> Deuda(int clienteId)
    {
        return Ok(await _service.DeudaAsync(clienteId));
    }
}

// Anulación de comprobantes con nota de crédito (RN-34): solo el Administrador, con motivo.
[ApiController]
[Route("api/v1/admin/cobros")]
[Authorize(Roles = "Administrador")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AdminCobrosController : ControllerBase
{
    private readonly ICobroService _service;

    public AdminCobrosController(ICobroService service)
    {
        _service = service;
    }

    // POST /api/v1/admin/cobros/pagos/3/anulacion { motivo } → 201 con la nota de crédito
    [HttpPost("pagos/{id:int}/anulacion")]
    [ProducesResponseType(typeof(AnulacionRegistradaDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Anular(int id, AnulacionDto datos)
    {
        return Problemas.Desde(await _service.AnularAsync(User.CuentaId(), id, datos));
    }

    // GET /api/v1/admin/cobros/notas
    [HttpGet("notas")]
    [ProducesResponseType(typeof(List<NotaCreditoReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Notas()
    {
        return Ok(await _service.NotasAsync());
    }
}
