namespace Veterinaria.Blazor.Models;

// Internación de un animal (A-024, RF-VET-09 a RF-VET-11).
// Estados: Admitida (Recepción cargó lo administrativo) → Internado (el veterinario asignó box y plan) → Alta.
// Una admisión sin ingreso clínico se puede anular (Anulada). Un animal tiene a lo sumo una abierta (RN-21).
public class Internacion
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public int ClienteId { get; set; }
    public string Estado { get; set; } = "Admitida";

    // Datos administrativos del ingreso (Recepción, decisión de Eric del 31/10).
    public string Motivo { get; set; } = "";
    public string TelefonoContacto { get; set; } = "";
    public string Observaciones { get; set; } = "";
    public DateTime FechaAdmision { get; set; }
    public string AdmitidoPor { get; set; } = "";

    // Ingreso clínico (Veterinario): box y plan terapéutico (RF-VET-09). 0 = todavía sin box.
    public int BoxId { get; set; }
    public string PlanTerapeutico { get; set; } = "";
    public DateTime? FechaIngreso { get; set; }
    public string VeterinarioIngreso { get; set; } = "";

    // Alta (RF-VET-11, RN-25): evolución final e indicaciones para el domicilio.
    public string EvolucionFinal { get; set; } = "";
    public string IndicacionesDomicilio { get; set; } = "";
    public DateTime? FechaAlta { get; set; }
    public string VeterinarioAlta { get; set; } = "";

    // Anulación de una admisión que no llegó a ingresar.
    public string MotivoAnulacion { get; set; } = "";
}
