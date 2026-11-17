using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Disponibilidad y turnos de demostración (A-014 a A-018). Los horarios son orientativos:
// en la sección C la API los calcula y protege la agenda con SELECT … FOR UPDATE (DT-BD-02).
public class AgendaService
{
    // Reglas acordadas en P-RC-02 (parámetros por defecto, RF-ADM-10).
    public const int MinutosEntreHorarios = 15;
    public const int HorasDeAnticipacion = 2;
    public const int DiasHaciaAdelante = 60;
    public const int HorasParaCancelar = 24;
    public const int MinutosToleranciaAusencia = 15;

    // RN-35: una cirugía programada espera la seña antes de confirmarse; mientras tanto conserva el horario.
    public const string PendienteSena = "Pendiente de seña";

    // RN-08: ausencias sin aviso en 12 meses. Desde AvisoAusencias se advierte; desde LimiteAusencias el cliente
    // ya no reserva solo y lo hace Recepción (decisión de Eric, 2026-11-17: 2 y 3, configurable).
    public const int AvisoAusencias = 2;
    public int LimiteAusencias { get; set; } = 3;

    // Estados que ocupan la agenda (FLUJO_RESERVA_CONSULTA §4.1).
    private static readonly string[] EstadosQueOcupan = { PendienteSena, "Confirmado", "En espera", "En curso" };

    public static bool Ocupa(Turno turno) => EstadosQueOcupan.Contains(turno.Estado);

    private readonly ProfesionalService _profesionales;
    private readonly PrestacionService _prestaciones;
    private readonly AnimalService _animales;
    private readonly List<Turno> _turnos;
    private int _ultimoId;

    // Días no laborables de la clínica.
    private readonly List<DateOnly> _feriados = new()
    {
        new DateOnly(2026, 12, 25),
        new DateOnly(2027, 1, 1),
        new DateOnly(2027, 1, 6)
    };

    private readonly AuditoriaService? _auditoria;

    public AgendaService(ProfesionalService profesionales, PrestacionService prestaciones, AnimalService animales, AuditoriaService? auditoria = null)
    {
        _auditoria = auditoria;
        _profesionales = profesionales;
        _prestaciones = prestaciones;
        _animales = animales;

        _turnos = new();
        CargarTurnosDemo(DateOnly.FromDateTime(DateTime.Today));
        CargarTurnosDemo(DateOnly.FromDateTime(DateTime.Today.AddDays(1)));
    }

    // Turnos de los clientes de demostración (Ana = 100, Pedro = 101) para hoy y mañana,
    // así las agendas del personal no aparecen vacías. Solo se cargan si el profesional trabaja ese día.
    private void CargarTurnosDemo(DateOnly fecha)
    {
        // (profesional, cliente, animal, servicio, hora, minuto)
        var demo = new[]
        {
            (1, 100, 1, 1, 9, 0),    // Ferreira: Toby, consulta
            (1, 100, 2, 2, 10, 0),   // Ferreira: Mishi, vacunación
            (1, 100, 2, 3, 11, 0),   // Ferreira: Mishi, desparasitación
            (5, 100, 1, 6, 12, 0),   // Méndez: Toby, baño y estética
            (2, 101, 3, 1, 14, 0),   // Silveira: rodeo, consulta
            (2, 101, 3, 2, 15, 0),   // Silveira: rodeo, vacunación
            (3, 101, 4, 1, 9, 0),    // Acosta: Canela, consulta
            (1, 100, 2, 8, 12, 0)    // Ferreira: Mishi, castración (pide consentimiento, A-025)
        };

        foreach (var (profesionalId, clienteId, animalId, prestacionId, hora, minuto) in demo)
        {
            var profesional = _profesionales.ObtenerPorId(profesionalId);
            var prestacion = _prestaciones.ObtenerPorId(prestacionId);
            var inicio = fecha.ToDateTime(new TimeOnly(hora, minuto));

            if (profesional is null || prestacion is null || !profesional.Jornada.Any(f => f.Dia == fecha.DayOfWeek))
            {
                continue;
            }

            _ultimoId++;
            _turnos.Add(new Turno
            {
                Id = _ultimoId,
                ClienteId = clienteId,
                AnimalId = animalId,
                PrestacionId = prestacionId,
                ProfesionalId = profesionalId,
                Inicio = inicio,
                Fin = inicio.AddMinutes(prestacion.DuracionMinutos),
                FechaCreacion = DateTime.Today.AddDays(-3)
            });
        }
    }

    // Feriados y turnos afectados (A-027, RF-ADM-04). El permiso lo controla AdministracionService.
    public List<DateOnly> Feriados()
    {
        return _feriados.OrderBy(f => f).ToList();
    }

    public void AgregarFeriado(DateOnly fecha)
    {
        if (!_feriados.Contains(fecha))
        {
            _feriados.Add(fecha);
        }
    }

    public bool QuitarFeriado(DateOnly fecha)
    {
        return _feriados.Remove(fecha);
    }

    // Todos los turnos de un rango, en cualquier estado (indicadores, RF-ADM-07).
    public List<Turno> TurnosEntre(DateOnly desde, DateOnly hasta)
    {
        return _turnos.Where(t => DateOnly.FromDateTime(t.Inicio) >= desde && DateOnly.FromDateTime(t.Inicio) <= hasta).ToList();
    }

    // Turnos que siguen activos (Confirmado o En espera) en un rango, de un profesional o de todos.
    public List<Turno> TurnosActivosEntre(DateOnly desde, DateOnly hasta, int? profesionalId = null)
    {
        return _turnos
            .Where(t => (t.Estado == "Confirmado" || t.Estado == "En espera")
                && DateOnly.FromDateTime(t.Inicio) >= desde && DateOnly.FromDateTime(t.Inicio) <= hasta
                && (profesionalId is null || t.ProfesionalId == profesionalId))
            .ToList();
    }

    public bool EsFeriado(DateOnly fecha)
    {
        return _feriados.Contains(fecha);
    }

    public DateOnly PrimerDia => DateOnly.FromDateTime(DateTime.Now.AddHours(HorasDeAnticipacion));

    public DateOnly UltimoDia => DateOnly.FromDateTime(DateTime.Today.AddDays(DiasHaciaAdelante));

    // Primer día, desde `desde`, con al menos un horario libre para ese profesional, servicio y animal.
    // Evita que una cirugía larga (90 min) muestre «sin horarios» en un día ya cargado (RF-CLI-04).
    public DateOnly? ProximoDiaConHorarios(int profesionalId, int prestacionId, int animalId, DateOnly desde, int excluirTurnoId = 0)
    {
        var dia = desde < PrimerDia ? PrimerDia : desde;

        for (; dia <= UltimoDia; dia = dia.AddDays(1))
        {
            if (HorariosLibres(profesionalId, prestacionId, animalId, dia, excluirTurnoId).Count > 0)
            {
                return dia;
            }
        }

        return null;
    }

    // Horarios de inicio libres cada 15 minutos. Un turno ocupa toda su duración:
    // una consulta de 30 min a las 09:00 bloquea también las 09:15 (RN-01, RN-02, RN-07).
    // excluirTurnoId: al reprogramar, el turno que se mueve no se cuenta como ocupado.
    public List<DateTime> HorariosLibres(int profesionalId, int prestacionId, int animalId, DateOnly fecha, int excluirTurnoId = 0)
    {
        var libres = new List<DateTime>();
        var profesional = _profesionales.ObtenerPorId(profesionalId);
        var prestacion = _prestaciones.ObtenerPorId(prestacionId);

        if (profesional is null || prestacion is null || prestacion.DuracionMinutos <= 0)
        {
            return libres;
        }

        // Antelación mínima, horizonte, feriados y ausencias (RN-05, RF-ADM-04).
        if (fecha < PrimerDia || fecha > UltimoDia || EsFeriado(fecha))
        {
            return libres;
        }

        if (profesional.Ausencias.Any(a => fecha >= a.Desde && fecha <= a.Hasta))
        {
            return libres;
        }

        var duracion = TimeSpan.FromMinutes(prestacion.DuracionMinutos);
        var minimo = DateTime.Now.AddHours(HorasDeAnticipacion);
        var dia = fecha.ToDateTime(TimeOnly.MinValue);

        foreach (var franja in profesional.Jornada.Where(f => f.Dia == fecha.DayOfWeek))
        {
            var inicio = dia + franja.Inicio.ToTimeSpan();
            var finJornada = dia + franja.Fin.ToTimeSpan();

            for (var horario = inicio; horario + duracion <= finJornada; horario = horario.AddMinutes(MinutosEntreHorarios))
            {
                var fin = horario + duracion;

                if (horario < minimo)
                {
                    continue;
                }

                // No puede pisar un descanso del profesional.
                var pisaDescanso = profesional.Descansos.Any(d => d.Dia == fecha.DayOfWeek
                    && horario < dia + d.Fin.ToTimeSpan()
                    && fin > dia + d.Inicio.ToTimeSpan());

                if (!pisaDescanso && !Superpone(profesionalId, animalId, horario, fin, excluirTurnoId))
                {
                    libres.Add(horario);
                }
            }
        }

        return libres;
    }

    // Revalida todo al confirmar: el horario pudo ocuparse mientras el cliente elegía (CU-CLI-02, paso 5).
    public ResultadoOperacion Reservar(int clienteId, int animalId, int prestacionId, int profesionalId, DateTime inicio, out Turno? turno)
    {
        turno = null;

        // RN-18: el animal tiene que ser del cliente.
        if (_animales.ObtenerPorId(animalId, clienteId) is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No encontramos ese animal entre los tuyos." };
        }

        // RN-08: con demasiadas ausencias sin aviso, el turno se pide en Recepción.
        if (BloqueadoPorAusencias(clienteId))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = MensajeAusencias(clienteId) };
        }

        var prestacion = _prestaciones.ObtenerPorId(prestacionId);
        if (prestacion is null || !HorariosLibres(profesionalId, prestacionId, animalId, DateOnly.FromDateTime(inicio)).Contains(inicio))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ese horario ya no está disponible. Te mostramos los horarios actualizados." };
        }

        _ultimoId++;
        turno = new Turno
        {
            Id = _ultimoId,
            ClienteId = clienteId,
            AnimalId = animalId,
            PrestacionId = prestacionId,
            ProfesionalId = profesionalId,
            Inicio = inicio,
            Fin = inicio.AddMinutes(prestacion.DuracionMinutos),
            Estado = EstadoInicial(prestacion),
            FechaCreacion = DateTime.Now
        };
        _turnos.Add(turno);

        return new ResultadoOperacion { Exito = true, Mensaje = turno.Estado == PendienteSena ? MensajeSena : "Turno confirmado." };
    }

    public const string MensajeSena = "Horario reservado. La cirugía se confirma cuando pagues la seña en Recepción (RN-35).";

    // RN-35: las cirugías (las prestaciones que piden consentimiento) arrancan esperando la seña.
    public static string EstadoInicial(Prestacion prestacion)
    {
        return prestacion.RequiereConsentimiento ? PendienteSena : "Confirmado";
    }

    // Lo llama CobroService cuando la seña quedó pagada, y al anularla para volver atrás.
    public bool ConfirmarSena(int turnoId)
    {
        var turno = ObtenerPorId(turnoId);
        if (turno is null || turno.Estado != PendienteSena)
        {
            return false;
        }

        turno.Estado = "Confirmado";
        return true;
    }

    public bool VolverAPendienteSena(int turnoId)
    {
        var turno = ObtenerPorId(turnoId);
        if (turno is null || turno.Estado != "Confirmado")
        {
            return false;
        }

        turno.Estado = PendienteSena;
        return true;
    }

    // RN-08: ausencias sin aviso del cliente en los últimos 12 meses.
    public int AusenciasRecientes(int clienteId)
    {
        var desde = DateTime.Now.AddMonths(-12);
        return _turnos.Count(t => t.ClienteId == clienteId && t.Estado == "Ausente" && t.Inicio >= desde);
    }

    public bool BloqueadoPorAusencias(int clienteId) => AusenciasRecientes(clienteId) >= LimiteAusencias;

    // Texto para el cliente: vacío si no tiene ausencias para advertir.
    public string MensajeAusencias(int clienteId)
    {
        var ausencias = AusenciasRecientes(clienteId);
        if (ausencias >= LimiteAusencias)
        {
            return $"Tenés {ausencias} turnos sin asistir ni avisar en los últimos 12 meses: para reservar comunicate con Recepción.";
        }

        if (ausencias >= AvisoAusencias)
        {
            return $"Tenés {ausencias} turnos sin asistir ni avisar en los últimos 12 meses. Si llegás a {LimiteAusencias}, vas a tener que reservar por Recepción. Si no podés venir, cancelá con anticipación.";
        }

        return "";
    }

    // RF-REC-02 y RN-09: turno dado en el mostrador. Sin sobreturno respeta los horarios libres; con sobreturno puede
    // ocupar un horario ya tomado o fuera de la grilla, solo lo da Recepción o el Administrador y exige motivo.
    // El profesional tiene que atender ese día y el animal no puede tener otro turno a esa hora. No aplica RN-08.
    public ResultadoOperacion ReservarPorMostrador(int clienteId, int animalId, int prestacionId, int profesionalId, DateTime inicio,
        bool sobreturno, string motivo, Personal? persona, out Turno? turno)
    {
        turno = null;
        if (persona is null || (persona.Rol != PersonalService.Recepcion && persona.Rol != PersonalService.Administrador))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo Recepción o el Administrador dan turnos por mostrador." };
        }

        if (_animales.ObtenerPorId(animalId, clienteId) is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ese animal no es de ese cliente." };
        }

        var prestacion = _prestaciones.ObtenerPorId(prestacionId);
        var profesional = _profesionales.ObtenerPorId(profesionalId);
        if (prestacion is null || prestacion.DuracionMinutos <= 0 || profesional is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Elegí un servicio con turno y un profesional." };
        }

        var fecha = DateOnly.FromDateTime(inicio);
        var fin = inicio.AddMinutes(prestacion.DuracionMinutos);
        motivo = (motivo ?? "").Trim();

        if (!sobreturno)
        {
            if (!HorariosLibres(profesionalId, prestacionId, animalId, fecha).Contains(inicio))
            {
                return new ResultadoOperacion { Exito = false, Mensaje = "Ese horario no está libre. Elegí otro o marcá sobreturno con su motivo." };
            }
        }
        else
        {
            if (motivo.Length < 5)
            {
                return new ResultadoOperacion { Exito = false, Mensaje = "Para un sobreturno escribí el motivo (al menos 5 caracteres)." };
            }

            if (inicio < DateTime.Now.AddMinutes(-MinutosEntreHorarios))
            {
                return new ResultadoOperacion { Exito = false, Mensaje = "No se da un sobreturno en un horario que ya pasó." };
            }

            var atiende = profesional.Jornada.Where(f => f.Dia == fecha.DayOfWeek)
                .Any(f => inicio >= fecha.ToDateTime(f.Inicio) && inicio < fecha.ToDateTime(f.Fin));
            if (EsFeriado(fecha) || profesional.Ausencias.Any(a => fecha >= a.Desde && fecha <= a.Hasta) || !atiende)
            {
                return new ResultadoOperacion { Exito = false, Mensaje = $"{profesional.Nombre} no atiende en ese horario: el sobreturno tiene que caer dentro de su jornada." };
            }

            if (_turnos.Any(t => Ocupa(t) && t.AnimalId == animalId && t.Inicio < fin && t.Fin > inicio))
            {
                return new ResultadoOperacion { Exito = false, Mensaje = "Ese animal ya tiene un turno en ese horario." };
            }
        }

        _ultimoId++;
        turno = new Turno
        {
            Id = _ultimoId,
            ClienteId = clienteId,
            AnimalId = animalId,
            PrestacionId = prestacionId,
            ProfesionalId = profesionalId,
            Inicio = inicio,
            Fin = fin,
            Estado = EstadoInicial(prestacion),
            FechaCreacion = DateTime.Now,
            EsSobreturno = sobreturno,
            MotivoSobreturno = sobreturno ? motivo : "",
            CreadoPor = persona.Nombre
        };
        _turnos.Add(turno);

        // RN-09: el sobreturno queda en la bitácora (sin el motivo, que puede ser clínico).
        if (sobreturno)
        {
            _auditoria?.Registrar(persona, BitacoraService.Agenda, "Sobreturno", $"turno n.º {turno.Id} con {profesional.Nombre}, {inicio:dd/MM/yyyy HH:mm}");
        }

        var texto = sobreturno ? "Sobreturno registrado." : "Turno registrado.";
        if (turno.Estado == PendienteSena)
        {
            texto += " La cirugía se confirma al cobrar la seña.";
        }

        return new ResultadoOperacion { Exito = true, Mensaje = texto };
    }

    // Próximos turnos del cliente (RF-CLI-04).
    public List<Turno> ProximosDe(int clienteId)
    {
        return _turnos
            .Where(t => t.ClienteId == clienteId && t.Fin >= DateTime.Now && Ocupa(t))
            .OrderBy(t => t.Inicio)
            .ToList();
    }

    // Turnos cancelados o ya pasados del cliente (RF-CLI-04).
    public List<Turno> HistorialDe(int clienteId)
    {
        return _turnos
            .Where(t => t.ClienteId == clienteId && (!Ocupa(t) || t.Fin < DateTime.Now))
            .OrderByDescending(t => t.Inicio)
            .ToList();
    }

    // Solo turnos del propio cliente (RN-18).
    public Turno? ObtenerDe(int turnoId, int clienteId)
    {
        return _turnos.FirstOrDefault(t => t.Id == turnoId && t.ClienteId == clienteId);
    }

    // RN-06: el cliente cancela o reprograma solo un turno Confirmado y con más de 24 h de anticipación.
    public bool PuedeGestionar(Turno turno)
    {
        return (turno.Estado == "Confirmado" || turno.Estado == PendienteSena) && turno.Inicio - DateTime.Now > TimeSpan.FromHours(HorasParaCancelar);
    }

    public ResultadoOperacion Cancelar(int turnoId, int clienteId, string motivo)
    {
        var turno = ObtenerDe(turnoId, clienteId);
        if (turno is null || (turno.Estado != "Confirmado" && turno.Estado != PendienteSena))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ese turno ya no se puede cancelar." };
        }

        if (!PuedeGestionar(turno))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"Faltan menos de {HorasParaCancelar} horas: la cancelación se hace por Recepción." };
        }

        // El horario queda libre porque solo los turnos Confirmado ocupan la agenda.
        turno.Estado = "Cancelado";
        turno.MotivoCancelacion = (motivo ?? "").Trim();
        turno.FechaCancelacion = DateTime.Now;
        turno.CanceladoPor = "Cliente";
        return new ResultadoOperacion { Exito = true, Mensaje = "El turno quedó cancelado y el horario se liberó." };
    }

    // ---------- Personal (A-017): agenda de Recepción y del Veterinario ----------

    // Turnos de un día, opcionalmente de un profesional y en un estado.
    public List<Turno> TurnosDelDia(DateOnly fecha, int? profesionalId = null, string? estado = null)
    {
        return _turnos
            .Where(t => DateOnly.FromDateTime(t.Inicio) == fecha)
            .Where(t => profesionalId is null || t.ProfesionalId == profesionalId)
            .Where(t => string.IsNullOrEmpty(estado) || t.Estado == estado)
            .OrderBy(t => t.Inicio)
            .ToList();
    }

    public Turno? ObtenerPorId(int turnoId)
    {
        return _turnos.FirstOrDefault(t => t.Id == turnoId);
    }

    // Recepción: Confirmado → En espera, el mismo día del turno.
    public ResultadoOperacion RegistrarLlegada(int turnoId)
    {
        var turno = ObtenerPorId(turnoId);
        if (turno is null || turno.Estado != "Confirmado")
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo se registra la llegada de un turno Confirmado." };
        }

        if (turno.Inicio.Date != DateTime.Today)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "La llegada se registra el mismo día del turno." };
        }

        turno.Estado = "En espera";
        turno.HoraLlegada = DateTime.Now;
        return new ResultadoOperacion { Exito = true, Mensaje = "Llegada registrada: el turno pasó a En espera." };
    }

    // Recepción: Confirmado → Ausente, recién pasada la tolerancia (nunca automático).
    public ResultadoOperacion RegistrarAusencia(int turnoId)
    {
        var turno = ObtenerPorId(turnoId);
        if (turno is null || turno.Estado != "Confirmado")
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo se marca ausente un turno Confirmado." };
        }

        if (DateTime.Now < turno.Inicio.AddMinutes(MinutosToleranciaAusencia))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"Todavía no pasaron {MinutosToleranciaAusencia} minutos desde la hora del turno." };
        }

        turno.Estado = "Ausente";
        return new ResultadoOperacion { Exito = true, Mensaje = "El turno quedó como Ausente." };
    }

    // Recepción: cancela en cualquier momento (incluso dentro de las 24 h) o registra el retiro de alguien En espera.
    public ResultadoOperacion CancelarPorRecepcion(int turnoId, string motivo)
    {
        var turno = ObtenerPorId(turnoId);
        if (turno is null || (turno.Estado != "Confirmado" && turno.Estado != "En espera" && turno.Estado != PendienteSena))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Este turno ya no se puede cancelar." };
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Escribí el motivo de la cancelación." };
        }

        var eraRetiro = turno.Estado == "En espera";
        turno.Estado = "Cancelado";
        turno.MotivoCancelacion = motivo.Trim();
        turno.FechaCancelacion = DateTime.Now;
        turno.CanceladoPor = "Recepción";
        return new ResultadoOperacion { Exito = true, Mensaje = eraRetiro ? "Retiro registrado: el turno quedó Cancelado." : "El turno quedó cancelado y el horario se liberó." };
    }

    // Ocupación del día de un profesional: minutos con turnos activos sobre minutos de atención (sin descansos).
    public (int Ocupados, int Disponibles) Ocupacion(int profesionalId, DateOnly fecha)
    {
        var profesional = _profesionales.ObtenerPorId(profesionalId);
        if (profesional is null || EsFeriado(fecha) || profesional.Ausencias.Any(a => fecha >= a.Desde && fecha <= a.Hasta))
        {
            return (0, 0);
        }

        var jornada = profesional.Jornada.Where(f => f.Dia == fecha.DayOfWeek).Sum(f => (f.Fin - f.Inicio).TotalMinutes);
        var descansos = profesional.Descansos.Where(d => d.Dia == fecha.DayOfWeek).Sum(d => (d.Fin - d.Inicio).TotalMinutes);
        var ocupados = TurnosDelDia(fecha, profesionalId).Where(Ocupa).Sum(t => (t.Fin - t.Inicio).TotalMinutes);

        return ((int)ocupados, (int)(jornada - descansos));
    }

    // Cambia el horario del mismo turno. Si el nuevo horario no está libre, el turno queda como estaba (atómico).
    public ResultadoOperacion Reprogramar(int turnoId, int clienteId, DateTime nuevoInicio)
    {
        var turno = ObtenerDe(turnoId, clienteId);
        if (turno is null || !PuedeGestionar(turno))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"Este turno ya no se puede reprogramar. Si faltan menos de {HorasParaCancelar} horas, consultá en Recepción." };
        }

        var libres = HorariosLibres(turno.ProfesionalId, turno.PrestacionId, turno.AnimalId, DateOnly.FromDateTime(nuevoInicio), turno.Id);
        if (!libres.Contains(nuevoInicio))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ese horario ya no está disponible. Tu turno sigue como estaba; te mostramos los horarios actualizados." };
        }

        var duracion = turno.Fin - turno.Inicio;
        turno.InicioAnterior = turno.Inicio;
        turno.Inicio = nuevoInicio;
        turno.Fin = nuevoInicio + duracion;
        return new ResultadoOperacion { Exito = true, Mensaje = "Turno reprogramado." };
    }

    // ---------- Consulta clínica (A-019): los llama ConsultaService, no las páginas ----------

    // En espera → En curso, al iniciar la atención.
    public bool IniciarAtencion(int turnoId)
    {
        var turno = ObtenerPorId(turnoId);
        if (turno is null || turno.Estado != "En espera")
        {
            return false;
        }

        turno.Estado = "En curso";
        return true;
    }

    // En curso → Finalizado, al firmar la consulta (RN-11).
    public bool Finalizar(int turnoId)
    {
        var turno = ObtenerPorId(turnoId);
        if (turno is null || turno.Estado != "En curso")
        {
            return false;
        }

        turno.Estado = "Finalizado";
        return true;
    }

    // En curso → Cancelado, solo si no hubo atención ni consumos (P-RC-04).
    public bool CancelarSinAtencion(int turnoId, string motivo)
    {
        var turno = ObtenerPorId(turnoId);
        if (turno is null || turno.Estado != "En curso" || string.IsNullOrWhiteSpace(motivo))
        {
            return false;
        }

        turno.Estado = "Cancelado";
        turno.MotivoCancelacion = motivo.Trim();
        turno.FechaCancelacion = DateTime.Now;
        turno.CanceladoPor = "Veterinario";
        return true;
    }

    // P-RC-05: con un turno activo cualquier veterinario lee la historia sin dar un motivo.
    public bool TieneTurnoActivo(int animalId)
    {
        // Un Confirmado de un día anterior que nunca se cerró no habilita el acceso.
        return _turnos.Any(t => t.AnimalId == animalId && Ocupa(t)
            && (t.Estado != "Confirmado" || t.Inicio.Date >= DateTime.Today));
    }

    // Para el ejercicio de demostración: ocupa un horario como si otro cliente lo hubiera reservado.
    public void OcuparComoOtroCliente(int profesionalId, int prestacionId, DateTime inicio)
    {
        var prestacion = _prestaciones.ObtenerPorId(prestacionId);
        if (prestacion is null)
        {
            return;
        }

        _ultimoId++;
        _turnos.Add(new Turno
        {
            Id = _ultimoId,
            ClienteId = 0,
            AnimalId = 1,
            PrestacionId = prestacionId,
            ProfesionalId = profesionalId,
            Inicio = inicio,
            Fin = inicio.AddMinutes(prestacion.DuracionMinutos),
            FechaCreacion = DateTime.Now
        });
    }

    // Choca con otro turno activo del mismo profesional o del mismo animal.
    private bool Superpone(int profesionalId, int animalId, DateTime inicio, DateTime fin, int excluirTurnoId)
    {
        return _turnos.Any(t => Ocupa(t)
            && t.Id != excluirTurnoId
            && (t.ProfesionalId == profesionalId || t.AnimalId == animalId)
            && inicio < t.Fin
            && fin > t.Inicio);
    }
}
