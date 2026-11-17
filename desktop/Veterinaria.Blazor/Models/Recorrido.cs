namespace Veterinaria.Blazor.Models;

// Recorrido demostrativo de un rol (A-030): pasos ordenados para mostrar el sistema con datos ficticios.
public class Recorrido
{
    public string Rol { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Resumen { get; set; } = "";

    // Correo con el que se ingresa; vacío para el Visitante, que no necesita cuenta.
    public string Correo { get; set; } = "";

    public List<PasoRecorrido> Pasos { get; set; } = new();
}

public class PasoRecorrido
{
    public string Accion { get; set; } = "";

    // Ruta relativa de la pantalla (por ejemplo "panel/agenda"), sin barra inicial.
    public string Ruta { get; set; } = "";

    public string Resultado { get; set; } = "";
}
