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

    // Confirmado, En espera, En curso, Finalizado, Ausente o Cancelado (FLUJO_RESERVA_CONSULTA §4.1, A-018).
    public string Estado { get; set; } = "Confirmado";

    public DateTime FechaCreacion { get; set; }

    // Cancelación (A-016): motivo opcional y cuándo se canceló.
    public string MotivoCancelacion { get; set; } = "";
    public DateTime? FechaCancelacion { get; set; }

    // Quién canceló: «Cliente» o «Recepción» (A-017).
    public string CanceladoPor { get; set; } = "";

    // Hora real de llegada que registra Recepción (Confirmado → En espera).
    public DateTime? HoraLlegada { get; set; }

    // Reprogramación (A-016): el turno conserva su número y se guarda el horario anterior.
    public DateTime? InicioAnterior { get; set; }
}
