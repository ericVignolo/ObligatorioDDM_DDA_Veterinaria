using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Internación para el personal (A-024, RF-VET-09 a RF-VET-11): Recepción admite o anula; el Veterinario ingresa
// con box y plan, publica novedades y da el alta. Todos los del personal ven el tablero.
[ApiController]
[Route("api/v1/internaciones")]
[Authorize(Roles = "Recepcionista,Veterinario,Administrador")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class InternacionesController : ControllerBase
{
    private readonly IInternacionService _service;

    public InternacionesController(IInternacionService service)
    {
        _service = service;
    }

    // GET /api/v1/internaciones → abiertas, primero las que esperan el ingreso clínico
    [HttpGet]
    [ProducesResponseType(typeof(List<InternacionReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Abiertas() => Ok(await _service.AbiertasAsync());

    // GET /api/v1/internaciones/altas → las últimas 50
    [HttpGet("altas")]
    [ProducesResponseType(typeof(List<InternacionReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Altas() => Ok(await _service.AltasAsync());

    // GET /api/v1/internaciones/3 → el veterinario ve también las notas internas (RN-23)
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InternacionReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Detalle(int id) => Problemas.Desde(await _service.DetalleAsync(id, User.IsInRole("Veterinario")));

    // GET /api/v1/internaciones/boxes → con el animal que ocupa cada uno
    [HttpGet("boxes")]
    [ProducesResponseType(typeof(List<BoxReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Boxes() => Ok(await _service.BoxesAsync());

    // GET /api/v1/internaciones/3/boxes-libres → los que sirven para la especie del animal
    [HttpGet("{id:int}/boxes-libres")]
    [ProducesResponseType(typeof(List<BoxReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> BoxesLibres(int id) => Problemas.Desde(await _service.BoxesLibresAsync(id));

    // POST /api/v1/internaciones { animalId, motivo, telefonoContacto, observaciones } → 201 (Admitida)
    [HttpPost]
    [Authorize(Roles = "Recepcionista")]
    [ProducesResponseType(typeof(InternacionReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Admitir(AdmisionDto datos) => Problemas.Desde(await _service.AdmitirAsync(User.CuentaId(), datos));

    // POST /api/v1/internaciones/3/anulacion { motivo } → solo si todavía no tiene ingreso clínico
    [HttpPost("{id:int}/anulacion")]
    [Authorize(Roles = "Recepcionista")]
    [ProducesResponseType(typeof(InternacionReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Anular(int id, AnulacionAdmisionDto datos) => Problemas.Desde(await _service.AnularAdmisionAsync(User.CuentaId(), id, datos));

    // POST /api/v1/internaciones/3/ingreso { boxId, planTerapeutico }
    [HttpPost("{id:int}/ingreso")]
    [Authorize(Roles = "Veterinario")]
    [ProducesResponseType(typeof(InternacionReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Ingresar(int id, IngresoClinicoDto datos) => Problemas.Desde(await _service.IngresarAsync(User.CuentaId(), id, datos));

    // POST /api/v1/internaciones/3/novedades { tipo: "Novedad"|"Evolución", texto, visible }
    [HttpPost("{id:int}/novedades")]
    [Authorize(Roles = "Veterinario")]
    [ProducesResponseType(typeof(InternacionReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Publicar(int id, NovedadCreateDto datos) => Problemas.Desde(await _service.PublicarAsync(User.CuentaId(), id, datos));

    // POST /api/v1/internaciones/3/novedades/7/rectificacion { texto } → la original se conserva (RN-24)
    [HttpPost("{id:int}/novedades/{novedadId:int}/rectificacion")]
    [Authorize(Roles = "Veterinario")]
    [ProducesResponseType(typeof(InternacionReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Rectificar(int id, int novedadId, RectificacionDto datos)
        => Problemas.Desde(await _service.RectificarAsync(User.CuentaId(), id, novedadId, datos));

    // POST /api/v1/internaciones/3/alta { evolucionFinal, indicacionesDomicilio } (RN-25)
    [HttpPost("{id:int}/alta")]
    [Authorize(Roles = "Veterinario")]
    [ProducesResponseType(typeof(InternacionReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Alta(int id, AltaDto datos) => Problemas.Desde(await _service.DarAltaAsync(User.CuentaId(), id, datos));
}

// Internaciones de los animales del cliente (RF-CLI-09, RN-18): solo lo publicado como visible y el alta.
[ApiController]
[Route("api/v1/mis-internaciones")]
[Authorize(Roles = "Cliente")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class MisInternacionesController : ControllerBase
{
    private readonly IInternacionService _service;

    public MisInternacionesController(IInternacionService service)
    {
        _service = service;
    }

    // GET /api/v1/mis-internaciones
    [HttpGet]
    [ProducesResponseType(typeof(List<InternacionReadDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult> Mias() => Ok(await _service.MiasAsync(User.CuentaId()));

    // GET /api/v1/mis-internaciones/3 → con las novedades visibles
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(InternacionReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Mia(int id) => Problemas.Desde(await _service.MiaAsync(User.CuentaId(), id));
}

// Boxes (RF-ADM-03): solo el Administrador.
[ApiController]
[Route("api/v1/admin/boxes")]
[Authorize(Roles = "Administrador")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class BoxesController : ControllerBase
{
    private readonly IInternacionService _service;

    public BoxesController(IInternacionService service)
    {
        _service = service;
    }

    // POST /api/v1/admin/boxes { nombre, tipo: "Pequeños animales"|"Grandes animales" } → 201
    [HttpPost]
    [ProducesResponseType(typeof(BoxReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Agregar(BoxCreateDto datos) => Problemas.Desde(await _service.AgregarBoxAsync(User.CuentaId(), datos));

    // PUT /api/v1/admin/boxes/2/servicio { activo: false } → 409 si está ocupado
    [HttpPut("{id:int}/servicio")]
    [ProducesResponseType(typeof(BoxReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> CambiarServicio(int id, BoxServicioDto datos) => Problemas.Desde(await _service.CambiarServicioBoxAsync(User.CuentaId(), id, datos.Activo));
}
