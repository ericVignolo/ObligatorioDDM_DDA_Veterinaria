namespace Veterinaria.Api.DTOs;

// Profesional del área pública (RF-VIS-02). Sin matrícula, cuenta ni ausencias: son datos internos.
public class ProfesionalReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public string Descripcion { get; set; } = "";

    // Especies que atiende, por nombre.
    public List<string> Especies { get; set; } = new();

    // Solo los servicios públicos que realiza.
    public List<ServicioResumenDto> Servicios { get; set; } = new();
}

// Detalle con el horario semanal armado por día.
public class ProfesionalDetalleDto : ProfesionalReadDto
{
    public List<DiaHorarioDto> Horario { get; set; } = new();
}

public class ServicioResumenDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}

// Un día de atención: «Lunes», jornadas de 09:00 a 14:00 y descansos de 11:30 a 12:00.
public class DiaHorarioDto
{
    public string Dia { get; set; } = "";
    public List<TramoHorarioDto> Jornadas { get; set; } = new();
    public List<TramoHorarioDto> Descansos { get; set; } = new();
}

public class TramoHorarioDto
{
    // Formato "HH:mm".
    public string Desde { get; set; } = "";
    public string Hasta { get; set; } = "";
}
