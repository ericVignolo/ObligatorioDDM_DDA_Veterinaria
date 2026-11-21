using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Configuración del Administrador (RF-ADM-01 a RF-ADM-04): servicios y tarifas, personal y roles, ausencias,
// feriados, parámetros, stock mínimo y auditoría. Cada cambio queda auditado con la cuenta del token.
[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Administrador")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AdministracionController : ControllerBase
{
    private readonly IAdministracionService _service;

    public AdministracionController(IAdministracionService service)
    {
        _service = service;
    }

    // ---------- Servicios y tarifas ----------

    // GET /api/v1/admin/servicios → todos, también los no publicados
    [HttpGet("servicios")]
    [ProducesResponseType(typeof(List<ServicioAdminDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Servicios() => Ok(await _service.ServiciosAsync());

    // POST /api/v1/admin/servicios → 201; crea también su tarifa inicial
    [HttpPost("servicios")]
    [ProducesResponseType(typeof(ServicioAdminDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> CrearServicio(ServicioFormDto datos)
        => Problemas.Desde(await _service.CrearServicioAsync(User.CuentaId(), datos));

    // PUT /api/v1/admin/servicios/3 → sin el precio: el precio se cambia con una tarifa (RN-33)
    [HttpPut("servicios/{id:int}")]
    [ProducesResponseType(typeof(ServicioAdminDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> EditarServicio(int id, ServicioFormDto datos)
        => Problemas.Desde(await _service.EditarServicioAsync(User.CuentaId(), id, datos));

    // GET /api/v1/admin/servicios/3/tarifas
    [HttpGet("servicios/{id:int}/tarifas")]
    [ProducesResponseType(typeof(List<TarifaReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Tarifas(int id) => Problemas.Desde(await _service.TarifasAsync(id));

    // POST /api/v1/admin/tarifas { prestacionId, precio, vigenteDesde } → desde hoy o una fecha futura
    [HttpPost("tarifas")]
    [ProducesResponseType(typeof(TarifaReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> ProgramarTarifa(TarifaCreateDto datos)
        => Problemas.Desde(await _service.ProgramarTarifaAsync(User.CuentaId(), datos));

    // ---------- Personal ----------

    // GET /api/v1/admin/personal
    [HttpGet("personal")]
    [ProducesResponseType(typeof(List<PersonalReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Personal() => Ok(await _service.PersonalAsync());

    // POST /api/v1/admin/personal → 201
    [HttpPost("personal")]
    [ProducesResponseType(typeof(PersonalReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> CrearPersonal(PersonalCreateDto datos)
        => Problemas.Desde(await _service.CrearPersonalAsync(User.CuentaId(), datos));

    // PUT /api/v1/admin/personal/2/rol { rol, profesionalId } → cierra sus sesiones
    [HttpPut("personal/{id:int}/rol")]
    [ProducesResponseType(typeof(PersonalReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> CambiarRol(int id, CambioRolDto datos)
        => Problemas.Desde(await _service.CambiarRolAsync(User.CuentaId(), id, datos));

    // PUT /api/v1/admin/personal/2/bloqueo { bloquear: true }
    [HttpPut("personal/{id:int}/bloqueo")]
    [ProducesResponseType(typeof(PersonalReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> CambiarBloqueo(int id, BloqueoDto datos)
        => Problemas.Desde(await _service.CambiarBloqueoAsync(User.CuentaId(), id, datos.Bloquear));

    // ---------- Ausencias y feriados ----------

    // GET /api/v1/admin/ausencias?profesionalId=1 → de hoy en adelante
    [HttpGet("ausencias")]
    [ProducesResponseType(typeof(List<AusenciaReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Ausencias([FromQuery] int? profesionalId) => Ok(await _service.AusenciasAsync(profesionalId));

    // POST /api/v1/admin/ausencias → 409 si hay turnos activos en esas fechas
    [HttpPost("ausencias")]
    [ProducesResponseType(typeof(AusenciaReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> AgregarAusencia(AusenciaCreateDto datos)
        => Problemas.Desde(await _service.AgregarAusenciaAsync(User.CuentaId(), datos));

    // DELETE /api/v1/admin/ausencias/4
    [HttpDelete("ausencias/{id:int}")]
    [ProducesResponseType(typeof(OperacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> QuitarAusencia(int id) => Problemas.Desde(await _service.QuitarAusenciaAsync(User.CuentaId(), id));

    // GET /api/v1/admin/feriados → de hoy en adelante
    [HttpGet("feriados")]
    [ProducesResponseType(typeof(List<FeriadoReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Feriados() => Ok(await _service.FeriadosAsync());

    // POST /api/v1/admin/feriados { fecha, motivo }
    [HttpPost("feriados")]
    [ProducesResponseType(typeof(FeriadoReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> AgregarFeriado(FeriadoCreateDto datos)
        => Problemas.Desde(await _service.AgregarFeriadoAsync(User.CuentaId(), datos));

    // DELETE /api/v1/admin/feriados/2026-12-08
    [HttpDelete("feriados/{fecha}")]
    [ProducesResponseType(typeof(OperacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> QuitarFeriado(DateOnly fecha) => Problemas.Desde(await _service.QuitarFeriadoAsync(User.CuentaId(), fecha));

    // ---------- Parámetros, stock mínimo y auditoría ----------

    // GET /api/v1/admin/parametros → con el rango de los que se pueden cambiar
    [HttpGet("parametros")]
    [ProducesResponseType(typeof(List<ParametroReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Parametros() => Ok(await _service.ParametrosAsync());

    // PUT /api/v1/admin/parametros/cobro.porcentaje_sena { valor: "40" }
    [HttpPut("parametros/{clave}")]
    [ProducesResponseType(typeof(ParametroReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CambiarParametro(string clave, ParametroUpdateDto datos)
        => Problemas.Desde(await _service.CambiarParametroAsync(User.CuentaId(), clave, datos));

    // PUT /api/v1/admin/inventario/articulos/8/minimo { stockMinimo: 20 }
    [HttpPut("inventario/articulos/{id:int}/minimo")]
    [ProducesResponseType(typeof(OperacionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> CambiarStockMinimo(int id, StockMinimoDto datos)
        => Problemas.Desde(await _service.CambiarStockMinimoAsync(User.CuentaId(), id, datos));

    // GET /api/v1/admin/auditoria?entidad=Turno&desde=2026-11-01&page=1
    [HttpGet("auditoria")]
    [ProducesResponseType(typeof(PaginaDto<AuditoriaReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> Auditoria([FromQuery] string? entidad, [FromQuery] DateOnly? desde, [FromQuery] int page = 1, [FromQuery] int pageSize = Paginacion.PageSizePorDefecto)
    {
        var error = Paginacion.Validar(page, pageSize);
        if (error != null)
        {
            return BadRequest(Problemas.Crear(400, "Paginación inválida", error, "PAGINACION_INVALIDA"));
        }

        return Ok(await _service.AuditoriaAsync(entidad, desde, page, pageSize));
    }
}
