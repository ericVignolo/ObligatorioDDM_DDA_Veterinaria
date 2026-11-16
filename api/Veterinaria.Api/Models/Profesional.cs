using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Profesional y su agenda (RF-VIS-02, RF-ADM-04).
public class Profesional
{
    public int Id { get; set; }

    // Cuenta del personal con la que ingresa. Null mientras no tenga usuario.
    public int? CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }

    [MaxLength(120)] public string Nombre { get; set; } = "";
    [MaxLength(120)] public string Especialidad { get; set; } = "";
    [MaxLength(500)] public string Descripcion { get; set; } = "";
    [MaxLength(30)] public string Matricula { get; set; } = "";

    public bool Publicado { get; set; } = true;
    public bool Activo { get; set; } = true;

    public List<FranjaHoraria> Franjas { get; set; } = new();
    public List<Ausencia> Ausencias { get; set; } = new();
    public List<ProfesionalEspecie> Especies { get; set; } = new();
    public List<ProfesionalPrestacion> Prestaciones { get; set; } = new();
}

// Qué especies atiende.
public class ProfesionalEspecie
{
    public int ProfesionalId { get; set; }
    public Profesional? Profesional { get; set; }

    public int EspecieId { get; set; }
    public Especie? Especie { get; set; }
}

// Qué servicios realiza.
public class ProfesionalPrestacion
{
    public int ProfesionalId { get; set; }
    public Profesional? Profesional { get; set; }

    public int PrestacionId { get; set; }
    public Prestacion? Prestacion { get; set; }
}

// Tramo semanal: jornada de trabajo o descanso dentro de ella.
public class FranjaHoraria
{
    public int Id { get; set; }

    public int ProfesionalId { get; set; }
    public Profesional? Profesional { get; set; }

    // "Jornada" o "Descanso".
    [MaxLength(10)] public string Tipo { get; set; } = "Jornada";

    public DayOfWeek Dia { get; set; }

    // Columnas TIME. En Blazor son TimeOnly.
    public TimeSpan Inicio { get; set; }
    public TimeSpan Fin { get; set; }
}

// Días sin atención: licencia, capacitación, etc.
public class Ausencia
{
    public int Id { get; set; }

    public int ProfesionalId { get; set; }
    public Profesional? Profesional { get; set; }

    // Columnas DATE.
    public DateTime Desde { get; set; }
    public DateTime Hasta { get; set; }

    [MaxLength(120)] public string Motivo { get; set; } = "";
}
