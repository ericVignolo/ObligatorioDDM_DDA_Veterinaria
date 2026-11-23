using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Verificación en dos pasos de la cuenta propia (RNF-SEG-04): cualquiera la puede activar; el Administrador está
// obligado y no la puede desactivar. Se usa también sin el segundo factor, para poder activarlo.
[ApiController]
[Route("api/v1/cuentas/yo/segundo-factor")]
[Authorize]
[PermitirSinSegundoFactor]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class SegundoFactorController : ControllerBase
{
    private readonly ISegundoFactorService _service;

    public SegundoFactorController(ISegundoFactorService service)
    {
        _service = service;
    }

    // GET /api/v1/cuentas/yo/segundo-factor → { activo, obligatorio }
    [HttpGet]
    [ProducesResponseType(typeof(EstadoSegundoFactorDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> Estado() => Problemas.Desde(await _service.EstadoAsync(User.CuentaId()));

    // POST /api/v1/cuentas/yo/segundo-factor/configuracion → clave y enlace otpauth:// para el código QR
    [HttpPost("configuracion")]
    [ProducesResponseType(typeof(ConfiguracionSegundoFactorDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Configurar() => Problemas.Desde(await _service.ConfigurarAsync(User.CuentaId()));

    // POST /api/v1/cuentas/yo/segundo-factor/activacion { codigo } → cierra las sesiones; se vuelve a ingresar
    [HttpPost("activacion")]
    [ProducesResponseType(typeof(MensajeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Activar(CodigoSegundoFactorDto datos) => Problemas.Desde(await _service.ActivarAsync(User.CuentaId(), datos.Codigo));

    // POST /api/v1/cuentas/yo/segundo-factor/desactivacion { contrasena, codigo } → no para el Administrador
    [HttpPost("desactivacion")]
    [ProducesResponseType(typeof(MensajeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Desactivar(DesactivacionSegundoFactorDto datos) => Problemas.Desde(await _service.DesactivarAsync(User.CuentaId(), datos));
}
