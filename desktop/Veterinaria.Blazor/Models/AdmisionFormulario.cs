using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Lo que carga Recepción al admitir una internación (A-024).
public class AdmisionFormulario
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí el animal.")]
    public int AnimalId { get; set; }

    [Required(ErrorMessage = "Escribí el motivo de la internación.")]
    [StringLength(200, ErrorMessage = "El motivo puede tener hasta 200 caracteres.")]
    public string Motivo { get; set; } = "";

    [Required(ErrorMessage = "Escribí un teléfono de contacto del responsable.")]
    [StringLength(30, ErrorMessage = "El teléfono puede tener hasta 30 caracteres.")]
    public string TelefonoContacto { get; set; } = "";

    [StringLength(500, ErrorMessage = "Las observaciones pueden tener hasta 500 caracteres.")]
    public string Observaciones { get; set; } = "";
}
