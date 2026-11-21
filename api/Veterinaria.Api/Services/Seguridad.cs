using System.Security.Cryptography;
using System.Text;

namespace Veterinaria.Api.Services;

// Hash de contraseñas, códigos y tokens (RNF-SEG-01, D-REG-02). Nunca se guarda el texto.
public static class Seguridad
{
    private const int Iteraciones = 100_000;

    // PBKDF2-SHA256 con sal aleatoria: el mismo formato "sal.derivado" (Base64) que Blazor (CuentaService).
    public static string DerivarContrasena(string contrasena)
    {
        var sal = RandomNumberGenerator.GetBytes(16);
        var derivado = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(sal)}.{Convert.ToBase64String(derivado)}";
    }

    public static bool ContrasenaCoincide(string contrasena, string guardado)
    {
        var partes = (guardado ?? "").Split('.');
        if (partes.Length != 2)
        {
            return false;
        }

        try
        {
            var sal = Convert.FromBase64String(partes[0]);
            var esperado = Convert.FromBase64String(partes[1]);
            var derivado = Rfc2898DeriveBytes.Pbkdf2(contrasena ?? "", sal, Iteraciones, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(derivado, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // Hace el mismo trabajo que una comprobación real, para que un correo inexistente
    // tarde lo mismo que uno registrado (REG-08).
    public static void GastarTiempoComoComprobacion(string contrasena)
    {
        Rfc2898DeriveBytes.Pbkdf2(contrasena ?? "", new byte[16], Iteraciones, HashAlgorithmName.SHA256, 32);
    }

    // Código de 6 dígitos (D-REG-02).
    public static string GenerarCodigo()
    {
        return RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
    }

    // Hash del código con sal propia, formato "sal.hash" (D-REG-02: el código no se guarda en claro).
    public static string HashCodigo(string codigo)
    {
        var sal = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        return $"{sal}.{Sha256(sal + (codigo ?? ""))}";
    }

    public static bool CodigoCoincide(string codigo, string guardado)
    {
        var partes = (guardado ?? "").Split('.');
        return partes.Length == 2 && HashesIguales(Sha256(partes[0] + (codigo ?? "")), partes[1]);
    }

    // Token de refresco: 32 bytes aleatorios en Base64 apto para URL.
    public static string GenerarTokenRefresco()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static string HashToken(string token)
    {
        return Sha256(token ?? "");
    }

    public static bool HashesIguales(string a, string b)
    {
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(a ?? ""), Encoding.ASCII.GetBytes(b ?? ""));
    }

    private static string Sha256(string texto)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
    }
}
