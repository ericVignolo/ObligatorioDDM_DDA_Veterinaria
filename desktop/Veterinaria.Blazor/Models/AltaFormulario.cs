using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Alta de internación: sin evolución final e indicaciones no hay alta (A-024, RN-25).
public class AltaFormulario
{
    [Required(ErrorMessage = "Escribí la evolución final.")]
    [StringLength(1000, ErrorMessage = "La evolución final puede tener hasta 1000 caracteres.")]
    public string EvolucionFinal { get; set; } = "";

    [Required(ErrorMessage = "Escribí las indicaciones para el domicilio.")]
    [StringLength(2000, ErrorMessage = "Las indicaciones pueden tener hasta 2000 caracteres.")]
    public string IndicacionesDomicilio { get; set; } = "";
}
