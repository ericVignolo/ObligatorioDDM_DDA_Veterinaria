namespace Veterinaria.Blazor.Models;

// Consulta clínica de un turno (A-019, CU-VET-01, RF-VET-02). Un turno tiene 0..1 consulta.
// Estados: Borrador (editable por su autor) → Firmada (inmutable, RN-12). Las correcciones son enmiendas.
public class Consulta
{
    public int Id { get; set; }

    // 0 = registro anterior al sistema (demostración), sin turno de origen.
    public int TurnoId { get; set; }
    public int AnimalId { get; set; }
    public int ProfesionalId { get; set; }

    // Veterinario que la inició y la firma (Personal.Id).
    public int AutorId { get; set; }
    public string AutorNombre { get; set; } = "";

    public string Estado { get; set; } = "Borrador";

    // Control de versión: un guardado con versión vieja se rechaza (RNF-CON-05, A-RC-02).
    public int Version { get; set; } = 1;

    public DateTime FechaInicio { get; set; }

    // Datos clínicos (RF-VET-02). Para firmar solo son obligatorios motivo y diagnóstico (P-RC-06, RN-20).
    public string Motivo { get; set; } = "";
    public string Anamnesis { get; set; } = "";
    public string ExamenFisico { get; set; } = "";
    public decimal? TemperaturaC { get; set; }
    public int? FrecuenciaCardiaca { get; set; }
    public int? FrecuenciaRespiratoria { get; set; }
    public decimal? PesoKg { get; set; }
    public string Alergias { get; set; } = "";
    public string Diagnostico { get; set; } = "";
    public string TipoDiagnostico { get; set; } = "Presuntivo";
    public string Tratamiento { get; set; } = "";
    public string Indicaciones { get; set; } = "";

    // Firma (P-RC-06): autor, fecha y hora, y huella del contenido firmado.
    public DateTime? FechaFirma { get; set; }
    public string HashContenido { get; set; } = "";

    public List<Enmienda> Enmiendas { get; set; } = new();
}
