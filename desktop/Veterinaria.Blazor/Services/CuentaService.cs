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
        CargarClientesDemo();
    }

    // Clientes de demostración con turnos y animales ya cargados (sin contraseña: no pueden ingresar).
    private void CargarClientesDemo()
    {
        _cuentas.Add(new Cuenta { Id = 100, Nombre = "Ana", Apellido = "Rodríguez", Correo = "ana.demo@vitalia.uy", Telefono = "+59899111222", Verificada = true, FechaCreacion = new DateTime(2026, 10, 1) });
        _cuentas.Add(new Cuenta { Id = 101, Nombre = "Pedro", Apellido = "Gómez", Correo = "pedro.demo@vitalia.uy", Telefono = "+59899333444", Verificada = true, FechaCreacion = new DateTime(2026, 10, 1) });
    }

    public Cuenta? ObtenerPorId(int id)
    {
        return _cuentas.FirstOrDefault(c => c.Id == id);
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

    // A-009: carga el formulario de «Mi cuenta» con los datos actuales.
    public PerfilFormulario ParaEditarPerfil(Cuenta cuenta)
    {
        return new PerfilFormulario
        {
            Nombre = cuenta.Nombre,
            Apellido = cuenta.Apellido,
            Telefono = cuenta.Telefono,
            Calle = cuenta.Calle,
            NumeroPuerta = cuenta.NumeroPuerta,
            Apartamento = cuenta.Apartamento,
            Localidad = cuenta.Localidad,
            Departamento = cuenta.Departamento,
            Referencias = cuenta.Referencias
        };
    }

    // A-009: guarda los datos editables. El servicio vuelve a validar aunque el formulario ya lo haya hecho.
    // El correo se cambia aparte, con verificación (D-PER-04); el documento, en Recepción (RN-42).
    public ResultadoOperacion ActualizarPerfil(int cuentaId, PerfilFormulario datos)
    {
        var cuenta = ObtenerPorId(cuentaId);
        if (cuenta is null || !cuenta.Verificada)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No encontramos tu cuenta. Volvé a ingresar." };
        }

        var telefono = ValidacionDatos.NormalizarTelefono(datos.Telefono);
        if (telefono is null
            || string.IsNullOrWhiteSpace(datos.Nombre) || string.IsNullOrWhiteSpace(datos.Apellido)
            || string.IsNullOrWhiteSpace(datos.Calle) || string.IsNullOrWhiteSpace(datos.NumeroPuerta)
            || string.IsNullOrWhiteSpace(datos.Localidad) || !Departamentos.Contains(datos.Departamento)
            || datos.Nombre.Trim().Length > 60 || datos.Apellido.Trim().Length > 60 || datos.Calle.Trim().Length > 100
            || datos.NumeroPuerta.Trim().Length > 10 || (datos.Apartamento ?? "").Trim().Length > 10
            || datos.Localidad.Trim().Length > 60 || (datos.Referencias ?? "").Trim().Length > 200)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Revisá los datos marcados antes de guardar." };
        }

        cuenta.Nombre = datos.Nombre.Trim();
        cuenta.Apellido = datos.Apellido.Trim();
        cuenta.Telefono = telefono;
        cuenta.Calle = datos.Calle.Trim();
        cuenta.NumeroPuerta = datos.NumeroPuerta.Trim();
        cuenta.Apartamento = (datos.Apartamento ?? "").Trim();
        cuenta.Localidad = datos.Localidad.Trim();
        cuenta.Departamento = datos.Departamento;
        cuenta.Referencias = (datos.Referencias ?? "").Trim();

        return new ResultadoOperacion { Exito = true, Mensaje = "Guardamos los cambios de tu cuenta." };
    }

    // D-PER-04: pide el cambio de correo. Exige la contraseña actual y manda un código al correo nuevo;
    // hasta verificarlo, la cuenta sigue usando el correo anterior.
    public ResultadoOperacion SolicitarCambioCorreo(int cuentaId, CambioCorreoFormulario datos)
    {
        var cuenta = ObtenerPorId(cuentaId);
        if (cuenta is null || !cuenta.Verificada)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No encontramos tu cuenta. Volvé a ingresar." };
        }

        if (!ContrasenaCoincide(datos.ContrasenaActual ?? "", cuenta.HashContrasena))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "La contraseña actual no es correcta." };
        }

        var nuevo = NormalizarCorreo(datos.CorreoNuevo);
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(nuevo) || nuevo.Length > 254)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "El correo no tiene un formato válido." };
        }

        if (nuevo == cuenta.Correo)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ese ya es el correo de tu cuenta." };
        }

        // Igual que en el registro (REG-02): no se dice de quién es el correo.
        if (_cuentas.Any(c => c.Id != cuenta.Id && (c.Correo == nuevo || c.CorreoNuevoPendiente == nuevo)))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No pudimos usar ese correo. Probá con otro." };
        }

        var haceUnaHora = DateTime.Now.AddHours(-1);
        cuenta.PedidosCambioCorreo.RemoveAll(fecha => fecha < haceUnaHora);
        if (cuenta.PedidosCambioCorreo.Count >= ReenviosPorHora)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ya pediste 3 códigos en la última hora. Esperá un rato y volvé a intentar." };
        }

        cuenta.PedidosCambioCorreo.Add(DateTime.Now);
        var codigo = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        cuenta.CorreoNuevoPendiente = nuevo;
        cuenta.HashCodigoCorreo = Hash(codigo);
        cuenta.CodigoCorreoVence = DateTime.Now.AddMinutes(MinutosVigenciaCodigo);
        cuenta.IntentosCorreo = IntentosPorCodigo;
        cuenta.CodigoCorreoDemo = codigo;
        return new ResultadoOperacion { Exito = true, Mensaje = $"Te enviamos un código a {nuevo}. Hasta que lo ingreses, tu cuenta sigue con el correo anterior." };
    }

    // D-PER-04: con el código correcto, el correo nuevo reemplaza al anterior. Después se vuelve a ingresar.
    public ResultadoOperacion ConfirmarCambioCorreo(int cuentaId, string codigo)
    {
        var error = new ResultadoOperacion { Exito = false, Mensaje = "El código no es válido o venció. Pedí uno nuevo." };
        var cuenta = ObtenerPorId(cuentaId);
        if (cuenta is null || string.IsNullOrEmpty(cuenta.CorreoNuevoPendiente) || string.IsNullOrEmpty(cuenta.HashCodigoCorreo))
        {
            return error;
        }

        if (cuenta.IntentosCorreo <= 0 || DateTime.Now > cuenta.CodigoCorreoVence)
        {
            return error;
        }

        if (Hash((codigo ?? "").Trim()) != cuenta.HashCodigoCorreo)
        {
            cuenta.IntentosCorreo--;
            return cuenta.IntentosCorreo > 0
                ? new ResultadoOperacion { Exito = false, Mensaje = $"El código no es correcto. Te quedan {cuenta.IntentosCorreo} intentos." }
                : new ResultadoOperacion { Exito = false, Mensaje = "El código no es correcto y se invalidó. Pedí uno nuevo." };
        }

        // Se vuelve a controlar: otra cuenta pudo registrarse con ese correo mientras tanto.
        if (_cuentas.Any(c => c.Id != cuenta.Id && c.Correo == cuenta.CorreoNuevoPendiente))
        {
            CancelarCambioCorreo(cuenta.Id);
            return new ResultadoOperacion { Exito = false, Mensaje = "No pudimos usar ese correo. Probá con otro." };
        }

        cuenta.Correo = cuenta.CorreoNuevoPendiente;
        CancelarCambioCorreo(cuenta.Id);
        // Un código de recuperación pedido con el correo anterior deja de servir.
        cuenta.HashCodigoRecuperacion = "";
        cuenta.CodigoRecuperacionDemo = "";
        return new ResultadoOperacion { Exito = true, Mensaje = "Tu correo cambió. Por seguridad cerramos la sesión: ingresá con el correo nuevo." };
    }

    public void CancelarCambioCorreo(int cuentaId)
    {
        var cuenta = ObtenerPorId(cuentaId);
        if (cuenta is null)
        {
            return;
        }

        cuenta.CorreoNuevoPendiente = "";
        cuenta.HashCodigoCorreo = "";
        cuenta.CodigoCorreoDemo = "";
        cuenta.IntentosCorreo = 0;
    }

    // D-PER-05: cambia la contraseña con la sesión iniciada. Exige la actual; después se vuelve a ingresar.
    public ResultadoOperacion CambiarContrasena(int cuentaId, CambioContrasenaFormulario datos)
    {
        var cuenta = ObtenerPorId(cuentaId);
        if (cuenta is null || !cuenta.Verificada)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No encontramos tu cuenta. Volvé a ingresar." };
        }

        if (!ContrasenaCoincide(datos.ContrasenaActual ?? "", cuenta.HashContrasena))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "La contraseña actual no es correcta." };
        }

        var nueva = datos.Contrasena ?? "";
        if (nueva.Length < 8 || nueva.Length > 64 || nueva != datos.RepetirContrasena)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Revisá la contraseña nueva." };
        }

        if (nueva == datos.ContrasenaActual)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "La contraseña nueva tiene que ser distinta de la actual." };
        }

        cuenta.HashContrasena = DerivarContrasena(nueva);
        cuenta.HashCodigoRecuperacion = "";
        cuenta.CodigoRecuperacionDemo = "";
        return new ResultadoOperacion { Exito = true, Mensaje = "Tu contraseña cambió. Por seguridad cerramos la sesión: ingresá con la contraseña nueva." };
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
    public static string DerivarContrasena(string contrasena)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var derivado = Rfc2898DeriveBytes.Pbkdf2(contrasena, salt, 100_000, HashAlgorithmName.SHA256, 32);
        return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(derivado)}";
    }

    public static bool ContrasenaCoincide(string contrasena, string guardado)
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
