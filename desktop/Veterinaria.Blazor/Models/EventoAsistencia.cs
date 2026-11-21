namespace Veterinaria.Blazor.Models;

// Historial inmutable de una solicitud: estados, ofertas, cambios de nivel y empeoramientos (RN-51, RN-52).
public class EventoAsistencia
{
    public int Id { get; set; }
    public int SolicitudId { get; set; }
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = "";
    public string Texto { get; set; } = "";
    public string Autor { get; set; } = "";

    // Solo en los cambios de nivel: valor anterior y nuevo.
    public string NivelAnterior { get; set; } = "";
    public string NivelNuevo { get; set; } = "";
}
