namespace Veterinaria.Blazor.Models;

// Artículo del inventario: una vacuna, un antiparasitario o un insumo de la atención (A-022, RF-ADM-05).
// El stock no se guarda acá: es la suma de los lotes habilitados (InventarioService.Stock).
public class Articulo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";

    // "Vacuna", "Antiparasitario" o "Insumo".
    public string Tipo { get; set; } = "Insumo";

    // Cómo se cuenta: "dosis", "unidad", "par", "sobre"…
    public string Unidad { get; set; } = "unidad";

    // Por debajo de este stock se avisa al Administrador (RN-28). Lo configura el Administrador (RF-ADM-10).
    public int StockMinimo { get; set; }
}
