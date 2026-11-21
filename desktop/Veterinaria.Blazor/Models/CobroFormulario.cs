using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Cobro que registra Recepción (A-023). El monto es en pesos enteros.
public class CobroFormulario
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí un cargo.")]
    public int CargoId { get; set; }

    [Range(1, 1000000, ErrorMessage = "El monto debe estar entre $ 1 y $ 1.000.000.")]
    public int Monto { get; set; }

    [Required(ErrorMessage = "Elegí el medio de pago.")]
    public string Medio { get; set; } = "";

    [StringLength(40, ErrorMessage = "La referencia puede tener hasta 40 caracteres.")]
    public string Referencia { get; set; } = "";
}
