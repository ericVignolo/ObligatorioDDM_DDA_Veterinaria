using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Datos de una enmienda (A-020, HU-VET-06). Se firma con la contraseña, igual que la consulta.
public class EnmiendaFormulario
{
    [Required(ErrorMessage = "Indicá el motivo de la enmienda.")]
    [StringLength(200, ErrorMessage = "El motivo puede tener hasta 200 caracteres.")]
    public string Motivo { get; set; } = "";

    [Required(ErrorMessage = "Escribí la corrección o el agregado.")]
    [StringLength(2000, ErrorMessage = "La enmienda puede tener hasta 2000 caracteres.")]
    public string Texto { get; set; } = "";

    [Required(ErrorMessage = "Ingresá tu contraseña para firmar.")]
    public string Contrasena { get; set; } = "";
}
