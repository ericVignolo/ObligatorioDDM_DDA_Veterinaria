using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Repositories;

public class CobroRepository : ICobroRepository
{
    private readonly AppDbContext _context;

    public CobroRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cargo?> ObtenerCargoAsync(int cargoId)
    {
        return await _context.Cargos.Include(c => c.Cliente).Include(c => c.Animal).FirstOrDefaultAsync(c => c.Id == cargoId);
    }

    public async Task<List<Cargo>> ObtenerCargosAsync(int? clienteId)
    {
        return await _context.Cargos
            .AsNoTracking()
            .Include(c => c.Cliente)
            .Include(c => c.Animal)
            .Where(c => clienteId == null || c.ClienteId == clienteId)
            .OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id)
            .ToListAsync();
    }

    public async Task<Dictionary<int, decimal>> PagadoPorCargoAsync(IEnumerable<int> cargoIds)
    {
        var ids = cargoIds.ToList();
        return await _context.Pagos
            .Where(p => ids.Contains(p.CargoId) && !_context.NotasCredito.Any(n => n.PagoId == p.Id))
            .GroupBy(p => p.CargoId)
            .Select(g => new { CargoId = g.Key, Monto = g.Sum(p => p.Monto) })
            .ToDictionaryAsync(x => x.CargoId, x => x.Monto);
    }

    public async Task<Pago?> ObtenerPagoAsync(int pagoId)
    {
        return await _context.Pagos
            .Include(p => p.Cargo).ThenInclude(c => c!.Cliente)
            .Include(p => p.CobradoPor)
            .FirstOrDefaultAsync(p => p.Id == pagoId);
    }

    public async Task<List<Pago>> ObtenerPagosDeCargoAsync(int cargoId)
    {
        return await _context.Pagos
            .AsNoTracking()
            .Include(p => p.Cargo).ThenInclude(c => c!.Cliente)
            .Include(p => p.CobradoPor)
            .Where(p => p.CargoId == cargoId)
            .OrderBy(p => p.Fecha).ThenBy(p => p.Id)
            .ToListAsync();
    }

    public async Task<List<Pago>> ObtenerPagosEntreAsync(DateTime desde, DateTime hasta)
    {
        return await _context.Pagos
            .AsNoTracking()
            .Include(p => p.Cargo).ThenInclude(c => c!.Cliente)
            .Include(p => p.CobradoPor)
            .Where(p => p.Fecha >= desde && p.Fecha < hasta)
            .OrderByDescending(p => p.Fecha).ThenByDescending(p => p.Id)
            .ToListAsync();
    }

    public async Task<List<NotaCredito>> ObtenerNotasDePagosAsync(IEnumerable<int> pagoIds)
    {
        var ids = pagoIds.ToList();
        return await _context.NotasCredito.AsNoTracking().Where(n => ids.Contains(n.PagoId)).ToListAsync();
    }

    public async Task<List<NotaCredito>> ObtenerNotasAsync()
    {
        return await _context.NotasCredito
            .AsNoTracking()
            .Include(n => n.Pago)
            .Include(n => n.EmitidaPor)
            .OrderByDescending(n => n.Fecha).ThenByDescending(n => n.Id)
            .ToListAsync();
    }

    public async Task<List<Turno>> ObtenerTurnosSinSenaAsync(int? clienteId)
    {
        return await _context.Turnos
            .Include(t => t.Prestacion)
            .Include(t => t.Animal)
            .Where(t => t.Estado == CalculoAgenda.PendienteSena && (clienteId == null || t.ClienteId == clienteId)
                && !_context.Cargos.Any(c => c.EsSena && c.TurnoId == t.Id))
            .ToListAsync();
    }

    public async Task<bool> ExisteSenaAsync(int turnoId)
    {
        return await _context.Cargos.AnyAsync(c => c.EsSena && c.TurnoId == turnoId);
    }

    public async Task<Turno?> ObtenerTurnoAsync(int turnoId)
    {
        return await _context.Turnos.FirstOrDefaultAsync(t => t.Id == turnoId);
    }

    public async Task<TarifaPrestacion?> ObtenerTarifaVigenteAsync(int prestacionId, DateTime fecha)
    {
        return await _context.TarifasPrestacion
            .Where(t => t.PrestacionId == prestacionId && t.VigenteDesde <= fecha.Date)
            .OrderByDescending(t => t.VigenteDesde)
            .FirstOrDefaultAsync();
    }

    public async Task<Cuenta?> ObtenerCuentaAsync(int cuentaId)
    {
        return await _context.Cuentas.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cuentaId);
    }

    public async Task<Dictionary<string, string>> ObtenerParametrosAsync()
    {
        return await _context.Parametros.AsNoTracking().ToDictionaryAsync(p => p.Clave, p => p.Valor);
    }

    public async Task<int> SiguienteNumeroAsync(string clave)
    {
        var numerador = await _context.Numeradores.FirstAsync(n => n.Clave == clave);
        numerador.Ultimo++;
        return numerador.Ultimo;
    }

    public void Agregar<T>(T entidad) where T : class => _context.Add(entidad);

    public async Task GuardarAsync()
    {
        await _context.SaveChangesAsync();
    }

    public Task<T> EnTransaccionAsync<T>(int cargoId, string numerador, Func<Task<T>> accion)
    {
        return Transacciones.EjecutarAsync(_context, new[] { (Transacciones.Cargos, cargoId) }, accion, numerador);
    }
}
