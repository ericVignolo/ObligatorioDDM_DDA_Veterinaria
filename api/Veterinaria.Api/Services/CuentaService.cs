using Microsoft.Extensions.Options;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Registro, verificación por código y recuperación de la contraseña (A-006, A-007, D-REG-01 a D-REG-04).
// Mensajes iguales a los de CuentaService de Blazor. Las fechas se manejan en UTC.
public class CuentaService : ICuentaService
{
    public const string TipoVerificacion = "Verificacion";
    public const string TipoRecuperacion = "Recuperacion";

    // Rol "Cliente" de la semilla.
    public const int RolClienteId = 1;

    public const string MensajeRecuperacion = "Si el correo está registrado, te enviamos un código para crear una contraseña nueva.";

    private readonly ICuentaRepository _repository;
    private readonly IEnvioCorreo _correo;
    private readonly TimeProvider _reloj;
    private readonly OpcionesCuentas _opciones;

    public CuentaService(ICuentaRepository repository, IEnvioCorreo correo, TimeProvider reloj, IOptions<OpcionesCuentas> opciones)
    {
        _repository = repository;
        _correo = correo;
        _reloj = reloj;
        _opciones = opciones.Value;
    }

    private DateTime Ahora => _reloj.GetUtcNow().UtcDateTime;

    public async Task<Resultado<RespuestaCodigoDto>> RegistrarAsync(RegistroCreateDto datos)
    {
        var ahora = Ahora;
        var correo = ValidacionDatos.NormalizarCorreo(datos.Correo);

        var existente = await _repository.ObtenerPorCorreoAsync(correo);
        if (existente != null)
        {
            // Una cuenta que nunca se verificó en 7 días se elimina y el correo queda libre (D-REG-02).
            var abandonada = !existente.Verificada && existente.FechaCreacion < ahora.AddDays(-_opciones.DiasSinVerificar);
            if (!abandonada)
            {
                return CorreoNoDisponible<RespuestaCodigoDto>();
            }

            await _repository.EliminarNoVerificadaAsync(existente);
        }

        var tipoDocumento = datos.TipoDocumento.Trim();
        var numeroDocumento = ValidacionDatos.NormalizarDocumento(tipoDocumento, datos.NumeroDocumento);
        if (await _repository.ExisteDocumentoAsync(tipoDocumento, numeroDocumento))
        {
            return Resultado<RespuestaCodigoDto>.Error(409, "CUENTA_NO_DISPONIBLE", "No se pudo crear la cuenta",
                "No pudimos crear la cuenta con ese documento. Si ya tenés una, consultá en Recepción.");
        }

        // Términos y Privacidad vigentes (D-REG-04, RN-43).
        var legales = await _repository.ObtenerDocumentosVigentesAsync(ahora.Date);
        var terminos = legales.FirstOrDefault(d => d.Codigo == "terminos");
        var privacidad = legales.FirstOrDefault(d => d.Codigo == "privacidad");
        if (terminos == null || privacidad == null)
        {
            return Resultado<RespuestaCodigoDto>.Error(503, "LEGALES_NO_CONFIGURADOS", "Registro no disponible",
                "El registro no está disponible en este momento. Probá más tarde.");
        }

        var cuenta = new Cuenta
        {
            Nombre = datos.Nombre.Trim(),
            Apellido = datos.Apellido.Trim(),
            Correo = correo,
            Telefono = ValidacionDatos.NormalizarTelefono(datos.Telefono) ?? "",
            TipoDocumento = tipoDocumento,
            NumeroDocumento = numeroDocumento,
            Calle = datos.Calle.Trim(),
            NumeroPuerta = datos.NumeroPuerta.Trim(),
            Apartamento = (datos.Apartamento ?? "").Trim(),
            Localidad = datos.Localidad.Trim(),
            Departamento = datos.Departamento.Trim(),
            Referencias = (datos.Referencias ?? "").Trim(),
            HashContrasena = Seguridad.DerivarContrasena(datos.Contrasena),
            Verificada = false,
            Activa = true,
            FechaCreacion = ahora
        };
        cuenta.Roles.Add(new CuentaRol { RolId = RolClienteId });
        cuenta.Aceptaciones.Add(new AceptacionLegal { DocumentoLegalId = terminos.Id, FechaAceptacion = ahora });
        cuenta.Aceptaciones.Add(new AceptacionLegal { DocumentoLegalId = privacidad.Id, FechaAceptacion = ahora });

        // Cuenta, rol, aceptaciones y código en una sola transacción (un solo SaveChanges).
        var codigo = NuevoCodigo(cuenta, TipoVerificacion, ahora);
        _repository.AgregarCuenta(cuenta);

        try
        {
            await _repository.GuardarAsync();
        }
        catch (CuentaDuplicadaException)
        {
            // Otro registro con el mismo correo ganó la carrera: lo frena el índice único.
            return CorreoNoDisponible<RespuestaCodigoDto>();
        }

        // El correo sale después de guardar, fuera de la transacción.
        await _correo.EnviarCodigoAsync(cuenta.Correo, TipoVerificacion, codigo, ahora.AddMinutes(_opciones.MinutosCodigo));

        var respuesta = RespuestaConCodigo("Cuenta creada. Te enviamos un código para verificar tu correo.", codigo);
        return new Resultado<RespuestaCodigoDto> { Exito = true, Estado = 201, Valor = respuesta };
    }

    public async Task<Resultado<MensajeDto>> VerificarAsync(VerificacionDto datos)
    {
        var cuenta = await _repository.ObtenerPorCorreoAsync(ValidacionDatos.NormalizarCorreo(datos.Correo));
        if (cuenta == null || cuenta.Eliminada)
        {
            return Resultado<MensajeDto>.Error(404, "CUENTA_PENDIENTE_NO_ENCONTRADA", "Cuenta no encontrada",
                "No encontramos una cuenta pendiente de verificar con ese correo.");
        }

        if (cuenta.Verificada)
        {
            return Resultado<MensajeDto>.Ok(new MensajeDto { Mensaje = "Tu cuenta ya estaba verificada." });
        }

        var (error, codigo) = await ComprobarCodigoAsync(cuenta, TipoVerificacion, datos.Codigo);
        if (error != null)
        {
            return Resultado<MensajeDto>.Error(error.Estado, error.Codigo, error.Titulo, error.Mensaje);
        }

        cuenta.Verificada = true;
        codigo!.Usado = true;
        await _repository.GuardarAsync();

        return Resultado<MensajeDto>.Ok(new MensajeDto { Mensaje = "¡Listo! Tu correo quedó verificado." });
    }

    public async Task<Resultado<RespuestaCodigoDto>> ReenviarCodigoAsync(string correo)
    {
        var ahora = Ahora;
        var cuenta = await _repository.ObtenerPorCorreoAsync(ValidacionDatos.NormalizarCorreo(correo));
        if (cuenta == null || cuenta.Eliminada || cuenta.Verificada)
        {
            return Resultado<RespuestaCodigoDto>.Error(404, "SIN_CODIGO_PENDIENTE", "Sin código pendiente",
                "No hay un código pendiente para ese correo.");
        }

        if (await _repository.ContarCodigosDesdeAsync(cuenta.Id, TipoVerificacion, ahora.AddHours(-1)) >= _opciones.CodigosPorHora)
        {
            return Resultado<RespuestaCodigoDto>.Error(429, "DEMASIADOS_CODIGOS", "Demasiados pedidos",
                "Ya pediste 3 códigos en la última hora. Esperá un rato y volvé a intentar.");
        }

        var codigo = await ReemplazarCodigoAsync(cuenta, TipoVerificacion, ahora);
        await _correo.EnviarCodigoAsync(cuenta.Correo, TipoVerificacion, codigo, ahora.AddMinutes(_opciones.MinutosCodigo));

        return Resultado<RespuestaCodigoDto>.Ok(RespuestaConCodigo("Te enviamos un código nuevo. El anterior ya no sirve.", codigo));
    }

    public async Task<Resultado<RespuestaCodigoDto>> PedirRecuperacionAsync(string correo)
    {
        var ahora = Ahora;
        var respuesta = new RespuestaCodigoDto { Mensaje = MensajeRecuperacion };

        var cuenta = await _repository.ObtenerPorCorreoAsync(ValidacionDatos.NormalizarCorreo(correo));
        if (cuenta == null || cuenta.Eliminada || !cuenta.Activa || !cuenta.Verificada)
        {
            return Resultado<RespuestaCodigoDto>.Ok(respuesta);
        }

        // Pasado el límite por hora no se manda otro código, pero la respuesta es la misma.
        if (await _repository.ContarCodigosDesdeAsync(cuenta.Id, TipoRecuperacion, ahora.AddHours(-1)) >= _opciones.CodigosPorHora - 1)
        {
            return Resultado<RespuestaCodigoDto>.Ok(respuesta);
        }

        var codigo = await ReemplazarCodigoAsync(cuenta, TipoRecuperacion, ahora);
        await _correo.EnviarCodigoAsync(cuenta.Correo, TipoRecuperacion, codigo, ahora.AddMinutes(_opciones.MinutosCodigo));

        return Resultado<RespuestaCodigoDto>.Ok(RespuestaConCodigo(MensajeRecuperacion, codigo));
    }

    public async Task<Resultado<MensajeDto>> ConfirmarRecuperacionAsync(NuevaContrasenaDto datos)
    {
        var ahora = Ahora;
        var cuenta = await _repository.ObtenerPorCorreoAsync(ValidacionDatos.NormalizarCorreo(datos.Correo));
        if (cuenta == null || cuenta.Eliminada || !cuenta.Activa)
        {
            return CodigoNoValido();
        }

        var (error, codigo) = await ComprobarCodigoAsync(cuenta, TipoRecuperacion, datos.Codigo);
        if (error != null)
        {
            // Sin código o vencido: mensaje genérico, para no dar pistas sobre la cuenta.
            return error.Codigo == "CODIGO_INCORRECTO" || error.Codigo == "CODIGO_INVALIDADO"
                ? Resultado<MensajeDto>.Error(error.Estado, error.Codigo, error.Titulo, error.Mensaje)
                : CodigoNoValido();
        }

        cuenta.HashContrasena = Seguridad.DerivarContrasena(datos.Contrasena);
        cuenta.IntentosFallidos = 0;
        cuenta.BloqueadaHasta = null;
        codigo!.Usado = true;

        // Al cambiar la contraseña se cierran todas las sesiones (D-REG-03).
        foreach (var sesion in await _repository.ObtenerSesionesActivasAsync(cuenta.Id, ahora))
        {
            sesion.RevocadaEn = ahora;
        }

        await _repository.GuardarAsync();
        return Resultado<MensajeDto>.Ok(new MensajeDto { Mensaje = "Listo, tu contraseña cambió. Ya podés ingresar con la nueva." });
    }

    public async Task<Resultado<CuentaReadDto>> ObtenerPerfilAsync(int cuentaId)
    {
        var c = await _repository.ObtenerPorIdAsync(cuentaId);
        if (c == null || c.Eliminada || !c.Activa)
        {
            return Resultado<CuentaReadDto>.Error(404, "CUENTA_NO_ENCONTRADA", "Cuenta no encontrada", "No encontramos tu cuenta. Volvé a ingresar.");
        }

        return Resultado<CuentaReadDto>.Ok(new CuentaReadDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Apellido = c.Apellido,
            Correo = c.Correo,
            Telefono = c.Telefono,
            TipoDocumento = c.TipoDocumento,
            NumeroDocumento = c.NumeroDocumento,
            Calle = c.Calle,
            NumeroPuerta = c.NumeroPuerta,
            Apartamento = c.Apartamento,
            Localidad = c.Localidad,
            Departamento = c.Departamento,
            Referencias = c.Referencias,
            Verificada = c.Verificada,
            Roles = NombresRoles(c)
        });
    }

    public static List<string> NombresRoles(Cuenta cuenta)
    {
        return cuenta.Roles.Where(r => r.Rol != null).Select(r => r.Rol!.Nombre).OrderBy(n => n).ToList();
    }

    // ---------- Códigos ----------

    // Revisa el último código del tipo: que exista, no esté invalidado ni vencido, y coincida (5 intentos, D-REG-02).
    private async Task<(Resultado<MensajeDto>? Error, CodigoCuenta? Codigo)> ComprobarCodigoAsync(Cuenta cuenta, string tipo, string texto)
    {
        var codigo = await _repository.ObtenerUltimoCodigoAsync(cuenta.Id, tipo);
        if (codigo == null || codigo.Usado)
        {
            return (Resultado<MensajeDto>.Error(400, "SIN_CODIGO", "Sin código", "No hay un código pendiente para ese correo. Pedí uno nuevo."), null);
        }

        if (codigo.IntentosRestantes <= 0)
        {
            return (Resultado<MensajeDto>.Error(400, "CODIGO_INVALIDADO", "Código invalidado", "El código se invalidó por demasiados intentos. Pedí uno nuevo."), null);
        }

        if (codigo.Vence < Ahora)
        {
            return (Resultado<MensajeDto>.Error(400, "CODIGO_VENCIDO", "Código vencido", "El código venció. Pedí uno nuevo."), null);
        }

        if (!Seguridad.CodigoCoincide(texto, codigo.HashCodigo))
        {
            codigo.IntentosRestantes--;
            await _repository.GuardarAsync();

            return codigo.IntentosRestantes > 0
                ? (Resultado<MensajeDto>.Error(400, "CODIGO_INCORRECTO", "Código incorrecto", $"El código no es correcto. Te quedan {codigo.IntentosRestantes} intentos."), null)
                : (Resultado<MensajeDto>.Error(400, "CODIGO_INVALIDADO", "Código invalidado", "El código no es correcto y se invalidó. Pedí uno nuevo."), null);
        }

        return (null, codigo);
    }

    // Invalida los códigos anteriores del tipo y crea uno nuevo.
    private async Task<string> ReemplazarCodigoAsync(Cuenta cuenta, string tipo, DateTime ahora)
    {
        foreach (var anterior in await _repository.ObtenerCodigosSinUsarAsync(cuenta.Id, tipo))
        {
            anterior.Usado = true;
        }

        var codigo = NuevoCodigo(cuenta, tipo, ahora);
        await _repository.GuardarAsync();
        return codigo;
    }

    private string NuevoCodigo(Cuenta cuenta, string tipo, DateTime ahora)
    {
        var codigo = Seguridad.GenerarCodigo();
        _repository.AgregarCodigo(new CodigoCuenta
        {
            Cuenta = cuenta,
            CuentaId = cuenta.Id,
            Tipo = tipo,
            HashCodigo = Seguridad.HashCodigo(codigo),
            FechaCreacion = ahora,
            Vence = ahora.AddMinutes(_opciones.MinutosCodigo),
            IntentosRestantes = _opciones.IntentosPorCodigo
        });
        return codigo;
    }

    private RespuestaCodigoDto RespuestaConCodigo(string mensaje, string codigo)
    {
        return new RespuestaCodigoDto { Mensaje = mensaje, CodigoDesarrollo = _opciones.MostrarCodigoEnRespuesta ? codigo : null };
    }

    private static Resultado<T> CorreoNoDisponible<T>()
    {
        return Resultado<T>.Error(409, "CUENTA_NO_DISPONIBLE", "No se pudo crear la cuenta",
            "No pudimos crear la cuenta con ese correo. Si ya tenés una, ingresá o recuperá tu contraseña.");
    }

    private static Resultado<MensajeDto> CodigoNoValido()
    {
        return Resultado<MensajeDto>.Error(400, "CODIGO_INVALIDO", "Código no válido", "El código no es válido o venció. Pedí uno nuevo.");
    }
}
