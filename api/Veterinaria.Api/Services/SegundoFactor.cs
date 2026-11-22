using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using Veterinaria.Api.Controllers;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Códigos de 6 cifras de las apps autenticadoras (TOTP, RFC 6238: HMAC-SHA1, intervalos de 30 s).
// Hecho con lo que trae .NET, sin paquetes. Acepta el intervalo actual y uno antes o después (desfase del reloj).
public static class Totp
{
    public const int Digitos = 6;
    public const int Segundos = 30;
    private const string Alfabeto = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static long Paso(DateTime utc) => new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)).ToUnixTimeSeconds() / Segundos;

    public static string NuevoSecreto() => Base32(RandomNumberGenerator.GetBytes(20));

    public static string Codigo(string secretoBase32, long paso)
    {
        var contador = BitConverter.GetBytes(paso);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(contador);
        }

        var hash = HMACSHA1.HashData(DesdeBase32(secretoBase32), contador);
        var desplazamiento = hash[^1] & 0x0F;
        var valor = ((hash[desplazamiento] & 0x7F) << 24) | (hash[desplazamiento + 1] << 16) | (hash[desplazamiento + 2] << 8) | hash[desplazamiento + 3];
        return (valor % 1_000_000).ToString("D6");
    }

    // Devuelve el intervalo que coincide o null. Un intervalo ya usado (o anterior) no vale: evita repetir el código.
    public static long? Verificar(string secretoBase32, string? codigo, DateTime utc, long ultimoPasoUsado)
    {
        var limpio = new string((codigo ?? "").Where(char.IsDigit).ToArray());
        if (limpio.Length != Digitos)
        {
            return null;
        }

        var actual = Paso(utc);
        for (var paso = actual - 1; paso <= actual + 1; paso++)
        {
            if (paso > ultimoPasoUsado && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Codigo(secretoBase32, paso)), Encoding.ASCII.GetBytes(limpio)))
            {
                return paso;
            }
        }

        return null;
    }

    // Enlace que leen las apps (Google Authenticator, Microsoft Authenticator…) desde un código QR.
    public static string Enlace(string correo, string secretoBase32)
        => $"otpauth://totp/VITALIA:{Uri.EscapeDataString(correo)}?secret={secretoBase32}&issuer=VITALIA&digits={Digitos}&period={Segundos}";

    public static string Base32(byte[] datos)
    {
        var resultado = new StringBuilder();
        int buffer = 0, bits = 0;
        foreach (var b in datos)
        {
            buffer = (buffer << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                resultado.Append(Alfabeto[(buffer >> (bits - 5)) & 31]);
                bits -= 5;
            }
        }

        if (bits > 0)
        {
            resultado.Append(Alfabeto[(buffer << (5 - bits)) & 31]);
        }

        return resultado.ToString();
    }

    public static byte[] DesdeBase32(string texto)
    {
        var salida = new List<byte>();
        int buffer = 0, bits = 0;
        foreach (var c in texto.ToUpperInvariant().Where(c => c != '=' && c != ' '))
        {
            var indice = Alfabeto.IndexOf(c);
            if (indice < 0)
            {
                throw new FormatException("Secreto no válido.");
            }

            buffer = (buffer << 5) | indice;
            bits += 5;
            if (bits >= 8)
            {
                salida.Add((byte)((buffer >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }

        return salida.ToArray();
    }
}

// Cifra el secreto antes de guardarlo (protección de datos de ASP.NET Core; viene con el framework).
public class ProteccionSecretos
{
    private readonly IDataProtector _protector;

    public ProteccionSecretos(IDataProtectionProvider proveedor)
    {
        _protector = proveedor.CreateProtector("Vitalia.SegundoFactor.v1");
    }

    public string Cifrar(string secreto) => _protector.Protect(secreto);

    public string? Descifrar(string cifrado)
    {
        if (string.IsNullOrEmpty(cifrado))
        {
            return null;
        }

        try
        {
            return _protector.Unprotect(cifrado);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}

// Sección "Seguridad" de appsettings.
public class OpcionesSeguridad
{
    // RNF-SEG-04: el Administrador no usa sus funciones sin la verificación en dos pasos activa.
    public bool ExigirSegundoFactorAdministrador { get; set; } = true;
}

public interface ISegundoFactorService
{
    Task<Resultado<ConfiguracionSegundoFactorDto>> ConfigurarAsync(int cuentaId);
    Task<Resultado<MensajeDto>> ActivarAsync(int cuentaId, string codigo);
    Task<Resultado<MensajeDto>> DesactivarAsync(int cuentaId, DesactivacionSegundoFactorDto datos);
    Task<Resultado<EstadoSegundoFactorDto>> EstadoAsync(int cuentaId);
}

// Alta, activación y baja de la verificación en dos pasos desde la propia cuenta.
public class SegundoFactorService : ISegundoFactorService
{
    private readonly ICuentaRepository _repository;
    private readonly ProteccionSecretos _proteccion;
    private readonly TimeProvider _reloj;

    public SegundoFactorService(ICuentaRepository repository, ProteccionSecretos proteccion, TimeProvider reloj)
    {
        _repository = repository;
        _proteccion = proteccion;
        _reloj = reloj;
    }

    private DateTime Ahora => _reloj.GetUtcNow().UtcDateTime;

    public async Task<Resultado<EstadoSegundoFactorDto>> EstadoAsync(int cuentaId)
    {
        var cuenta = await _repository.ObtenerPorIdAsync(cuentaId);
        if (cuenta == null || cuenta.Eliminada)
        {
            return Resultado<EstadoSegundoFactorDto>.Error(404, "CUENTA_NO_ENCONTRADA", "No encontrado", "No encontramos tu cuenta.");
        }

        var obligatorio = CuentaService.NombresRoles(cuenta).Contains("Administrador");
        return Resultado<EstadoSegundoFactorDto>.Ok(new EstadoSegundoFactorDto { Activo = cuenta.SegundoFactorActivo, Obligatorio = obligatorio });
    }

    // Genera un secreto nuevo (todavía sin activar). Se muestra una sola vez para cargarlo en la app.
    public async Task<Resultado<ConfiguracionSegundoFactorDto>> ConfigurarAsync(int cuentaId)
    {
        var cuenta = await _repository.ObtenerPorIdAsync(cuentaId);
        if (cuenta == null || cuenta.Eliminada)
        {
            return Resultado<ConfiguracionSegundoFactorDto>.Error(404, "CUENTA_NO_ENCONTRADA", "No encontrado", "No encontramos tu cuenta.");
        }

        if (cuenta.SegundoFactorActivo)
        {
            return Resultado<ConfiguracionSegundoFactorDto>.Error(409, "YA_ACTIVO", "No se puede", "La verificación en dos pasos ya está activa. Para cambiar de teléfono, desactivala primero.");
        }

        var secreto = Totp.NuevoSecreto();
        cuenta.SecretoSegundoFactor = _proteccion.Cifrar(secreto);
        cuenta.UltimoPasoSegundoFactor = 0;
        await _repository.GuardarAsync();

        return Resultado<ConfiguracionSegundoFactorDto>.Ok(new ConfiguracionSegundoFactorDto
        {
            Secreto = secreto,
            Enlace = Totp.Enlace(cuenta.Correo, secreto),
            Mensaje = "Escaneá el código QR (o cargá la clave) en tu app autenticadora y escribí el código de 6 cifras para activarla."
        });
    }

    // Activa con el primer código correcto y cierra las demás sesiones: desde ahora se entra con el código.
    public async Task<Resultado<MensajeDto>> ActivarAsync(int cuentaId, string codigo)
    {
        var cuenta = await _repository.ObtenerPorIdAsync(cuentaId);
        var secreto = cuenta == null ? null : _proteccion.Descifrar(cuenta.SecretoSegundoFactor);
        if (cuenta == null || secreto == null)
        {
            return Resultado<MensajeDto>.Error(409, "SIN_CONFIGURAR", "No se puede", "Primero pedí la configuración para cargar la clave en tu app.");
        }

        if (cuenta.SegundoFactorActivo)
        {
            return Resultado<MensajeDto>.Error(409, "YA_ACTIVO", "No se puede", "La verificación en dos pasos ya está activa.");
        }

        var ahora = Ahora;
        var paso = Totp.Verificar(secreto, codigo, ahora, cuenta.UltimoPasoSegundoFactor);
        if (paso == null)
        {
            return Resultado<MensajeDto>.Error(400, "CODIGO_INVALIDO", "Código incorrecto", "El código no coincide. Revisá que la hora del teléfono esté bien y probá con el código nuevo.");
        }

        cuenta.SegundoFactorActivo = true;
        cuenta.UltimoPasoSegundoFactor = paso.Value;
        foreach (var sesion in await _repository.ObtenerSesionesActivasAsync(cuenta.Id, ahora))
        {
            sesion.RevocadaEn = ahora;
        }

        await _repository.GuardarAsync();
        return Resultado<MensajeDto>.Ok(new MensajeDto { Mensaje = "Verificación en dos pasos activada. Volvé a ingresar: desde ahora te pedimos el código de la app." });
    }

    // Pide la contraseña y un código vigente. El Administrador no la puede desactivar (RNF-SEG-04).
    public async Task<Resultado<MensajeDto>> DesactivarAsync(int cuentaId, DesactivacionSegundoFactorDto datos)
    {
        var cuenta = await _repository.ObtenerPorIdAsync(cuentaId);
        if (cuenta == null || !cuenta.SegundoFactorActivo)
        {
            return Resultado<MensajeDto>.Error(409, "NO_ACTIVO", "No se puede", "La verificación en dos pasos no está activa.");
        }

        if (CuentaService.NombresRoles(cuenta).Contains("Administrador"))
        {
            return Resultado<MensajeDto>.Error(409, "OBLIGATORIO", "No se puede", "Para el Administrador la verificación en dos pasos es obligatoria.");
        }

        var secreto = _proteccion.Descifrar(cuenta.SecretoSegundoFactor);
        var paso = secreto == null ? null : Totp.Verificar(secreto, datos.Codigo, Ahora, cuenta.UltimoPasoSegundoFactor);
        if (!Seguridad.ContrasenaCoincide(datos.Contrasena, cuenta.HashContrasena) || paso == null)
        {
            return Resultado<MensajeDto>.Error(400, "DATOS_INCORRECTOS", "Datos incorrectos", "La contraseña o el código no coinciden.");
        }

        cuenta.SegundoFactorActivo = false;
        cuenta.SecretoSegundoFactor = "";
        cuenta.UltimoPasoSegundoFactor = 0;
        await _repository.GuardarAsync();
        return Resultado<MensajeDto>.Ok(new MensajeDto { Mensaje = "Verificación en dos pasos desactivada." });
    }
}

// Marca los controladores que el Administrador puede usar sin la verificación en dos pasos (para poder activarla).
[AttributeUsage(AttributeTargets.Class)]
public class PermitirSinSegundoFactorAttribute : Attribute
{
}

// RNF-SEG-04: un Administrador con un token emitido sin el segundo factor solo puede ver su cuenta y activarlo.
public class ExigirSegundoFactorFiltro : IAsyncActionFilter
{
    public const string ClaimMetodo = "amr";
    public const string ConSegundoFactor = "mfa";

    private readonly OpcionesSeguridad _opciones;

    public ExigirSegundoFactorFiltro(IOptions<OpcionesSeguridad> opciones)
    {
        _opciones = opciones.Value;
    }

    public static bool Bloquea(System.Security.Claims.ClaimsPrincipal usuario, bool controladorPermitido, bool exigir)
        => exigir && !controladorPermitido
           && usuario.FindAll(TokenService.ClaimRol).Any(c => c.Value == "Administrador")
           && !usuario.FindAll(ClaimMetodo).Any(c => c.Value == ConSegundoFactor);

    public async Task OnActionExecutionAsync(ActionExecutingContext contexto, ActionExecutionDelegate siguiente)
    {
        var permitido = contexto.Controller.GetType().IsDefined(typeof(PermitirSinSegundoFactorAttribute), inherit: false);
        if (Bloquea(contexto.HttpContext.User, permitido, _opciones.ExigirSegundoFactorAdministrador))
        {
            contexto.Result = new ObjectResult(Problemas.Crear(403, "Falta la verificación en dos pasos",
                "Para usar las funciones de Administrador activá la verificación en dos pasos y volvé a ingresar.", "SEGUNDO_FACTOR_REQUERIDO"))
            { StatusCode = 403 };
            return;
        }

        await siguiente();
    }
}
