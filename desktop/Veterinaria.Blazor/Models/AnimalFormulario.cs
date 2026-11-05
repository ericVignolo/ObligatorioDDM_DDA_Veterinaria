using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Datos del formulario de alta y edición de un animal (A-013, CU-CLI-01).
public class AnimalFormulario : IValidatableObject
{
    [Required(ErrorMessage = "Ingresá el nombre.")]
    [StringLength(60, ErrorMessage = "El nombre puede tener hasta 60 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Elegí la especie.")]
    public string Especie { get; set; } = "";

    [StringLength(60, ErrorMessage = "La raza puede tener hasta 60 caracteres.")]
    public string Raza { get; set; } = "";

    [Required(ErrorMessage = "Elegí el sexo.")]
    public string Sexo { get; set; } = "";

    public DateOnly? FechaNacimiento { get; set; }

    // Microchip: 15 dígitos (ISO 11784/11785), opcional.
    [RegularExpression(@"^\d{15}$", ErrorMessage = "El microchip tiene 15 números.")]
    public string Microchip { get; set; } = "";

    [RegularExpression(@"^[A-Za-z0-9\-]{1,20}$", ErrorMessage = "La caravana puede tener hasta 20 letras, números o guiones.")]
    public string Caravana { get; set; } = "";

    public bool EsGrupo { get; set; }

    [Range(2, 10000, ErrorMessage = "Un grupo tiene entre 2 y 10.000 animales.")]
    public int? CantidadGrupo { get; set; }

    [StringLength(300, ErrorMessage = "Las señas particulares pueden tener hasta 300 caracteres.")]
    public string SenasParticulares { get; set; } = "";

    [StringLength(500, ErrorMessage = "Los antecedentes pueden tener hasta 500 caracteres.")]
    public string Antecedentes { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaNacimiento is not null && FechaNacimiento > DateOnly.FromDateTime(DateTime.Today))
        {
            yield return new ValidationResult("La fecha de nacimiento no puede ser futura.", new[] { nameof(FechaNacimiento) });
        }

        if (EsGrupo && CantidadGrupo is null)
        {
            yield return new ValidationResult("Indicá cuántos animales tiene el grupo.", new[] { nameof(CantidadGrupo) });
        }
    }
}
