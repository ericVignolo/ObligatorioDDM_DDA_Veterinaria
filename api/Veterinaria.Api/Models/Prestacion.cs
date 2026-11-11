using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Servicio del catálogo (RF-VIS-01). Se llama Prestacion para no confundir con la carpeta Services.
public class Prestacion
{
    public int Id { get; set; }
    [MaxLength(120)] public string Nombre { get; set; } = "";
    [MaxLength(500)] public string Descripcion { get; set; } = "";

    // decimal(12,2), en UYU.
    public decimal PrecioDesde { get; set; }

    public bool ReservableEnLinea { get; set; }

    // Duración del turno; 0 si no se reserva en línea (A-014).
    public int DuracionMinutos { get; set; }

    public bool Publicado { get; set; } = true;
}
