using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Cuenta de una persona: Cliente o personal (RF-VIS-07, D-REG-01). Los roles van en CuentaRol.
public class Cuenta
{
    public int Id { get; set; }

    [MaxLength(80)] public string Nombre { get; set; } = "";
    [MaxLength(80)] public string Apellido { get; set; } = "";

    // Único (índice en AppDbContext). Se guarda en minúsculas.
    [MaxLength(150)] public string Correo { get; set; } = "";

    [MaxLength(30)] public string Telefono { get; set; } = "";
    [MaxLength(20)] public string TipoDocumento { get; set; } = "";
    [MaxLength(30)] public string NumeroDocumento { get; set; } = "";

    [MaxLength(120)] public string Calle { get; set; } = "";
    [MaxLength(20)] public string NumeroPuerta { get; set; } = "";
    [MaxLength(20)] public string Apartamento { get; set; } = "";
    [MaxLength(80)] public string Localidad { get; set; } = "";
    [MaxLength(80)] public string Departamento { get; set; } = "";
    [MaxLength(250)] public string Referencias { get; set; } = "";

    // Solo el derivado de la contraseña, nunca el texto (RNF-SEG-01).
    [MaxLength(255)] public string HashContrasena { get; set; } = "";

    public bool Verificada { get; set; }

    // Baja lógica: no se borran cuentas con historia.
    public bool Activa { get; set; } = true;

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaBaja { get; set; }

    public List<CuentaRol> Roles { get; set; } = new();
    public List<AceptacionLegal> Aceptaciones { get; set; } = new();
    public List<Responsabilidad> Responsabilidades { get; set; } = new();
}
