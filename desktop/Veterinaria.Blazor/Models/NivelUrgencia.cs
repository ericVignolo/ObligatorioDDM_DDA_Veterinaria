namespace Veterinaria.Blazor.Models;

// Nivel informativo de asistencia (ESPECIFICACION_ALERTAS_ASISTENCIA.md). Se muestra en /urgencias.
public class NivelUrgencia
{
    public string Nombre { get; set; } = "";
    public string Clase { get; set; } = "";
    public string Significado { get; set; } = "";
    public string Horario { get; set; } = "";
}
