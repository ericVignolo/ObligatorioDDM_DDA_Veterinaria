using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Ajuste manual o baja de un lote (A-022). Solo el Administrador y siempre con motivo (RN-31).
// Ajuste: la cantidad puede ser positiva (sobrante) o negativa (faltante). Baja: se indica cuántas unidades salen.
public class AjusteFormulario
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí un lote.")]
    public int LoteId { get; set; }

    [Range(-10000, 10000, ErrorMessage = "La cantidad debe estar entre -10000 y 10000.")]
    public int Cantidad { get; set; }

    [Required(ErrorMessage = "Escribí el motivo.")]
    [StringLength(200, ErrorMessage = "El motivo puede tener hasta 200 caracteres.")]
    public string Motivo { get; set; } = "";
}
