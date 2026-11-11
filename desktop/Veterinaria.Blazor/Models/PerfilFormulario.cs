using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Datos que el Cliente puede cambiar desde «Mi cuenta» (A-009). Mismas reglas que el registro (D-REG-01).
// El correo identifica la cuenta (RN-37) y el documento al titular (RN-42): se muestran, pero no se editan acá.
public class PerfilFormulario : IValidatableObject
{
    [Required(ErrorMessage = "Ingresá tu nombre.")]
    [StringLength(60, ErrorMessage = "El nombre puede tener hasta 60 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingresá tu apellido.")]
    [StringLength(60, ErrorMessage = "El apellido puede tener hasta 60 caracteres.")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "Ingresá un teléfono.")]
    [StringLength(20, ErrorMessage = "El teléfono es demasiado largo.")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "Ingresá la calle.")]
    [StringLength(100, ErrorMessage = "La calle puede tener hasta 100 caracteres.")]
    public string Calle { get; set; } = "";

    [Required(ErrorMessage = "Ingresá el número de puerta.")]
    [StringLength(10, ErrorMessage = "El número puede tener hasta 10 caracteres.")]
    public string NumeroPuerta { get; set; } = "";

    [StringLength(10, ErrorMessage = "El apartamento puede tener hasta 10 caracteres.")]
    public string Apartamento { get; set; } = "";

    [Required(ErrorMessage = "Ingresá la localidad.")]
    [StringLength(60, ErrorMessage = "La localidad puede tener hasta 60 caracteres.")]
    public string Localidad { get; set; } = "";

    [Required(ErrorMessage = "Elegí el departamento.")]
    public string Departamento { get; set; } = "";

    [StringLength(200, ErrorMessage = "Las referencias pueden tener hasta 200 caracteres.")]
    public string Referencias { get; set; } = "";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ValidacionDatos.NormalizarTelefono(Telefono) is null)
        {
            yield return new ValidationResult("El teléfono no es válido. Ejemplo: 099 123 456 o +598 99 123 456.", new[] { nameof(Telefono) });
        }
    }
}
