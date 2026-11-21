using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

public class AsistenciaRepository : IAsistenciaRepository
{
    private static readonly string[] Cerradas = { "Finalizada", "Cancelada por la clínica", "No concretada" };

    private readonly AppDbContext _context;

    public AsistenciaRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<SolicitudAsistencia> ConDatos()
    {
        return _context.SolicitudesAsistencia
            .Include(s => s.Cliente)
            .Include(s => s.Animal)
            .Include(s => s.Veterinario)
            .Include(s => s.Ofertas).ThenInclude(o => o.Veterinario);
    }

    public async Task<SolicitudAsistencia?> ObtenerAsync(int id)
    {
        return await ConDatos().FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<SolicitudAsistencia>> ObtenerActivasAsync()
    {
        return await ConDatos().Where(s => !Cerradas.Contains(s.Estado)).ToListAsync();
    }

    public async Task<List<SolicitudAsistencia>> ObtenerCerradasAsync(int tomar)
    {
        return await ConDatos().AsNoTracking().Where(s => Cerradas.Contains(s.Estado)).OrderByDescending(s => s.Id).Take(tomar).ToListAsync();
    }

    public async Task<List<SolicitudAsistencia>> ObtenerDeClienteAsync(int clienteId)
    {
        return await ConDatos().AsNoTracking().Where(s => s.ClienteId == clienteId).OrderByDescending(s => s.Id).ToListAsync();
    }

    public async Task<SolicitudAsistencia?> ObtenerPorClaveAsync(int clienteId, string clave)
    {
        return await ConDatos().FirstOrDefaultAsync(s => s.ClienteId == clienteId && s.ClaveIdempotencia == clave);
    }

    public async Task<List<EventoAsistencia>> ObtenerEventosAsync(int solicitudId)
    {
        return await _context.EventosAsistencia
            .AsNoTracking()
            .Include(e => e.Autor)
            .Where(e => e.SolicitudAsistenciaId == solicitudId)
            .OrderByDescending(e => e.Id)
            .ToListAsync();
    }

    public async Task<Animal?> ObtenerAnimalAsync(int animalId)
    {
        return await _context.Animales.AsNoTracking().Include(a => a.Especie).FirstOrDefaultAsync(a => a.Id == animalId && a.Activo);
    }

    public async Task<bool> EsResponsableAsync(int animalId, int cuentaId)
    {
        return await _context.Responsabilidades.AnyAsync(r => r.AnimalId == animalId && r.CuentaId == cuentaId && r.Hasta == null);
    }

    public async Task<Especie?> ObtenerEspecieAsync(int especieId)
    {
        return await _context.Especies.AsNoTracking().FirstOrDefaultAsync(e => e.Id == especieId);
    }

    public async Task<Cuenta?> ObtenerClienteAsync(int cuentaId)
    {
        return await _context.Cuentas.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cuentaId && !c.Eliminada && c.Roles.Any(r => r.Rol!.Nombre == "Cliente"));
    }

    public async Task<List<Profesional>> ObtenerVeterinariosAsync()
    {
        return await _context.Profesionales
            .AsNoTracking()
            .Include(p => p.Cuenta)
            .Include(p => p.Especies).ThenInclude(e => e.Especie)
            .Include(p => p.Franjas)
            .Where(p => p.Activo && p.Cuenta != null && p.Cuenta.Activa && !p.Cuenta.Eliminada
                && p.Cuenta.Roles.Any(r => r.Rol!.Nombre == "Veterinario"))
            .ToListAsync();
    }

    public async Task<Dictionary<string, string>> ObtenerParametrosAsync()
    {
        return await _context.Parametros.AsNoTracking().ToDictionaryAsync(p => p.Clave, p => p.Valor);
    }

    public async Task<Parametro?> ObtenerParametroAsync(string clave)
    {
        return await _context.Parametros.FirstOrDefaultAsync(p => p.Clave == clave);
    }

    public async Task<bool> ExisteCargoAsync(int solicitudId)
    {
        return await _context.Cargos.AnyAsync(c => c.SolicitudAsistenciaId == solicitudId);
    }

    public void Agregar<T>(T entidad) where T : class => _context.Add(entidad);

    public async Task GuardarAsync()
    {
        await _context.SaveChangesAsync();
    }
}
