using System.Security.Cryptography;
using System.Text;
using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Cuentas de demostración en memoria (sección A). Se pierden al reiniciar la aplicación.
// En la sección C se reemplaza por CuentaApiService: la API valida, guarda y envía el correo.
public class CuentaService
{
    // Parámetros de D-REG-02.
    private const int MinutosVigenciaCodigo = 15;
    private const int IntentosPorCodigo = 5;
    private const int ReenviosPorHora = 3;

    private readonly DocumentoLegalService _documentosLegales;
    private readonly List<Cuenta> _cuentas = new();
    private int _ultimoId = 0;

    public CuentaService(DocumentoLegalService documentosLegales)
    {
        _documentosLegales = documentosLegales;
    }

    public List<string> Departamentos { get; } = new()
    {
        "Artigas", "Canelones", "Cerro Largo", "Colonia", "Durazno", "Flores", "Florida",
        "Lavalleja", "Maldonado", "Montevideo", "Paysandú", "Río Negro", "Rivera", "Rocha",
        "Salto", "San José", "Soriano", "Tacuarembó", "Treinta y Tres"
    };

    // Correo de la última cuenta registrada en esta sesión, para la pantalla de verificación.
    // Se guarda acá y no en la dirección web para no exponer el correo en la URL.
    public string CorreoPendiente { get; private set; } = "";

    public Cuenta? ObtenerPorCorreo(string correo)
    {
        return _cuentas.FirstOrDefault(c => c.Correo == NormalizarCorreo(correo));
    }

    // Crea la cuenta sin verificar y genera el primer código (CU-VIS-03).
    public ResultadoOperacion Registrar(RegistroCliente datos)
    {
        var correo = NormalizarCorreo(datos.Correo);
        var documento = ValidacionDatos.NormalizarDocumento(datos.TipoDocumento, datos.NumeroDocumento);
        var telefono = ValidacionDatos.NormalizarTelefono(datos.Telefono);

        // REG-02: no se crea un duplicado ni se dice de quién es el correo.
        if (_cuentas.Any(c => c.Correo == correo))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No pudimos crear la cuenta con ese correo. Si ya tenés una, ingresá o recuperá tu contraseña." };
        }

        if (_cuentas.Any(c => c.TipoDocumento == datos.TipoDocumento && c.NumeroDocumento == documento))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No pudimos crear la cuenta con ese documento. Si ya tenés una, consultá en Recepción." };
        }

        // REG-03: las aceptaciones obligatorias también se controlan acá, no solo en el formulario.
        if (!datos.AceptaTerminos || !datos.AceptaPrivacidad || telefono is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Revisá los datos y las aceptaciones obligatorias." };
        }

        _ultimoId++;
        var ahora = DateTime.Now;
        var cuenta = new Cuenta
        {
            Id = _ultimoId,
            Nombre = datos.Nombre.Trim(),
            Apellido = datos.Apellido.Trim(),
            Correo = correo,
            Telefono = telefono,
            TipoDocumento = datos.TipoDocumento,
            NumeroDocumento = documento,
            Calle = datos.Calle.Trim(),
            NumeroPuerta = datos.NumeroPuerta.Trim(),
            Apartamento = datos.Apartamento.Trim(),
            Localidad = datos.Localidad.Trim(),
            Departamento = datos.Departamento,
            Referencias = datos.Referencias.Trim(),
            HashContrasena = DerivarContrasena(datos.Contrasena),
            Verificada = false,
            FechaCreacion = ahora,
            VersionTerminosAceptada = _documentosLegales.ObtenerPorCodigo("terminos")?.Version ?? "",
            VersionPrivacidadAceptada = _documentosLegales.ObtenerPorCodigo("privacidad")?.Version ?? "",
            FechaAceptacion = ahora
        };

        GenerarCodigo(cuenta);
        _cuentas.Add(cuenta);
        CorreoPendiente = correo;

        return new ResultadoOperacion { Exito = true, Mensaje = "Cuenta creada. Te enviamos un código para verificar tu correo." };
    }

    public ResultadoOperacion Verificar(string correo, string codigo)
    {
        var cuenta = ObtenerPorCorreo(correo);
        if (cuenta is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No encontramos una cuenta pendiente de verificar con ese correo." };
        }

        if (cuenta.Verificada)
        {
            return new ResultadoOperacion { Exito = true, Mensaje = "Tu cuenta ya estaba verificada." };
        }

        if (cuenta.IntentosRestantes <= 0)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "El código se invalidó por demasiados intentos. Pedí uno nuevo." };
        }

        if (DateTime.Now > cuenta.CodigoVence)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "El código venció. Pedí uno nuevo." };
        }

        if (Hash((codigo ?? "").Trim()) != cuenta.HashCodigo)
        {
            cuenta.IntentosRestantes--;
            var mensaje = cuenta.IntentosRestantes > 0
                ? $"El código no es correcto. Te quedan {cuenta.IntentosRestantes} intentos."
                : "El código no es correcto y se invalidó. Pedí uno nuevo.";
            return new ResultadoOperacion { Exito = false, Mensaje = mensaje };
        }

        cuenta.Verificada = true;
        cuenta.HashCodigo = "";
        cuenta.CodigoDemo = "";
        CorreoPendiente = "";
        return new ResultadoOperacion { Exito = true, Mensaje = "¡Listo! Tu correo quedó verificado." };
    }

    public ResultadoOperacion ReenviarCodigo(string correo)
    {
        var cuenta = ObtenerPorCorreo(correo);
        if (cuenta is null || cuenta.Verificada)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No hay un código pendiente para ese correo." };
        }

        var haceUnaHora = DateTime.Now.AddHours(-1);
        cuenta.Reenvios.RemoveAll(fecha => fecha < haceUnaHora);
        if (cuenta.Reenvios.Count >= ReenviosPorHora)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ya pediste 3 códigos en la última hora. Esperá un rato y volvé a intentar." };
        }

        cuenta.Reenvios.Add(DateTime.Now);
        GenerarCodigo(cuenta);
        return new ResultadoOperacion { Exito = true, Mensaje = "Te enviamos un código nuevo. El anterior ya no sirve." };
    }

    // Correo para el que se pidió recuperar la contraseña en esta sesión.
    public string CorreoRecuperacion { get; private set; } = "";

    // D-REG-03: la respuesta es siempre la misma, exista o no la cuenta (REG-08).
    public ResultadoOperacion SolicitarRecuperacion(string correo)
    {
        CorreoRecuperacion = NormalizarCorreo(correo);
        var respuesta = new ResultadoOperacion { Exito = true, Mensaje = "Si el correo está registrado, te enviamos un código para crear una contraseña nueva." };

        var cuenta = ObtenerPorCorreo(correo);
        if (cuenta is null)
        {
            return respuesta;
        }

        var haceUnaHora = DateTime.Now.AddHours(-1);
        cuenta.PedidosRecuperacion.RemoveAll(fecha => fecha < haceUnaHora);
        if (cuenta.PedidosRecuperacion.Count >= ReenviosPorHora)
        {
            // Mismo mensaje: no se revela que la cuenta existe ni que llegó al límite.
            return respuesta;
        }

        cuenta.PedidosRecuperacion.Add(DateTime.Now);
        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        cuenta.HashCodigoRecuperacion = Hash(codigo);
        cuenta.CodigoRecuperacionVence = DateTime.Now.AddMinutes(MinutosVigenciaCodigo);
        cuenta.IntentosRecuperacion = IntentosPorCodigo;
        cuenta.CodigoRecuperacionDemo = codigo;
        return respuesta;
    }

    public ResultadoOperacion Restablecer(string correo, string codigo, string nuevaContrasena)
    {
        var error = new ResultadoOperacion { Exito = false, Mensaje = "El código no es válido o venció. Pedí uno nuevo." };
        var cuenta = ObtenerPorCorreo(correo);

        if (cuenta is null || string.IsNullOrEmpty(cuenta.HashCodigoRecuperacion))
        {
            return error;
        }

        if (cuenta.IntentosRecuperacion <= 0 || DateTime.Now > cuenta.CodigoRecuperacionVence)
        {
            return error;
        }

        if (Hash((codigo ?? "").Trim()) != cuenta.HashCodigoRecuperacion)
        {
            cuenta.IntentosRecuperacion--;
            return cuenta.IntentosRecuperacion > 0
                ? new ResultadoOperacion { Exito = false, Mensaje = $"El código no es correcto. Te quedan {cuenta.IntentosRecuperacion} intentos." }
                : new ResultadoOperacion { Exito = false, Mensaje = "El código no es correcto y se invalidó. Pedí uno nuevo." };
        }

        cuenta.HashContrasena = DerivarContrasena(nuevaContrasena);
        cuenta.HashCodigoRecuperacion = "";
        cuenta.CodigoRecuperacionDemo = "";
        CorreoRecuperacion = "";
        return new ResultadoOperacion { Exito = true, Mensaje = "Listo, tu contraseña cambió. Ya podés ingresar con la nueva." };
    }

    // Devuelve la cuenta si el correo y la contraseña coinciden; si no, null.
    // No distingue entre correo inexistente y contraseña incorrecta.
    public Cuenta? ValidarCredenciales(string correo, string contrasena)
    {
        var cuenta = ObtenerPorCorreo(correo);
        if (cuenta is null || !ContrasenaCoincide(contrasena, cuenta.HashContrasena))
        {
            return null;
        }

        return cuenta;
    }

    // Para retomar la verificación de una cuenta que todavía no la completó.
    public void MarcarPendiente(string correo)
    {
        CorreoPendiente = NormalizarCorreo(correo);
    }

    private void GenerarCodigo(Cuenta cuenta)
    {
        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        cuenta.HashCodigo = Hash(codigo);
        cuenta.CodigoVence = DateTime.Now.AddMinutes(MinutosVigenciaCodigo);
        cuenta.IntentosRestantes = IntentosPorCodigo;
        cuenta.CodigoDemo = codigo;
    }

    private static string NormalizarCorreo(string correo)
    {
        return (correo ?? "").Trim().ToLowerInvariant();
    }

    // Derivación lenta con salt individual (RNF-SEG-01).
    private static string DerivarContrasena(string contrasena)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var derivado = Rfc2898DeriveBytes.Pbkdf2(contrasena, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(derivado)}";
    }

    private static bool ContrasenaCoincide(string contrasena, string guardado)
    {
        var partes = guardado.Split('.');
        if (partes.Length != 2)
        {
            return false;
        }

        var salt = Convert.FromBase64String(partes[0]);
        var esperado = Convert.FromBase64String(partes[1]);
        var derivado = Rfc2898DeriveBytes.Pbkdf2(contrasena ?? "", salt, 100_000, HashAlgorithmName.SHA256, 32);
        return CryptographicOperations.FixedTimeEquals(derivado, esperado);
    }

    private static string Hash(string texto)
    {
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
    }
}
