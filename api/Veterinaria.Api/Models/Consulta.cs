using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Consulta clínica de un turno (A-019, CU-VET-01, RF-VET-02). Un turno tiene a lo sumo una consulta.
// Borrador (editable por su autor) → Firmada (inmutable, RN-12). Las correcciones son enmiendas.
// Los textos clínicos largos no llevan MaxLength: van como LONGTEXT y no cuentan para el límite de fila de MySQL.
public class Consulta
{
    public int Id { get; set; }

    // Null = registro anterior al sistema, sin turno de origen. Único: un turno, una consulta.
    public int? TurnoId { get; set; }
    public Turno? Turno { get; set; }

    public int AnimalId { get; set; }
    public Animal? Animal { get; set; }

    public int ProfesionalId { get; set; }
    public Profesional? Profesional { get; set; }

    // Veterinario que la inicia y la firma.
    public int AutorId { get; set; }
    public Cuenta? Autor { get; set; }

    // "Borrador" o "Firmada".
    [MaxLength(20)] public string Estado { get; set; } = "Borrador";

    // Un guardado con versión vieja se rechaza (RNF-CON-05).
    public int Version { get; set; } = 1;

    public DateTime FechaInicio { get; set; }

    // Datos clínicos (RF-VET-02). Para firmar solo son obligatorios motivo y diagnóstico (RN-20).
    [MaxLength(500)] public string Motivo { get; set; } = "";
    public string Anamnesis { get; set; } = "";
    public string ExamenFisico { get; set; } = "";
    public decimal? TemperaturaC { get; set; }
    public int? FrecuenciaCardiaca { get; set; }
    public int? FrecuenciaRespiratoria { get; set; }
    public decimal? PesoKg { get; set; }
    [MaxLength(500)] public string Alergias { get; set; } = "";
    public string Diagnostico { get; set; } = "";
    // "Presuntivo" o "Definitivo".
    [MaxLength(20)] public string TipoDiagnostico { get; set; } = "Presuntivo";
    public string Tratamiento { get; set; } = "";
    public string Indicaciones { get; set; } = "";

    // RN-17: alergia registrada que se indicó igual, confirmada al firmar. Vacío si no hubo.
    [MaxLength(500)] public string AlergiaConfirmada { get; set; } = "";

    // Firma: fecha y huella del contenido firmado.
    public DateTime? FechaFirma { get; set; }
    [MaxLength(100)] public string HashContenido { get; set; } = "";

    public List<Enmienda> Enmiendas { get; set; } = new();
}

// Corrección de una consulta firmada (A-020, RF-VET-13, RN-12). El original nunca cambia.
public class Enmienda
{
    public int Id { get; set; }

    public int ConsultaId { get; set; }
    public Consulta? Consulta { get; set; }

    [MaxLength(500)] public string Motivo { get; set; } = "";
    public string Texto { get; set; } = "";

    public int AutorId { get; set; }
    public Cuenta? Autor { get; set; }

    public DateTime Fecha { get; set; }
    [MaxLength(100)] public string HashContenido { get; set; } = "";
}

// Quién del personal leyó qué historia y cuándo (RN-40, RNF-SEG-10). No copia texto clínico.
public class LecturaHistoria
{
    public long Id { get; set; }

    public int CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }

    public int AnimalId { get; set; }
    public Animal? Animal { get; set; }

    public DateTime Fecha { get; set; }

    // Vacío si el animal tenía un turno activo; si no, el motivo del acceso (P-RC-05).
    [MaxLength(250)] public string Motivo { get; set; } = "";
}

// Texto del consentimiento informado de una prestación, por versión (A-025, RF-CLI-11).
// Una versión nueva reemplaza a la anterior, que se conserva.
public class DocumentoConsentimiento
{
    public int Id { get; set; }

    public int PrestacionId { get; set; }
    public Prestacion? Prestacion { get; set; }

    [MaxLength(20)] public string Version { get; set; } = "";
    [MaxLength(200)] public string Titulo { get; set; } = "";
    public string Texto { get; set; } = "";
    public DateTime FechaVigencia { get; set; }
    public bool Vigente { get; set; } = true;
}

// Respuesta del responsable a un consentimiento (RF-CLI-11, RN-10, RN-43). Nunca se modifica:
// una respuesta nueva (por ejemplo, firmar después de rechazar) es otro registro.
public class Consentimiento
{
    public int Id { get; set; }

    public int TurnoId { get; set; }
    public Turno? Turno { get; set; }

    public int ClienteId { get; set; }
    public Cuenta? Cliente { get; set; }

    public int AnimalId { get; set; }
    public Animal? Animal { get; set; }

    public int PrestacionId { get; set; }
    public Prestacion? Prestacion { get; set; }

    public int DocumentoConsentimientoId { get; set; }
    public DocumentoConsentimiento? DocumentoConsentimiento { get; set; }

    [MaxLength(20)] public string Version { get; set; } = "";

    // "Aceptado" o "Rechazado".
    [MaxLength(20)] public string Respuesta { get; set; } = "";

    // Nombre completo que escribió el responsable al aceptar.
    [MaxLength(160)] public string NombreEscrito { get; set; } = "";

    [MaxLength(30)] public string Canal { get; set; } = "En línea";
    public DateTime Fecha { get; set; }
    [MaxLength(100)] public string Hash { get; set; } = "";
}
