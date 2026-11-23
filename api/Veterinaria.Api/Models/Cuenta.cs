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

    // Bloqueo temporal progresivo por intentos fallidos de ingreso (RNF-SEG-09).
    public int IntentosFallidos { get; set; }
    public DateTime? BloqueadaHasta { get; set; }

    // Baja lógica: no se borran cuentas con historia.
    public bool Activa { get; set; } = true;

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaBaja { get; set; }

    // Derecho de supresión (A-028, RNF-LEG-02): los datos personales se reemplazan por valores neutros;
    // los animales y la historia clínica se conservan sin identificar al titular.
    public bool Eliminada { get; set; }
    public DateTime? FechaEliminacion { get; set; }

    // Cambio de correo (D-PER-04): el nuevo queda aparte hasta verificarlo con un código (CodigosCuenta, tipo "CambioCorreo").
    [MaxLength(150)] public string CorreoNuevoPendiente { get; set; } = "";

    // Verificación en dos pasos con app autenticadora (RNF-SEG-04): obligatoria para el Administrador.
    // El secreto se guarda cifrado con la protección de datos de ASP.NET Core, nunca en claro.
    public bool SegundoFactorActivo { get; set; }
    [MaxLength(512)] public string SecretoSegundoFactor { get; set; } = "";

    // Último intervalo de 30 s usado: el mismo código no sirve dos veces.
    public long UltimoPasoSegundoFactor { get; set; }

    public List<CuentaRol> Roles { get; set; } = new();
    public List<AceptacionLegal> Aceptaciones { get; set; } = new();
    public List<Responsabilidad> Responsabilidades { get; set; } = new();
}
