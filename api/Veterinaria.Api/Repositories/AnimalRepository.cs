using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

public class AnimalRepository : IAnimalRepository
{
    private static readonly string[] EstadosTerminados = { "Cancelado", "Finalizado", "Ausente" };

    private readonly AppDbContext _context;

    public AnimalRepository(AppDbContext context)
    {
        _context = context;
    }

    // Animales activos con una responsabilidad vigente de la cuenta (RN-18).
    private IQueryable<Animal> DeCliente(int cuentaId)
    {
        return _context.Animales
            .Include(a => a.Especie)
            .Where(a => a.Activo && a.Responsables.Any(r => r.CuentaId == cuentaId && r.Hasta == null));
    }

    public async Task<int> ContarDeClienteAsync(int cuentaId)
    {
        return await DeCliente(cuentaId).CountAsync();
    }

    public async Task<List<Animal>> ObtenerDeClienteAsync(int cuentaId, int saltar, int tomar)
    {
        return await DeCliente(cuentaId)
            .AsNoTracking()
            .OrderBy(a => a.Nombre)
            .ThenBy(a => a.Id)
            .Skip(saltar)
            .Take(tomar)
            .ToListAsync();
    }

    public async Task<Animal?> ObtenerDeClienteAsync(int id, int cuentaId)
    {
        return await DeCliente(cuentaId).FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<bool> ExisteMicrochipAsync(string microchip, int? excepto)
    {
        return await _context.Animales.AnyAsync(a => a.Microchip == microchip && a.Id != excepto);
    }

    public async Task<Especie?> ObtenerEspecieAsync(int id)
    {
        return await _context.Especies.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Especie>> ObtenerEspeciesAsync()
    {
        return await _context.Especies.AsNoTracking().OrderBy(e => e.Id).ToListAsync();
    }

    public async Task<bool> TieneTurnosPendientesAsync(int animalId, DateTime ahora)
    {
        return await _context.Turnos.AnyAsync(t => t.AnimalId == animalId && t.Fin > ahora && !EstadosTerminados.Contains(t.Estado));
    }

    public void Agregar(Animal animal) => _context.Animales.Add(animal);

    public async Task GuardarAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new MicrochipDuplicadoException(ex);
        }
    }
}
