using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Bandeja y preferencias del cliente (RF-CLI-18, RF-CLI-20). Cada cliente ve solo sus mensajes (RN-18).
[ApiController]
[Route("api/v1/comunicaciones")]
[Authorize(Roles = "Cliente")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class ComunicacionesController : ControllerBase
{
    private readonly IComunicacionService _service;

    public ComunicacionesController(IComunicacionService service)
    {
        _service = service;
    }

    // GET /api/v1/comunicaciones?page=1 → con la cantidad de no leídas
    [HttpGet]
    [ProducesResponseType(typeof(BandejaDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Bandeja([FromQuery] int page = 1, [FromQuery] int pageSize = Paginacion.PageSizePorDefecto)
    {
        var error = Paginacion.Validar(page, pageSize);
        if (error != null)
        {
            return BadRequest(Problemas.Crear(400, "Paginación inválida", error, "PAGINACION_INVALIDA"));
        }

        return Ok(await _service.BandejaAsync(User.CuentaId(), page, pageSize));
    }

    // POST /api/v1/comunicaciones/12/leida
    [HttpPost("{id:long}/leida")]
    [ProducesResponseType(typeof(ComunicacionReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Leida(long id) => Problemas.Desde(await _service.MarcarLeidaAsync(User.CuentaId(), id));

    // GET /api/v1/comunicaciones/preferencias
    [HttpGet("preferencias")]
    [ProducesResponseType(typeof(List<PreferenciaReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Preferencias() => Ok(await _service.PreferenciasAsync(User.CuentaId()));

    // PUT /api/v1/comunicaciones/preferencias/4 { canal: "Correo" | "Solo en la app" | "No recibir" }
    [HttpPut("preferencias/{tipoId:int}")]
    [ProducesResponseType(typeof(PreferenciaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CambiarPreferencia(int tipoId, PreferenciaUpdateDto datos)
        => Problemas.Desde(await _service.CambiarPreferenciaAsync(User.CuentaId(), tipoId, datos.Canal));
}

// Bandeja de salida del personal (RF-REC-07, RNF-CON-04): buscar, procesar la cola, reintentar, reenviar,
// enviar comprobantes y avisos de la clínica.
[ApiController]
[Route("api/v1/recepcion/comunicaciones")]
[Authorize(Roles = "Recepcionista,Administrador")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class SalidaComunicacionesController : ControllerBase
{
    private readonly IComunicacionService _service;

    public SalidaComunicacionesController(IComunicacionService service)
    {
        _service = service;
    }

    private string Rol => User.RolEntre("Recepcionista", "Administrador");

    // GET /api/v1/recepcion/comunicaciones?estado=Fallido&tipoId=&texto=&page=1
    [HttpGet]
    [ProducesResponseType(typeof(PaginaDto<ComunicacionReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Buscar([FromQuery] string? estado, [FromQuery] int? tipoId, [FromQuery] string? texto,
        [FromQuery] int page = 1, [FromQuery] int pageSize = Paginacion.PageSizePorDefecto)
    {
        var error = Paginacion.Validar(page, pageSize);
        if (error != null)
        {
            return BadRequest(Problemas.Crear(400, "Paginación inválida", error, "PAGINACION_INVALIDA"));
        }

        return Ok(await _service.SalidaAsync(estado, tipoId, texto, page, pageSize));
    }

    // POST /api/v1/recepcion/comunicaciones/cola → intenta los pendientes
    [HttpPost("cola")]
    [ProducesResponseType(typeof(ColaProcesadaDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> ProcesarCola() => Ok(await _service.ProcesarColaAsync());

    // POST /api/v1/recepcion/comunicaciones/12/reintento → solo si quedó Fallido
    [HttpPost("{id:long}/reintento")]
    [ProducesResponseType(typeof(ComunicacionReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<ActionResult> Reintentar(long id) => Problemas.Desde(await _service.ReintentarAsync(User.CuentaId(), Rol, id));

    // POST /api/v1/recepcion/comunicaciones/12/reenvio → copia nueva por correo
    [HttpPost("{id:long}/reenvio")]
    [ProducesResponseType(typeof(ComunicacionReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Reenviar(long id) => Problemas.Desde(await _service.ReenviarAsync(User.CuentaId(), Rol, id));

    // POST /api/v1/recepcion/comunicaciones/comprobantes/3 → envía el comprobante del pago 3
    [HttpPost("comprobantes/{pagoId:int}")]
    [ProducesResponseType(typeof(ComunicacionReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Comprobante(int pagoId) => Problemas.Desde(await _service.EnviarComprobanteAsync(User.CuentaId(), Rol, pagoId));

    // POST /api/v1/recepcion/comunicaciones/avisos { titulo, mensaje } → aviso de la clínica a todos los clientes
    [HttpPost("avisos")]
    [ProducesResponseType(typeof(DifusionResultadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Aviso(DifusionDto datos) => Problemas.Desde(await _service.AvisoOperativoAsync(User.CuentaId(), Rol, datos));
}

// Alerta sanitaria (Administrador o Veterinario): a los clientes con animales de una especie que la activaron.
[ApiController]
[Route("api/v1/difusion/alerta-sanitaria")]
[Authorize(Roles = "Administrador,Veterinario")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AlertaSanitariaController : ControllerBase
{
    private readonly IComunicacionService _service;

    public AlertaSanitariaController(IComunicacionService service)
    {
        _service = service;
    }

    // POST /api/v1/difusion/alerta-sanitaria { titulo, mensaje, especieId }
    [HttpPost]
    [ProducesResponseType(typeof(DifusionResultadoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Emitir(DifusionDto datos)
        => Problemas.Desde(await _service.AlertaSanitariaAsync(User.CuentaId(), User.RolEntre("Administrador", "Veterinario"), datos));
}
