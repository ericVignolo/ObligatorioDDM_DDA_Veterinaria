using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

public class ClinicaRepository : IClinicaRepository
{
    private readonly AppDbContext _context;

    public ClinicaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Clinica?> ObtenerAsync()
    {
        return await _context.Clinicas.AsNoTracking().OrderBy(c => c.Id).FirstOrDefaultAsync();
    }
}
