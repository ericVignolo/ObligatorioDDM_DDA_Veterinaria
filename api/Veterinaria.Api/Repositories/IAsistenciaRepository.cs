using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

// Acceso para las solicitudes de asistencia, sus ofertas y su historial. Las entidades quedan seguidas por EF.
public interface IAsistenciaRepository
{
    // Con cliente, animal, veterinario y ofertas (con su veterinario).
    Task<SolicitudAsistencia?> ObtenerAsync(int id);
    Task<List<SolicitudAsistencia>> ObtenerActivasAsync();
    Task<List<SolicitudAsistencia>> ObtenerCerradasAsync(int tomar);
    Task<List<SolicitudAsistencia>> ObtenerDeClienteAsync(int clienteId);
    Task<SolicitudAsistencia?> ObtenerPorClaveAsync(int clienteId, string clave);
    Task<List<EventoAsistencia>> ObtenerEventosAsync(int solicitudId);

    Task<Animal?> ObtenerAnimalAsync(int animalId);
    Task<bool> EsResponsableAsync(int animalId, int cuentaId);
    Task<Especie?> ObtenerEspecieAsync(int especieId);
    Task<Cuenta?> ObtenerClienteAsync(int cuentaId);

    // Fichas de profesional con cuenta de Veterinario activa, con especies y franjas.
    Task<List<Profesional>> ObtenerVeterinariosAsync();
    Task<Dictionary<string, string>> ObtenerParametrosAsync();
    Task<Parametro?> ObtenerParametroAsync(string clave);
    Task<bool> ExisteCargoAsync(int solicitudId);

    void Agregar<T>(T entidad) where T : class;
    Task GuardarAsync();
}
