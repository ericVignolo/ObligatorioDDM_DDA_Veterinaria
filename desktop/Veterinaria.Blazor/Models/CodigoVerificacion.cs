using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Código de 6 dígitos enviado al correo (A-007, D-REG-02).
public class CodigoVerificacion
{
    [Required(ErrorMessage = "Ingresá el código.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código tiene 6 números.")]
    public string Codigo { get; set; } = "";
}
