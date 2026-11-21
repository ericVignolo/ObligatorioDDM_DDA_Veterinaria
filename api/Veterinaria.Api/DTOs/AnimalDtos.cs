using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs;

// Alta de un animal o de un grupo (A-013, CU-CLI-01, RF-CLI-01). Mismas reglas y mensajes que AnimalFormulario de Blazor.
public class AnimalCreateDto : IValidatableObject
{
    public static readonly string[] Sexos = { "Macho", "Hembra", "Sin determinar" };

    [Required(ErrorMessage = "Ingresá el nombre.")]
    [StringLength(60, ErrorMessage = "El nombre puede tener hasta 60 caracteres.")]
    public string Nombre { get; set; } = "";

    // Id de GET /api/v1/especies.
    [Range(1, int.MaxValue, ErrorMessage = "Elegí la especie.")]
    public int EspecieId { get; set; }

    [StringLength(60, ErrorMessage = "La raza puede tener hasta 60 caracteres.")]
    public string Raza { get; set; } = "";

    [Required(ErrorMessage = "Elegí el sexo.")]
    public string Sexo { get; set; } = "";

    public DateOnly? FechaNacimiento { get; set; }

    // 15 dígitos (ISO 11784/11785), opcional y único (RN-41).
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
        if (!Sexos.Contains(Sexo))
        {
            yield return new ValidationResult("Elegí el sexo.", new[] { nameof(Sexo) });
        }

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

// Edición: los mismos datos y reglas que el alta.
public class AnimalUpdateDto : AnimalCreateDto
{
}

public class AnimalReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int EspecieId { get; set; }
    public string Especie { get; set; } = "";
    public string Raza { get; set; } = "";
    public string Sexo { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }
    public string Microchip { get; set; } = "";
    public string Caravana { get; set; } = "";
    public bool EsGrupo { get; set; }
    public int? CantidadGrupo { get; set; }
    public string SenasParticulares { get; set; } = "";
    public string Antecedentes { get; set; } = "";
    public DateTime FechaAlta { get; set; }
}

public class EspecieReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
}
