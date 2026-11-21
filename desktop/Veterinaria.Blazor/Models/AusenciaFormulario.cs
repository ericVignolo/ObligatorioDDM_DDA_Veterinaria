using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Licencia o ausencia de un profesional (A-027, RF-ADM-04).
public class AusenciaFormulario
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí el profesional.")]
    public int ProfesionalId { get; set; }

    [Required(ErrorMessage = "Elegí desde cuándo.")]
    public DateOnly? Desde { get; set; }

    [Required(ErrorMessage = "Elegí hasta cuándo.")]
    public DateOnly? Hasta { get; set; }

    [Required(ErrorMessage = "Escribí el motivo.")]
    [StringLength(100, ErrorMessage = "El motivo puede tener hasta 100 caracteres.")]
    public string Motivo { get; set; } = "";
}
