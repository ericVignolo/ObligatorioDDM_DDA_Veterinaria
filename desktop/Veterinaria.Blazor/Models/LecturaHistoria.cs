namespace Veterinaria.Blazor.Models;

// Registro de auditoría: quién del personal leyó qué historia y cuándo (RN-40, RNF-SEG-10).
// No copia texto clínico.
public class LecturaHistoria
{
    public int PersonalId { get; set; }
    public int AnimalId { get; set; }
    public DateTime Fecha { get; set; }

    // Vacío si el animal tenía un turno activo; si no, el motivo del acceso (P-RC-05).
    public string Motivo { get; set; } = "";
}
