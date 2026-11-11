using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Lo que el veterinario elige para registrar una aplicación dentro de la consulta (A-021).
// La fecha de aplicación es hoy y la de refuerzo la calcula el servicio (RN-19): no se escriben.
public class AplicacionFormulario
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí un producto.")]
    public int ProductoId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Elegí un lote.")]
    public int LoteId { get; set; }
}
