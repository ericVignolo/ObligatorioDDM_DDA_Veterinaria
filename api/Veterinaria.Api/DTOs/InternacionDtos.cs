using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs;

// ---------- Internación (A-024, RF-VET-09 a RF-VET-11, RF-CLI-09, RN-21 a RN-25) ----------

public class InternacionReadDto
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public string Animal { get; set; } = "";
    public string Especie { get; set; } = "";
    public int ClienteId { get; set; }
    public string Cliente { get; set; } = "";

    // "Admitida", "Internado", "Alta" o "Anulada".
    public string Estado { get; set; } = "";
    public string Motivo { get; set; } = "";
    public string TelefonoContacto { get; set; } = "";
    public string Observaciones { get; set; } = "";
    public DateTime FechaAdmision { get; set; }
    public int? BoxId { get; set; }
    public string Box { get; set; } = "";
    public string PlanTerapeutico { get; set; } = "";
    public DateTime? FechaIngreso { get; set; }
    public string EvolucionFinal { get; set; } = "";
    public string IndicacionesDomicilio { get; set; } = "";
    public DateTime? FechaAlta { get; set; }
    public string MotivoAnulacion { get; set; } = "";
    public int Version { get; set; }

    // En el detalle: todas para el veterinario; solo las visibles para el resto (RN-23).
    public List<NovedadReadDto> Novedades { get; set; } = new();

    // Al cambiar algo: lo que se le muestra a quien lo hizo.
    public string Mensaje { get; set; } = "";
}

public class NovedadReadDto
{
    public int Id { get; set; }
    public string Tipo { get; set; } = "";
    public string Texto { get; set; } = "";
    public bool Visible { get; set; }
    public int? RectificaId { get; set; }
    public DateTime Fecha { get; set; }
    public string Autor { get; set; } = "";
}

public class AdmisionDto
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

public class AnulacionAdmisionDto
{
    [Required(ErrorMessage = "Escribí el motivo de la anulación (hasta 200 caracteres).")]
    [StringLength(200, ErrorMessage = "Escribí el motivo de la anulación (hasta 200 caracteres).")]
    public string Motivo { get; set; } = "";
}

public class IngresoClinicoDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí un box libre.")]
    public int BoxId { get; set; }

    [Required(ErrorMessage = "Escribí el plan terapéutico.")]
    [StringLength(2000, ErrorMessage = "El plan puede tener hasta 2000 caracteres.")]
    public string PlanTerapeutico { get; set; } = "";
}

public class NovedadCreateDto
{
    // "Novedad" o "Evolución".
    public string Tipo { get; set; } = "Novedad";

    [Required(ErrorMessage = "Escribí el texto.")]
    [StringLength(1000, ErrorMessage = "El texto puede tener hasta 1000 caracteres.")]
    public string Texto { get; set; } = "";

    // true = la ve el cliente; false = nota interna (RN-23).
    public bool Visible { get; set; }
}

public class RectificacionDto
{
    [Required(ErrorMessage = "Escribí el texto.")]
    [StringLength(1000, ErrorMessage = "El texto puede tener hasta 1000 caracteres.")]
    public string Texto { get; set; } = "";
}

public class AltaDto
{
    [Required(ErrorMessage = "Escribí la evolución final.")]
    [StringLength(1000, ErrorMessage = "La evolución final puede tener hasta 1000 caracteres.")]
    public string EvolucionFinal { get; set; } = "";

    [Required(ErrorMessage = "Escribí las indicaciones para el domicilio.")]
    [StringLength(2000, ErrorMessage = "Las indicaciones pueden tener hasta 2000 caracteres.")]
    public string IndicacionesDomicilio { get; set; } = "";
}

public class BoxReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public bool Activo { get; set; }
    public bool Ocupado { get; set; }
    public int? InternacionId { get; set; }
    public string Animal { get; set; } = "";
}

public class BoxCreateDto
{
    [Required(ErrorMessage = "Escribí el nombre (hasta 40 caracteres) y elegí el tipo.")]
    [StringLength(40, ErrorMessage = "Escribí el nombre (hasta 40 caracteres) y elegí el tipo.")]
    public string Nombre { get; set; } = "";

    // "Pequeños animales" o "Grandes animales".
    [Required(ErrorMessage = "Escribí el nombre (hasta 40 caracteres) y elegí el tipo.")]
    public string Tipo { get; set; } = "";
}

public class BoxServicioDto
{
    public bool Activo { get; set; }
}
