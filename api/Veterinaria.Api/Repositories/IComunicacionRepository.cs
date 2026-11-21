using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

// Acceso para la bandeja del cliente, sus preferencias y la cola de envío.
public interface IComunicacionRepository
{
    Task<List<TipoComunicacion>> ObtenerTiposAsync();
    Task<TipoComunicacion?> ObtenerTipoAsync(int tipoId);
    Task<List<PreferenciaComunicacion>> ObtenerPreferenciasAsync(int cuentaId);
    Task<PreferenciaComunicacion?> ObtenerPreferenciaAsync(int cuentaId, int tipoId);
    Task<Cuenta?> ObtenerCuentaAsync(int cuentaId);

    // Idempotencia (RNF-CON-10): ya hay un mensaje original (no reenvío) con esa clave.
    Task<bool> ExisteConClaveAsync(int cuentaId, int tipoId, string clave);

    Task<List<Comunicacion>> ObtenerDeCuentaAsync(int cuentaId, int saltar, int tomar);
    Task<int> ContarDeCuentaAsync(int cuentaId);
    Task<int> ContarNoLeidasAsync(int cuentaId);

    // Con la cuenta y el tipo.
    Task<Comunicacion?> ObtenerAsync(long id);
    Task<List<Comunicacion>> BuscarAsync(string? estado, int? tipoId, string? texto, int saltar, int tomar);
    Task<int> ContarBusquedaAsync(string? estado, int? tipoId, string? texto);
    Task<List<Comunicacion>> ObtenerPendientesAsync(int tomar);

    // Clientes verificados y activos; con especie, solo los que tienen un animal activo de esa especie.
    Task<List<Cuenta>> ObtenerDestinatariosAsync(int? especieId);

    Task<Pago?> ObtenerPagoAsync(int pagoId);

    void Agregar<T>(T entidad) where T : class;
    Task GuardarAsync();
}
