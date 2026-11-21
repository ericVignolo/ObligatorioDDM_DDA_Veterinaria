using System.Data;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Veterinaria.Api.Data;
using Veterinaria.Api.Models;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Repositories;

public class TurnoRepository : ITurnoRepository
{
    private static readonly int[] EsperasMs = { 50, 100, 200 };
    private static readonly string[] EstadosTerminados = { "Cancelado", "Finalizado", "Ausente" };

    private readonly AppDbContext _context;

    public TurnoRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Profesional?> ObtenerProfesionalAsync(int id)
    {
        return await _context.Profesionales
            .Include(p => p.Franjas)
            .Include(p => p.Ausencias)
            .Include(p => p.Especies)
            .Include(p => p.Prestaciones)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<int?> ObtenerProfesionalIdDeCuentaAsync(int cuentaId)
    {
        return await _context.Profesionales
            .Where(p => p.CuentaId == cuentaId && p.Activo)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();
    }

    public async Task<Prestacion?> ObtenerPrestacionAsync(int id)
    {
        return await _context.Prestaciones.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Animal?> ObtenerAnimalDeClienteAsync(int animalId, int clienteId)
    {
        return await _context.Animales
            .Include(a => a.Especie)
            .FirstOrDefaultAsync(a => a.Id == animalId && a.Activo && a.Responsables.Any(r => r.CuentaId == clienteId && r.Hasta == null));
    }

    public async Task<List<Turno>> ObtenerTurnosAsync(int profesionalId, int animalId, DateTime desde, DateTime hasta)
    {
        return await _context.Turnos
            .AsNoTracking()
            .Where(t => (t.ProfesionalId == profesionalId || t.AnimalId == animalId) && t.Inicio < hasta && t.Fin > desde)
            .Where(t => CalculoAgenda.EstadosQueOcupan.Contains(t.Estado))
            .ToListAsync();
    }

    public async Task<HashSet<DateOnly>> ObtenerFeriadosAsync(DateOnly desde, DateOnly hasta)
    {
        var inicio = desde.ToDateTime(TimeOnly.MinValue);
        var fin = hasta.ToDateTime(TimeOnly.MinValue);
        var fechas = await _context.DiasNoLaborables.Where(d => d.Fecha >= inicio && d.Fecha <= fin).Select(d => d.Fecha).ToListAsync();
        return fechas.Select(DateOnly.FromDateTime).ToHashSet();
    }

    public async Task<Dictionary<string, string>> ObtenerParametrosAsync()
    {
        return await _context.Parametros.AsNoTracking().ToDictionaryAsync(p => p.Clave, p => p.Valor);
    }

    public async Task<int> ContarAusenciasAsync(int clienteId, DateTime desde)
    {
        return await _context.Turnos.CountAsync(t => t.ClienteId == clienteId && t.Estado == "Ausente" && t.Inicio >= desde);
    }

    private IQueryable<Turno> ConDatos()
    {
        return _context.Turnos
            .Include(t => t.Animal).ThenInclude(a => a!.Especie)
            .Include(t => t.Prestacion)
            .Include(t => t.Profesional)
            .Include(t => t.Cliente);
    }

    private IQueryable<Turno> DeCliente(int clienteId, bool proximos, DateTime ahora)
    {
        var consulta = _context.Turnos.Where(t => t.ClienteId == clienteId);
        return proximos
            ? consulta.Where(t => t.Fin > ahora && !EstadosTerminados.Contains(t.Estado))
            : consulta.Where(t => t.Fin <= ahora || EstadosTerminados.Contains(t.Estado));
    }

    public async Task<List<Turno>> ObtenerDeClienteAsync(int clienteId, bool proximos, DateTime ahora, int saltar, int tomar)
    {
        var ids = DeCliente(clienteId, proximos, ahora).Select(t => t.Id);
        var consulta = ConDatos().AsNoTracking().Where(t => ids.Contains(t.Id));
        consulta = proximos ? consulta.OrderBy(t => t.Inicio) : consulta.OrderByDescending(t => t.Inicio);
        return await consulta.Skip(saltar).Take(tomar).ToListAsync();
    }

    public async Task<int> ContarDeClienteAsync(int clienteId, bool proximos, DateTime ahora)
    {
        return await DeCliente(clienteId, proximos, ahora).CountAsync();
    }

    public async Task<Turno?> ObtenerAsync(int id)
    {
        return await ConDatos().FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<List<Turno>> ObtenerDelDiaAsync(DateOnly fecha, int? profesionalId, string? estado)
    {
        var desde = fecha.ToDateTime(TimeOnly.MinValue);
        var hasta = desde.AddDays(1);
        var consulta = ConDatos().AsNoTracking().Where(t => t.Inicio >= desde && t.Inicio < hasta);

        if (profesionalId != null)
        {
            consulta = consulta.Where(t => t.ProfesionalId == profesionalId);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            consulta = consulta.Where(t => t.Estado == estado);
        }

        return await consulta.OrderBy(t => t.Inicio).ThenBy(t => t.Id).ToListAsync();
    }

    // Clientes: cuentas activas con rol Cliente (Id 1).
    private IQueryable<Cuenta> Clientes(string texto)
    {
        var t = (texto ?? "").Trim().ToLower();
        return _context.Cuentas
            .Where(c => c.Activa && !c.Eliminada && c.Roles.Any(r => r.RolId == CuentaService.RolClienteId))
            .Where(c => t == "" || c.Nombre.ToLower().Contains(t) || c.Apellido.ToLower().Contains(t)
                || c.Correo.Contains(t) || c.NumeroDocumento.Contains(t));
    }

    public async Task<List<Cuenta>> BuscarClientesAsync(string texto, int saltar, int tomar)
    {
        return await Clientes(texto).AsNoTracking().OrderBy(c => c.Apellido).ThenBy(c => c.Nombre).ThenBy(c => c.Id).Skip(saltar).Take(tomar).ToListAsync();
    }

    public async Task<int> ContarClientesAsync(string texto)
    {
        return await Clientes(texto).CountAsync();
    }

    public async Task<List<Animal>> ObtenerAnimalesDeClienteAsync(int clienteId)
    {
        return await _context.Animales
            .AsNoTracking()
            .Include(a => a.Especie)
            .Where(a => a.Activo && a.Responsables.Any(r => r.CuentaId == clienteId && r.Hasta == null))
            .OrderBy(a => a.Nombre)
            .ToListAsync();
    }

    public void Agregar(Turno turno) => _context.Turnos.Add(turno);
    public void AgregarAuditoria(Auditoria registro) => _context.Auditorias.Add(registro);

    public async Task GuardarAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<T> EnTransaccionConBloqueoAsync<T>(int profesionalId, int animalId, Func<Task<T>> accion)
    {
        for (var intento = 0; ; intento++)
        {
            await using var transaccion = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted);
            try
            {
                // Orden fijo: primero el profesional, después el animal (evita interbloqueos entre dos reservas).
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT `Id` FROM `Profesionales` WHERE `Id` = {profesionalId} FOR UPDATE");
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT `Id` FROM `Animales` WHERE `Id` = {animalId} FOR UPDATE");

                var resultado = await accion();
                await transaccion.CommitAsync();
                return resultado;
            }
            catch (Exception ex) when (intento < EsperasMs.Length && EsInterbloqueo(ex))
            {
                await transaccion.RollbackAsync();
                _context.ChangeTracker.Clear();
                await Task.Delay(EsperasMs[intento]);
            }
        }
    }

    // 1213: interbloqueo; 1205: tiempo de espera del bloqueo agotado.
    private static bool EsInterbloqueo(Exception ex)
    {
        for (Exception? e = ex; e != null; e = e.InnerException)
        {
            if (e is MySqlException mysql && (mysql.Number == 1213 || mysql.Number == 1205))
            {
                return true;
            }
        }

        return false;
    }
}
