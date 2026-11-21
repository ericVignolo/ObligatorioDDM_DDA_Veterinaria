using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Box de internación (RF-ADM-03). Un solo paciente a la vez (RN-22).
public class Box
{
    public int Id { get; set; }
    [MaxLength(60)] public string Nombre { get; set; } = "";

    // "Pequeños animales" o "Grandes animales".
    [MaxLength(30)] public string Tipo { get; set; } = "";

    // Un box fuera de servicio no recibe internaciones.
    public bool Activo { get; set; } = true;
}

// Internación (A-024, RF-VET-09 a RF-VET-11).
// Admitida (Recepción) → Internado (Veterinario: box y plan) → Alta; o Anulada si no llegó a ingresar.
// Un animal tiene a lo sumo una abierta (RN-21): lo controla la API dentro de la transacción.
public class Internacion
{
    public int Id { get; set; }

    public int AnimalId { get; set; }
    public Animal? Animal { get; set; }

    public int ClienteId { get; set; }
    public Cuenta? Cliente { get; set; }

    [MaxLength(20)] public string Estado { get; set; } = "Admitida";

    // Datos administrativos (Recepción).
    [MaxLength(500)] public string Motivo { get; set; } = "";
    [MaxLength(30)] public string TelefonoContacto { get; set; } = "";
    public string Observaciones { get; set; } = "";
    public DateTime FechaAdmision { get; set; }
    public int? AdmitidoPorId { get; set; }
    public Cuenta? AdmitidoPor { get; set; }

    // Ingreso clínico (Veterinario): box y plan terapéutico (RF-VET-09). Null = todavía sin box.
    public int? BoxId { get; set; }
    public Box? Box { get; set; }
    public string PlanTerapeutico { get; set; } = "";
    public DateTime? FechaIngreso { get; set; }
    public int? VeterinarioIngresoId { get; set; }
    public Cuenta? VeterinarioIngreso { get; set; }

    // Alta (RF-VET-11, RN-25).
    public string EvolucionFinal { get; set; } = "";
    public string IndicacionesDomicilio { get; set; } = "";
    public DateTime? FechaAlta { get; set; }
    public int? VeterinarioAltaId { get; set; }
    public Cuenta? VeterinarioAlta { get; set; }

    [MaxLength(250)] public string MotivoAnulacion { get; set; } = "";

    public int Version { get; set; }

    public List<NovedadInternacion> Novedades { get; set; } = new();
}

// Entrada del seguimiento (RF-VET-10). No se edita ni se borra: se corrige con una rectificación (RN-24).
// Visible = la ve el cliente; si no, es nota interna (RN-23).
public class NovedadInternacion
{
    public int Id { get; set; }

    public int InternacionId { get; set; }
    public Internacion? Internacion { get; set; }

    // "Novedad", "Evolución" o "Rectificación".
    [MaxLength(20)] public string Tipo { get; set; } = "Novedad";
    public string Texto { get; set; } = "";
    public bool Visible { get; set; }

    // Si es una rectificación, la entrada que corrige.
    public int? RectificaId { get; set; }
    public NovedadInternacion? Rectifica { get; set; }

    public DateTime Fecha { get; set; }

    public int AutorId { get; set; }
    public Cuenta? Autor { get; set; }
}
