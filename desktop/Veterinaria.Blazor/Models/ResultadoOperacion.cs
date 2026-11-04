namespace Veterinaria.Blazor.Models;

// Resultado simple de una operación del servicio: si salió bien y qué mostrar.
public class ResultadoOperacion
{
    public bool Exito { get; set; }
    public string Mensaje { get; set; } = "";
}
