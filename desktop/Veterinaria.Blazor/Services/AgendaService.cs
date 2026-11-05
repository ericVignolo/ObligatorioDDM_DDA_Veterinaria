using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Disponibilidad y turnos de demostración (A-014, A-015). Los horarios son orientativos:
// en la sección C la API los calcula y protege la agenda con SELECT … FOR UPDATE (DT-BD-02).
public class AgendaService
{
    // Reglas acordadas en P-RC-02 (parámetros por defecto, RF-ADM-10).
    public const int MinutosEntreHorarios = 15;
    public const int HorasDeAnticipacion = 2;
    public const int DiasHaciaAdelante = 60;
    public const int HorasParaCancelar = 24;

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

    public AgendaService(ProfesionalService profesionales, PrestacionService prestaciones, AnimalService animales)
    {
        _profesionales = profesionales;
        _prestaciones = prestaciones;
        _animales = animales;

        // Turnos de otros clientes, para que se vean horarios ocupados en la demostración.
        var manana = DateTime.Today.AddDays(1);
        _turnos = new()
        {
            new() { Id = 1, ClienteId = 0, AnimalId = 1, PrestacionId = 1, ProfesionalId = 1, Inicio = manana.AddHours(10), Fin = manana.AddHours(10).AddMinutes(30) },
            new() { Id = 2, ClienteId = 0, AnimalId = 1, PrestacionId = 2, ProfesionalId = 1, Inicio = manana.AddHours(12), Fin = manana.AddHours(12).AddMinutes(15) }
        };
        _ultimoId = _turnos.Count;
    }

    public bool EsFeriado(DateOnly fecha)
    {
        return _feriados.Contains(fecha);
    }

    public DateOnly PrimerDia => DateOnly.FromDateTime(DateTime.Now.AddHours(HorasDeAnticipacion));

    public DateOnly UltimoDia => DateOnly.FromDateTime(DateTime.Today.AddDays(DiasHaciaAdelante));

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
            Estado = "Confirmado",
            FechaCreacion = DateTime.Now
        };
        _turnos.Add(turno);

        return new ResultadoOperacion { Exito = true, Mensaje = "Turno confirmado." };
    }

    // Próximos turnos del cliente (RF-CLI-04).
    public List<Turno> ProximosDe(int clienteId)
    {
        return _turnos
            .Where(t => t.ClienteId == clienteId && t.Fin >= DateTime.Now && t.Estado == "Confirmado")
            .OrderBy(t => t.Inicio)
            .ToList();
    }

    // Turnos cancelados o ya pasados del cliente (RF-CLI-04).
    public List<Turno> HistorialDe(int clienteId)
    {
        return _turnos
            .Where(t => t.ClienteId == clienteId && (t.Estado != "Confirmado" || t.Fin < DateTime.Now))
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
        return turno.Estado == "Confirmado" && turno.Inicio - DateTime.Now > TimeSpan.FromHours(HorasParaCancelar);
    }

    public ResultadoOperacion Cancelar(int turnoId, int clienteId, string motivo)
    {
        var turno = ObtenerDe(turnoId, clienteId);
        if (turno is null || turno.Estado != "Confirmado")
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
        return new ResultadoOperacion { Exito = true, Mensaje = "El turno quedó cancelado y el horario se liberó." };
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
        return _turnos.Any(t => t.Estado == "Confirmado"
            && t.Id != excluirTurnoId
            && (t.ProfesionalId == profesionalId || t.AnimalId == animalId)
            && inicio < t.Fin
            && fin > t.Inicio);
    }
}
