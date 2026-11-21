namespace Veterinaria.Blazor.Models;

// Canal elegido por el cliente para un tipo de comunicación (RF-CLI-18, RF-CLI-20).
public class PreferenciaComunicacion
{
    public int CuentaId { get; set; }
    public string Tipo { get; set; } = "";
    public string Canal { get; set; } = "";
    public DateTime Fecha { get; set; }
}
