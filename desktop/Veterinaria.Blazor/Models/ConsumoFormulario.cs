using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Insumo usado en la consulta (A-022, RF-VET-06). El lote lo elige el sistema por vencimiento (FEFO, RN-27).
public class ConsumoFormulario
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí un insumo.")]
    public int ArticuloId { get; set; }

    [Range(1, 100, ErrorMessage = "La cantidad debe estar entre 1 y 100.")]
    public int Cantidad { get; set; } = 1;
}
