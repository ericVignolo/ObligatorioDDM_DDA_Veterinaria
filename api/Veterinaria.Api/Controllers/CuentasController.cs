using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Registro, verificación y recuperación (ESPECIFICACION_REGISTRO_CLIENTE.md). Límite de pedidos por origen (RNF-SEG-09).
[ApiController]
[Route("api/v1/cuentas")]
[EnableRateLimiting(Problemas.PoliticaCuentas)]
public class CuentasController : ControllerBase
{
    private readonly ICuentaService _service;

    public CuentasController(ICuentaService service)
    {
        _service = service;
    }

    // POST /api/v1/cuentas → 201; 409 si el correo o el documento ya tienen cuenta
    [HttpPost]
    [ProducesResponseType(typeof(RespuestaCodigoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Registrar(RegistroCreateDto datos)
    {
        return Problemas.Desde(await _service.RegistrarAsync(datos));
    }

    // POST /api/v1/cuentas/verificacion
    [HttpPost("verificacion")]
    [ProducesResponseType(typeof(MensajeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Verificar(VerificacionDto datos)
    {
        return Problemas.Desde(await _service.VerificarAsync(datos));
    }

    // POST /api/v1/cuentas/verificacion/reenvio
    [HttpPost("verificacion/reenvio")]
    [ProducesResponseType(typeof(RespuestaCodigoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult> Reenviar(CorreoDto datos)
    {
        return Problemas.Desde(await _service.ReenviarCodigoAsync(datos.Correo));
    }

    // POST /api/v1/cuentas/recuperacion → siempre 200 con el mismo mensaje (D-REG-03)
    [HttpPost("recuperacion")]
    [ProducesResponseType(typeof(RespuestaCodigoDto), StatusCodes.Status200OK)]
    public async Task<ActionResult> PedirRecuperacion(CorreoDto datos)
    {
        return Problemas.Desde(await _service.PedirRecuperacionAsync(datos.Correo));
    }

    // POST /api/v1/cuentas/recuperacion/confirmar → cambia la contraseña y cierra todas las sesiones
    [HttpPost("recuperacion/confirmar")]
    [ProducesResponseType(typeof(MensajeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> ConfirmarRecuperacion(NuevaContrasenaDto datos)
    {
        return Problemas.Desde(await _service.ConfirmarRecuperacionAsync(datos));
    }

    // GET /api/v1/cuentas/yo → datos de la cuenta del token
    [HttpGet("yo")]
    [Authorize]
    [DisableRateLimiting]
    [ProducesResponseType(typeof(CuentaReadDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> Yo()
    {
        if (!int.TryParse(User.FindFirst(TokenService.ClaimId)?.Value, out var cuentaId))
        {
            return Unauthorized();
        }

        return Problemas.Desde(await _service.ObtenerPerfilAsync(cuentaId));
    }
}
