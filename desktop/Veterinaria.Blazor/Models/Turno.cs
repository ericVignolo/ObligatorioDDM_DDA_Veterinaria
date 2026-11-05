namespace Veterinaria.Blazor.Models;

// Turno reservado (A-015, CU-CLI-02). En la sección C lo crea la API dentro de una transacción (DT-BD-02).
public class Turno
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public int AnimalId { get; set; }
    public int PrestacionId { get; set; }
    public int ProfesionalId { get; set; }
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }

    // Confirmado o Cancelado por ahora; En espera, En curso, Finalizado y Ausente llegan con A-017 y A-018.
    public string Estado { get; set; } = "Confirmado";

    public DateTime FechaCreacion { get; set; }

    // Cancelación (A-016): motivo opcional y cuándo se canceló.
    public string MotivoCancelacion { get; set; } = "";
    public DateTime? FechaCancelacion { get; set; }

    // Reprogramación (A-016): el turno conserva su número y se guarda el horario anterior.
    public DateTime? InicioAnterior { get; set; }
}
