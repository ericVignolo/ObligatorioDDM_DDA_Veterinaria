using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

public interface ITokenService
{
    // Token de acceso corto (RNF-SEG-03) con la cuenta, el nombre y los roles.
    (string Token, DateTime Vence) CrearTokenAcceso(Cuenta cuenta, List<string> roles, DateTime ahora);
}
