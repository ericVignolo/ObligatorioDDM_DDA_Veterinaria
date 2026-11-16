namespace Veterinaria.Blazor.Models;

// Corrección de una consulta firmada (A-020, RF-VET-13, RN-12). El original nunca cambia.
public class Enmienda
{
    public int Id { get; set; }
    public int ConsultaId { get; set; }
    public string Motivo { get; set; } = "";
    public string Texto { get; set; } = "";
    public int AutorId { get; set; }
    public string AutorNombre { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string HashContenido { get; set; } = "";
}
