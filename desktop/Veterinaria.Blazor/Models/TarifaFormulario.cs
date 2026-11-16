using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Precio nuevo de un servicio desde una fecha, hoy o futura (A-027, RF-ADM-02, RN-33).
public class TarifaFormulario
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí el servicio.")]
    public int PrestacionId { get; set; }

    [Range(1, 1000000, ErrorMessage = "El precio debe estar entre $ 1 y $ 1.000.000.")]
    public int Precio { get; set; }

    [Required(ErrorMessage = "Elegí desde qué fecha rige.")]
    public DateOnly? VigenteDesde { get; set; }
}
