namespace Veterinaria.Blazor.Models;

// Respuesta del responsable a un consentimiento informado (A-025, RF-CLI-11, RN-10, RN-43, P-RC-06).
// Se guarda versión, procedimiento, animal, fecha, canal, nombre escrito y una huella. Nunca se modifica:
// una respuesta nueva (por ejemplo, firmar después de rechazar) es otro registro.
public class Consentimiento
{
    public int Id { get; set; }
    public int TurnoId { get; set; }
    public int ClienteId { get; set; }
    public int AnimalId { get; set; }
    public int PrestacionId { get; set; }
    public int DocumentoId { get; set; }
    public string Version { get; set; } = "";

    // «Aceptado» o «Rechazado».
    public string Respuesta { get; set; } = "";

    // Nombre completo que escribió el responsable al aceptar.
    public string NombreEscrito { get; set; } = "";
    public string Canal { get; set; } = "En línea";
    public DateTime Fecha { get; set; }
    public string Hash { get; set; } = "";
}
