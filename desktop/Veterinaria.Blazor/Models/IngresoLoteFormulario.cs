using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Compra: ingreso de un lote nuevo (A-022, RF-ADM-05). Solo el Administrador.
public class IngresoLoteFormulario
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí un artículo.")]
    public int ArticuloId { get; set; }

    [Required(ErrorMessage = "Escribí el número de lote.")]
    [StringLength(30, ErrorMessage = "El número de lote puede tener hasta 30 caracteres.")]
    public string Numero { get; set; } = "";

    [Required(ErrorMessage = "Indicá el vencimiento.")]
    public DateOnly? Vencimiento { get; set; }

    [Range(1, 10000, ErrorMessage = "La cantidad debe estar entre 1 y 10000.")]
    public int Cantidad { get; set; } = 1;
}
