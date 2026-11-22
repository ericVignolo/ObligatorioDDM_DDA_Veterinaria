using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

// Acceso para exportar los datos del titular, sus pedidos y la anonimización de la cuenta.
public interface IDerechosRepository
{
    // Seguida por EF, con sus aceptaciones legales (y el documento).
    Task<Cuenta?> ObtenerCuentaAsync(int cuentaId);
    Task<List<PoliticaConservacion>> ObtenerPoliticasAsync();

    // Para la exportación (sin seguimiento).
    Task<List<Animal>> ObtenerAnimalesAsync(int cuentaId);
    Task<List<Turno>> ObtenerTurnosAsync(int clienteId);
    Task<List<Cargo>> ObtenerCargosAsync(int clienteId);
    Task<List<Pago>> ObtenerPagosAsync(IEnumerable<int> cargoIds);
    Task<List<int>> ObtenerPagosAnuladosAsync(IEnumerable<int> pagoIds);
    Task<List<Consentimiento>> ObtenerConsentimientosAsync(int clienteId);
    Task<List<Comunicacion>> ObtenerComunicacionesAsync(int cuentaId);
    Task<List<PreferenciaComunicacion>> ObtenerPreferenciasAsync(int cuentaId);

    // Obstáculos para eliminar.
    Task<decimal> SaldoPendienteAsync(int clienteId);
    Task<int> ContarTurnosProximosAsync(int clienteId, DateTime ahora);
    Task<bool> TieneInternacionAbiertaAsync(int clienteId);

    Task<List<SolicitudDerecho>> ObtenerSolicitudesAsync(int? cuentaId);
    Task<SolicitudDerecho?> ObtenerSolicitudAsync(int id);

    // Para anonimizar (seguidas por EF): se borran.
    Task<List<Comunicacion>> ObtenerComunicacionesParaBorrarAsync(int cuentaId);
    Task<List<PreferenciaComunicacion>> ObtenerPreferenciasParaBorrarAsync(int cuentaId);
    Task<List<CodigoCuenta>> ObtenerCodigosAsync(int cuentaId);
    Task<List<SesionRefresco>> ObtenerSesionesActivasAsync(int cuentaId);

    void Agregar<T>(T entidad) where T : class;
    void Quitar<T>(T entidad) where T : class;
    Task GuardarAsync();
}
