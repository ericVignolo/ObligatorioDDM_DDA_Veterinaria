using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

// Registro, verificación y recuperación de la contraseña (ESPECIFICACION_REGISTRO_CLIENTE.md, D-REG-01 a 04).
public interface ICuentaService
{
    Task<Resultado<RespuestaCodigoDto>> RegistrarAsync(RegistroCreateDto datos);
    Task<Resultado<MensajeDto>> VerificarAsync(VerificacionDto datos);
    Task<Resultado<RespuestaCodigoDto>> ReenviarCodigoAsync(string correo);

    // Siempre responde lo mismo, exista o no la cuenta (D-REG-03).
    Task<Resultado<RespuestaCodigoDto>> PedirRecuperacionAsync(string correo);
    Task<Resultado<MensajeDto>> ConfirmarRecuperacionAsync(NuevaContrasenaDto datos);

    Task<Resultado<CuentaReadDto>> ObtenerPerfilAsync(int cuentaId);

    // D-REG-02: borra las cuentas que no se verificaron en 7 días (lo corre la tarea diaria). Devuelve cuántas.
    Task<int> EliminarNoVerificadasAsync();
}
