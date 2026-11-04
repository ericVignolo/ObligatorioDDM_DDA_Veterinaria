namespace Veterinaria.Blazor.Models;

// Documento legal versionado (RF-VIS-10, RN-43). La aceptación por versión se registra en la API (sección C).
public class DocumentoLegal
{
    public string Codigo { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Version { get; set; } = "";
    public DateOnly VigenteDesde { get; set; }
    public string Ruta { get; set; } = "";
}
