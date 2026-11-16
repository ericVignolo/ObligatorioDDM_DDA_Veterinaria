using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Motivo obligatorio: cancelación por Recepción, consulta sin atención (P-RC-04) y acceso a la historia sin turno (P-RC-05).
// El servicio vuelve a validar que no esté vacío.
public class MotivoFormulario
{
    [Required(ErrorMessage = "Escribí el motivo.")]
    [StringLength(200, ErrorMessage = "El motivo puede tener hasta 200 caracteres.")]
    public string Motivo { get; set; } = "";
}
