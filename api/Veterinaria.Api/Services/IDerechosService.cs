using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

// Derechos del titular (A-028, RF-CLI-19, RNF-LEG-01, RNF-LEG-02, RNF-LEG-06): exportar sus datos y pedir la
// eliminación de su cuenta, que resuelve el Administrador.
public interface IDerechosService
{
    Task<List<PoliticaConservacionDto>> PoliticasAsync();

    // Cliente
    Task<Resultado<string>> ExportarAsync(int cuentaId);
    Task<List<SolicitudDerechoReadDto>> SolicitudesAsync(int cuentaId);
    Task<ObstaculosDto> ObstaculosAsync(int cuentaId);
    Task<Resultado<SolicitudDerechoReadDto>> PedirEliminacionAsync(int cuentaId, bool confirmado);
    Task<Resultado<SolicitudDerechoReadDto>> CancelarAsync(int cuentaId, int id);

    // Administrador
    Task<List<SolicitudDerechoReadDto>> TodasAsync();
    Task<Resultado<SolicitudDerechoReadDto>> ResolverAsync(int adminId, int id, ResolucionDerechoDto datos);
}
