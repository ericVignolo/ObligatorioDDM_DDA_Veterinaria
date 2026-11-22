using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// Guarda los avisos en tiempo real que mandaría el hub, para poder revisarlos en las pruebas.
public class NotificadorDePrueba : INotificador
{
    public List<(string Destino, string Evento, object Datos)> Enviados { get; } = new();

    public Task ACuentaAsync(int cuentaId, string evento, object datos)
    {
        Enviados.Add(($"cuenta-{cuentaId}", evento, datos));
        return Task.CompletedTask;
    }

    public Task ARolAsync(string rol, string evento, object datos)
    {
        Enviados.Add(($"rol-{rol}", evento, datos));
        return Task.CompletedTask;
    }

    // Nombres de las propiedades del aviso (es un objeto anónimo).
    public static List<string> Campos(object datos) => datos.GetType().GetProperties().Select(p => p.Name).ToList();
}
