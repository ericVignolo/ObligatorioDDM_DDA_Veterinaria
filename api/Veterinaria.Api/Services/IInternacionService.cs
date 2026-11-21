using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

// Internación (A-024, RF-VET-09 a RF-VET-11, RF-CLI-09, RN-21 a RN-25). Recepción admite; el Veterinario hace el
// ingreso clínico con box y plan, publica novedades y da el alta (decisión de Eric, 2026-10-31).
public interface IInternacionService
{
    // Personal
    Task<List<InternacionReadDto>> AbiertasAsync();
    Task<List<InternacionReadDto>> AltasAsync();
    Task<Resultado<InternacionReadDto>> DetalleAsync(int id, bool esVeterinario);
    Task<List<BoxReadDto>> BoxesAsync();
    Task<Resultado<List<BoxReadDto>>> BoxesLibresAsync(int internacionId);

    // Recepción
    Task<Resultado<InternacionReadDto>> AdmitirAsync(int personaId, AdmisionDto datos);
    Task<Resultado<InternacionReadDto>> AnularAdmisionAsync(int personaId, int id, AnulacionAdmisionDto datos);

    // Veterinario
    Task<Resultado<InternacionReadDto>> IngresarAsync(int personaId, int id, IngresoClinicoDto datos);
    Task<Resultado<InternacionReadDto>> PublicarAsync(int personaId, int id, NovedadCreateDto datos);
    Task<Resultado<InternacionReadDto>> RectificarAsync(int personaId, int id, int novedadId, RectificacionDto datos);
    Task<Resultado<InternacionReadDto>> DarAltaAsync(int personaId, int id, AltaDto datos);

    // Cliente (RN-18): solo de sus animales y solo lo visible.
    Task<List<InternacionReadDto>> MiasAsync(int cuentaId);
    Task<Resultado<InternacionReadDto>> MiaAsync(int cuentaId, int id);

    // Administrador (RF-ADM-03)
    Task<Resultado<BoxReadDto>> AgregarBoxAsync(int adminId, BoxCreateDto datos);
    Task<Resultado<BoxReadDto>> CambiarServicioBoxAsync(int adminId, int boxId, bool activo);
}
