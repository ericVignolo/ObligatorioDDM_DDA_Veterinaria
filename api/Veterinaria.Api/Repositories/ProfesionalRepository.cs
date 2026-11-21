using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Repositories;

public class ProfesionalRepository : IProfesionalRepository
{
    private readonly AppDbContext _context;

    public ProfesionalRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> ContarPublicosAsync()
    {
        return await _context.Profesionales
            .Where(ReglasProfesionales.EsPublico)
            .CountAsync();
    }

    public async Task<List<Profesional>> ObtenerPublicosAsync(int saltar, int tomar)
    {
        return await ConRelaciones()
            .Where(ReglasProfesionales.EsPublico)
            .OrderBy(p => p.Id)
            .Skip(saltar)
            .Take(tomar)
            .ToListAsync();
    }

    public async Task<Profesional?> ObtenerPublicoPorIdAsync(int id)
    {
        return await ConRelaciones()
            .Where(ReglasProfesionales.EsPublico)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    // Especies, servicios y franjas en la misma consulta. AsSplitQuery evita multiplicar filas con tres colecciones.
    private IQueryable<Profesional> ConRelaciones()
    {
        return _context.Profesionales
            .AsNoTracking()
            .Include(p => p.Especies).ThenInclude(pe => pe.Especie)
            .Include(p => p.Prestaciones).ThenInclude(pp => pp.Prestacion)
            .Include(p => p.Franjas)
            .AsSplitQuery();
    }
}
