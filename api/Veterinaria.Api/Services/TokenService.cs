using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

public class TokenService : ITokenService
{
    // Nombres de los datos dentro del token (Program.cs valida con los mismos).
    public const string ClaimId = "sub";
    public const string ClaimNombre = "name";
    public const string ClaimCorreo = "email";
    public const string ClaimRol = "role";

    private readonly OpcionesJwt _opciones;

    public TokenService(IOptions<OpcionesJwt> opciones)
    {
        _opciones = opciones.Value;
    }

    public static SymmetricSecurityKey Clave(OpcionesJwt opciones)
    {
        if (string.IsNullOrWhiteSpace(opciones.Clave) || opciones.Clave.Length < 32)
        {
            throw new InvalidOperationException("Falta la clave Jwt:Clave (al menos 32 caracteres) en appsettings.");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opciones.Clave));
    }

    public (string Token, DateTime Vence) CrearTokenAcceso(Cuenta cuenta, List<string> roles, DateTime ahora)
    {
        var vence = ahora.AddMinutes(_opciones.MinutosAcceso);

        var datos = new List<Claim>
        {
            new(ClaimId, cuenta.Id.ToString()),
            new(ClaimNombre, $"{cuenta.Nombre} {cuenta.Apellido}".Trim()),
            new(ClaimCorreo, cuenta.Correo)
        };
        datos.AddRange(roles.Select(r => new Claim(ClaimRol, r)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _opciones.Emisor,
            Audience = _opciones.Audiencia,
            Subject = new ClaimsIdentity(datos),
            IssuedAt = ahora,
            NotBefore = ahora,
            Expires = vence,
            SigningCredentials = new SigningCredentials(Clave(_opciones), SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), vence);
    }
}
