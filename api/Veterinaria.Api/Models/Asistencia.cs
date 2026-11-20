using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Solicitud de orientación o asistencia (A-026, RF-CLI-21 a 25, RF-REC-08/09, RF-VET-16/17).
// La crea el cliente desde la app o Recepción por teléfono. Nivel: Rojo, Naranja, Amarillo o Verde (RN-47).
public class SolicitudAsistencia
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cuenta? Cliente { get; set; }

    // Null = animal no registrado o grupo descrito en la solicitud (RN-59).
    public int? AnimalId { get; set; }
    public Animal? Animal { get; set; }

    [MaxLength(40)] public string Especie { get; set; } = "";
    public int Cantidad { get; set; } = 1;
    public string Sintomas { get; set; } = "";

    // "Visita" u "Orientación y traslado".
    [MaxLength(30)] public string Modalidad { get; set; } = "";

    [MaxLength(250)] public string Direccion { get; set; } = "";
    [MaxLength(250)] public string Referencias { get; set; } = "";

    // GPS consentido (RNF-LEG-09). Las coordenadas se usan solo durante la asistencia.
    public bool UbicacionGps { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }

    // Distancia aproximada desde la clínica; cobertura por defecto 30 km.
    public int DistanciaKm { get; set; }

    [MaxLength(30)] public string TelefonoContacto { get; set; } = "";

    // "App" o "Teléfono".
    [MaxLength(20)] public string Canal { get; set; } = "";

    // Evita crear dos veces la misma solicitud desde la app. Única por cliente; null en las telefónicas.
    [MaxLength(80)] public string? ClaveIdempotencia { get; set; }

    // Nivel que indicó el cliente y nivel vigente (RN-46).
    [MaxLength(20)] public string NivelCliente { get; set; } = "";
    [MaxLength(20)] public string Nivel { get; set; } = "";
    [MaxLength(30)] public string Estado { get; set; } = "";

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaOferta { get; set; }

    // Veterinario que aceptó y estimación de llegada (RN-53: solo después de aceptar).
    public int? VeterinarioId { get; set; }
    public Cuenta? Veterinario { get; set; }
    public DateTime? FechaAceptacion { get; set; }
    public int? EtaMinutos { get; set; }

    // Costo informado antes de confirmar (RF-CLI-25, RN-58).
    public decimal Costo { get; set; }

    public bool ReevaluacionPedida { get; set; }
    [MaxLength(500)] public string MotivoCoordinacion { get; set; } = "";
    [MaxLength(500)] public string MotivoCierre { get; set; } = "";
    public string Resultado { get; set; } = "";

    public int Version { get; set; }

    public List<OfertaAsistencia> Ofertas { get; set; } = new();
    public List<EventoAsistencia> Eventos { get; set; } = new();
}

// A qué veterinarios se ofreció una solicitud roja y qué respondieron (reemplaza la lista OfrecidaA de Blazor).
public class OfertaAsistencia
{
    public int SolicitudAsistenciaId { get; set; }
    public SolicitudAsistencia? SolicitudAsistencia { get; set; }

    public int VeterinarioId { get; set; }
    public Cuenta? Veterinario { get; set; }

    public DateTime Fecha { get; set; }

    // "Pendiente", "Aceptada", "Rechazada" o "Vencida".
    [MaxLength(20)] public string Respuesta { get; set; } = "Pendiente";
}

// Historial inmutable de una solicitud: estados, ofertas, cambios de nivel y empeoramientos (RN-51, RN-52).
public class EventoAsistencia
{
    public long Id { get; set; }

    public int SolicitudAsistenciaId { get; set; }
    public SolicitudAsistencia? SolicitudAsistencia { get; set; }

    public DateTime Fecha { get; set; }
    [MaxLength(40)] public string Tipo { get; set; } = "";
    [MaxLength(1000)] public string Texto { get; set; } = "";

    // Null = el sistema.
    public int? AutorId { get; set; }
    public Cuenta? Autor { get; set; }

    // Solo en los cambios de nivel.
    [MaxLength(20)] public string NivelAnterior { get; set; } = "";
    [MaxLength(20)] public string NivelNuevo { get; set; } = "";
}
