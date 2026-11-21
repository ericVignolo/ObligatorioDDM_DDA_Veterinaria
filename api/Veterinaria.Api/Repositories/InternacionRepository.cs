using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

public class InternacionRepository : IInternacionRepository
{
    private static readonly string[] Abiertas = { "Admitida", "Internado" };

    private readonly AppDbContext _context;

    public InternacionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Animal?> ObtenerAnimalAsync(int animalId)
    {
        return await _context.Animales
            .Include(a => a.Especie)
            .Include(a => a.Responsables.Where(r => r.Hasta == null))
            .FirstOrDefaultAsync(a => a.Id == animalId && a.Activo);
    }

    public async Task<bool> EsResponsableAsync(int animalId, int cuentaId)
    {
        return await _context.Responsabilidades.AnyAsync(r => r.AnimalId == animalId && r.CuentaId == cuentaId && r.Hasta == null);
    }

    public async Task<Internacion?> ObtenerAbiertaDeAnimalAsync(int animalId)
    {
        return await _context.Internaciones.FirstOrDefaultAsync(i => i.AnimalId == animalId && Abiertas.Contains(i.Estado));
    }

    private IQueryable<Internacion> ConDatos()
    {
        return _context.Internaciones
            .Include(i => i.Animal).ThenInclude(a => a!.Especie)
            .Include(i => i.Cliente)
            .Include(i => i.Box);
    }

    public async Task<Internacion?> ObtenerAsync(int id)
    {
        return await ConDatos().FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<List<Internacion>> ObtenerAbiertasAsync()
    {
        return await ConDatos().AsNoTracking()
            .Where(i => Abiertas.Contains(i.Estado))
            .OrderBy(i => i.Estado == "Admitida" ? 0 : 1).ThenBy(i => i.FechaAdmision)
            .ToListAsync();
    }

    public async Task<List<Internacion>> ObtenerAltasAsync(int tomar)
    {
        return await ConDatos().AsNoTracking()
            .Where(i => i.Estado == "Alta")
            .OrderByDescending(i => i.FechaAlta)
            .Take(tomar)
            .ToListAsync();
    }

    public async Task<List<Internacion>> ObtenerDeClienteAsync(int clienteId)
    {
        return await ConDatos().AsNoTracking()
            .Where(i => i.Estado != "Anulada" && _context.Responsabilidades.Any(r => r.AnimalId == i.AnimalId && r.CuentaId == clienteId && r.Hasta == null))
            .OrderByDescending(i => i.FechaAdmision)
            .ToListAsync();
    }

    public async Task<List<Box>> ObtenerBoxesAsync()
    {
        return await _context.Boxes.AsNoTracking().OrderBy(b => b.Id).ToListAsync();
    }

    public async Task<Box?> ObtenerBoxAsync(int boxId)
    {
        return await _context.Boxes.FirstOrDefaultAsync(b => b.Id == boxId);
    }

    public async Task<bool> ExisteBoxConNombreAsync(string nombre)
    {
        return await _context.Boxes.AnyAsync(b => b.Nombre == nombre);
    }

    public async Task<Internacion?> ObtenerOcupanteAsync(int boxId)
    {
        return await _context.Internaciones.Include(i => i.Animal).FirstOrDefaultAsync(i => i.BoxId == boxId && i.Estado == "Internado");
    }

    public async Task<List<Internacion>> ObtenerOcupantesAsync()
    {
        return await _context.Internaciones.AsNoTracking().Include(i => i.Animal).Where(i => i.BoxId != null && i.Estado == "Internado").ToListAsync();
    }

    public async Task<List<NovedadInternacion>> ObtenerNovedadesAsync(int internacionId, bool soloVisibles)
    {
        return await _context.NovedadesInternacion
            .AsNoTracking()
            .Include(n => n.Autor)
            .Where(n => n.InternacionId == internacionId && (!soloVisibles || n.Visible))
            .OrderByDescending(n => n.Fecha).ThenByDescending(n => n.Id)
            .ToListAsync();
    }

    public async Task<NovedadInternacion?> ObtenerNovedadAsync(int novedadId)
    {
        return await _context.NovedadesInternacion.FirstOrDefaultAsync(n => n.Id == novedadId);
    }

    public void Agregar<T>(T entidad) where T : class => _context.Add(entidad);

    public async Task GuardarAsync()
    {
        await _context.SaveChangesAsync();
    }

    public Task<T> EnTransaccionConAnimalAsync<T>(int animalId, Func<Task<T>> accion)
        => Transacciones.EjecutarAsync(_context, new[] { (Transacciones.Animales, animalId) }, accion);

    public Task<T> EnTransaccionConBoxAsync<T>(int boxId, Func<Task<T>> accion)
        => Transacciones.EjecutarAsync(_context, new[] { (Transacciones.Boxes, boxId) }, accion);
}
