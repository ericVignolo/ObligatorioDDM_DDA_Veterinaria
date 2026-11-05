namespace Veterinaria.Blazor.Models;

// Tramo de un día de la semana: jornada de trabajo o descanso de un profesional (A-014).
public class FranjaHoraria
{
    public DayOfWeek Dia { get; set; }
    public TimeOnly Inicio { get; set; }
    public TimeOnly Fin { get; set; }
}
