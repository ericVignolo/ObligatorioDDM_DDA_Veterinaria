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

    // Por ahora siempre Confirmado; Cancelado y los demás estados llegan con A-016 y A-018.
    public string Estado { get; set; } = "Confirmado";

    public DateTime FechaCreacion { get; set; }
}
