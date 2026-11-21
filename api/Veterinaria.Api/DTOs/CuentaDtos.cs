using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.DTOs;

// Registro de cliente (A-006, D-REG-01, D-REG-04). Mismas reglas y mensajes que RegistroCliente de Blazor.
// La API vuelve a validar todo: no confía en la pantalla (RT-08).
public class RegistroCreateDto : IValidatableObject
{
    [Required(ErrorMessage = "Ingresá tu nombre.")]
    [StringLength(60, ErrorMessage = "El nombre puede tener hasta 60 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingresá tu apellido.")]
    [StringLength(60, ErrorMessage = "El apellido puede tener hasta 60 caracteres.")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "Ingresá tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    [StringLength(150, ErrorMessage = "El correo es demasiado largo.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Ingresá un teléfono.")]
    [StringLength(20, ErrorMessage = "El teléfono es demasiado largo.")]
    public string Telefono { get; set; } = "";

    [Required(ErrorMessage = "Elegí el tipo de documento.")]
    [RegularExpression("^(CI|Pasaporte|Otro)$", ErrorMessage = "El tipo de documento no es válido.")]
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
    [StringLength(40, ErrorMessage = "El departamento no es válido.")]
    public string Departamento { get; set; } = "";

    [StringLength(200, ErrorMessage = "Las referencias pueden tener hasta 200 caracteres.")]
    public string Referencias { get; set; } = "";

    [Required(ErrorMessage = "Ingresá una contraseña.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 64 caracteres.")]
    public string Contrasena { get; set; } = "";

    // Dos aceptaciones obligatorias y separadas, sin marcar de antemano (RN-43, D-REG-04).
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

// Respuesta de las operaciones que mandan un código por correo.
public class RespuestaCodigoDto
{
    public string Mensaje { get; set; } = "";

    // SOLO DESARROLLO: el código, porque todavía no se envían correos. Fuera de Development no aparece.
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CodigoDesarrollo { get; set; }
}

public class MensajeDto
{
    public string Mensaje { get; set; } = "";
}

public class CorreoDto
{
    [Required(ErrorMessage = "Ingresá tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Correo { get; set; } = "";
}

public class VerificacionDto
{
    [Required(ErrorMessage = "Ingresá tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Ingresá el código.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código tiene 6 números.")]
    public string Codigo { get; set; } = "";
}

public class NuevaContrasenaDto
{
    [Required(ErrorMessage = "Ingresá tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Ingresá el código.")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "El código tiene 6 números.")]
    public string Codigo { get; set; } = "";

    [Required(ErrorMessage = "Ingresá una contraseña nueva.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 64 caracteres.")]
    public string Contrasena { get; set; } = "";
}

public class InicioSesionDto
{
    [Required(ErrorMessage = "Ingresá tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Ingresá tu contraseña.")]
    public string Contrasena { get; set; } = "";
}

public class RefrescoDto
{
    [Required(ErrorMessage = "Falta el token de refresco.")]
    public string TokenRefresco { get; set; } = "";
}

// Sesión iniciada: token corto para cada pedido y token de refresco para renovarlo (RNF-SEG-03).
public class SesionReadDto
{
    public string TokenAcceso { get; set; } = "";
    public DateTime VenceTokenAcceso { get; set; }
    public string TokenRefresco { get; set; } = "";
    public DateTime VenceTokenRefresco { get; set; }
    public int CuentaId { get; set; }
    public string Nombre { get; set; } = "";
    public List<string> Roles { get; set; } = new();
}

// Datos de la cuenta propia. Nunca incluye hashes, códigos ni intentos.
public class CuentaReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string NumeroDocumento { get; set; } = "";
    public string Calle { get; set; } = "";
    public string NumeroPuerta { get; set; } = "";
    public string Apartamento { get; set; } = "";
    public string Localidad { get; set; } = "";
    public string Departamento { get; set; } = "";
    public string Referencias { get; set; } = "";
    public bool Verificada { get; set; }
    public List<string> Roles { get; set; } = new();
}
