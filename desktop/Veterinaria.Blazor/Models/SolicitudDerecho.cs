namespace Veterinaria.Blazor.Models;

// Pedido del titular sobre sus datos personales (RF-CLI-19, RNF-LEG-01, RNF-LEG-02).
public class SolicitudDerecho
{
    public int Id { get; set; }
    public int CuentaId { get; set; }

    // Exportación o Eliminación.
    public string Tipo { get; set; } = "";

    // Recibida, Completada, Rechazada o Cancelada.
    public string Estado { get; set; } = "Recibida";
    public DateTime Fecha { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public string ResueltaPor { get; set; } = "";

    // Motivo del rechazo o nota de la resolución.
    public string Detalle { get; set; } = "";
}
