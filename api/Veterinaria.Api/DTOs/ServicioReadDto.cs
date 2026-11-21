namespace Veterinaria.Api.DTOs;

// Servicio del catálogo público (RF-VIS-01). Solo datos públicos: sin costos internos ni datos de clientes.
public class ServicioReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public bool ReservableEnLinea { get; set; }
    public int DuracionMinutos { get; set; }

    // Importe de referencia. Blazor lo rotula «Desde» (D-CAT-01); el cargo real se calcula al prestar (RN-33).
    public PrecioReferenciaDto PrecioReferencia { get; set; } = new();
}

public class PrecioReferenciaDto
{
    public decimal Importe { get; set; }
    public string Moneda { get; set; } = "UYU";
}
