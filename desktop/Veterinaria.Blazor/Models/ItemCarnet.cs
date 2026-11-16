namespace Veterinaria.Blazor.Models;

// Una línea del carnet sanitario de un animal: la última aplicación de cada producto y su estado (A-021).
public class ItemCarnet
{
    public AplicacionSanitaria Aplicacion { get; set; } = new();

    // «Vigente», «Refuerzo próximo» o «Vencida» (constantes de VacunaService).
    public string Estado { get; set; } = "";

    // Días hasta el refuerzo; negativo si ya pasó.
    public int DiasParaRefuerzo { get; set; }
}
