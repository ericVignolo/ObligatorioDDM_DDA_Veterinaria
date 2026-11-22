using System.Security.Claims;
using System.Text;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — verificación en dos pasos (RNF-SEG-04): códigos TOTP (RFC 6238) y filtro del Administrador.
public class ApiSegundoFactorTests
{
    // Secreto de los vectores de prueba de la RFC 6238 (SHA-1): "12345678901234567890".
    private static readonly string SecretoRfc = Totp.Base32(Encoding.ASCII.GetBytes("12345678901234567890"));

    [Fact]
    public void Codigos_de_la_RFC_6238_con_6_cifras()
    {
        Assert.Equal("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", SecretoRfc);
        Assert.Equal("287082", Totp.Codigo(SecretoRfc, Totp.Paso(DateTimeOffset.FromUnixTimeSeconds(59).UtcDateTime)));
        Assert.Equal("081804", Totp.Codigo(SecretoRfc, Totp.Paso(DateTimeOffset.FromUnixTimeSeconds(1111111109).UtcDateTime)));
        Assert.Equal("005924", Totp.Codigo(SecretoRfc, Totp.Paso(DateTimeOffset.FromUnixTimeSeconds(1234567890).UtcDateTime)));
    }

    [Fact]
    public void Base32_ida_y_vuelta_y_secretos_nuevos_distintos()
    {
        var bytes = new byte[] { 1, 2, 3, 250, 99, 0, 7, 128, 64, 33 };
        Assert.Equal(bytes, Totp.DesdeBase32(Totp.Base32(bytes)));
        var a = Totp.NuevoSecreto();
        Assert.Equal(32, a.Length); // 20 bytes
        Assert.NotEqual(a, Totp.NuevoSecreto());
        Assert.StartsWith("otpauth://totp/VITALIA:ana%40correo.uy?secret=", Totp.Enlace("ana@correo.uy", a));
    }

    [Fact]
    public void Verificar_acepta_un_intervalo_de_desfase_y_no_repite_codigos()
    {
        var ahora = new DateTime(2026, 11, 22, 18, 0, 0, DateTimeKind.Utc);
        var paso = Totp.Paso(ahora);

        Assert.Equal(paso, Totp.Verificar(SecretoRfc, Totp.Codigo(SecretoRfc, paso), ahora, 0));
        Assert.Equal(paso - 1, Totp.Verificar(SecretoRfc, Totp.Codigo(SecretoRfc, paso - 1), ahora, 0));
        Assert.Equal(paso + 1, Totp.Verificar(SecretoRfc, Totp.Codigo(SecretoRfc, paso + 1), ahora, 0));
        Assert.Null(Totp.Verificar(SecretoRfc, Totp.Codigo(SecretoRfc, paso - 2), ahora, 0));
        Assert.Null(Totp.Verificar(SecretoRfc, Totp.Codigo(SecretoRfc, paso), ahora, paso)); // ya usado
        Assert.Null(Totp.Verificar(SecretoRfc, "12345", ahora, 0));
        Assert.Equal(paso, Totp.Verificar(SecretoRfc, Totp.Codigo(SecretoRfc, paso).Insert(3, " "), ahora, 0)); // con espacio
    }

    private static ClaimsPrincipal Usuario(string rol, bool conCodigo)
    {
        var claims = new List<Claim> { new(TokenService.ClaimId, "5"), new(TokenService.ClaimRol, rol) };
        if (conCodigo)
        {
            claims.Add(new Claim(ExigirSegundoFactorFiltro.ClaimMetodo, ExigirSegundoFactorFiltro.ConSegundoFactor));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "prueba"));
    }

    [Fact]
    public void Filtro_frena_al_Administrador_sin_segundo_factor_salvo_en_su_cuenta()
    {
        Assert.True(ExigirSegundoFactorFiltro.Bloquea(Usuario("Administrador", false), controladorPermitido: false, exigir: true));
        Assert.False(ExigirSegundoFactorFiltro.Bloquea(Usuario("Administrador", true), false, true));
        Assert.False(ExigirSegundoFactorFiltro.Bloquea(Usuario("Administrador", false), controladorPermitido: true, exigir: true));
        Assert.False(ExigirSegundoFactorFiltro.Bloquea(Usuario("Administrador", false), false, exigir: false));
        Assert.False(ExigirSegundoFactorFiltro.Bloquea(Usuario("Recepcionista", false), false, true));
    }
}
