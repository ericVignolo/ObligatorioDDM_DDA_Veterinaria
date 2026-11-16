using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Ingreso clínico del Veterinario: box y plan terapéutico (A-024, RF-VET-09).
public class IngresoClinicoFormulario
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí un box libre.")]
    public int BoxId { get; set; }

    [Required(ErrorMessage = "Escribí el plan terapéutico.")]
    [StringLength(2000, ErrorMessage = "El plan puede tener hasta 2000 caracteres.")]
    public string PlanTerapeutico { get; set; } = "";
}
