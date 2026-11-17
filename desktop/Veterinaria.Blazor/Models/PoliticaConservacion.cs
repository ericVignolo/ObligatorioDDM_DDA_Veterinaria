namespace Veterinaria.Blazor.Models;

// Qué se guarda de cada categoría de dato, para qué, quién accede, cuánto se conserva y qué pasa al eliminar (RNF-LEG-06).
public class PoliticaConservacion
{
    public string Categoria { get; set; } = "";
    public string Finalidad { get; set; } = "";
    public string Acceso { get; set; } = "";
    public string Conservacion { get; set; } = "";
    public string AlEliminar { get; set; } = "";
}
