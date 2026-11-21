using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

public class SaludRepository : ISaludRepository
{
    private static readonly string[] TurnoAbierto = { "Confirmado", "En espera", "Pendiente de seña" };

    private readonly AppDbContext _context;

    public SaludRepository(AppDbContext context)
    {
        _context = context;
    }

    // ---------- Animales y cuentas ----------

    public async Task<Animal?> ObtenerAnimalAsync(int animalId)
    {
        return await _context.Animales.Include(a => a.Especie).FirstOrDefaultAsync(a => a.Id == animalId);
    }

    public async Task<bool> EsResponsableAsync(int animalId, int cuentaId)
    {
        return await _context.Responsabilidades.AnyAsync(r => r.AnimalId == animalId && r.CuentaId == cuentaId && r.Hasta == null);
    }

    public async Task<List<Animal>> ObtenerAnimalesDeClienteAsync(int cuentaId)
    {
        return await _context.Animales
            .AsNoTracking()
            .Where(a => a.Activo && a.Responsables.Any(r => r.CuentaId == cuentaId && r.Hasta == null))
            .OrderBy(a => a.Nombre)
            .ToListAsync();
    }

    public async Task<Cuenta?> ObtenerCuentaAsync(int cuentaId)
    {
        return await _context.Cuentas.FirstOrDefaultAsync(c => c.Id == cuentaId);
    }

    public async Task<Dictionary<string, string>> ObtenerParametrosAsync()
    {
        return await _context.Parametros.AsNoTracking().ToDictionaryAsync(p => p.Clave, p => p.Valor);
    }

    // ---------- Carnet y avisos ----------

    public async Task<List<AplicacionSanitaria>> ObtenerAplicacionesQueCuentanAsync(IEnumerable<int> animalIds)
    {
        var ids = animalIds.ToList();
        return await _context.AplicacionesSanitarias
            .AsNoTracking()
            .Where(a => ids.Contains(a.AnimalId) && (a.ConsultaId == null || a.Consulta!.Estado == "Firmada"))
            .OrderByDescending(a => a.FechaAplicacion).ThenByDescending(a => a.Id)
            .ToListAsync();
    }

    public async Task<List<Animal>> ObtenerAnimalesConRefuerzoHastaAsync(DateTime hasta)
    {
        return await _context.Animales
            .AsNoTracking()
            .Include(a => a.Responsables)
            .Where(a => a.Activo && _context.AplicacionesSanitarias.Any(x => x.AnimalId == a.Id && x.FechaRefuerzo <= hasta
                && (x.ConsultaId == null || x.Consulta!.Estado == "Firmada")))
            .ToListAsync();
    }

    public async Task<bool> ExisteComunicacionAsync(int cuentaId, int tipoComunicacionId, string clave)
    {
        return await _context.Comunicaciones.AnyAsync(c => c.CuentaId == cuentaId && c.TipoComunicacionId == tipoComunicacionId && c.Clave == clave);
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

    // ---------- Inventario ----------

    public async Task<List<Articulo>> ObtenerArticulosAsync()
    {
        return await _context.Articulos.AsNoTracking().Where(a => a.Activo).OrderBy(a => a.Id).ToListAsync();
    }

    public async Task<Articulo?> ObtenerArticuloAsync(int articuloId)
    {
        return await _context.Articulos.FirstOrDefaultAsync(a => a.Id == articuloId && a.Activo);
    }

    public async Task<List<LoteProducto>> ObtenerLotesAsync(int? articuloId)
    {
        return await _context.Lotes
            .Where(l => articuloId == null || l.ArticuloId == articuloId)
            .OrderBy(l => l.Vencimiento).ThenBy(l => l.Id)
            .ToListAsync();
    }

    public async Task<LoteProducto?> ObtenerLoteAsync(int loteId)
    {
        return await _context.Lotes.Include(l => l.Articulo).FirstOrDefaultAsync(l => l.Id == loteId);
    }

    public async Task<Dictionary<int, int>> ReservadoPorLoteAsync(int? articuloId)
    {
        return await _context.ReservasStock
            .Where(r => !r.Confirmada && (articuloId == null || r.ArticuloId == articuloId))
            .GroupBy(r => r.LoteId)
            .Select(g => new { LoteId = g.Key, Cantidad = g.Sum(r => r.Cantidad) })
            .ToDictionaryAsync(x => x.LoteId, x => x.Cantidad);
    }

    public async Task<bool> ExisteLoteAsync(int articuloId, string numero)
    {
        return await _context.Lotes.AnyAsync(l => l.ArticuloId == articuloId && l.Numero == numero);
    }

    public async Task<List<MovimientoStock>> ObtenerMovimientosAsync(int? articuloId, int saltar, int tomar)
    {
        return await _context.MovimientosStock
            .AsNoTracking()
            .Include(m => m.Articulo)
            .Include(m => m.Lote)
            .Include(m => m.Persona)
            .Where(m => articuloId == null || m.ArticuloId == articuloId)
            .OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id)
            .Skip(saltar).Take(tomar)
            .ToListAsync();
    }

    public async Task<int> ContarMovimientosAsync(int? articuloId)
    {
        return await _context.MovimientosStock.CountAsync(m => articuloId == null || m.ArticuloId == articuloId);
    }

    public async Task<List<AlertaStock>> ObtenerAlertasAsync(bool soloPendientes)
    {
        return await _context.AlertasStock
            .AsNoTracking()
            .Include(a => a.Articulo)
            .Where(a => !soloPendientes || !a.Atendida)
            .OrderByDescending(a => a.Fecha)
            .ToListAsync();
    }

    public async Task<AlertaStock?> ObtenerAlertaAsync(int alertaId)
    {
        return await _context.AlertasStock.Include(a => a.Articulo).FirstOrDefaultAsync(a => a.Id == alertaId);
    }

    public async Task<List<ProductoSanitario>> ObtenerProductosAsync()
    {
        return await _context.ProductosSanitarios.AsNoTracking().Include(p => p.Especies).OrderBy(p => p.Id).ToListAsync();
    }

    // ---------- Consentimiento ----------

    public async Task<Turno?> ObtenerTurnoAsync(int turnoId)
    {
        return await _context.Turnos.Include(t => t.Prestacion).Include(t => t.Animal).FirstOrDefaultAsync(t => t.Id == turnoId);
    }

    public async Task<List<Turno>> ObtenerTurnosConConsentimientoAsync(int clienteId, DateTime ahora)
    {
        return await _context.Turnos
            .AsNoTracking()
            .Include(t => t.Prestacion)
            .Include(t => t.Animal)
            .Where(t => t.ClienteId == clienteId && t.Fin > ahora && TurnoAbierto.Contains(t.Estado) && t.Prestacion!.RequiereConsentimiento)
            .OrderBy(t => t.Inicio)
            .ToListAsync();
    }

    public async Task<DocumentoConsentimiento?> ObtenerDocumentoVigenteAsync(int prestacionId)
    {
        return await _context.DocumentosConsentimiento
            .Where(d => d.PrestacionId == prestacionId && d.Vigente)
            .OrderByDescending(d => d.FechaVigencia).ThenByDescending(d => d.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<DocumentoConsentimiento>> ObtenerDocumentosAsync(int prestacionId)
    {
        return await _context.DocumentosConsentimiento.Where(d => d.PrestacionId == prestacionId).ToListAsync();
    }

    public async Task<Consentimiento?> ObtenerUltimaRespuestaAsync(int turnoId)
    {
        return await _context.Consentimientos
            .Where(c => c.TurnoId == turnoId)
            .OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<Prestacion?> ObtenerPrestacionAsync(int prestacionId)
    {
        return await _context.Prestaciones.FirstOrDefaultAsync(p => p.Id == prestacionId);
    }

    public void Agregar<T>(T entidad) where T : class => _context.Add(entidad);

    public async Task GuardarAsync()
    {
        await _context.SaveChangesAsync();
    }

    public Task<T> EnTransaccionAsync<T>(IEnumerable<int> articuloIds, Func<Task<T>> accion)
    {
        var bloqueos = articuloIds.Distinct().OrderBy(id => id).Select(id => (Transacciones.Articulos, id));
        return Transacciones.EjecutarAsync(_context, bloqueos, accion);
    }
}
