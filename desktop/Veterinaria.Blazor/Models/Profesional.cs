namespace Veterinaria.Blazor.Models;

// Profesional visible en el área pública (RF-VIS-02, HU-VIS-02).
public class Profesional
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public List<string> Horarios { get; set; } = new();

    // Solo se muestran los habilitados para publicación.
    public bool Publicado { get; set; } = true;
}
