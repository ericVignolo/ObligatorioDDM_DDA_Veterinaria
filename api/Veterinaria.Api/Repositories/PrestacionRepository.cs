using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Repositories;

public class PrestacionRepository : IPrestacionRepository
{
    private readonly AppDbContext _context;

    public PrestacionRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> ContarPublicasAsync()
    {
        return await _context.Prestaciones
            .Where(ReglasCatalogo.EsPublica)
            .CountAsync();
    }

    // Orden estable por Id para que las páginas no se mezclen (CAT-05).
    public async Task<List<Prestacion>> ObtenerPublicasAsync(int saltar, int tomar)
    {
        return await _context.Prestaciones
            .AsNoTracking()
            .Where(ReglasCatalogo.EsPublica)
            .OrderBy(p => p.Id)
            .Skip(saltar)
            .Take(tomar)
            .ToListAsync();
    }

    public async Task<Prestacion?> ObtenerPublicaPorIdAsync(int id)
    {
        return await _context.Prestaciones
            .AsNoTracking()
            .Where(ReglasCatalogo.EsPublica)
            .FirstOrDefaultAsync(p => p.Id == id);
    }
}
