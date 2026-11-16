using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Código de 6 dígitos por correo para verificar la cuenta (D-REG-02) o recuperar la contraseña (D-REG-03).
// Vale 15 minutos, 5 intentos; los reenvíos (3 por hora) se cuentan con las filas de la última hora.
public class CodigoCuenta
{
    public int Id { get; set; }

    public int CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }

    // "Verificacion" o "Recuperacion".
    [MaxLength(20)] public string Tipo { get; set; } = "";

    // Solo el hash, nunca el código (D-REG-02).
    [MaxLength(255)] public string HashCodigo { get; set; } = "";

    public DateTime FechaCreacion { get; set; }
    public DateTime Vence { get; set; }
    public int IntentosRestantes { get; set; } = 5;
    public bool Usado { get; set; }
}
