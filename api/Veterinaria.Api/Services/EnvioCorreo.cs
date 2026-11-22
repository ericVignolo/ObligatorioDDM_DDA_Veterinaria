namespace Veterinaria.Api.Services;

// Envío de los códigos de verificación y de recuperación (D-REG-02, D-REG-03).
// Se envía directo y fuera de la transacción: el código no se guarda en claro en ninguna tabla.
public interface IEnvioCorreo
{
    Task EnviarCodigoAsync(string destino, string tipo, string codigo, DateTime vence);
}

// Versión de desarrollo: todavía no hay servidor de correo (Diego, 2026-11-19, opción A).
// No envía nada ni escribe el código en el log; en Development el código vuelve en la respuesta de la API.
// Para el envío real se reemplaza esta clase en Program.cs, sin tocar nada más.
public class EnvioCorreoDesarrollo : IEnvioCorreo
{
    private readonly ILogger<EnvioCorreoDesarrollo> _logger;

    public EnvioCorreoDesarrollo(ILogger<EnvioCorreoDesarrollo> logger)
    {
        _logger = logger;
    }

    public Task EnviarCodigoAsync(string destino, string tipo, string codigo, DateTime vence)
    {
        _logger.LogInformation("Código de {Tipo} generado (envío de correo pendiente de configurar).", tipo);
        return Task.CompletedTask;
    }
}

// Envío de los mensajes de la cola de Comunicaciones (RNF-CON-04). Devuelve null si salió, o el motivo del error.
public interface IEnvioComunicacion
{
    Task<string?> EnviarAsync(string destino, string asunto, string cuerpo);
}

// Versión de desarrollo: no hay servidor de correo todavía, así que no manda nada y da el envío por hecho.
// Sirve para probar la cola y los estados. Para el envío real se reemplaza esta clase en Program.cs.
public class EnvioComunicacionDesarrollo : IEnvioComunicacion
{
    private readonly ILogger<EnvioComunicacionDesarrollo> _logger;

    public EnvioComunicacionDesarrollo(ILogger<EnvioComunicacionDesarrollo> logger)
    {
        _logger = logger;
    }

    public Task<string?> EnviarAsync(string destino, string asunto, string cuerpo)
    {
        // Sin el destino ni el contenido en el log (RNF-SEG-11).
        _logger.LogInformation("Correo simulado: envío pendiente de configurar.");
        return Task.FromResult<string?>(null);
    }
}

// Servidor de correo (sección "Correo" de appsettings). La contraseña va en appsettings.Development.json o en
// los secretos de usuario, nunca en GitHub.
public class OpcionesCorreo
{
    public string Servidor { get; set; } = "";
    public int Puerto { get; set; } = 587;
    public bool UsarSsl { get; set; } = true;
    public string Usuario { get; set; } = "";
    public string Contrasena { get; set; } = "";
    public string Remitente { get; set; } = "";
    public string NombreRemitente { get; set; } = "Clínica VITALIA";

    // Sin servidor ni remitente, la API sigue con el envío de prueba.
    public bool Configurado => !string.IsNullOrWhiteSpace(Servidor) && !string.IsNullOrWhiteSpace(Remitente);
}

// Envío real por SMTP con System.Net.Mail (viene con .NET; no se instala nada). Sirve para los códigos y para la
// cola de Comunicaciones. Nunca escribe en el log el destino, el código ni el contenido (RNF-SEG-11).
public class CorreoSmtp : IEnvioCorreo, IEnvioComunicacion
{
    private readonly OpcionesCorreo _opciones;
    private readonly ILogger<CorreoSmtp> _logger;

    public CorreoSmtp(Microsoft.Extensions.Options.IOptions<OpcionesCorreo> opciones, ILogger<CorreoSmtp> logger)
    {
        _opciones = opciones.Value;
        _logger = logger;
    }

    // Texto del correo con el código. Público para poder probarlo.
    public static (string Asunto, string Cuerpo) ArmarCodigo(string tipo, string codigo, DateTime vence)
    {
        var hora = (vence.Kind == DateTimeKind.Utc ? vence.ToLocalTime() : vence).ToString("HH:mm");
        return tipo == CuentaService.TipoRecuperacion
            ? ("Recuperá tu contraseña de VITALIA",
               $"Tu código para elegir una contraseña nueva es {codigo}.\nVale hasta las {hora}.\n\nSi no lo pediste, ignorá este correo: tu contraseña sigue igual.")
            : ("Verificá tu correo en VITALIA",
               $"Tu código de verificación es {codigo}.\nVale hasta las {hora}.\n\nSi no creaste una cuenta en la Clínica VITALIA, ignorá este correo.");
    }

    // Si el servidor falla, la cuenta no se pierde: el cliente pide otro código con «Reenviar código».
    public async Task EnviarCodigoAsync(string destino, string tipo, string codigo, DateTime vence)
    {
        var (asunto, cuerpo) = ArmarCodigo(tipo, codigo, vence);
        var error = await EnviarAsync(destino, asunto, cuerpo);
        if (error != null)
        {
            _logger.LogWarning("No se pudo enviar el código de {Tipo}: {Error}", tipo, error);
        }
    }

    public async Task<string?> EnviarAsync(string destino, string asunto, string cuerpo)
    {
        try
        {
            using var mensaje = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(_opciones.Remitente, _opciones.NombreRemitente),
                Subject = asunto,
                Body = cuerpo,
                IsBodyHtml = false,
                BodyEncoding = System.Text.Encoding.UTF8,
                SubjectEncoding = System.Text.Encoding.UTF8
            };
            mensaje.To.Add(destino);

            using var cliente = new System.Net.Mail.SmtpClient(_opciones.Servidor, _opciones.Puerto)
            {
                EnableSsl = _opciones.UsarSsl,
                DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                Credentials = string.IsNullOrEmpty(_opciones.Usuario) ? null : new System.Net.NetworkCredential(_opciones.Usuario, _opciones.Contrasena),
                Timeout = 15000
            };
            await cliente.SendMailAsync(mensaje);
            return null;
        }
        catch (System.Net.Mail.SmtpException ex)
        {
            return $"El servidor de correo respondió con error ({ex.StatusCode}).";
        }
        catch (Exception ex) when (ex is FormatException or InvalidOperationException or System.Net.Sockets.SocketException or IOException)
        {
            return "No se pudo conectar con el servidor de correo.";
        }
    }
}
