namespace Veterinaria.Blazor.Models;

// Entrada del seguimiento de una internación (RF-VET-10, RF-VET-11).
// No se edita ni se borra: se corrige con una rectificación nueva (RN-24).
// Visible = la ve el cliente; si no, es una nota interna del personal (RN-23).
public class NovedadInternacion
{
    public int Id { get; set; }
    public int InternacionId { get; set; }

    // «Novedad», «Evolución» o «Rectificación».
    public string Tipo { get; set; } = "Novedad";
    public string Texto { get; set; } = "";
    public bool Visible { get; set; }

    // Si es una rectificación, la entrada que corrige.
    public int RectificaId { get; set; }

    public DateTime Fecha { get; set; }
    public int AutorId { get; set; }
    public string AutorNombre { get; set; } = "";
}
