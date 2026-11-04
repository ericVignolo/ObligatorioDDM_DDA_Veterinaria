using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Datos del formulario de registro (A-006, D-REG-01, D-REG-04).
// Las reglas que dependen de más de un campo están en Validate.
public class RegistroCliente : IValidatableObject
{
    [Required(ErrorMessage = "Ingresá tu nombre.")]
    [StringLength(60, ErrorMessage = "El nombre puede tener hasta 60 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingresá tu apellido.")]
    [StringLength(60, ErrorMessage = "El apellido puede tener hasta 60 caracteres.")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "Ingresá tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(254, ErrorMessage = "El correo es demasiado largo.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Ingresá un teléfono.")]
    [StringLength(20, ErrorMessage = "El teléfono es demasiado largo.")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "Elegí el tipo de documento.")]
    public string TipoDocumento { get; set; } = "CI";

    [Required(ErrorMessage = "Ingresá el número de documento.")]
    [StringLength(20, ErrorMessage = "El documento es demasiado largo.")]
    public string NumeroDocumento { get; set; } = "";

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

    [Required(ErrorMessage = "Ingresá una contraseña.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 64 caracteres.")]
    public string Contrasena { get; set; } = "";

    [Required(ErrorMessage = "Repetí la contraseña.")]
    [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
    public string RepetirContrasena { get; set; } = "";

    // Dos aceptaciones obligatorias y separadas, sin marcar de antemano (RN-43).
    [Range(typeof(bool), "true", "true", ErrorMessage = "Para crear la cuenta tenés que aceptar los Términos y Condiciones.")]
    public bool AceptaTerminos { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Para crear la cuenta tenés que aceptar la Política de Privacidad.")]
    public bool AceptaPrivacidad { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ValidacionDatos.DocumentoValido(TipoDocumento, NumeroDocumento))
        {
            var mensaje = TipoDocumento == "CI"
                ? "La cédula no es válida. Revisá los números y el dígito verificador."
                : "El documento debe tener entre 5 y 20 letras o números.";
            yield return new ValidationResult(mensaje, new[] { nameof(NumeroDocumento) });
        }

        if (ValidacionDatos.NormalizarTelefono(Telefono) is null)
        {
            yield return new ValidationResult("El teléfono no es válido. Ejemplo: 099 123 456 o +598 99 123 456.", new[] { nameof(Telefono) });
        }
    }
}
