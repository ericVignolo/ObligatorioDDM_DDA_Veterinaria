using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

// Consentimiento informado de las cirugías (A-025, RF-CLI-11, RN-10, P-RC-06).
public interface IConsentimientoService
{
    Task<Resultado<ConsentimientoEstadoDto>> EstadoAsync(int cuentaId, int turnoId);

    // Turnos próximos del cliente que piden consentimiento y todavía no lo tienen firmado en la versión vigente.
    Task<List<ConsentimientoEstadoDto>> PendientesAsync(int cuentaId);

    Task<Resultado<ConsentimientoEstadoDto>> ResponderAsync(int cuentaId, int turnoId, RespuestaConsentimientoDto datos);

    // Administrador: versiones de un servicio y publicación de una nueva (la anterior deja de estar vigente).
    Task<Resultado<List<DocumentoConsentimientoReadDto>>> DocumentosAsync(int prestacionId);
    Task<Resultado<DocumentoConsentimientoReadDto>> PublicarAsync(DocumentoConsentimientoCreateDto datos);
}
