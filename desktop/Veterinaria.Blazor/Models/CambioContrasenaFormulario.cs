using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Cambio de contraseña con la sesión iniciada (D-PER-05). Pide la actual y, al terminar, se vuelve a ingresar.
public class CambioContrasenaFormulario : IValidatableObject
{
    [Required(ErrorMessage = "Ingresá tu contraseña actual.")]
    public string ContrasenaActual { get; set; } = "";

    [Required(ErrorMessage = "Ingresá una contraseña nueva.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 64 caracteres.")]
    public string Contrasena { get; set; } = "";

    [Required(ErrorMessage = "Repetí la contraseña nueva.")]
    [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
    public string RepetirContrasena { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(Contrasena) && Contrasena == ContrasenaActual)
        {
            yield return new ValidationResult("La contraseña nueva tiene que ser distinta de la actual.", new[] { nameof(Contrasena) });
        }
    }
}
