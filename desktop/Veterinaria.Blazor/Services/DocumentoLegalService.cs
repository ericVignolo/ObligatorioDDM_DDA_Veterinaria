using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Versiones vigentes de los documentos legales. En la sección C las provee la API.
public class DocumentoLegalService
{
    private readonly List<DocumentoLegal> _documentos = new()
    {
        new() { Codigo = "privacidad", Titulo = "Política de Privacidad", Version = "1.0", VigenteDesde = new DateOnly(2026, 11, 2), Ruta = "privacidad" },
        new() { Codigo = "terminos", Titulo = "Términos y Condiciones", Version = "1.0", VigenteDesde = new DateOnly(2026, 11, 2), Ruta = "terminos" },
        new() { Codigo = "cookies", Titulo = "Política de Cookies", Version = "1.0", VigenteDesde = new DateOnly(2026, 11, 2), Ruta = "cookies" }
    };

    public List<DocumentoLegal> ObtenerVigentes()
    {
        return _documentos;
    }

    public DocumentoLegal? ObtenerPorCodigo(string codigo)
    {
        return _documentos.FirstOrDefault(d => d.Codigo == codigo);
    }
}
