namespace Veterinaria.Blazor.Models;

// Producto del catálogo sanitario: una vacuna o un antiparasitario (A-021, RF-VET-04).
// El intervalo de refuerzo vive en el producto, por eso la fecha de refuerzo se calcula y no se escribe (RN-19).
// El catálogo de esta versión es de demostración: especies e intervalos están a validar por un veterinario.
public class ProductoSanitario
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";

    // "Vacuna" o "Antiparasitario".
    public string Tipo { get; set; } = "Vacuna";

    public string Laboratorio { get; set; } = "";

    // Especies para las que está indicado (los mismos nombres que AnimalService.Especies).
    public List<string> Especies { get; set; } = new();

    // Artículo del inventario con sus lotes (A-022). Cada aplicación descuenta una dosis.
    public int ArticuloId { get; set; }

    // Días entre una aplicación y su refuerzo.
    public int IntervaloRefuerzoDias { get; set; }
}
