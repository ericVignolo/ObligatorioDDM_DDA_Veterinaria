using System.ComponentModel.DataAnnotations;
using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Acciones de configuración del Administrador (A-027): servicios y tarifas (RF-ADM-02), usuarios y roles (RF-ADM-01),
// licencias y feriados (RF-ADM-04), boxes (RF-ADM-03) y parámetros (RF-ADM-10, RF-ADM-13).
// Cada acción exige el rol Administrador, valida y queda en la bitácora (RF-ADM-08).
// En la sección C cada una es un endpoint de la API con [Authorize(Roles = "Administrador")].
public class AdministracionService
{
    private readonly PrestacionService _prestaciones;
    private readonly PersonalService _personal;
    private readonly ProfesionalService _profesionales;
    private readonly AgendaService _agenda;
    private readonly InternacionService _internaciones;
    private readonly AsistenciaService _asistencias;
    private readonly InventarioService _inventario;
    private readonly VacunaService _vacunas;
    private readonly ConsentimientoService _consentimientos;
    private readonly AuditoriaService _auditoria;

    public AdministracionService(PrestacionService prestaciones, PersonalService personal, ProfesionalService profesionales,
        AgendaService agenda, InternacionService internaciones, AsistenciaService asistencias, InventarioService inventario,
        VacunaService vacunas, ConsentimientoService consentimientos, AuditoriaService auditoria)
    {
        _prestaciones = prestaciones;
        _personal = personal;
        _profesionales = profesionales;
        _agenda = agenda;
        _internaciones = internaciones;
        _asistencias = asistencias;
        _inventario = inventario;
        _vacunas = vacunas;
        _consentimientos = consentimientos;
        _auditoria = auditoria;
    }

    public static readonly string[] Roles = { PersonalService.Recepcion, PersonalService.Veterinario, PersonalService.Administrador };

    private static bool EsAdministrador(Personal? persona)
    {
        return persona is not null && persona.Rol == PersonalService.Administrador && persona.Activo;
    }

    private static ResultadoOperacion SinPermiso()
    {
        return new ResultadoOperacion { Exito = false, Mensaje = "Solo el Administrador cambia la configuración." };
    }

    private static ResultadoOperacion Error(string mensaje)
    {
        return new ResultadoOperacion { Exito = false, Mensaje = mensaje };
    }

    private ResultadoOperacion Hecho(Personal admin, string entidad, string accion, string detalle)
    {
        _auditoria.Registrar(admin, entidad, accion, detalle);
        return new ResultadoOperacion { Exito = true, Mensaje = $"{accion}: {detalle}" };
    }

    private static bool Valido(object datos)
    {
        return Validator.TryValidateObject(datos, new ValidationContext(datos), new List<ValidationResult>(), validateAllProperties: true);
    }

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);

    // ---------- Servicios y tarifas (RF-ADM-02) ----------

    public ResultadoOperacion CrearServicio(PrestacionFormulario datos, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        var error = ValidarServicio(datos, idActual: 0);
        if (error is not null)
        {
            return error;
        }

        var prestacion = _prestaciones.Agregar(datos, admin!.Nombre);
        return Hecho(admin, "Servicio", "Servicio creado", $"{prestacion.Nombre}, $ {datos.Precio:N0}");
    }

    // El precio no se edita acá: se programa una tarifa con fecha (RN-33).
    public ResultadoOperacion EditarServicio(int id, PrestacionFormulario datos, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        var prestacion = _prestaciones.ObtenerFicha(id);
        if (prestacion is null)
        {
            return Error("Ese servicio no existe.");
        }

        // El precio del formulario no se usa al editar; se completa para pasar la validación.
        datos.Precio = Math.Max(1, (int)prestacion.PrecioDesde);
        var error = ValidarServicio(datos, id);
        if (error is not null)
        {
            return error;
        }

        PrestacionService.CopiarDatos(datos, prestacion);
        return Hecho(admin!, "Servicio", "Servicio editado", $"{prestacion.Nombre}: {(prestacion.Publicado ? "publicado" : "no publicado")}, {(prestacion.ReservableEnLinea ? $"reservable en línea ({prestacion.DuracionMinutos} min)" : "no reservable en línea")}");
    }

    private ResultadoOperacion? ValidarServicio(PrestacionFormulario datos, int idActual)
    {
        if (!Valido(datos))
        {
            return Error("Corregí los campos marcados: nombre, precio entre $ 1 y $ 1.000.000 y duración hasta 480 minutos.");
        }

        if (datos.ReservableEnLinea && datos.DuracionMinutos < 15)
        {
            return Error("Un servicio que se reserva en línea necesita una duración de al menos 15 minutos.");
        }

        if (_prestaciones.ObtenerTodas().Any(p => p.Id != idActual && string.Equals(p.Nombre.Trim(), datos.Nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Error("Ya hay un servicio con ese nombre.");
        }

        return null;
    }

    // RN-33: el precio nuevo rige desde hoy o una fecha futura; lo ya atendido conserva su precio.
    public ResultadoOperacion ProgramarTarifa(TarifaFormulario datos, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        if (!Valido(datos))
        {
            return Error("Elegí el servicio, un precio entre $ 1 y $ 1.000.000 y la fecha.");
        }

        var prestacion = _prestaciones.ObtenerFicha(datos.PrestacionId);
        if (prestacion is null)
        {
            return Error("Ese servicio no existe.");
        }

        if (datos.VigenteDesde!.Value < Hoy)
        {
            return Error("La tarifa nueva rige desde hoy o una fecha futura: no se cambian precios pasados (RN-33).");
        }

        _prestaciones.ProgramarTarifa(prestacion.Id, datos.Precio, datos.VigenteDesde.Value, admin!.Nombre);
        return Hecho(admin, "Tarifa", "Tarifa programada", $"{prestacion.Nombre}: $ {datos.Precio:N0} desde el {datos.VigenteDesde.Value:dd/MM/yyyy}");
    }

    // ---------- Usuarios y roles (RF-ADM-01) ----------

    public ResultadoOperacion CrearPersonal(PersonalFormulario datos, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        if (!Valido(datos) || !Roles.Contains(datos.Rol))
        {
            return Error("Corregí los campos marcados: nombre, correo válido y rol.");
        }

        var correo = datos.Correo.Trim().ToLowerInvariant();
        if (_personal.ObtenerTodos().Any(p => p.Correo == correo))
        {
            return Error("Ya hay una cuenta del personal con ese correo.");
        }

        int? profesionalId = null;
        if (datos.Rol == PersonalService.Veterinario)
        {
            var error = ValidarProfesional(datos.ProfesionalId, personaId: 0);
            if (error is not null)
            {
                return error;
            }

            profesionalId = datos.ProfesionalId;
        }

        var persona = _personal.Agregar(datos.Nombre, correo, datos.Rol, profesionalId);
        return Hecho(admin!, "Personal", "Cuenta creada", $"{persona.Nombre} ({persona.Rol}, {persona.Correo})");
    }

    // Un veterinario necesita una ficha de profesional que no esté asociada a otra cuenta.
    private ResultadoOperacion? ValidarProfesional(int profesionalId, int personaId)
    {
        if (_profesionales.ObtenerFicha(profesionalId) is null)
        {
            return Error("Un veterinario necesita su ficha de profesional (matrícula, agenda y especies).");
        }

        if (_personal.ObtenerTodos().Any(p => p.Id != personaId && p.ProfesionalId == profesionalId))
        {
            return Error("Esa ficha de profesional ya está asociada a otra cuenta.");
        }

        return null;
    }

    public ResultadoOperacion CambiarRol(int personaId, string rol, int profesionalId, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        var persona = _personal.ObtenerPorId(personaId);
        if (persona is null || !Roles.Contains(rol))
        {
            return Error("Elegí una cuenta y un rol válidos.");
        }

        if (persona.Id == admin!.Id)
        {
            return Error("No podés cambiar tu propio rol.");
        }

        if (persona.Rol == PersonalService.Administrador && rol != PersonalService.Administrador && !QuedaOtroAdministrador(persona.Id))
        {
            return Error("Tiene que quedar al menos un Administrador activo.");
        }

        if (persona.Id == _asistencias.GuardiaId && rol != PersonalService.Veterinario)
        {
            return Error("Es el veterinario de guardia: asigná otra guardia antes de cambiarle el rol.");
        }

        int? nuevoProfesional = null;
        if (rol == PersonalService.Veterinario)
        {
            var error = ValidarProfesional(profesionalId, persona.Id);
            if (error is not null)
            {
                return error;
            }

            nuevoProfesional = profesionalId;
        }

        var anterior = persona.Rol;
        persona.Rol = rol;
        persona.ProfesionalId = nuevoProfesional;
        return Hecho(admin, "Personal", "Rol cambiado", $"{persona.Nombre}: {anterior} → {rol}");
    }

    // Bloquear impide ingresar; no borra la cuenta ni su historial.
    public ResultadoOperacion CambiarBloqueo(int personaId, bool bloquear, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        var persona = _personal.ObtenerPorId(personaId);
        if (persona is null)
        {
            return Error("Esa cuenta no existe.");
        }

        if (bloquear && persona.Id == admin!.Id)
        {
            return Error("No podés bloquear tu propia cuenta.");
        }

        if (bloquear && persona.Rol == PersonalService.Administrador && !QuedaOtroAdministrador(persona.Id))
        {
            return Error("Tiene que quedar al menos un Administrador activo.");
        }

        if (bloquear && persona.Id == _asistencias.GuardiaId)
        {
            return Error("Es el veterinario de guardia: asigná otra guardia antes de bloquearlo.");
        }

        if (persona.Activo == !bloquear)
        {
            return Error(bloquear ? "La cuenta ya está bloqueada." : "La cuenta ya está activa.");
        }

        persona.Activo = !bloquear;
        return Hecho(admin!, "Personal", bloquear ? "Cuenta bloqueada" : "Cuenta desbloqueada", persona.Nombre);
    }

    private bool QuedaOtroAdministrador(int exceptoId)
    {
        return _personal.ObtenerTodos().Any(p => p.Id != exceptoId && p.Rol == PersonalService.Administrador && p.Activo);
    }

    // ---------- Licencias y feriados (RF-ADM-04) ----------

    // No se carga una ausencia sobre turnos activos: primero se reprograman o cancelan.
    public ResultadoOperacion AgregarAusencia(AusenciaFormulario datos, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        if (!Valido(datos))
        {
            return Error("Elegí el profesional, las fechas y el motivo.");
        }

        var profesional = _profesionales.ObtenerFicha(datos.ProfesionalId);
        if (profesional is null)
        {
            return Error("Ese profesional no existe.");
        }

        var desde = datos.Desde!.Value;
        var hasta = datos.Hasta!.Value;
        if (hasta < desde || desde < Hoy)
        {
            return Error("Las fechas tienen que ser de hoy en adelante y «hasta» no puede ser antes que «desde».");
        }

        if (profesional.Ausencias.Any(a => desde <= a.Hasta && hasta >= a.Desde))
        {
            return Error("Se superpone con otra ausencia de este profesional.");
        }

        var turnos = _agenda.TurnosActivosEntre(desde, hasta, profesional.Id);
        if (turnos.Count > 0)
        {
            return Error($"Hay {turnos.Count} turno(s) activo(s) en esas fechas: reprogramalos o cancelalos antes.");
        }

        profesional.Ausencias.Add(new Ausencia { Desde = desde, Hasta = hasta, Motivo = datos.Motivo.Trim() });
        return Hecho(admin!, "Horario", "Ausencia agregada", $"{profesional.Nombre}: {desde:dd/MM/yyyy} a {hasta:dd/MM/yyyy} ({datos.Motivo.Trim()})");
    }

    public ResultadoOperacion QuitarAusencia(int profesionalId, DateOnly desde, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        var profesional = _profesionales.ObtenerFicha(profesionalId);
        var ausencia = profesional?.Ausencias.FirstOrDefault(a => a.Desde == desde);
        if (profesional is null || ausencia is null)
        {
            return Error("Esa ausencia no existe.");
        }

        profesional.Ausencias.Remove(ausencia);
        return Hecho(admin!, "Horario", "Ausencia quitada", $"{profesional.Nombre}: {ausencia.Desde:dd/MM/yyyy} a {ausencia.Hasta:dd/MM/yyyy}");
    }

    public ResultadoOperacion AgregarFeriado(DateOnly? fecha, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        if (fecha is null || fecha.Value < Hoy)
        {
            return Error("Elegí una fecha de hoy en adelante.");
        }

        if (_agenda.EsFeriado(fecha.Value))
        {
            return Error("Ese día ya es feriado.");
        }

        var turnos = _agenda.TurnosActivosEntre(fecha.Value, fecha.Value);
        if (turnos.Count > 0)
        {
            return Error($"Hay {turnos.Count} turno(s) activo(s) ese día: reprogramalos o cancelalos antes.");
        }

        _agenda.AgregarFeriado(fecha.Value);
        return Hecho(admin!, "Feriado", "Feriado agregado", fecha.Value.ToString("dd/MM/yyyy"));
    }

    public ResultadoOperacion QuitarFeriado(DateOnly fecha, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        return _agenda.QuitarFeriado(fecha)
            ? Hecho(admin!, "Feriado", "Feriado quitado", fecha.ToString("dd/MM/yyyy"))
            : Error("Ese día no es feriado.");
    }

    // ---------- Boxes (RF-ADM-03) ----------

    public ResultadoOperacion AgregarBox(string nombre, string tipo, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 40
            || (tipo != InternacionService.PequenosAnimales && tipo != InternacionService.GrandesAnimales))
        {
            return Error("Escribí el nombre (hasta 40 caracteres) y elegí el tipo.");
        }

        if (_internaciones.Boxes().Any(b => string.Equals(b.Nombre, nombre.Trim(), StringComparison.OrdinalIgnoreCase)))
        {
            return Error("Ya hay un box con ese nombre.");
        }

        var box = _internaciones.AgregarBox(nombre, tipo);
        return Hecho(admin!, "Box", "Box agregado", $"{box.Nombre} ({box.Tipo})");
    }

    // Un box ocupado no se saca de servicio.
    public ResultadoOperacion CambiarServicioBox(int boxId, bool activo, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        var box = _internaciones.ObtenerBox(boxId);
        if (box is null)
        {
            return Error("Ese box no existe.");
        }

        if (!activo && _internaciones.OcupanteDe(box.Id) is not null)
        {
            return Error($"{box.Nombre} tiene un animal internado: dale el alta antes.");
        }

        box.Activo = activo;
        return Hecho(admin!, "Box", activo ? "Box en servicio" : "Box fuera de servicio", box.Nombre);
    }

    // ---------- Parámetros (RF-ADM-10, RF-ADM-13) ----------

    public ResultadoOperacion CambiarGuardia(int personaId, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        var persona = _personal.ObtenerPorId(personaId);
        if (persona is null || persona.Rol != PersonalService.Veterinario || !persona.Activo)
        {
            return Error("La guardia tiene que ser un veterinario activo.");
        }

        _asistencias.GuardiaId = persona.Id;
        return Hecho(admin!, "Parámetro", "Guardia asignada", persona.Nombre);
    }

    public ResultadoOperacion CambiarParametrosAsistencia(int coberturaKm, int minutosRojo, int tarifaBase, int porKm, int porAnimal, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        if (coberturaKm is < 1 or > 200 || minutosRojo is < 1 or > 60 || tarifaBase is < 0 or > 100000 || porKm is < 0 or > 10000 || porAnimal is < 0 or > 10000)
        {
            return Error("Cobertura entre 1 y 200 km, espera del rojo entre 1 y 60 minutos y tarifas entre $ 0 y los máximos permitidos.");
        }

        _asistencias.Cobertura = coberturaKm;
        _asistencias.MinutosEscalarRojo = minutosRojo;
        _asistencias.Base = tarifaBase;
        _asistencias.PorKm = porKm;
        _asistencias.PorAnimalExtra = porAnimal;
        return Hecho(admin!, "Parámetro", "Asistencia actualizada", $"cobertura {coberturaKm} km, rojo {minutosRojo} min, $ {tarifaBase:N0} + $ {porKm:N0}/km + $ {porAnimal:N0}/animal extra");
    }

    // Días de «próximo a vencer» del inventario y de aviso de refuerzo (RF-ADM-10; el aviso va de 3 a 5 días, decisión del 31/10).
    public ResultadoOperacion CambiarDiasAvisos(int diasVencimiento, int diasRefuerzo, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        if (diasVencimiento is < 1 or > 180 || diasRefuerzo is < 3 or > 5)
        {
            return Error("Próximo a vencer entre 1 y 180 días; aviso de refuerzo entre 3 y 5 días.");
        }

        _inventario.DiasProximoAVencer = diasVencimiento;
        _vacunas.DiasAvisoRefuerzo = diasRefuerzo;
        return Hecho(admin!, "Parámetro", "Avisos actualizados", $"próximo a vencer {diasVencimiento} días, refuerzo {diasRefuerzo} días antes");
    }

    // RF-ADM-10: umbral de stock mínimo por artículo.
    public ResultadoOperacion CambiarStockMinimo(int articuloId, int minimo, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        var articulo = _inventario.ObtenerArticulo(articuloId);
        if (articulo is null || minimo is < 0 or > 100000)
        {
            return Error("Elegí un artículo y un mínimo entre 0 y 100000.");
        }

        var anterior = articulo.StockMinimo;
        articulo.StockMinimo = minimo;
        return Hecho(admin!, "Parámetro", "Stock mínimo cambiado", $"{articulo.Nombre}: {anterior} → {minimo}");
    }

    // Versión nueva del consentimiento: lo firmado con la anterior hay que volver a firmarlo (K-07).
    public ResultadoOperacion PublicarConsentimiento(int prestacionId, string version, string texto, Personal? admin)
    {
        if (!EsAdministrador(admin))
        {
            return SinPermiso();
        }

        var resultado = _consentimientos.PublicarVersion(prestacionId, version, texto, admin);
        if (!resultado.Exito)
        {
            return resultado;
        }

        return Hecho(admin!, "Consentimiento", "Versión publicada", $"prestación {prestacionId}, versión {version.Trim()}");
    }
}
