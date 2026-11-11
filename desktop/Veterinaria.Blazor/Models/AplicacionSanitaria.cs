namespace Veterinaria.Blazor.Models;

// Aplicación de una vacuna o un antiparasitario a un animal (A-021, RF-VET-04).
// Se registra dentro de la consulta; cuenta para el carnet recién cuando la consulta se firma.
// Guarda copia del nombre, laboratorio y lote para que el carnet no cambie si el catálogo cambia.
public class AplicacionSanitaria
{
    public int Id { get; set; }
    public int AnimalId { get; set; }

    // 0 = registro anterior al sistema (demostración), sin consulta de origen.
    public int ConsultaId { get; set; }

    public int ProductoId { get; set; }
    public string Producto { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Laboratorio { get; set; } = "";

    public int LoteId { get; set; }
    public string Lote { get; set; } = "";
    public DateOnly LoteVence { get; set; }

    public DateOnly FechaAplicacion { get; set; }

    // Calculada por el servicio: fecha de aplicación + intervalo del producto (RN-19).
    public DateOnly FechaRefuerzo { get; set; }

    public int AplicadaPorId { get; set; }
    public string AplicadaPor { get; set; } = "";
}
