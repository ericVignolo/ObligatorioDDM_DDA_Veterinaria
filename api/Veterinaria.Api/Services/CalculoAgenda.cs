using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

// Parámetros de la agenda (P-RC-02, RF-ADM-10). Se leen de la tabla Parametros; si falta uno, vale el de acá.
public class ReglasAgenda
{
    public int AnticipacionHoras { get; set; } = 2;
    public int HorizonteDias { get; set; } = 60;
    public int IntervaloMinutos { get; set; } = 15;
    public int CancelacionHoras { get; set; } = 24;
    public int ToleranciaAusenciaMinutos { get; set; } = 15;

    // RN-08: desde AvisoAusencias se advierte; desde LimiteAusencias el cliente reserva por Recepción.
    public int AvisoAusencias { get; set; } = 2;
    public int LimiteAusencias { get; set; } = 3;

    public static ReglasAgenda Desde(IReadOnlyDictionary<string, string> parametros)
    {
        int Leer(string clave, int porDefecto)
            => parametros.TryGetValue(clave, out var valor) && int.TryParse(valor, out var numero) && numero >= 0 ? numero : porDefecto;

        return new ReglasAgenda
        {
            AnticipacionHoras = Leer("agenda.anticipacion_minima_horas", 2),
            HorizonteDias = Leer("agenda.horizonte_dias", 60),
            IntervaloMinutos = Math.Max(5, Leer("agenda.intervalo_inicio_minutos", 15)),
            CancelacionHoras = Leer("agenda.cancelacion_sin_costo_horas", 24),
            ToleranciaAusenciaMinutos = Leer("agenda.tolerancia_ausencia_minutos", 15),
            AvisoAusencias = Leer("agenda.aviso_ausencias", 2),
            LimiteAusencias = Leer("agenda.limite_ausencias", 3)
        };
    }
}

// Cálculo de horarios libres, igual que AgendaService de Blazor (RN-01, RN-02, RN-05, RN-07, RF-ADM-04).
// Es puro: recibe todo lo que necesita y no toca la base, para probarlo sin MySQL.
public static class CalculoAgenda
{
    public const string PendienteSena = "Pendiente de seña";

    // Estados que ocupan la agenda (FLUJO_RESERVA_CONSULTA §4.1).
    public static readonly string[] EstadosQueOcupan = { PendienteSena, "Confirmado", "En espera", "En curso" };

    public static bool Ocupa(Turno turno) => EstadosQueOcupan.Contains(turno.Estado);

    public static DateOnly PrimerDia(DateTime ahora, ReglasAgenda reglas) => DateOnly.FromDateTime(ahora.AddHours(reglas.AnticipacionHoras));

    public static DateOnly UltimoDia(DateTime ahora, ReglasAgenda reglas) => DateOnly.FromDateTime(ahora.Date.AddDays(reglas.HorizonteDias));

    // Horarios de inicio libres cada `IntervaloMinutos`. Un turno ocupa toda su duración: una consulta de 30 min
    // a las 09:00 bloquea también las 09:15. `ocupados` son los turnos del profesional o del animal en esas fechas;
    // `excluirTurnoId` es el turno que se reprograma, que no cuenta como ocupado.
    public static List<DateTime> HorariosLibres(Profesional profesional, int animalId, int duracionMinutos, DateOnly fecha,
        IEnumerable<Turno> ocupados, ISet<DateOnly> feriados, ReglasAgenda reglas, DateTime ahora, int excluirTurnoId = 0)
    {
        var libres = new List<DateTime>();
        if (duracionMinutos <= 0)
        {
            return libres;
        }

        if (fecha < PrimerDia(ahora, reglas) || fecha > UltimoDia(ahora, reglas) || feriados.Contains(fecha) || EstaAusente(profesional, fecha))
        {
            return libres;
        }

        var duracion = TimeSpan.FromMinutes(duracionMinutos);
        var minimo = ahora.AddHours(reglas.AnticipacionHoras);
        var dia = fecha.ToDateTime(TimeOnly.MinValue);
        var activos = ocupados.Where(t => Ocupa(t) && t.Id != excluirTurnoId && (t.ProfesionalId == profesional.Id || t.AnimalId == animalId)).ToList();

        foreach (var jornada in profesional.Franjas.Where(f => f.Tipo == "Jornada" && f.Dia == fecha.DayOfWeek).OrderBy(f => f.Inicio))
        {
            var finJornada = dia + jornada.Fin;

            for (var horario = dia + jornada.Inicio; horario + duracion <= finJornada; horario = horario.AddMinutes(reglas.IntervaloMinutos))
            {
                var fin = horario + duracion;
                if (horario < minimo || PisaDescanso(profesional, fecha, horario, fin))
                {
                    continue;
                }

                if (!activos.Any(t => t.Inicio < fin && t.Fin > horario))
                {
                    libres.Add(horario);
                }
            }
        }

        return libres;
    }

    public static bool EstaAusente(Profesional profesional, DateOnly fecha)
    {
        var dia = fecha.ToDateTime(TimeOnly.MinValue);
        return profesional.Ausencias.Any(a => dia >= a.Desde.Date && dia <= a.Hasta.Date);
    }

    // Para un sobreturno (RN-09): el inicio tiene que caer dentro de una jornada del profesional ese día.
    public static bool AtiendeA(Profesional profesional, DateTime inicio)
    {
        var dia = inicio.Date;
        return profesional.Franjas.Any(f => f.Tipo == "Jornada" && f.Dia == inicio.DayOfWeek && inicio >= dia + f.Inicio && inicio < dia + f.Fin);
    }

    private static bool PisaDescanso(Profesional profesional, DateOnly fecha, DateTime inicio, DateTime fin)
    {
        var dia = fecha.ToDateTime(TimeOnly.MinValue);
        return profesional.Franjas.Any(d => d.Tipo == "Descanso" && d.Dia == fecha.DayOfWeek && inicio < dia + d.Fin && fin > dia + d.Inicio);
    }
}
