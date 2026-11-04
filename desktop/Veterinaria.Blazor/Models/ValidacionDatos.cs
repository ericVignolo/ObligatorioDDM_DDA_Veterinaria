using System.Text.RegularExpressions;

namespace Veterinaria.Blazor.Models;

// Formatos de D-REG-01: documento por tipo y teléfono en E.164 (+598 por defecto).
public static class ValidacionDatos
{
    // Cédula uruguaya: 7 u 8 dígitos; el último es el dígito verificador.
    public static bool CedulaValida(string cedula)
    {
        var digitos = Regex.Replace(cedula ?? "", @"[\.\-\s]", "");
        if (!Regex.IsMatch(digitos, @"^\d{7,8}$"))
        {
            return false;
        }

        var cuerpo = digitos[..^1].PadLeft(7, '0');
        var verificador = digitos[^1] - '0';
        int[] pesos = { 2, 9, 8, 7, 6, 3, 4 };

        var suma = 0;
        for (var i = 0; i < 7; i++)
        {
            suma += (cuerpo[i] - '0') * pesos[i];
        }

        return (10 - suma % 10) % 10 == verificador;
    }

    public static bool DocumentoValido(string tipo, string numero)
    {
        if (tipo == "CI")
        {
            return CedulaValida(numero);
        }

        // Pasaporte u otro documento extranjero: alfanumérico de 5 a 20 caracteres.
        return Regex.IsMatch(numero ?? "", @"^[A-Za-z0-9]{5,20}$");
    }

    // Devuelve la cédula solo con dígitos, o el documento en mayúsculas.
    public static string NormalizarDocumento(string tipo, string numero)
    {
        return tipo == "CI"
            ? Regex.Replace(numero ?? "", @"\D", "")
            : (numero ?? "").Trim().ToUpperInvariant();
    }

    // Devuelve el teléfono en E.164 (por ejemplo +59899123456) o null si no es válido.
    public static string? NormalizarTelefono(string telefono)
    {
        var limpio = Regex.Replace(telefono ?? "", @"[\s\-\(\)\.]", "");

        if (limpio.StartsWith("00"))
        {
            limpio = "+" + limpio[2..];
        }
        else if (!limpio.StartsWith("+"))
        {
            // Sin prefijo internacional se asume Uruguay; se quita el 0 inicial (099 → +59899).
            limpio = "+598" + limpio.TrimStart('0');
        }

        return Regex.IsMatch(limpio, @"^\+[1-9]\d{7,14}$") ? limpio : null;
    }
}
