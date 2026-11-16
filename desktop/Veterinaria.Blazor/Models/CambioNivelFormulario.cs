using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Cambio de nivel de una solicitud: empeoramiento informado o revisión del veterinario con motivo (RN-51, RN-52).
public class CambioNivelFormulario
{
    [Required(ErrorMessage = "Elegí el nivel.")]
    public string Nivel { get; set; } = "";

    [Required(ErrorMessage = "Escribí el motivo o lo que informó el cliente.")]
    [StringLength(300, ErrorMessage = "Hasta 300 caracteres.")]
    public string Motivo { get; set; } = "";
}
