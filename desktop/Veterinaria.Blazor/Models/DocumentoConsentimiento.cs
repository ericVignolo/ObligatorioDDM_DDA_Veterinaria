namespace Veterinaria.Blazor.Models;

// Texto del consentimiento informado de una prestación, por versión (A-025, RF-CLI-11).
// Una versión nueva reemplaza a la anterior, que se conserva como historial.
public class DocumentoConsentimiento
{
    public int Id { get; set; }
    public int PrestacionId { get; set; }
    public string Version { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Texto { get; set; } = "";
    public DateTime FechaVigencia { get; set; }
    public bool Vigente { get; set; } = true;
}
