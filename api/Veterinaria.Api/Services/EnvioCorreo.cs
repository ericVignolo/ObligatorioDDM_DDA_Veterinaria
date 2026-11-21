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
