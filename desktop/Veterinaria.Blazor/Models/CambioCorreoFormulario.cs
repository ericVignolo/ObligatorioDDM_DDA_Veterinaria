using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Pedido de cambio de correo desde «Mi cuenta» (D-PER-04). Se confirma con la contraseña actual
// y el correo nuevo recién reemplaza al anterior después de verificarlo con un código.
public class CambioCorreoFormulario
{
    [Required(ErrorMessage = "Ingresá el correo nuevo.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(254, ErrorMessage = "El correo es demasiado largo.")]
    public string CorreoNuevo { get; set; } = "";

    [Required(ErrorMessage = "Ingresá tu contraseña actual.")]
    public string ContrasenaActual { get; set; } = "";
}
