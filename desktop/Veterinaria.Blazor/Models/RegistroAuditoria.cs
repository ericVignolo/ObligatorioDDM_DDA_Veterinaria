namespace Veterinaria.Blazor.Models;

// Entrada de la bitácora de auditoría (A-027, RF-ADM-08). No se modifica ni se borra.
public class RegistroAuditoria
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Usuario { get; set; } = "";
    public string Rol { get; set; } = "";

    // Qué se tocó: Servicio, Tarifa, Personal, Horario, Feriado, Box, Parámetro, Consentimiento…
    public string Entidad { get; set; } = "";
    public string Accion { get; set; } = "";
    public string Detalle { get; set; } = "";
}
