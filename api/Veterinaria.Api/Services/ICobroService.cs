using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

// Cargos, cobro presencial con comprobante, notas de crédito y señas (A-023, RF-REC-05, RN-32 a RN-36).
// No hay pago en línea: el cliente paga en Recepción (DT-ALC-02).
public interface ICobroService
{
    // Cliente: sus cargos (RN-18).
    Task<PaginaDto<CargoReadDto>> MisCargosAsync(int cuentaId, int page, int pageSize);

    // Recepción y Administrador.
    Task<PaginaDto<CargoReadDto>> CargosAsync(int? clienteId, bool soloConSaldo, int page, int pageSize);
    Task<Resultado<CargoReadDto>> CargoAsync(int cargoId);
    Task<Resultado<CobroRegistradoDto>> CobrarAsync(int personaId, CobroCreateDto datos);
    Task<List<PagoReadDto>> ComprobantesDelDiaAsync(DateOnly fecha);
    Task<Resultado<PagoReadDto>> ComprobanteAsync(int pagoId);
    Task<ResumenCajaDto> ResumenAsync(DateOnly fecha);
    Task<DeudaClienteDto> DeudaAsync(int clienteId);

    // Administrador (RN-34).
    Task<Resultado<AnulacionRegistradaDto>> AnularAsync(int personaId, int pagoId, AnulacionDto datos);
    Task<List<NotaCreditoReadDto>> NotasAsync();

    // RN-35: genera los cargos de seña que falten. Idempotente.
    Task<int> GenerarSenasAsync(int? clienteId);
}
