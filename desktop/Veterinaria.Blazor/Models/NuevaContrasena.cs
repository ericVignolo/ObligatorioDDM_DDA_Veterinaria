using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Código recibido y contraseña nueva (A-007, D-REG-03).
public class NuevaContrasena
{
    [Required(ErrorMessage = "Ingresá el código.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código tiene 6 números.")]
    public string Codigo { get; set; } = "";

    [Required(ErrorMessage = "Ingresá una contraseña nueva.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 64 caracteres.")]
    public string Contrasena { get; set; } = "";

    [Required(ErrorMessage = "Repetí la contraseña.")]
    [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
    public string RepetirContrasena { get; set; } = "";
}
