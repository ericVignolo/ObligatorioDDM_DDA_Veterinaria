namespace Veterinaria.Blazor.Models;

// Lote de un artículo del inventario, con su vencimiento y su cantidad (A-022, RN-15, RN-27, RN-29).
// La cantidad solo cambia con un movimiento (compra, consumo, ajuste o baja, RN-30).
public class LoteProducto
{
    public int Id { get; set; }
    public int ArticuloId { get; set; }
    public string Numero { get; set; } = "";
    public DateOnly Vencimiento { get; set; }
    public int Cantidad { get; set; }

    // Un lote vencido queda inhabilitado para consumo y se da de baja solo (RN-29).
    public bool Inhabilitado { get; set; }

    public DateTime FechaIngreso { get; set; }
}
