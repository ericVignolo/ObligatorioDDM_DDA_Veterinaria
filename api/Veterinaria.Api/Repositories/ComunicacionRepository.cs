using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

public class ComunicacionRepository : IComunicacionRepository
{
    private readonly AppDbContext _context;

    public ComunicacionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TipoComunicacion>> ObtenerTiposAsync()
    {
        return await _context.TiposComunicacion.AsNoTracking().OrderBy(t => t.Id).ToListAsync();
    }

    public async Task<TipoComunicacion?> ObtenerTipoAsync(int tipoId)
    {
        return await _context.TiposComunicacion.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tipoId);
    }

    public async Task<List<PreferenciaComunicacion>> ObtenerPreferenciasAsync(int cuentaId)
    {
        return await _context.PreferenciasComunicacion.AsNoTracking().Where(p => p.CuentaId == cuentaId).ToListAsync();
    }

    public async Task<PreferenciaComunicacion?> ObtenerPreferenciaAsync(int cuentaId, int tipoId)
    {
        return await _context.PreferenciasComunicacion.FirstOrDefaultAsync(p => p.CuentaId == cuentaId && p.TipoComunicacionId == tipoId);
    }

    public async Task<Cuenta?> ObtenerCuentaAsync(int cuentaId)
    {
        return await _context.Cuentas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cuentaId);
    }

    public async Task<bool> ExisteConClaveAsync(int cuentaId, int tipoId, string clave)
    {
        return await _context.Comunicaciones.AnyAsync(c => c.CuentaId == cuentaId && c.TipoComunicacionId == tipoId && c.Clave == clave && c.ReenvioDeId == null);
    }

    public async Task<List<Comunicacion>> ObtenerDeCuentaAsync(int cuentaId, int saltar, int tomar)
    {
        return await _context.Comunicaciones
            .AsNoTracking()
            .Include(c => c.TipoComunicacion)
            .Where(c => c.CuentaId == cuentaId)
            .OrderByDescending(c => c.Id)
            .Skip(saltar).Take(tomar)
            .ToListAsync();
    }

    public async Task<int> ContarDeCuentaAsync(int cuentaId)
    {
        return await _context.Comunicaciones.CountAsync(c => c.CuentaId == cuentaId);
    }

    public async Task<int> ContarNoLeidasAsync(int cuentaId)
    {
        return await _context.Comunicaciones.CountAsync(c => c.CuentaId == cuentaId && !c.Leida);
    }

    public async Task<Comunicacion?> ObtenerAsync(long id)
    {
        return await _context.Comunicaciones.Include(c => c.Cuenta).Include(c => c.TipoComunicacion).FirstOrDefaultAsync(c => c.Id == id);
    }

    private IQueryable<Comunicacion> Busqueda(string? estado, int? tipoId, string? texto)
    {
        return _context.Comunicaciones
            .AsNoTracking()
            .Where(c => (estado == null || c.Estado == estado) && (tipoId == null || c.TipoComunicacionId == tipoId)
                && (texto == null || c.Asunto.Contains(texto) || c.Destino.Contains(texto)
                    || (c.Cuenta!.Nombre + " " + c.Cuenta.Apellido).Contains(texto)));
    }

    public async Task<List<Comunicacion>> BuscarAsync(string? estado, int? tipoId, string? texto, int saltar, int tomar)
    {
        return await Busqueda(estado, tipoId, texto)
            .Include(c => c.Cuenta)
            .Include(c => c.TipoComunicacion)
            .OrderByDescending(c => c.Id)
            .Skip(saltar).Take(tomar)
            .ToListAsync();
    }

    public async Task<int> ContarBusquedaAsync(string? estado, int? tipoId, string? texto)
    {
        return await Busqueda(estado, tipoId, texto).CountAsync();
    }

    public async Task<List<Comunicacion>> ObtenerPendientesAsync(int tomar)
    {
        return await _context.Comunicaciones
            .Where(c => c.Estado == "Pendiente")
            .OrderBy(c => c.FechaCreacion).ThenBy(c => c.Id)
            .Take(tomar)
            .ToListAsync();
    }

    public async Task<List<Cuenta>> ObtenerDestinatariosAsync(int? especieId)
    {
        return await _context.Cuentas
            .AsNoTracking()
            .Where(c => c.Verificada && c.Activa && !c.Eliminada && c.Roles.Any(r => r.Rol!.Nombre == "Cliente")
                && (especieId == null || _context.Responsabilidades.Any(r => r.CuentaId == c.Id && r.Hasta == null
                    && r.Animal!.Activo && r.Animal.EspecieId == especieId)))
            .ToListAsync();
    }

    public async Task<Pago?> ObtenerPagoAsync(int pagoId)
    {
        return await _context.Pagos.AsNoTracking().Include(p => p.Cargo).FirstOrDefaultAsync(p => p.Id == pagoId);
    }

    public void Agregar<T>(T entidad) where T : class => _context.Add(entidad);

    public async Task GuardarAsync()
    {
        await _context.SaveChangesAsync();
    }
}
