using System.Security.Claims;
using Veterinaria.Api.Services;

namespace Veterinaria.Api.Controllers;

// Datos de la cuenta que hizo el pedido, sacados del token (nunca del cuerpo ni de la URL, RN-18).
public static class UsuarioActual
{
    public static int CuentaId(this ClaimsPrincipal usuario)
        => int.TryParse(usuario.FindFirst(TokenService.ClaimId)?.Value, out var id) ? id : 0;

    // El rol con el que actúa entre los pedidos: el primero que coincida, para la auditoría.
    public static string RolEntre(this ClaimsPrincipal usuario, params string[] roles)
        => roles.FirstOrDefault(usuario.IsInRole) ?? "";
}
