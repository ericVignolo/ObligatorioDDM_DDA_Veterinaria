using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Novedad o evolución que publica el Veterinario (A-024, RF-VET-10, RF-VET-11).
public class NovedadFormulario
{
    [Required(ErrorMessage = "Escribí el texto.")]
    [StringLength(1000, ErrorMessage = "El texto puede tener hasta 1000 caracteres.")]
    public string Texto { get; set; } = "";

    // «Novedad» o «Evolución».
    public string Tipo { get; set; } = "Novedad";

    // Marcar para que la vea el cliente; sin marcar es una nota interna (RN-23).
    public bool Visible { get; set; }
}
