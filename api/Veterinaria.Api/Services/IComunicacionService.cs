using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

// Comunicaciones (RF-CLI-18, RF-CLI-20, RF-REC-07, RNF-CON-04, RNF-CON-10): bandeja y preferencias del cliente,
// bandeja de salida del personal, cola de envío con reintentos, comprobantes, avisos y alertas sanitarias.
public interface IComunicacionService
{
    // Cliente
    Task<BandejaDto> BandejaAsync(int cuentaId, int page, int pageSize);
    Task<Resultado<ComunicacionReadDto>> MarcarLeidaAsync(int cuentaId, long id);
    Task<List<PreferenciaReadDto>> PreferenciasAsync(int cuentaId);
    Task<Resultado<PreferenciaReadDto>> CambiarPreferenciaAsync(int cuentaId, int tipoId, string canal);

    // Recepción y Administrador
    Task<PaginaDto<ComunicacionReadDto>> SalidaAsync(string? estado, int? tipoId, string? texto, int page, int pageSize);
    Task<ColaProcesadaDto> ProcesarColaAsync();
    Task<Resultado<ComunicacionReadDto>> ReintentarAsync(int personaId, string rol, long id);
    Task<Resultado<ComunicacionReadDto>> ReenviarAsync(int personaId, string rol, long id);
    Task<Resultado<ComunicacionReadDto>> EnviarComprobanteAsync(int personaId, string rol, int pagoId);
    Task<Resultado<DifusionResultadoDto>> AvisoOperativoAsync(int personaId, string rol, DifusionDto datos);

    // Administrador y Veterinario
    Task<Resultado<DifusionResultadoDto>> AlertaSanitariaAsync(int personaId, string rol, DifusionDto datos);
}
