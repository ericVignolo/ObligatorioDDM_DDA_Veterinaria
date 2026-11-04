using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Datos del formulario de ingreso (A-008).
public class InicioSesion
{
    [Required(ErrorMessage = "Ingresá tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Ingresá tu contraseña.")]
    public string Contrasena { get; set; } = "";
}
