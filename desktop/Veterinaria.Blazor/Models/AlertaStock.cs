namespace Veterinaria.Blazor.Models;

// Aviso al Administrador cuando un artículo baja del stock mínimo (A-022, RN-28).
// Se genera en el mismo momento que el movimiento que lo provoca.
public class AlertaStock
{
    public int Id { get; set; }
    public int ArticuloId { get; set; }
    public string Articulo { get; set; } = "";
    public int StockQueQuedo { get; set; }
    public int StockMinimo { get; set; }
    public string Origen { get; set; } = "";
    public DateTime Fecha { get; set; }
}
