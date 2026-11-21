using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Alta y edición de un servicio por el Administrador (A-027, RF-ADM-02). El precio solo se usa al crear;
// después se cambia con una tarifa con fecha de vigencia.
public class PrestacionFormulario
{
    [Required(ErrorMessage = "Escribí el nombre.")]
    [StringLength(80, ErrorMessage = "El nombre puede tener hasta 80 caracteres.")]
    public string Nombre { get; set; } = "";

    [StringLength(300, ErrorMessage = "La descripción puede tener hasta 300 caracteres.")]
    public string Descripcion { get; set; } = "";

    [Range(1, 1000000, ErrorMessage = "El precio debe estar entre $ 1 y $ 1.000.000.")]
    public int Precio { get; set; }

    [Range(0, 480, ErrorMessage = "La duración debe estar entre 0 y 480 minutos.")]
    public int DuracionMinutos { get; set; }

    public bool ReservableEnLinea { get; set; }
    public bool Publicado { get; set; } = true;
}
