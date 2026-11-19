using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Tipo de comunicación con su finalidad (RNF-LEG-06) y si el cliente puede dejar de recibirla.
public class TipoComunicacion
{
    public int Id { get; set; }
    [MaxLength(60)] public string Nombre { get; set; } = "";
    [MaxLength(250)] public string Descripcion { get; set; } = "";

    // Esencial: se elige el canal pero no se puede dejar de recibir.
    public bool Esencial { get; set; }

    // "Correo", "Solo en la app" o "No recibir". Los opcionales arrancan sin recibir (RN-43).
    [MaxLength(20)] public string CanalPorDefecto { get; set; } = "";
}

// Canal elegido por el cliente para un tipo de comunicación (RF-CLI-18, RF-CLI-20).
public class PreferenciaComunicacion
{
    public int CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }

    public int TipoComunicacionId { get; set; }
    public TipoComunicacion? TipoComunicacion { get; set; }

    [MaxLength(20)] public string Canal { get; set; } = "";
    public DateTime Fecha { get; set; }
}

// Mensaje al cliente: resumen clínico, aviso de refuerzo, comprobante, alerta (A-028).
// Es la cola de correos: la API envía fuera de la transacción y reintenta (RNF-CON-04).
public class Comunicacion
{
    public long Id { get; set; }

    public int CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }

    public int TipoComunicacionId { get; set; }
    public TipoComunicacion? TipoComunicacion { get; set; }

    [MaxLength(200)] public string Asunto { get; set; } = "";
    public string Cuerpo { get; set; } = "";

    // "Correo" o "En la app", según la preferencia al generarla.
    [MaxLength(20)] public string Canal { get; set; } = "";

    // Pendiente, Enviado o Fallido.
    [MaxLength(20)] public string Estado { get; set; } = "Pendiente";

    // Dirección usada: se guarda al encolar para que un cambio posterior de correo no la redirija.
    [MaxLength(150)] public string Destino { get; set; } = "";

    public int Intentos { get; set; }
    [MaxLength(500)] public string UltimoError { get; set; } = "";

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEnvio { get; set; }

    // Idempotencia: el mismo aviso (tipo + clave) no se genera dos veces para una cuenta. Null si no aplica.
    [MaxLength(120)] public string? Clave { get; set; }

    // Reenvío bajo pedido (RF-REC-07): el mensaje original.
    public long? ReenvioDeId { get; set; }
    public Comunicacion? ReenvioDe { get; set; }

    // Null = la generó el sistema.
    public int? GeneradaPorId { get; set; }
    public Cuenta? GeneradaPor { get; set; }

    public bool Leida { get; set; }
}

// Pedido del titular sobre sus datos (RF-CLI-19, RNF-LEG-01, RNF-LEG-02).
public class SolicitudDerecho
{
    public int Id { get; set; }

    public int CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }

    // "Exportación" o "Eliminación".
    [MaxLength(20)] public string Tipo { get; set; } = "";

    // Recibida, Completada, Rechazada o Cancelada.
    [MaxLength(20)] public string Estado { get; set; } = "Recibida";

    public DateTime Fecha { get; set; }
    public DateTime? FechaResolucion { get; set; }

    public int? ResueltaPorId { get; set; }
    public Cuenta? ResueltaPor { get; set; }

    [MaxLength(500)] public string Detalle { get; set; } = "";
}

// Qué se guarda de cada categoría de dato, para qué, quién accede y cuánto se conserva (RNF-LEG-06).
public class PoliticaConservacion
{
    public int Id { get; set; }
    [MaxLength(120)] public string Categoria { get; set; } = "";
    [MaxLength(250)] public string Finalidad { get; set; } = "";
    [MaxLength(250)] public string Acceso { get; set; } = "";
    [MaxLength(250)] public string Conservacion { get; set; } = "";
    [MaxLength(250)] public string AlEliminar { get; set; } = "";
}
