using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Repositories;

public class AdministracionRepository : IAdministracionRepository
{
    private readonly AppDbContext _context;

    public AdministracionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Prestacion>> ObtenerPrestacionesAsync()
    {
        return await _context.Prestaciones.AsNoTracking().OrderBy(p => p.Nombre).ToListAsync();
    }

    public async Task<Prestacion?> ObtenerPrestacionAsync(int id)
    {
        return await _context.Prestaciones.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<bool> ExistePrestacionConNombreAsync(string nombre, int exceptoId)
    {
        return await _context.Prestaciones.AnyAsync(p => p.Id != exceptoId && p.Nombre == nombre);
    }

    public async Task<List<TarifaPrestacion>> ObtenerTarifasAsync(int prestacionId)
    {
        return await _context.TarifasPrestacion
            .AsNoTracking()
            .Include(t => t.RegistradoPor)
            .Where(t => t.PrestacionId == prestacionId)
            .OrderByDescending(t => t.VigenteDesde)
            .ToListAsync();
    }

    public async Task<List<Cuenta>> ObtenerPersonalAsync()
    {
        return await _context.Cuentas
            .AsNoTracking()
            .Include(c => c.Roles).ThenInclude(r => r.Rol)
            .Where(c => !c.Eliminada && c.Roles.Any(r => r.Rol!.Nombre != "Cliente"))
            .OrderBy(c => c.Apellido).ThenBy(c => c.Nombre)
            .ToListAsync();
    }

    public async Task<Cuenta?> ObtenerCuentaConRolesAsync(int cuentaId)
    {
        return await _context.Cuentas.Include(c => c.Roles).ThenInclude(r => r.Rol).FirstOrDefaultAsync(c => c.Id == cuentaId && !c.Eliminada);
    }

    public async Task<bool> ExisteCorreoAsync(string correo)
    {
        return await _context.Cuentas.AnyAsync(c => c.Correo == correo);
    }

    public async Task<List<Rol>> ObtenerRolesAsync()
    {
        return await _context.Roles.ToListAsync();
    }

    public async Task<Profesional?> ObtenerProfesionalAsync(int profesionalId)
    {
        return await _context.Profesionales.FirstOrDefaultAsync(p => p.Id == profesionalId);
    }

    public async Task<Profesional?> ObtenerProfesionalDeCuentaAsync(int cuentaId)
    {
        return await _context.Profesionales.FirstOrDefaultAsync(p => p.CuentaId == cuentaId);
    }

    public async Task<int> ContarAdministradoresActivosAsync(int exceptoCuentaId)
    {
        return await _context.Cuentas.CountAsync(c => c.Id != exceptoCuentaId && c.Activa && !c.Eliminada
            && c.Roles.Any(r => r.Rol!.Nombre == "Administrador"));
    }

    public async Task<List<SesionRefresco>> ObtenerSesionesActivasAsync(int cuentaId)
    {
        return await _context.SesionesRefresco.Where(s => s.CuentaId == cuentaId && s.RevocadaEn == null).ToListAsync();
    }

    public async Task<List<Ausencia>> ObtenerAusenciasAsync(int? profesionalId, DateTime desde)
    {
        return await _context.Ausencias
            .AsNoTracking()
            .Include(a => a.Profesional)
            .Where(a => (profesionalId == null || a.ProfesionalId == profesionalId) && a.Hasta >= desde)
            .OrderBy(a => a.Desde)
            .ToListAsync();
    }

    public async Task<Ausencia?> ObtenerAusenciaAsync(int id)
    {
        return await _context.Ausencias.Include(a => a.Profesional).FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<int> ContarTurnosActivosAsync(DateTime desde, DateTime hasta, int? profesionalId)
    {
        return await _context.Turnos.CountAsync(t => t.Inicio < hasta && t.Fin > desde
            && (profesionalId == null || t.ProfesionalId == profesionalId)
            && CalculoAgenda.EstadosQueOcupan.Contains(t.Estado));
    }

    public async Task<List<DiaNoLaborable>> ObtenerFeriadosAsync(DateTime desde)
    {
        return await _context.DiasNoLaborables.AsNoTracking().Where(d => d.Fecha >= desde).OrderBy(d => d.Fecha).ToListAsync();
    }

    public async Task<DiaNoLaborable?> ObtenerFeriadoAsync(DateTime fecha)
    {
        return await _context.DiasNoLaborables.FirstOrDefaultAsync(d => d.Fecha == fecha.Date);
    }

    public async Task<List<Parametro>> ObtenerParametrosAsync()
    {
        return await _context.Parametros.AsNoTracking().OrderBy(p => p.Clave).ToListAsync();
    }

    public async Task<Parametro?> ObtenerParametroAsync(string clave)
    {
        return await _context.Parametros.FirstOrDefaultAsync(p => p.Clave == clave);
    }

    public async Task<Articulo?> ObtenerArticuloAsync(int articuloId)
    {
        return await _context.Articulos.FirstOrDefaultAsync(a => a.Id == articuloId);
    }

    public async Task<List<Auditoria>> ObtenerAuditoriaAsync(string? entidad, DateTime? desde, int saltar, int tomar)
    {
        return await FiltroAuditoria(entidad, desde)
            .OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id)
            .Skip(saltar).Take(tomar)
            .ToListAsync();
    }

    public async Task<int> ContarAuditoriaAsync(string? entidad, DateTime? desde)
    {
        return await FiltroAuditoria(entidad, desde).CountAsync();
    }

    private IQueryable<Auditoria> FiltroAuditoria(string? entidad, DateTime? desde)
    {
        return _context.Auditorias.AsNoTracking()
            .Where(a => (entidad == null || a.Entidad == entidad) && (desde == null || a.Fecha >= desde));
    }

    public async Task<Dictionary<int, string>> NombresDeCuentasAsync(IEnumerable<int> ids)
    {
        var lista = ids.Distinct().ToList();
        return await _context.Cuentas.AsNoTracking()
            .Where(c => lista.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Nombre + " " + c.Apellido);
    }

    public void Agregar<T>(T entidad) where T : class => _context.Add(entidad);

    public void Quitar<T>(T entidad) where T : class => _context.Remove(entidad);

    public async Task GuardarAsync()
    {
        await _context.SaveChangesAsync();
    }
}
