using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Bitácora de auditoría de demostración en memoria (A-027, RF-ADM-08). Solo se agrega; nada se modifica ni se borra.
// En la sección C la API registra cada acción relevante en la tabla Auditoria, en la misma transacción.
public class AuditoriaService
{
    private readonly List<RegistroAuditoria> _registros = new();
    private int _ultimo;

    public void Registrar(Personal persona, string entidad, string accion, string detalle)
    {
        _registros.Add(new RegistroAuditoria
        {
            Id = ++_ultimo,
            Fecha = DateTime.Now,
            Usuario = persona.Nombre,
            Rol = persona.Rol,
            Entidad = entidad,
            Accion = accion,
            Detalle = detalle
        });
    }

    // RF-ADM-08: filtros por usuario, entidad, acción y fechas; de la más reciente a la más antigua.
    public List<RegistroAuditoria> Buscar(string usuario = "", string entidad = "", string accion = "", DateOnly? desde = null, DateOnly? hasta = null)
    {
        return _registros
            .Where(r => string.IsNullOrWhiteSpace(usuario) || r.Usuario.Contains(usuario.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrWhiteSpace(entidad) || r.Entidad == entidad)
            .Where(r => string.IsNullOrWhiteSpace(accion) || r.Accion.Contains(accion.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(r => desde is null || DateOnly.FromDateTime(r.Fecha) >= desde)
            .Where(r => hasta is null || DateOnly.FromDateTime(r.Fecha) <= hasta)
            .OrderByDescending(r => r.Id)
            .ToList();
    }
}
