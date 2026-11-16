using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Lo que el veterinario elige para registrar una aplicación dentro de la consulta (A-021).
// El lote lo elige el sistema por vencimiento (FEFO, RN-27, decisión de Eric del 2026-11-11), la fecha de
// aplicación es hoy y la de refuerzo la calcula el servicio (RN-19): ninguno se escribe.
public class AplicacionFormulario
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí un producto.")]
    public int ProductoId { get; set; }
}
