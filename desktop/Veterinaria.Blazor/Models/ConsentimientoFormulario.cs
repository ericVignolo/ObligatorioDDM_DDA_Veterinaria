using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Aceptación del consentimiento informado: casilla sin marcar de antemano y nombre completo (P-RC-06, RN-43).
public class ConsentimientoFormulario
{
    [Range(typeof(bool), "true", "true", ErrorMessage = "Marcá que leíste y aceptás el consentimiento.")]
    public bool LeyoYAcepta { get; set; }

    [Required(ErrorMessage = "Escribí tu nombre completo.")]
    [StringLength(120, ErrorMessage = "El nombre puede tener hasta 120 caracteres.")]
    public string NombreCompleto { get; set; } = "";
}
