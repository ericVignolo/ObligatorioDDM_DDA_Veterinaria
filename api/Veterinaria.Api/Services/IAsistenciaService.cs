using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

// Asistencia veterinaria (A-026, RF-CLI-21 a 25, RF-REC-08/09, RF-VET-16/17, RN-46 a RN-59).
public interface IAsistenciaService
{
    // Cliente (app)
    Task<Resultado<SolicitudAsistenciaReadDto>> CrearDesdeAppAsync(int cuentaId, SolicitudAppDto datos);
    Task<List<SolicitudAsistenciaReadDto>> MiasAsync(int cuentaId);
    Task<Resultado<SolicitudAsistenciaReadDto>> MiaAsync(int cuentaId, int id);
    Task<Resultado<SolicitudAsistenciaReadDto>> EmpeoramientoClienteAsync(int cuentaId, int id, CambioNivelDto datos);

    // Recepción
    Task<Resultado<SolicitudAsistenciaReadDto>> CrearTelefonicaAsync(int personaId, SolicitudTelefonicaDto datos);
    Task<List<SolicitudAsistenciaReadDto>> ColaAsync();
    Task<List<SolicitudAsistenciaReadDto>> CerradasAsync();
    Task<Resultado<SolicitudAsistenciaReadDto>> DetalleAsync(int id);
    Task<Resultado<List<VeterinarioElegibleDto>>> ElegiblesAsync(int id);
    Task<Resultado<SolicitudAsistenciaReadDto>> AsignarAsync(int personaId, int id, int veterinarioId);
    Task<Resultado<SolicitudAsistenciaReadDto>> EmpeoramientoTelefonicoAsync(int personaId, int id, CambioNivelDto datos);
    Task<Resultado<SolicitudAsistenciaReadDto>> CerrarAsync(int personaId, int id, CierreAsistenciaDto datos);
    Task<PlazosRevisadosDto> RevisarPlazosAsync();

    // Veterinario
    Task<List<SolicitudAsistenciaReadDto>> DeVeterinarioAsync(int cuentaId);
    Task<Resultado<SolicitudAsistenciaReadDto>> AceptarAsync(int cuentaId, int id);
    Task<Resultado<SolicitudAsistenciaReadDto>> RechazarAsync(int cuentaId, int id, string motivo);
    Task<Resultado<SolicitudAsistenciaReadDto>> AvanzarAsync(int cuentaId, int id);
    Task<Resultado<SolicitudAsistenciaReadDto>> FinalizarAsync(int cuentaId, int id, string resultado);
    Task<Resultado<SolicitudAsistenciaReadDto>> CambiarNivelAsync(int cuentaId, int id, CambioNivelDto datos);

    // Administrador (RF-ADM-13)
    Task<Resultado<VeterinarioElegibleDto>> CambiarGuardiaAsync(int adminId, int cuentaId);
}
