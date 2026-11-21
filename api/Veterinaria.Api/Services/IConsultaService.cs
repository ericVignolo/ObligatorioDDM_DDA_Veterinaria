using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

// Consulta clínica (CU-VET-01, A-019 a A-021, RF-VET-02 a 06, RF-VET-13). `cuentaId` es el veterinario del token.
public interface IConsultaService
{
    Task<Resultado<ConsultaReadDto>> IniciarAsync(int cuentaId, int turnoId);
    Task<Resultado<ConsultaReadDto>> ObtenerAsync(int cuentaId, int consultaId);
    Task<List<ConsultaReadDto>> AbiertasAsync(int cuentaId);
    Task<Resultado<ConsultaReadDto>> GuardarAsync(int cuentaId, int consultaId, ConsultaGuardarDto datos);

    Task<Resultado<ConsultaReadDto>> AgregarInsumoAsync(int cuentaId, int consultaId, InsumoDto datos);
    Task<Resultado<ConsultaReadDto>> QuitarInsumoAsync(int cuentaId, int consultaId, int articuloId);
    Task<Resultado<ConsultaReadDto>> AgregarVacunaAsync(int cuentaId, int consultaId, AplicacionDto datos);
    Task<Resultado<ConsultaReadDto>> QuitarVacunaAsync(int cuentaId, int consultaId, int aplicacionId);

    // Cierre atómico: firma, stock, vacunas, cargo, turno Finalizado y resumen pendiente (FLUJO §8.3).
    Task<Resultado<FirmaReadDto>> FirmarAsync(int cuentaId, int consultaId, ConsultaFirmaDto datos);

    // P-RC-04: sin atención ni consumos, el autor cancela el turno con motivo; el borrador se conserva.
    Task<Resultado<MensajeDto>> CancelarSinAtencionAsync(int cuentaId, int consultaId, string motivo);

    // RN-12: registro nuevo firmado con la contraseña; el original no cambia.
    Task<Resultado<ConsultaReadDto>> EnmendarAsync(int cuentaId, int consultaId, EnmiendaDto datos);

    // Historia completa para el veterinario; la lectura queda registrada (RN-40, P-RC-05).
    Task<Resultado<HistoriaDto>> HistoriaParaPersonalAsync(int cuentaId, int animalId, string motivo);

    // Historia de su animal para el cliente (solo lectura).
    Task<Resultado<HistoriaDto>> HistoriaParaClienteAsync(int cuentaId, int animalId);
}
