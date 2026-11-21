using System.Globalization;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Comunicaciones, igual a ComunicacionesService de Blazor. Cada cliente elige el canal por tipo; un tipo esencial no
// se apaga. Lo que va solo a la app queda entregado en su bandeja; lo que va por correo pasa por la cola, con hasta
// 3 intentos y después Fallido (el personal lo reintenta). Un reenvío crea un mensaje nuevo; el original no cambia.
public class ComunicacionService : IComunicacionService
{
    public const string Correo = "Correo";
    public const string SoloApp = "Solo en la app";
    public const string Ninguno = "No recibir";
    public static readonly string[] Canales = { Correo, SoloApp, Ninguno };

    public const string Pendiente = "Pendiente";
    public const string Enviado = "Enviado";
    public const string Fallido = "Fallido";
    public const int MaxIntentos = 3;

    // Tipos de la semilla.
    public const int TipoComprobante = 2;
    public const int TipoAvisoClinica = 3;
    public const int TipoAlertaSanitaria = 5;

    private static readonly CultureInfo Cultura = CultureInfo.GetCultureInfo("es-UY");

    private readonly IComunicacionRepository _repository;
    private readonly IEnvioComunicacion _envio;
    private readonly TimeProvider _reloj;

    public ComunicacionService(IComunicacionRepository repository, IEnvioComunicacion envio, TimeProvider reloj)
    {
        _repository = repository;
        _envio = envio;
        _reloj = reloj;
    }

    private DateTime Ahora => _reloj.GetLocalNow().DateTime;

    // ===================== Cliente =====================

    public async Task<BandejaDto> BandejaAsync(int cuentaId, int page, int pageSize)
    {
        var items = await _repository.ObtenerDeCuentaAsync(cuentaId, (page - 1) * pageSize, pageSize);
        return new BandejaDto
        {
            Items = items.Select(c => ADto(c, paraPersonal: false)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = await _repository.ContarDeCuentaAsync(cuentaId),
            NoLeidas = await _repository.ContarNoLeidasAsync(cuentaId)
        };
    }

    public async Task<Resultado<ComunicacionReadDto>> MarcarLeidaAsync(int cuentaId, long id)
    {
        var comunicacion = await _repository.ObtenerAsync(id);
        if (comunicacion == null || comunicacion.CuentaId != cuentaId)
        {
            return Error(404, "MENSAJE_NO_ENCONTRADO", "No encontramos ese mensaje en tu bandeja.");
        }

        comunicacion.Leida = true;
        await _repository.GuardarAsync();
        return Resultado<ComunicacionReadDto>.Ok(ADto(comunicacion, paraPersonal: false));
    }

    public async Task<List<PreferenciaReadDto>> PreferenciasAsync(int cuentaId)
    {
        var elegidas = await _repository.ObtenerPreferenciasAsync(cuentaId);
        return (await _repository.ObtenerTiposAsync()).Select(t => new PreferenciaReadDto
        {
            TipoId = t.Id,
            Tipo = t.Nombre,
            Descripcion = t.Descripcion,
            Esencial = t.Esencial,
            Canal = elegidas.FirstOrDefault(p => p.TipoComunicacionId == t.Id)?.Canal ?? t.CanalPorDefecto
        }).ToList();
    }

    // RF-CLI-18, RF-CLI-20: el cliente elige el canal por tipo; un tipo esencial no puede quedar en «No recibir».
    public async Task<Resultado<PreferenciaReadDto>> CambiarPreferenciaAsync(int cuentaId, int tipoId, string canal)
    {
        var tipo = await _repository.ObtenerTipoAsync(tipoId);
        if (tipo == null || !Canales.Contains(canal))
        {
            return Resultado<PreferenciaReadDto>.Error(400, "PREFERENCIA_INVALIDA", "Datos no válidos", "No pudimos guardar esa preferencia.");
        }

        if (tipo.Esencial && canal == Ninguno)
        {
            return Resultado<PreferenciaReadDto>.Error(400, "TIPO_ESENCIAL", "Datos no válidos",
                $"«{tipo.Nombre}» es parte del servicio: podés elegir correo o solo la app, pero no dejar de recibirlo.");
        }

        var preferencia = await _repository.ObtenerPreferenciaAsync(cuentaId, tipoId);
        if (preferencia == null)
        {
            _repository.Agregar(new PreferenciaComunicacion { CuentaId = cuentaId, TipoComunicacionId = tipoId, Canal = canal, Fecha = Ahora });
        }
        else
        {
            preferencia.Canal = canal;
            preferencia.Fecha = Ahora;
        }

        await _repository.GuardarAsync();
        return Resultado<PreferenciaReadDto>.Ok(new PreferenciaReadDto { TipoId = tipo.Id, Tipo = tipo.Nombre, Descripcion = tipo.Descripcion, Esencial = tipo.Esencial, Canal = canal });
    }

    // ===================== Personal =====================

    public async Task<PaginaDto<ComunicacionReadDto>> SalidaAsync(string? estado, int? tipoId, string? texto, int page, int pageSize)
    {
        var e = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim();
        var t = string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
        var items = await _repository.BuscarAsync(e, tipoId, t, (page - 1) * pageSize, pageSize);
        return new PaginaDto<ComunicacionReadDto>
        {
            Items = items.Select(c => ADto(c, paraPersonal: true)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = await _repository.ContarBusquedaAsync(e, tipoId, t)
        };
    }

    // RNF-CON-04: intenta los pendientes. Lo que va solo a la app queda entregado en la bandeja del cliente.
    public async Task<ColaProcesadaDto> ProcesarColaAsync()
    {
        var resultado = new ColaProcesadaDto();
        foreach (var comunicacion in await _repository.ObtenerPendientesAsync(100))
        {
            resultado.Intentados++;
            await IntentarAsync(comunicacion);
            if (comunicacion.Estado == Enviado)
            {
                resultado.Enviados++;
            }
            else if (comunicacion.Estado == Fallido)
            {
                resultado.Fallidos++;
            }
        }

        await _repository.GuardarAsync();
        return resultado;
    }

    // Un mensaje que agotó los intentos vuelve a la cola por decisión del personal.
    public async Task<Resultado<ComunicacionReadDto>> ReintentarAsync(int personaId, string rol, long id)
    {
        var comunicacion = await _repository.ObtenerAsync(id);
        if (comunicacion == null || comunicacion.Estado != Fallido)
        {
            return Error(409, "NO_FALLIDO", "Solo se reintenta un envío que quedó Fallido.");
        }

        comunicacion.Intentos = 0;
        comunicacion.Estado = Pendiente;
        await IntentarAsync(comunicacion);
        Auditar(personaId, rol, "Reintento de envío", comunicacion.Id, $"Comunicación {comunicacion.Id} ({comunicacion.TipoComunicacion?.Nombre}).");
        await _repository.GuardarAsync();

        return comunicacion.Estado == Enviado
            ? Resultado<ComunicacionReadDto>.Ok(ADto(comunicacion, paraPersonal: true))
            : Error(502, "ENVIO_FALLIDO", "Sigue sin poder enviarse; quedó en la cola para otro intento.");
    }

    // RF-REC-07: reenvío bajo pedido. Mensaje nuevo, siempre por correo y sin depender de la preferencia.
    public async Task<Resultado<ComunicacionReadDto>> ReenviarAsync(int personaId, string rol, long id)
    {
        var original = await _repository.ObtenerAsync(id);
        var cuenta = original?.Cuenta;
        if (original == null || cuenta == null || cuenta.Eliminada || !cuenta.Verificada)
        {
            return Error(404, "NO_REENVIABLE", "No se puede reenviar: el mensaje o la cuenta ya no existen.");
        }

        var copia = new Comunicacion
        {
            CuentaId = original.CuentaId,
            Cuenta = cuenta,
            TipoComunicacionId = original.TipoComunicacionId,
            TipoComunicacion = original.TipoComunicacion,
            Asunto = original.Asunto,
            Cuerpo = original.Cuerpo,
            Canal = Correo,
            Estado = Pendiente,
            Destino = cuenta.Correo,
            ReenvioDeId = original.Id,
            GeneradaPorId = personaId,
            FechaCreacion = Ahora
        };
        await IntentarAsync(copia);
        _repository.Agregar(copia);
        Auditar(personaId, rol, "Reenvío", original.Id, $"Comunicación {original.Id} ({original.TipoComunicacion?.Nombre}) reenviada a la cuenta {original.CuentaId}.");
        await _repository.GuardarAsync();

        return new Resultado<ComunicacionReadDto> { Exito = true, Estado = 201, Valor = ADto(copia, paraPersonal: true) };
    }

    // RF-REC-07: comprobante de un cobro. Una vez enviado, se manda de nuevo con un reenvío.
    public async Task<Resultado<ComunicacionReadDto>> EnviarComprobanteAsync(int personaId, string rol, int pagoId)
    {
        var pago = await _repository.ObtenerPagoAsync(pagoId);
        if (pago?.Cargo == null)
        {
            return Error(404, "COMPROBANTE_NO_ENCONTRADO", "Ese comprobante no existe.");
        }

        var clave = $"pago-{pago.Id}";
        if (await _repository.ExisteConClaveAsync(pago.Cargo.ClienteId, TipoComprobante, clave))
        {
            return Error(409, "COMPROBANTE_ENVIADO", "Ese comprobante ya se envió. Para mandarlo de nuevo, reenviá el mensaje desde Comunicaciones.");
        }

        var cuerpo = $"Comprobante {pago.Numero} del {pago.Fecha.ToString("dd/MM/yyyy HH:mm", Cultura)}.\n"
            + $"Servicio: {pago.Cargo.Descripcion}.\n"
            + $"Importe cobrado: $ {pago.Monto.ToString("N0", Cultura)} ({pago.Medio}).";

        var comunicacion = await EncolarAsync(pago.Cargo.ClienteId, TipoComprobante, $"Comprobante de cobro {pago.Numero}", cuerpo, clave, personaId);
        if (comunicacion == null)
        {
            return Error(409, "SIN_DESTINATARIO", "No se pudo generar el mensaje: la cuenta del cliente no existe o no está verificada.");
        }

        Auditar(personaId, rol, "Envío de comprobante", 0, $"Comprobante {pago.Numero} a la cuenta {pago.Cargo.ClienteId}.");
        await _repository.GuardarAsync();
        return new Resultado<ComunicacionReadDto> { Exito = true, Estado = 201, Valor = ADto(comunicacion, paraPersonal: true) };
    }

    // Aviso de la clínica para todos los clientes (cierres, cambios de horario).
    public Task<Resultado<DifusionResultadoDto>> AvisoOperativoAsync(int personaId, string rol, DifusionDto datos)
        => DifundirAsync(personaId, rol, TipoAvisoClinica, datos.Titulo, datos.Mensaje, null);

    // Alerta sanitaria para los clientes con animales de una especie (o todos). Solo la reciben quienes la activaron.
    public Task<Resultado<DifusionResultadoDto>> AlertaSanitariaAsync(int personaId, string rol, DifusionDto datos)
        => DifundirAsync(personaId, rol, TipoAlertaSanitaria, datos.Titulo, datos.Mensaje, datos.EspecieId);

    // ===================== Apoyo =====================

    private async Task<Resultado<DifusionResultadoDto>> DifundirAsync(int personaId, string rol, int tipoId, string titulo, string mensaje, int? especieId)
    {
        titulo = (titulo ?? "").Trim();
        mensaje = (mensaje ?? "").Trim();
        if (titulo.Length == 0 || titulo.Length > 120 || mensaje.Length == 0 || mensaje.Length > 1000)
        {
            return Resultado<DifusionResultadoDto>.Error(400, "DIFUSION_INVALIDA", "Datos no válidos", "Completá el título (hasta 120 caracteres) y el mensaje (hasta 1000).");
        }

        var tipo = await _repository.ObtenerTipoAsync(tipoId);
        var resultado = new DifusionResultadoDto();
        foreach (var cuenta in await _repository.ObtenerDestinatariosAsync(especieId))
        {
            if (await EncolarAsync(cuenta.Id, tipoId, titulo, mensaje, null, personaId, cuenta) == null)
            {
                resultado.Omitidos++;
            }
            else
            {
                resultado.Generados++;
            }
        }

        var destino = especieId == null ? "todos los clientes" : $"clientes con animales de la especie {especieId}";
        Auditar(personaId, rol, $"Emisión de {tipo?.Nombre.ToLowerInvariant()}", 0, $"«{titulo}» para {destino}: {resultado.Generados} generados, {resultado.Omitidos} omitidos por preferencia.");
        await _repository.GuardarAsync();

        resultado.Mensaje = $"{resultado.Generados} mensajes generados. {resultado.Omitidos} clientes no lo reciben por su preferencia.";
        return Resultado<DifusionResultadoDto>.Ok(resultado);
    }

    // Genera un mensaje si la cuenta existe y está verificada, el tipo existe, el cliente no eligió «No recibir» y no
    // hay otro con la misma clave (RNF-CON-10). Por correo se intenta enseguida; si falla queda en la cola.
    private async Task<Comunicacion?> EncolarAsync(int cuentaId, int tipoId, string asunto, string cuerpo, string? clave, int? generadaPorId, Cuenta? cuenta = null)
    {
        cuenta ??= await _repository.ObtenerCuentaAsync(cuentaId);
        var tipo = await _repository.ObtenerTipoAsync(tipoId);
        if (cuenta == null || cuenta.Eliminada || !cuenta.Verificada || tipo == null)
        {
            return null;
        }

        if (clave != null && await _repository.ExisteConClaveAsync(cuentaId, tipoId, clave))
        {
            return null;
        }

        var canal = (await _repository.ObtenerPreferenciaAsync(cuentaId, tipoId))?.Canal ?? tipo.CanalPorDefecto;
        if (canal == Ninguno)
        {
            return null;
        }

        var comunicacion = new Comunicacion
        {
            CuentaId = cuentaId,
            TipoComunicacionId = tipoId,
            Asunto = asunto,
            Cuerpo = cuerpo,
            Canal = canal == Correo ? Correo : "En la app",
            Estado = Pendiente,
            Destino = canal == Correo ? cuenta.Correo : "",
            Clave = clave,
            GeneradaPorId = generadaPorId,
            FechaCreacion = Ahora
        };
        await IntentarAsync(comunicacion);
        _repository.Agregar(comunicacion);
        return comunicacion;
    }

    // Un intento: lo de la app queda entregado; el correo se envía y, si falla 3 veces, queda Fallido.
    private async Task IntentarAsync(Comunicacion comunicacion)
    {
        if (comunicacion.Canal != Correo)
        {
            comunicacion.Estado = Enviado;
            comunicacion.FechaEnvio ??= Ahora;
            return;
        }

        comunicacion.Intentos++;
        string? error;
        try
        {
            error = await _envio.EnviarAsync(comunicacion.Destino, comunicacion.Asunto, comunicacion.Cuerpo);
        }
        catch (Exception)
        {
            error = "El servidor de correo no respondió.";
        }

        if (error == null)
        {
            comunicacion.Estado = Enviado;
            comunicacion.FechaEnvio = Ahora;
            comunicacion.UltimoError = "";
        }
        else
        {
            comunicacion.UltimoError = error.Length > 500 ? error[..500] : error;
            comunicacion.Estado = comunicacion.Intentos >= MaxIntentos ? Fallido : Pendiente;
        }
    }

    private void Auditar(int personaId, string rol, string accion, long id, string detalle)
    {
        _repository.Agregar(new Auditoria
        {
            Fecha = Ahora,
            CuentaId = personaId,
            Rol = rol,
            Accion = accion,
            Entidad = "Comunicaciones",
            EntidadId = id == 0 ? "" : id.ToString(),
            Origen = "API",
            Detalle = detalle.Length > 500 ? detalle[..500] : detalle
        });
    }

    private static ComunicacionReadDto ADto(Comunicacion c, bool paraPersonal) => new()
    {
        Id = c.Id,
        TipoId = c.TipoComunicacionId,
        Tipo = c.TipoComunicacion?.Nombre ?? "",
        Asunto = c.Asunto,
        Cuerpo = c.Cuerpo,
        Canal = c.Canal,
        Estado = c.Estado,
        FechaCreacion = c.FechaCreacion,
        FechaEnvio = c.FechaEnvio,
        Leida = c.Leida,
        CuentaId = c.CuentaId,
        Cliente = paraPersonal && c.Cuenta != null ? $"{c.Cuenta.Nombre} {c.Cuenta.Apellido}".Trim() : "",
        Destino = paraPersonal ? c.Destino : "",
        Intentos = paraPersonal ? c.Intentos : 0,
        UltimoError = paraPersonal ? c.UltimoError : "",
        ReenvioDeId = paraPersonal ? c.ReenvioDeId : null
    };

    private static Resultado<ComunicacionReadDto> Error(int estado, string codigo, string mensaje)
        => Resultado<ComunicacionReadDto>.Error(estado, codigo, estado switch { 404 => "No encontrado", 409 => "No se puede", 502 => "Envío fallido", _ => "Datos no válidos" }, mensaje);
}
