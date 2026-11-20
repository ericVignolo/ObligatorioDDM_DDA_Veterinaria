using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Token de refresco de una sesión (RNF-SEG-03): se puede revocar una por una (cerrar sesión)
// o todas juntas al cambiar la contraseña (D-REG-03). Se guarda solo el hash del token.
public class SesionRefresco
{
    public int Id { get; set; }

    public int CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }

    [MaxLength(100)] public string HashToken { get; set; } = "";

    // Fechas en UTC.
    public DateTime FechaCreacion { get; set; }
    public DateTime Vence { get; set; }
    public DateTime? RevocadaEn { get; set; }

    // Dirección de origen, para auditoría.
    [MaxLength(60)] public string Origen { get; set; } = "";
}
