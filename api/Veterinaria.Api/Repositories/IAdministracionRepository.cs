using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

// Acceso para la configuración del Administrador. Las entidades que devuelve quedan seguidas por EF.
public interface IAdministracionRepository
{
    // ---------- Servicios y tarifas ----------
    Task<List<Prestacion>> ObtenerPrestacionesAsync();
    Task<Prestacion?> ObtenerPrestacionAsync(int id);
    Task<bool> ExistePrestacionConNombreAsync(string nombre, int exceptoId);
    Task<List<TarifaPrestacion>> ObtenerTarifasAsync(int prestacionId);

    // ---------- Personal ----------
    // Cuentas con algún rol que no sea Cliente, con sus roles y su ficha de profesional.
    Task<List<Cuenta>> ObtenerPersonalAsync();
    Task<Cuenta?> ObtenerCuentaConRolesAsync(int cuentaId);
    Task<bool> ExisteCorreoAsync(string correo);
    Task<List<Rol>> ObtenerRolesAsync();
    Task<Profesional?> ObtenerProfesionalAsync(int profesionalId);
    Task<Profesional?> ObtenerProfesionalDeCuentaAsync(int cuentaId);
    Task<int> ContarAdministradoresActivosAsync(int exceptoCuentaId);
    Task<List<SesionRefresco>> ObtenerSesionesActivasAsync(int cuentaId);

    // ---------- Agenda ----------
    Task<List<Ausencia>> ObtenerAusenciasAsync(int? profesionalId, DateTime desde);
    Task<Ausencia?> ObtenerAusenciaAsync(int id);
    Task<int> ContarTurnosActivosAsync(DateTime desde, DateTime hasta, int? profesionalId);
    Task<List<DiaNoLaborable>> ObtenerFeriadosAsync(DateTime desde);
    Task<DiaNoLaborable?> ObtenerFeriadoAsync(DateTime fecha);

    // ---------- Parámetros, stock mínimo y auditoría ----------
    Task<List<Parametro>> ObtenerParametrosAsync();
    Task<Parametro?> ObtenerParametroAsync(string clave);
    Task<Articulo?> ObtenerArticuloAsync(int articuloId);
    Task<List<Auditoria>> ObtenerAuditoriaAsync(string? entidad, DateTime? desde, int saltar, int tomar);
    Task<int> ContarAuditoriaAsync(string? entidad, DateTime? desde);
    Task<Dictionary<int, string>> NombresDeCuentasAsync(IEnumerable<int> ids);

    void Agregar<T>(T entidad) where T : class;
    void Quitar<T>(T entidad) where T : class;
    Task GuardarAsync();
}
