using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Veterinaria.Api.Services;

// Avisos en tiempo real (RF-CLI-09, sección 15 de la especificación de asistencia). Solo avisan que algo cambió:
// llevan ids y estados, nunca coordenadas ni texto clínico (sección 14); la app vuelve a pedir el detalle a la API.
public interface INotificador
{
    Task ACuentaAsync(int cuentaId, string evento, object datos);
    Task ARolAsync(string rol, string evento, object datos);
}

// Sin conexión en tiempo real (pruebas, o si no hay nadie conectado): no hace nada.
public class NotificadorNulo : INotificador
{
    public static readonly NotificadorNulo Instancia = new();
    public Task ACuentaAsync(int cuentaId, string evento, object datos) => Task.CompletedTask;
    public Task ARolAsync(string rol, string evento, object datos) => Task.CompletedTask;
}

// Hub de SignalR (viene con ASP.NET Core). Cada conexión entra al grupo de su cuenta y al de cada rol que tiene.
// El token va en el encabezado o, desde el navegador y el móvil, en ?access_token= (ver Program.cs).
[Authorize]
public class AvisosHub : Hub
{
    public const string Ruta = "/hubs/avisos";

    public static string GrupoCuenta(int cuentaId) => $"cuenta-{cuentaId}";
    public static string GrupoRol(string rol) => $"rol-{rol}";

    public override async Task OnConnectedAsync()
    {
        var usuario = Context.User;
        if (int.TryParse(usuario?.FindFirst(TokenService.ClaimId)?.Value, out var cuentaId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoCuenta(cuentaId));
        }

        foreach (var rol in usuario?.FindAll(TokenService.ClaimRol).Select(c => c.Value) ?? Enumerable.Empty<string>())
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GrupoRol(rol));
        }

        await base.OnConnectedAsync();
    }
}

// Envía los avisos por el hub. Un error al avisar nunca corta la operación que ya se guardó.
public class NotificadorSignalR : INotificador
{
    private readonly IHubContext<AvisosHub> _hub;
    private readonly ILogger<NotificadorSignalR> _logger;

    public NotificadorSignalR(IHubContext<AvisosHub> hub, ILogger<NotificadorSignalR> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public Task ACuentaAsync(int cuentaId, string evento, object datos) => EnviarAsync(AvisosHub.GrupoCuenta(cuentaId), evento, datos);

    public Task ARolAsync(string rol, string evento, object datos) => EnviarAsync(AvisosHub.GrupoRol(rol), evento, datos);

    private async Task EnviarAsync(string grupo, string evento, object datos)
    {
        try
        {
            await _hub.Clients.Group(grupo).SendAsync(evento, datos);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("No se pudo enviar el aviso «{Evento}» ({Tipo}).", evento, ex.GetType().Name);
        }
    }
}
