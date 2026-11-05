namespace Veterinaria.Blazor.Models;

// Profesional visible en el área pública (RF-VIS-02, HU-VIS-02) y su agenda de demostración (A-014).
public class Profesional
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Especialidad { get; set; } = "";
    public string Descripcion { get; set; } = "";

    // Horario semanal de trabajo, descansos dentro de ese horario y días sin atención.
    public List<FranjaHoraria> Jornada { get; set; } = new();
    public List<FranjaHoraria> Descansos { get; set; } = new();
    public List<Ausencia> Ausencias { get; set; } = new();

    // Qué especies atiende y qué servicios (Id de Prestacion) realiza.
    public List<string> Especies { get; set; } = new();
    public List<int> PrestacionIds { get; set; } = new();

    // Solo se muestran los habilitados para publicación.
    public bool Publicado { get; set; } = true;
}
