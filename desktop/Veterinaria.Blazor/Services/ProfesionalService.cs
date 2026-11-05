using System.Globalization;
using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Datos ficticios de la sección A. En la sección C se reemplaza por ProfesionalApiService
// y el Administrador carga jornadas, descansos y ausencias (RF-ADM-04).
// Sin premios, títulos ni afirmaciones que no puedan demostrarse (RN-45).
public class ProfesionalService
{
    private static readonly DayOfWeek[] LunesAViernes =
    {
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday
    };

    private readonly List<Profesional> _profesionales;

    public ProfesionalService()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);

        _profesionales = new()
        {
            new()
            {
                Id = 1,
                Nombre = "Dra. Lucía Ferreira",
                Especialidad = "Clínica de pequeños animales",
                Descripcion = "Consultas generales, vacunación y controles de perros, gatos y otros animales de compañía.",
                Jornada = Franjas(LunesAViernes, 9, 0, 14, 0),
                Descansos = Franjas(LunesAViernes, 11, 30, 12, 0),
                // Ausencia de ejemplo, relativa a hoy para que siempre se vea en la demostración.
                Ausencias = new() { new() { Desde = hoy.AddDays(8), Hasta = hoy.AddDays(8), Motivo = "Capacitación" } },
                Especies = new() { "Perro", "Gato", "Ave", "Conejo", "Reptil", "Otra" },
                PrestacionIds = new() { 1, 2, 3 }
            },
            new()
            {
                Id = 2,
                Nombre = "Dr. Martín Silveira",
                Especialidad = "Grandes animales",
                Descripcion = "Atención de bovinos, ovinos, caprinos y porcinos, revisiones de rodeo y visitas a campo.",
                Jornada = Franjas(LunesAViernes, 14, 0, 19, 0)
                    .Concat(Franjas(new[] { DayOfWeek.Saturday }, 9, 0, 13, 0)).ToList(),
                Descansos = Franjas(LunesAViernes, 16, 30, 17, 0),
                Ausencias = new() { new() { Desde = hoy.AddDays(18), Hasta = hoy.AddDays(22), Motivo = "Licencia" } },
                Especies = new() { "Vaca", "Oveja", "Cabra", "Cerdo" },
                PrestacionIds = new() { 1, 2, 3 }
            },
            new()
            {
                Id = 3,
                Nombre = "Dra. Valentina Acosta",
                Especialidad = "Equinos",
                Descripcion = "Clínica y rehabilitación de caballos. La rehabilitación requiere evaluación previa.",
                Jornada = Franjas(new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }, 9, 0, 17, 0),
                Descansos = Franjas(new[] { DayOfWeek.Tuesday, DayOfWeek.Thursday }, 12, 30, 13, 30),
                Especies = new() { "Caballo" },
                PrestacionIds = new() { 1 }
            },
            new()
            {
                Id = 5,
                Nombre = "Sofía Méndez",
                Especialidad = "Baño y estética",
                Descripcion = "Baño, corte y cuidado estético de perros y gatos. No realiza atención clínica.",
                Jornada = Franjas(LunesAViernes, 10, 0, 18, 0)
                    .Concat(Franjas(new[] { DayOfWeek.Saturday }, 9, 0, 13, 0)).ToList(),
                Descansos = Franjas(LunesAViernes, 13, 0, 14, 0),
                Especies = new() { "Perro", "Gato" },
                PrestacionIds = new() { 6 }
            },
            new()
            {
                Id = 4,
                Nombre = "Dr. Andrés Pereira",
                Especialidad = "Clínica general",
                Descripcion = "No publicado: no debe aparecer.",
                Publicado = false
            }
        };
    }

    public List<Profesional> ObtenerPublicos()
    {
        return _profesionales
            .Where(p => p.Publicado)
            .ToList();
    }

    public Profesional? ObtenerPorId(int id)
    {
        return ObtenerPublicos().FirstOrDefault(p => p.Id == id);
    }

    // Solo quienes realizan ese servicio y atienden esa especie (A-015, paso 2).
    public List<Profesional> ObtenerPara(int prestacionId, string especie)
    {
        return ObtenerPublicos()
            .Where(p => p.PrestacionIds.Contains(prestacionId) && p.Especies.Contains(especie))
            .ToList();
    }

    // Texto para la página del profesional, por ejemplo «Lunes: 09:00 a 14:00 (descanso 11:30 a 12:00)».
    public List<string> DescribirJornada(Profesional profesional)
    {
        var cultura = CultureInfo.GetCultureInfo("es-UY");
        var lineas = new List<string>();

        foreach (var franja in profesional.Jornada.OrderBy(f => ((int)f.Dia + 6) % 7).ThenBy(f => f.Inicio))
        {
            var dia = cultura.TextInfo.ToTitleCase(cultura.DateTimeFormat.GetDayName(franja.Dia));
            var texto = $"{dia}: {franja.Inicio:HH\\:mm} a {franja.Fin:HH\\:mm}";

            var descanso = profesional.Descansos.FirstOrDefault(d => d.Dia == franja.Dia && d.Inicio >= franja.Inicio && d.Fin <= franja.Fin);
            if (descanso is not null)
            {
                texto += $" (descanso {descanso.Inicio:HH\\:mm} a {descanso.Fin:HH\\:mm})";
            }

            lineas.Add(texto);
        }

        return lineas;
    }

    private static List<FranjaHoraria> Franjas(IEnumerable<DayOfWeek> dias, int horaInicio, int minutoInicio, int horaFin, int minutoFin)
    {
        return dias
            .Select(dia => new FranjaHoraria
            {
                Dia = dia,
                Inicio = new TimeOnly(horaInicio, minutoInicio),
                Fin = new TimeOnly(horaFin, minutoFin)
            })
            .ToList();
    }
}
