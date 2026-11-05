namespace Veterinaria.Blazor.Models;

// Días en que un profesional no atiende: licencia, capacitación, etc. (RF-ADM-04).
public class Ausencia
{
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public string Motivo { get; set; } = "";
}
