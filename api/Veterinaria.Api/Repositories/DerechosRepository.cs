using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Models;
using Veterinaria.Api.Data;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Repositories;

public class DerechosRepository : IDerechosRepository
{
    private readonly AppDbContext _context;

    public DerechosRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cuenta?> ObtenerCuentaAsync(int cuentaId)
    {
        return await _context.Cuentas.Include(c => c.Aceptaciones).ThenInclude(a => a.DocumentoLegal).FirstOrDefaultAsync(c => c.Id == cuentaId);
    }

    public async Task<List<PoliticaConservacion>> ObtenerPoliticasAsync()
    {
        return await _context.PoliticasConservacion.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
    }

    public async Task<List<Animal>> ObtenerAnimalesAsync(int cuentaId)
    {
        return await _context.Animales.AsNoTracking()
            .Include(a => a.Especie)
            .Where(a => a.Responsables.Any(r => r.CuentaId == cuentaId && r.Hasta == null))
            .OrderBy(a => a.Nombre)
            .ToListAsync();
    }

    public async Task<List<Turno>> ObtenerTurnosAsync(int clienteId)
    {
        return await _context.Turnos.AsNoTracking().Include(t => t.Animal).Include(t => t.Prestacion)
            .Where(t => t.ClienteId == clienteId).OrderBy(t => t.Inicio).ToListAsync();
    }

    public async Task<List<Cargo>> ObtenerCargosAsync(int clienteId)
    {
        return await _context.Cargos.AsNoTracking().Where(c => c.ClienteId == clienteId).OrderBy(c => c.Fecha).ToListAsync();
    }

    public async Task<List<Pago>> ObtenerPagosAsync(IEnumerable<int> cargoIds)
    {
        var ids = cargoIds.ToList();
        return await _context.Pagos.AsNoTracking().Where(p => ids.Contains(p.CargoId)).OrderBy(p => p.Fecha).ToListAsync();
    }

    public async Task<List<int>> ObtenerPagosAnuladosAsync(IEnumerable<int> pagoIds)
    {
        var ids = pagoIds.ToList();
        return await _context.NotasCredito.Where(n => ids.Contains(n.PagoId)).Select(n => n.PagoId).ToListAsync();
    }

    public async Task<List<Consentimiento>> ObtenerConsentimientosAsync(int clienteId)
    {
        return await _context.Consentimientos.AsNoTracking().Include(c => c.Animal).Where(c => c.ClienteId == clienteId).OrderBy(c => c.Fecha).ToListAsync();
    }

    public async Task<List<Comunicacion>> ObtenerComunicacionesAsync(int cuentaId)
    {
        return await _context.Comunicaciones.AsNoTracking().Include(c => c.TipoComunicacion).Where(c => c.CuentaId == cuentaId).OrderBy(c => c.Id).ToListAsync();
    }

    public async Task<List<PreferenciaComunicacion>> ObtenerPreferenciasAsync(int cuentaId)
    {
        return await _context.PreferenciasComunicacion.AsNoTracking().Include(p => p.TipoComunicacion).Where(p => p.CuentaId == cuentaId).ToListAsync();
    }

    public async Task<decimal> SaldoPendienteAsync(int clienteId)
    {
        var monto = await _context.Cargos.Where(c => c.ClienteId == clienteId).SumAsync(c => (decimal?)c.Monto) ?? 0;
        var pagado = await _context.Pagos
            .Where(p => p.Cargo!.ClienteId == clienteId && !_context.NotasCredito.Any(n => n.PagoId == p.Id))
            .SumAsync(p => (decimal?)p.Monto) ?? 0;
        return monto - pagado;
    }

    public async Task<int> ContarTurnosProximosAsync(int clienteId, DateTime ahora)
    {
        return await _context.Turnos.CountAsync(t => t.ClienteId == clienteId && t.Inicio > ahora && CalculoAgenda.EstadosQueOcupan.Contains(t.Estado));
    }

    public async Task<bool> TieneInternacionAbiertaAsync(int clienteId)
    {
        return await _context.Internaciones.AnyAsync(i => i.ClienteId == clienteId && (i.Estado == "Admitida" || i.Estado == "Internado"));
    }

    public async Task<List<SolicitudDerecho>> ObtenerSolicitudesAsync(int? cuentaId)
    {
        return await _context.SolicitudesDerecho.AsNoTracking()
            .Include(s => s.Cuenta).Include(s => s.ResueltaPor)
            .Where(s => cuentaId == null || s.CuentaId == cuentaId)
            .OrderByDescending(s => s.Id)
            .ToListAsync();
    }

    public async Task<SolicitudDerecho?> ObtenerSolicitudAsync(int id)
    {
        return await _context.SolicitudesDerecho.Include(s => s.Cuenta).FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Comunicacion>> ObtenerComunicacionesParaBorrarAsync(int cuentaId)
    {
        return await _context.Comunicaciones.Where(c => c.CuentaId == cuentaId).ToListAsync();
    }

    public async Task<List<PreferenciaComunicacion>> ObtenerPreferenciasParaBorrarAsync(int cuentaId)
    {
        return await _context.PreferenciasComunicacion.Where(p => p.CuentaId == cuentaId).ToListAsync();
    }

    public async Task<List<CodigoCuenta>> ObtenerCodigosAsync(int cuentaId)
    {
        return await _context.CodigosCuenta.Where(c => c.CuentaId == cuentaId).ToListAsync();
    }

    public async Task<List<SesionRefresco>> ObtenerSesionesActivasAsync(int cuentaId)
    {
        return await _context.SesionesRefresco.Where(s => s.CuentaId == cuentaId && s.RevocadaEn == null).ToListAsync();
    }

    public void Agregar<T>(T entidad) where T : class => _context.Add(entidad);

    public void Quitar<T>(T entidad) where T : class => _context.Remove(entidad);

    public async Task GuardarAsync()
    {
        await _context.SaveChangesAsync();
    }
}
