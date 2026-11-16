using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Privacidad, Términos y Cookies, versionados (RF-VIS-10, RNF-LEG-03).
public class DocumentoLegal
{
    public int Id { get; set; }
    [MaxLength(30)] public string Codigo { get; set; } = "";
    [MaxLength(120)] public string Titulo { get; set; } = "";
    [MaxLength(20)] public string Version { get; set; } = "";
    public DateTime VigenteDesde { get; set; }
    [MaxLength(60)] public string Ruta { get; set; } = "";
}

// Qué versión aceptó cada cuenta y cuándo (RN-43). Se guarda en la misma transacción que la cuenta.
public class AceptacionLegal
{
    public int Id { get; set; }

    public int CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }

    public int DocumentoLegalId { get; set; }
    public DocumentoLegal? DocumentoLegal { get; set; }

    public DateTime FechaAceptacion { get; set; }
}
