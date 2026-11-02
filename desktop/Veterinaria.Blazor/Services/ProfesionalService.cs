using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Datos ficticios de la sección A. En la sección C se reemplaza por ProfesionalApiService.
// Sin premios, títulos ni afirmaciones que no puedan demostrarse (RN-45).
public class ProfesionalService
{
    private readonly List<Profesional> _profesionales = new()
    {
        new()
        {
            Id = 1,
            Nombre = "Dra. Lucía Ferreira",
            Especialidad = "Clínica de pequeños animales",
            Descripcion = "Consultas generales, vacunación y controles de perros y gatos.",
            Horarios = new() { "Lunes a viernes de 9 a 14 h" }
        },
        new()
        {
            Id = 2,
            Nombre = "Dr. Martín Silveira",
            Especialidad = "Grandes animales",
            Descripcion = "Atención de bovinos y ovinos, revisiones de rodeo y visitas a campo.",
            Horarios = new() { "Lunes a viernes de 14 a 19 h", "Sábados de 9 a 13 h" }
        },
        new()
        {
            Id = 3,
            Nombre = "Dra. Valentina Acosta",
            Especialidad = "Equinos",
            Descripcion = "Clínica y rehabilitación de caballos. La rehabilitación requiere evaluación previa.",
            Horarios = new() { "Martes y jueves de 9 a 17 h" }
        },
        new()
        {
            Id = 4,
            Nombre = "Dr. Andrés Pereira",
            Especialidad = "Clínica general",
            Descripcion = "No publicado: no debe aparecer.",
            Horarios = new() { "A confirmar" },
            Publicado = false
        }
    };

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
}
