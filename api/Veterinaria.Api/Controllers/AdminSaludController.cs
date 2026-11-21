using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Tareas del Administrador sobre avisos de refuerzo y textos de consentimiento (RF-CLI-07, RN-10).
[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Administrador")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class AdminSaludController : ControllerBase
{
    private readonly ICarnetService _carnet;
    private readonly IConsentimientoService _consentimientos;

    public AdminSaludController(ICarnetService carnet, IConsentimientoService consentimientos)
    {
        _carnet = carnet;
        _consentimientos = consentimientos;
    }

    // POST /api/v1/admin/avisos-refuerzo → encola los avisos de los refuerzos próximos (se puede repetir sin duplicar)
    [HttpPost("avisos-refuerzo")]
    [ProducesResponseType(typeof(AvisosGeneradosDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> AvisosRefuerzo()
    {
        return Ok(await _carnet.GenerarAvisosAsync());
    }

    // GET /api/v1/admin/consentimientos?prestacionId=4 → versiones del texto, la vigente primero
    [HttpGet("consentimientos")]
    [ProducesResponseType(typeof(List<DocumentoConsentimientoReadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Documentos([FromQuery] int prestacionId)
    {
        return Problemas.Desde(await _consentimientos.DocumentosAsync(prestacionId));
    }

    // POST /api/v1/admin/consentimientos → publica una versión nueva; la anterior deja de estar vigente
    [HttpPost("consentimientos")]
    [ProducesResponseType(typeof(DocumentoConsentimientoReadDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Publicar(DocumentoConsentimientoCreateDto datos)
    {
        return Problemas.Desde(await _consentimientos.PublicarAsync(datos));
    }
}
