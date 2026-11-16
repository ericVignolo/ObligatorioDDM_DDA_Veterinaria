namespace Veterinaria.Blazor.Models;

// Precio de una prestación desde una fecha (A-027, RF-ADM-02). El cargo usa el precio vigente en la fecha de la atención (RN-33).
public class TarifaPrestacion
{
    public int PrestacionId { get; set; }
    public decimal Precio { get; set; }
    public DateOnly VigenteDesde { get; set; }
    public string RegistradoPor { get; set; } = "";
}
