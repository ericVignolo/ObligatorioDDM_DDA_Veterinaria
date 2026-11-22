using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

public class CuentaRepository : ICuentaRepository
{
    private readonly AppDbContext _context;

    public CuentaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Cuenta?> ObtenerPorCorreoAsync(string correo)
    {
        return await _context.Cuentas
            .Include(c => c.Roles).ThenInclude(cr => cr.Rol)
            .FirstOrDefaultAsync(c => c.Correo == correo);
    }

    public async Task<Cuenta?> ObtenerPorIdAsync(int id)
    {
        return await _context.Cuentas
            .Include(c => c.Roles).ThenInclude(cr => cr.Rol)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<bool> ExisteDocumentoAsync(string tipo, string numero)
    {
        return await _context.Cuentas.AnyAsync(c => c.TipoDocumento == tipo && c.NumeroDocumento == numero && !c.Eliminada);
    }

    public async Task<List<DocumentoLegal>> ObtenerDocumentosVigentesAsync(DateTime hoy)
    {
        var vigentes = await _context.DocumentosLegales
            .AsNoTracking()
            .Where(d => d.VigenteDesde <= hoy)
            .ToListAsync();

        return vigentes
            .GroupBy(d => d.Codigo)
            .Select(g => g.OrderByDescending(d => d.VigenteDesde).First())
            .ToList();
    }

    public async Task<int> ContarCodigosDesdeAsync(int cuentaId, string tipo, DateTime desde)
    {
        return await _context.CodigosCuenta.CountAsync(c => c.CuentaId == cuentaId && c.Tipo == tipo && c.FechaCreacion >= desde);
    }

    public async Task<CodigoCuenta?> ObtenerUltimoCodigoAsync(int cuentaId, string tipo)
    {
        return await _context.CodigosCuenta
            .Where(c => c.CuentaId == cuentaId && c.Tipo == tipo)
            .OrderByDescending(c => c.FechaCreacion)
            .ThenByDescending(c => c.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<List<CodigoCuenta>> ObtenerCodigosSinUsarAsync(int cuentaId, string tipo)
    {
        return await _context.CodigosCuenta
            .Where(c => c.CuentaId == cuentaId && c.Tipo == tipo && !c.Usado)
            .ToListAsync();
    }

    public async Task<SesionRefresco?> ObtenerSesionAsync(string hashToken)
    {
        return await _context.SesionesRefresco.FirstOrDefaultAsync(s => s.HashToken == hashToken);
    }

    public async Task<List<SesionRefresco>> ObtenerSesionesActivasAsync(int cuentaId, DateTime ahora)
    {
        return await _context.SesionesRefresco
            .Where(s => s.CuentaId == cuentaId && s.RevocadaEn == null && s.Vence > ahora)
            .ToListAsync();
    }

    public void AgregarCuenta(Cuenta cuenta) => _context.Cuentas.Add(cuenta);
    public void AgregarCodigo(CodigoCuenta codigo) => _context.CodigosCuenta.Add(codigo);
    public void AgregarSesion(SesionRefresco sesion) => _context.SesionesRefresco.Add(sesion);

    public async Task EliminarNoVerificadaAsync(Cuenta cuenta)
    {
        _context.CodigosCuenta.RemoveRange(_context.CodigosCuenta.Where(c => c.CuentaId == cuenta.Id));
        _context.AceptacionesLegales.RemoveRange(_context.AceptacionesLegales.Where(a => a.CuentaId == cuenta.Id));
        _context.SesionesRefresco.RemoveRange(_context.SesionesRefresco.Where(s => s.CuentaId == cuenta.Id));
        _context.CuentaRoles.RemoveRange(_context.CuentaRoles.Where(cr => cr.CuentaId == cuenta.Id));
        _context.Cuentas.Remove(cuenta);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Cuenta>> ObtenerNoVerificadasAntesDeAsync(DateTime limite)
    {
        return await _context.Cuentas.Where(c => !c.Verificada && !c.Eliminada && c.FechaCreacion < limite).ToListAsync();
    }

    public async Task GuardarAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase) == true)
        {
            throw new CuentaDuplicadaException(ex);
        }
    }
}
