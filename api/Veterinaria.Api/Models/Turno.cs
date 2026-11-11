using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Consultorio, box, corral u otro recurso físico que ocupa un turno (RN-07).
public class Recurso
{
    public int Id { get; set; }
    [MaxLength(80)] public string Nombre { get; set; } = "";
    [MaxLength(40)] public string Tipo { get; set; } = "";
    public bool Activo { get; set; } = true;
}

// Turno reservado (CU-CLI-02). La API lo crea en una transacción con SELECT … FOR UPDATE
// sobre profesional, animal y recursos, en ese orden (DT-BD-02).
public class Turno
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cuenta? Cliente { get; set; }

    public int AnimalId { get; set; }
    public Animal? Animal { get; set; }

    public int PrestacionId { get; set; }
    public Prestacion? Prestacion { get; set; }

    public int ProfesionalId { get; set; }
    public Profesional? Profesional { get; set; }

    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }

    // Confirmado, En espera, En curso, Finalizado, Ausente o Cancelado (FLUJO_RESERVA_CONSULTA §4).
    [MaxLength(20)] public string Estado { get; set; } = "Confirmado";

    public DateTime FechaCreacion { get; set; }

    [MaxLength(250)] public string MotivoCancelacion { get; set; } = "";
    public DateTime? FechaCancelacion { get; set; }

    // Reprogramación (A-016): conserva el número y guarda el horario anterior.
    public DateTime? InicioAnterior { get; set; }

    // Control de concurrencia optimista para cambios de estado.
    public int Version { get; set; }

    public List<TurnoRecurso> Recursos { get; set; } = new();
}

// Asignación de un recurso a un turno. Copia Inicio y Fin para el índice (RecursoId, Inicio, Fin) de DT-BD-02.
public class TurnoRecurso
{
    public int TurnoId { get; set; }
    public Turno? Turno { get; set; }

    public int RecursoId { get; set; }
    public Recurso? Recurso { get; set; }

    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
}
