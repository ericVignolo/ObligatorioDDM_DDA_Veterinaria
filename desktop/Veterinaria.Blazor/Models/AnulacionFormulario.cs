using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Motivo de la nota de crédito que anula un comprobante (A-023, RN-34).
public class AnulacionFormulario
{
    [Required(ErrorMessage = "Escribí el motivo de la anulación.")]
    [StringLength(200, ErrorMessage = "El motivo puede tener hasta 200 caracteres.")]
    public string Motivo { get; set; } = "";
}
