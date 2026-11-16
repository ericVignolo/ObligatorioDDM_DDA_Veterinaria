namespace Veterinaria.Blazor.Models;

// Solicitud de orientación o asistencia veterinaria (A-026, RF-CLI-21 a RF-CLI-25, RF-REC-08, RF-REC-09, RF-VET-16, RF-VET-17).
// La crea el cliente desde el móvil (B-013) o Recepción cuando llama por teléfono. Nivel: Rojo, Naranja, Amarillo o Verde (RN-47).
public class SolicitudAsistencia
{
    public int Id { get; set; }
    public int ClienteId { get; set; }

    // 0 = animal no registrado o grupo descrito en la solicitud (RN-59).
    public int AnimalId { get; set; }
    public string Especie { get; set; } = "";
    public int Cantidad { get; set; } = 1;
    public string Sintomas { get; set; } = "";

    // «Visita» al lugar u «Orientación y traslado» a la clínica.
    public string Modalidad { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Referencias { get; set; } = "";
    public bool UbicacionGps { get; set; }

    // Distancia aproximada desde la clínica; la cobertura es de 30 km (parámetro de Eric, 31/10).
    public int DistanciaKm { get; set; }
    public string TelefonoContacto { get; set; } = "";

    // «App» o «Teléfono».
    public string Canal { get; set; } = "";
    public string ClaveIdempotencia { get; set; } = "";

    // Nivel que indicó el cliente y nivel vigente (RN-46: antes de la revisión profesional solo puede subir).
    public string NivelCliente { get; set; } = "";
    public string Nivel { get; set; } = "";

    public string Estado { get; set; } = "";
    public DateTime FechaCreacion { get; set; }

    // Veterinarios (Personal.Id) a los que está ofrecida y desde cuándo.
    public List<int> OfrecidaA { get; set; } = new();
    public DateTime? FechaOferta { get; set; }

    // Veterinario que aceptó (Personal.Id) y estimación de llegada (RN-53: solo después de aceptar).
    public int VeterinarioId { get; set; }
    public string VeterinarioNombre { get; set; } = "";
    public DateTime? FechaAceptacion { get; set; }
    public int? EtaMinutos { get; set; }

    // Costo informado antes de confirmar (RF-CLI-25, RN-58).
    public decimal Costo { get; set; }

    public bool ReevaluacionPedida { get; set; }
    public string MotivoCoordinacion { get; set; } = "";
    public string MotivoCierre { get; set; } = "";
    public string Resultado { get; set; } = "";
}
