namespace Veterinaria.Blazor.Models;

// Un mensaje al cliente (resumen clínico, aviso de refuerzo, comprobante, alerta). Vive en la bandeja de salida (A-028).
// En la sección C es la tabla de la cola de correos: la API envía fuera de la transacción y reintenta (RNF-CON-04).
public class Comunicacion
{
    public int Id { get; set; }
    public int CuentaId { get; set; }

    // Uno de los tipos de ComunicacionesService.
    public string Tipo { get; set; } = "";
    public string Asunto { get; set; } = "";
    public string Cuerpo { get; set; } = "";

    // «Correo» o «En la app», según la preferencia del cliente al momento de generarla.
    public string Canal { get; set; } = "";

    // Pendiente, Enviado o Fallido.
    public string Estado { get; set; } = "Pendiente";

    // Dirección a la que se envió: se guarda al encolar para que un cambio posterior de correo no la redirija.
    public string Destino { get; set; } = "";
    public int Intentos { get; set; }
    public string UltimoError { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEnvio { get; set; }

    // Clave de idempotencia: el mismo aviso (tipo + clave) no se genera dos veces para una cuenta.
    public string Clave { get; set; } = "";

    // Si es un reenvío bajo pedido (RF-REC-07), el mensaje original.
    public int ReenvioDeId { get; set; }

    // Quién la generó: «Sistema» o el nombre de la persona del personal.
    public string Origen { get; set; } = "Sistema";
    public bool Leida { get; set; }
}
