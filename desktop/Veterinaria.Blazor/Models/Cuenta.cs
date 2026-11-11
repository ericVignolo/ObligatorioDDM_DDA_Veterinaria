namespace Veterinaria.Blazor.Models;

// Cuenta de Cliente de demostración. En la sección C la crea y guarda la API.
public class Cuenta
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string TipoDocumento { get; set; } = "";
    public string NumeroDocumento { get; set; } = "";
    public string Calle { get; set; } = "";
    public string NumeroPuerta { get; set; } = "";
    public string Apartamento { get; set; } = "";
    public string Localidad { get; set; } = "";
    public string Departamento { get; set; } = "";
    public string Referencias { get; set; } = "";

    // Solo el derivado de la contraseña (RNF-SEG-01), nunca el texto.
    public string HashContrasena { get; set; } = "";

    public bool Verificada { get; set; }
    public DateTime FechaCreacion { get; set; }

    // Aceptaciones obligatorias con su versión (RN-43, RNF-LEG-03).
    public string VersionTerminosAceptada { get; set; } = "";
    public string VersionPrivacidadAceptada { get; set; } = "";
    public DateTime FechaAceptacion { get; set; }

    // Verificación por correo (D-REG-02).
    public string HashCodigo { get; set; } = "";
    public DateTime CodigoVence { get; set; }
    public int IntentosRestantes { get; set; }
    public List<DateTime> Reenvios { get; set; } = new();

    // Recuperación de contraseña (D-REG-03): mismos límites que la verificación.
    public string HashCodigoRecuperacion { get; set; } = "";
    public DateTime CodigoRecuperacionVence { get; set; }
    public int IntentosRecuperacion { get; set; }
    public List<DateTime> PedidosRecuperacion { get; set; } = new();
    public string CodigoRecuperacionDemo { get; set; } = "";

    // Cambio de correo (D-PER-04): el correo nuevo se guarda aparte hasta verificarlo con un código.
    public string CorreoNuevoPendiente { get; set; } = "";
    public string HashCodigoCorreo { get; set; } = "";
    public DateTime CodigoCorreoVence { get; set; }
    public int IntentosCorreo { get; set; }
    public List<DateTime> PedidosCambioCorreo { get; set; } = new();
    public string CodigoCorreoDemo { get; set; } = "";

    // SOLO DEMOSTRACIÓN: el código se muestra en pantalla porque todavía no se envían correos.
    // En la sección C lo genera la API y no se guarda en claro.
    public string CodigoDemo { get; set; } = "";
}
