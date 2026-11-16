namespace Veterinaria.Blazor.Models;

// Box de internación (A-024, RF-ADM-03). Admite un solo paciente a la vez (RN-22).
// Tipo: «Pequeños animales» o «Grandes animales» (caballos y bovinos).
public class Box
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";

    // Un box fuera de servicio no recibe internaciones (RF-ADM-03, A-027).
    public bool Activo { get; set; } = true;
}
