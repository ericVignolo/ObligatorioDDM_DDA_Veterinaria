using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Alta de una cuenta del personal por el Administrador (A-027, RF-ADM-01).
public class PersonalFormulario
{
    [Required(ErrorMessage = "Escribí el nombre.")]
    [StringLength(80, ErrorMessage = "El nombre puede tener hasta 80 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Escribí el correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(120)]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Elegí el rol.")]
    public string Rol { get; set; } = "";

    // Para un Veterinario: su ficha de profesional (agenda y especies).
    public int ProfesionalId { get; set; }
}
