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
