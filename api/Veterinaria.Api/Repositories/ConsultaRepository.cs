using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

public class ConsultaRepository : IConsultaRepository
{
    private readonly AppDbContext _context;

    public ConsultaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Turno?> ObtenerTurnoAsync(int turnoId)
    {
        return await _context.Turnos
            .Include(t => t.Prestacion)
            .Include(t => t.Animal).ThenInclude(a => a!.Especie)
            .FirstOrDefaultAsync(t => t.Id == turnoId);
    }

    private IQueryable<Consulta> ConDatos()
    {
        return _context.Consultas
            .Include(c => c.Turno).ThenInclude(t => t!.Prestacion)
            .Include(c => c.Animal).ThenInclude(a => a!.Especie)
            .Include(c => c.Profesional)
            .Include(c => c.Autor)
            .Include(c => c.Enmiendas).ThenInclude(e => e.Autor)
            .AsSplitQuery();
    }

    public async Task<Consulta?> ObtenerAsync(int consultaId)
    {
        return await ConDatos().FirstOrDefaultAsync(c => c.Id == consultaId);
    }

    public async Task<Consulta?> ObtenerPorTurnoAsync(int turnoId)
    {
        return await ConDatos().FirstOrDefaultAsync(c => c.TurnoId == turnoId);
    }

    public async Task<List<Consulta>> ObtenerAbiertasAsync(int? autorId)
    {
        return await ConDatos()
            .AsNoTracking()
            .Where(c => c.Estado == "Borrador" && c.Turno != null && c.Turno.Estado == "En curso")
            .Where(c => autorId == null || c.AutorId == autorId)
            .OrderBy(c => c.FechaInicio)
            .ToListAsync();
    }

    public async Task<Cuenta?> ObtenerCuentaAsync(int cuentaId)
    {
        return await _context.Cuentas.FirstOrDefaultAsync(c => c.Id == cuentaId);
    }

    public async Task<Profesional?> ObtenerProfesionalDeCuentaAsync(int cuentaId)
    {
        return await _context.Profesionales.FirstOrDefaultAsync(p => p.CuentaId == cuentaId && p.Activo);
    }

    public async Task<bool> TieneConsentimientoAceptadoAsync(int turnoId)
    {
        var ultima = await _context.Consentimientos
            .Where(c => c.TurnoId == turnoId)
            .OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id)
            .Select(c => c.Respuesta)
            .FirstOrDefaultAsync();
        return ultima == "Aceptado";
    }

    public async Task<List<string>> AlergiasRegistradasAsync(int animalId)
    {
        return await _context.Consultas
            .Where(c => c.AnimalId == animalId && c.Estado == "Firmada" && c.Alergias != "")
            .Select(c => c.Alergias)
            .ToListAsync();
    }

    public async Task<Articulo?> ObtenerArticuloAsync(int articuloId)
    {
        return await _context.Articulos.FirstOrDefaultAsync(a => a.Id == articuloId && a.Activo);
    }

    public async Task<List<LoteProducto>> ObtenerLotesAsync(int articuloId)
    {
        return await _context.Lotes.Where(l => l.ArticuloId == articuloId).OrderBy(l => l.Vencimiento).ThenBy(l => l.Id).ToListAsync();
    }

    public async Task<Dictionary<int, int>> ReservadoPorLoteAsync(int articuloId)
    {
        return await _context.ReservasStock
            .Where(r => r.ArticuloId == articuloId && !r.Confirmada)
            .GroupBy(r => r.LoteId)
            .Select(g => new { LoteId = g.Key, Cantidad = g.Sum(r => r.Cantidad) })
            .ToDictionaryAsync(x => x.LoteId, x => x.Cantidad);
    }

    public async Task<List<ReservaStock>> ObtenerReservasAsync(int consultaId)
    {
        return await _context.ReservasStock
            .Include(r => r.Articulo)
            .Include(r => r.Lote)
            .Where(r => r.ConsultaId == consultaId)
            .OrderBy(r => r.Id)
            .ToListAsync();
    }

    public async Task<ProductoSanitario?> ObtenerProductoAsync(int productoId)
    {
        return await _context.ProductosSanitarios.Include(p => p.Especies).FirstOrDefaultAsync(p => p.Id == productoId);
    }

    public async Task<List<AplicacionSanitaria>> ObtenerAplicacionesDeConsultaAsync(int consultaId)
    {
        return await _context.AplicacionesSanitarias.Where(a => a.ConsultaId == consultaId).OrderBy(a => a.Id).ToListAsync();
    }

    public async Task<List<AplicacionSanitaria>> ObtenerAplicacionesQueCuentanAsync(int animalId)
    {
        return await _context.AplicacionesSanitarias
            .AsNoTracking()
            .Where(a => a.AnimalId == animalId && (a.ConsultaId == null || a.Consulta!.Estado == "Firmada"))
            .OrderByDescending(a => a.FechaAplicacion).ThenByDescending(a => a.Id)
            .ToListAsync();
    }

    public async Task<TarifaPrestacion?> ObtenerTarifaVigenteAsync(int prestacionId, DateTime fecha)
    {
        return await _context.TarifasPrestacion
            .Where(t => t.PrestacionId == prestacionId && t.VigenteDesde <= fecha.Date)
            .OrderByDescending(t => t.VigenteDesde)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ExisteCargoDeConsultaAsync(int consultaId)
    {
        return await _context.Cargos.AnyAsync(c => c.ConsultaId == consultaId);
    }

    public async Task<int?> ObtenerResponsableEconomicoAsync(int animalId, int clientePreferido)
    {
        var vigentes = await _context.Responsabilidades
            .Where(r => r.AnimalId == animalId && r.Hasta == null)
            .OrderBy(r => r.Desde)
            .Select(r => r.CuentaId)
            .ToListAsync();

        return vigentes.Contains(clientePreferido) ? clientePreferido : vigentes.Cast<int?>().FirstOrDefault();
    }

    public async Task<string?> ObtenerCanalPreferidoAsync(int cuentaId, int tipoComunicacionId)
    {
        return await _context.PreferenciasComunicacion
            .Where(p => p.CuentaId == cuentaId && p.TipoComunicacionId == tipoComunicacionId)
            .Select(p => p.Canal)
            .FirstOrDefaultAsync();
    }

    public async Task<TipoComunicacion?> ObtenerTipoComunicacionAsync(int tipoComunicacionId)
    {
        return await _context.TiposComunicacion.FirstOrDefaultAsync(t => t.Id == tipoComunicacionId);
    }

    public async Task<Animal?> ObtenerAnimalAsync(int animalId)
    {
        return await _context.Animales.Include(a => a.Especie).FirstOrDefaultAsync(a => a.Id == animalId);
    }

    public async Task<bool> EsResponsableAsync(int animalId, int cuentaId)
    {
        return await _context.Responsabilidades.AnyAsync(r => r.AnimalId == animalId && r.CuentaId == cuentaId && r.Hasta == null);
    }

    public async Task<List<Consulta>> ObtenerFirmadasAsync(int animalId)
    {
        return await ConDatos()
            .AsNoTracking()
            .Where(c => c.AnimalId == animalId && c.Estado == "Firmada")
            .OrderByDescending(c => c.FechaFirma)
            .ToListAsync();
    }

    public async Task<bool> TieneCasoActivoAsync(int animalId, DateTime ahora)
    {
        var hoy = ahora.Date;
        var manana = hoy.AddDays(1);
        var turno = await _context.Turnos.AnyAsync(t => t.AnimalId == animalId
            && (t.Estado == "En espera" || t.Estado == "En curso" || (t.Estado == "Confirmado" && t.Inicio >= hoy && t.Inicio < manana)));
        var internacion = await _context.Internaciones.AnyAsync(i => i.AnimalId == animalId && (i.Estado == "Admitida" || i.Estado == "Internado"));
        return turno || internacion;
    }

    public void Agregar<T>(T entidad) where T : class => _context.Add(entidad);
    public void Quitar<T>(T entidad) where T : class => _context.Remove(entidad);

    public async Task GuardarAsync()
    {
        await _context.SaveChangesAsync();
    }

    // Orden fijo: la consulta y después los artículos de menor a mayor Id.
    public Task<T> EnTransaccionAsync<T>(int consultaId, IEnumerable<int> articuloIds, Func<Task<T>> accion)
    {
        var bloqueos = new List<(string, int)> { (Transacciones.Consultas, consultaId) };
        bloqueos.AddRange(articuloIds.Distinct().OrderBy(id => id).Select(id => (Transacciones.Articulos, id)));
        return Transacciones.EjecutarAsync(_context, bloqueos, accion);
    }
}
