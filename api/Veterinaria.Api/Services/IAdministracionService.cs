using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

// Configuración del Administrador (RF-ADM-01 a RF-ADM-04, RN-33). Cada cambio queda en la auditoría.
public interface IAdministracionService
{
    // Servicios y tarifas
    Task<List<ServicioAdminDto>> ServiciosAsync();
    Task<Resultado<ServicioAdminDto>> CrearServicioAsync(int adminId, ServicioFormDto datos);
    Task<Resultado<ServicioAdminDto>> EditarServicioAsync(int adminId, int id, ServicioFormDto datos);
    Task<Resultado<List<TarifaReadDto>>> TarifasAsync(int prestacionId);
    Task<Resultado<TarifaReadDto>> ProgramarTarifaAsync(int adminId, TarifaCreateDto datos);

    // Personal
    Task<List<PersonalReadDto>> PersonalAsync();
    Task<Resultado<PersonalReadDto>> CrearPersonalAsync(int adminId, PersonalCreateDto datos);
    Task<Resultado<PersonalReadDto>> CambiarRolAsync(int adminId, int cuentaId, CambioRolDto datos);
    Task<Resultado<PersonalReadDto>> CambiarBloqueoAsync(int adminId, int cuentaId, bool bloquear);

    // Ausencias y feriados
    Task<List<AusenciaReadDto>> AusenciasAsync(int? profesionalId);
    Task<Resultado<AusenciaReadDto>> AgregarAusenciaAsync(int adminId, AusenciaCreateDto datos);
    Task<Resultado<OperacionDto>> QuitarAusenciaAsync(int adminId, int id);
    Task<List<FeriadoReadDto>> FeriadosAsync();
    Task<Resultado<FeriadoReadDto>> AgregarFeriadoAsync(int adminId, FeriadoCreateDto datos);
    Task<Resultado<OperacionDto>> QuitarFeriadoAsync(int adminId, DateOnly fecha);

    // Parámetros, stock mínimo y auditoría
    Task<List<ParametroReadDto>> ParametrosAsync();
    Task<Resultado<ParametroReadDto>> CambiarParametroAsync(int adminId, string clave, ParametroUpdateDto datos);
    Task<Resultado<OperacionDto>> CambiarStockMinimoAsync(int adminId, int articuloId, StockMinimoDto datos);
    Task<PaginaDto<AuditoriaReadDto>> AuditoriaAsync(string? entidad, DateOnly? desde, int page, int pageSize);
}
