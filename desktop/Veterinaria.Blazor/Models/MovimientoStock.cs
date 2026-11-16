namespace Veterinaria.Blazor.Models;

// Movimiento del inventario, con origen tipado y trazable (A-022, RN-30).
// Tipos: Compra, Consumo, Ajuste o Baja. No hay venta: el inventario es solo material de trabajo (DT-ALC-01).
public class MovimientoStock
{
    public int Id { get; set; }
    public int ArticuloId { get; set; }
    public int LoteId { get; set; }
    public string Tipo { get; set; } = "";

    // Positiva si entra stock, negativa si sale.
    public int Cantidad { get; set; }

    // De dónde viene: «Consulta 5», «Compra», «Vencimiento automático»…
    public string Origen { get; set; } = "";
    public string Motivo { get; set; } = "";

    public DateTime Fecha { get; set; }

    // 0 = el sistema (por ejemplo, la baja automática por vencimiento).
    public int PersonaId { get; set; }
    public string PersonaNombre { get; set; } = "";
}
