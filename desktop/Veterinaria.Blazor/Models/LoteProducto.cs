namespace Veterinaria.Blazor.Models;

// Lote de un producto sanitario, con su vencimiento (A-021, RN-15).
// Es la versión simple de A-021: A-022 lo reemplaza por el inventario completo (stock, movimientos y mínimos).
public class LoteProducto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string Numero { get; set; } = "";
    public DateOnly Vencimiento { get; set; }
}
