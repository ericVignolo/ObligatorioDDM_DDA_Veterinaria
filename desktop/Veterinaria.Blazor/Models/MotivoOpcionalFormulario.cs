using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Motivo opcional del cliente al cancelar su turno (A-016).
public class MotivoOpcionalFormulario
{
    [StringLength(200, ErrorMessage = "El motivo puede tener hasta 200 caracteres.")]
    public string Motivo { get; set; } = "";
}
