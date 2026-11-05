using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Correo para pedir un código de recuperación (A-007, D-REG-03).
public class SolicitudRecuperacion
{
    [Required(ErrorMessage = "Ingresá tu correo electrónico.")]
    [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
    public string Correo { get; set; } = "";
}
