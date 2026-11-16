using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Bitácora de auditoría unificada (A-027 parte 2, RF-ADM-08, RN-40). Junta, sin copiar texto clínico:
// cambios de configuración, lecturas de historia clínica, firmas de consulta, movimientos de stock, cobros y notas de crédito,
// consentimientos, internaciones y eventos de asistencia. Solo lectura: nada se modifica ni se borra.
// En la sección C la API escribe todo en la tabla Auditoria y esta pantalla la consulta con filtros.
public class BitacoraService
{
    public const string Configuracion = "Configuración";
    public const string Historia = "Historia clínica";
    public const string Stock = "Inventario";
    public const string Cobros = "Cobros";
    public const string Consentimientos = "Consentimiento";
    public const string Internaciones = "Internación";
    public const string Asistencias = "Asistencia";

    public static readonly string[] Entidades = { Configuracion, Historia, Stock, Cobros, Consentimientos, Internaciones, Asistencias };

    private readonly AuditoriaService _auditoria;
    private readonly ConsultaService _consultas;
    private readonly InventarioService _inventario;
    private readonly CobroService _cobros;
    private readonly ConsentimientoService _consentimientos;
    private readonly InternacionService _internaciones;
    private readonly AsistenciaService _asistencias;
    private readonly PersonalService _personal;
    private readonly CuentaService _cuentas;

    public BitacoraService(AuditoriaService auditoria, ConsultaService consultas, InventarioService inventario, CobroService cobros,
        ConsentimientoService consentimientos, InternacionService internaciones, AsistenciaService asistencias,
        PersonalService personal, CuentaService cuentas)
    {
        _auditoria = auditoria;
        _consultas = consultas;
        _inventario = inventario;
        _cobros = cobros;
        _consentimientos = consentimientos;
        _internaciones = internaciones;
        _asistencias = asistencias;
        _personal = personal;
        _cuentas = cuentas;
    }

    // RF-ADM-08: filtra por usuario (contiene), entidad (exacta), acción (contiene) y fechas; lo más reciente primero.
    // Solo el Administrador consulta la bitácora; para otro rol devuelve una lista vacía.
    public List<RegistroAuditoria> Buscar(Personal? persona, string usuario = "", string entidad = "", string accion = "", DateOnly? desde = null, DateOnly? hasta = null)
    {
        if (persona is null || persona.Rol != PersonalService.Administrador || !persona.Activo)
        {
            return new();
        }

        return Todo()
            .Where(r => string.IsNullOrWhiteSpace(usuario) || r.Usuario.Contains(usuario.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(r => string.IsNullOrWhiteSpace(entidad) || r.Entidad == entidad)
            .Where(r => string.IsNullOrWhiteSpace(accion) || r.Accion.Contains(accion.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(r => desde is null || DateOnly.FromDateTime(r.Fecha) >= desde)
            .Where(r => hasta is null || DateOnly.FromDateTime(r.Fecha) <= hasta)
            .OrderByDescending(r => r.Fecha)
            .ToList();
    }

    private IEnumerable<RegistroAuditoria> Todo()
    {
        foreach (var r in _auditoria.Buscar())
        {
            yield return Registro(r.Fecha, r.Usuario, r.Rol, Configuracion, r.Accion, $"{r.Entidad}: {r.Detalle}");
        }

        // RN-40: lecturas de historia; solo animal y motivo, nunca el contenido.
        foreach (var l in _consultas.Lecturas())
        {
            var persona = _personal.ObtenerPorId(l.PersonalId);
            yield return Registro(l.Fecha, persona?.Nombre ?? "Personal", persona?.Rol ?? "", Historia, "Lectura de historia",
                $"Animal n.º {l.AnimalId}" + (string.IsNullOrEmpty(l.Motivo) ? " (con turno activo)" : $". Motivo: {l.Motivo}"));
        }

        foreach (var c in _consultas.Firmadas().Where(c => c.FechaFirma is not null))
        {
            yield return Registro(c.FechaFirma!.Value, c.AutorNombre, PersonalService.Veterinario, Historia, "Consulta firmada", $"Consulta n.º {c.Id}, animal n.º {c.AnimalId}");
            foreach (var e in c.Enmiendas)
            {
                yield return Registro(e.Fecha, e.AutorNombre, PersonalService.Veterinario, Historia, "Enmienda firmada", $"Consulta n.º {c.Id}");
            }
        }

        foreach (var m in _inventario.Movimientos())
        {
            var articulo = _inventario.ObtenerArticulo(m.ArticuloId)?.Nombre ?? "Artículo";
            yield return Registro(m.Fecha, m.PersonaNombre, m.PersonaId == 0 ? "Sistema" : _personal.ObtenerPorId(m.PersonaId)?.Rol ?? "",
                Stock, m.Tipo, $"{articulo}: {m.Cantidad:+#;-#;0} ({m.Origen}{(string.IsNullOrEmpty(m.Motivo) ? "" : ": " + m.Motivo)})");
        }

        foreach (var p in _cobros.Pagos())
        {
            yield return Registro(p.Fecha, p.PersonalNombre, PersonalService.Recepcion, Cobros, "Cobro", $"Comprobante {p.Numero}: $ {p.Monto:N0} ({p.Medio})");
        }

        foreach (var n in _cobros.Notas())
        {
            yield return Registro(n.Fecha, n.PersonalNombre, PersonalService.Administrador, Cobros, "Nota de crédito", $"{n.Numero} anula {_cobros.ObtenerPago(n.PagoId)?.Numero}: {n.Motivo}");
        }

        foreach (var r in _consentimientos.Respuestas())
        {
            var cuenta = _cuentas.ObtenerPorId(r.ClienteId);
            yield return Registro(r.Fecha, cuenta is null ? "Cliente" : $"{cuenta.Nombre} {cuenta.Apellido}", "Cliente", Consentimientos,
                r.Respuesta == ConsentimientoService.Aceptado ? "Consentimiento firmado" : "Consentimiento rechazado", $"Turno n.º {r.TurnoId}, versión {r.Version}");
        }

        foreach (var i in _internaciones.Todas())
        {
            yield return Registro(i.FechaAdmision, i.AdmitidoPor, PersonalService.Recepcion, Internaciones, "Admisión", $"Internación n.º {i.Id}, animal n.º {i.AnimalId}");
            if (i.FechaIngreso is not null)
            {
                yield return Registro(i.FechaIngreso.Value, i.VeterinarioIngreso, PersonalService.Veterinario, Internaciones, "Ingreso clínico", $"Internación n.º {i.Id}, box {_internaciones.ObtenerBox(i.BoxId)?.Nombre}");
            }

            if (i.FechaAlta is not null)
            {
                yield return Registro(i.FechaAlta.Value, i.VeterinarioAlta, PersonalService.Veterinario, Internaciones, "Alta", $"Internación n.º {i.Id}");
            }
        }

        foreach (var e in _asistencias.TodosLosEventos())
        {
            yield return Registro(e.Fecha, e.Autor, "", Asistencias, e.Tipo,
                $"Solicitud n.º {e.SolicitudId}" + (string.IsNullOrEmpty(e.NivelNuevo) ? "" : $": {e.NivelAnterior} → {e.NivelNuevo}"));
        }
    }

    private static RegistroAuditoria Registro(DateTime fecha, string usuario, string rol, string entidad, string accion, string detalle)
    {
        return new RegistroAuditoria { Fecha = fecha, Usuario = usuario, Rol = rol, Entidad = entidad, Accion = accion, Detalle = detalle };
    }
}
