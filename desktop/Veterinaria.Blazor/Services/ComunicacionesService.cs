using System.Globalization;
using System.Text;
using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Comunicaciones al cliente de demostración en memoria (A-028, RF-CLI-07, RF-CLI-10, RF-CLI-18, RF-CLI-20, RF-REC-07).
// No se envía ningún correo real: cada mensaje queda en una bandeja de salida con su estado (Pendiente, Enviado, Fallido).
// En la sección C la API guarda la cola en una tabla, envía por SMTP fuera de la transacción y reintenta (RNF-CON-03, RNF-CON-04);
// este servicio se reemplaza por ComunicacionesApiService.
public class ComunicacionesService
{
    public const string ResumenClinico = "Resumen clínico";
    public const string Comprobante = "Comprobante de cobro";
    public const string AvisoRefuerzo = "Aviso de refuerzo";
    public const string AlertaSanitaria = "Alerta sanitaria";
    public const string AvisoOperativo = "Aviso de la clínica";

    public const string Correo = "Correo";
    public const string SoloApp = "Solo en la app";
    public const string Ninguno = "No recibir";

    public const string Pendiente = "Pendiente";
    public const string Enviado = "Enviado";
    public const string Fallido = "Fallido";

    // RNF-CON-04: intentos antes de pasar a Fallido; después solo Recepción puede reintentar a mano.
    public const int MaxIntentos = 3;

    private readonly CuentaService _cuentas;
    private readonly AnimalService _animales;
    private readonly AuditoriaService _auditoria;
    private readonly List<Comunicacion> _comunicaciones = new();
    private readonly List<PreferenciaComunicacion> _preferencias = new();
    private int _ultimoId;

    public ComunicacionesService(CuentaService cuentas, AnimalService animales, AuditoriaService auditoria)
    {
        _cuentas = cuentas;
        _animales = animales;
        _auditoria = auditoria;
    }

    // SOLO DEMOSTRACIÓN: hace fallar los envíos por correo para probar los reintentos.
    public bool SimularFalla { get; set; }

    public List<string> Canales { get; } = new() { Correo, SoloApp, Ninguno };

    // Catálogo con finalidad (RNF-LEG-06). Los esenciales no se pueden apagar; los opcionales arrancan apagados (RN-43).
    public List<TipoComunicacion> Tipos { get; } = new()
    {
        new() { Nombre = ResumenClinico, Descripcion = "Resumen de la consulta cuando el veterinario la firma (RF-CLI-10).", Esencial = true, CanalPorDefecto = Correo },
        new() { Nombre = Comprobante, Descripcion = "Comprobante de un cobro, cuando Recepción lo envía.", Esencial = true, CanalPorDefecto = Correo },
        new() { Nombre = AvisoOperativo, Descripcion = "Cambios que afectan tu atención: cierres, horarios, feriados.", Esencial = true, CanalPorDefecto = Correo },
        new() { Nombre = AvisoRefuerzo, Descripcion = "Vacunas y desparasitaciones próximas a vencer de tus animales (RF-CLI-07).", Esencial = false, CanalPorDefecto = Correo },
        new() { Nombre = AlertaSanitaria, Descripcion = "Alertas de la clínica para animales de tu misma especie, como un brote en la zona.", Esencial = false, CanalPorDefecto = Ninguno }
    };

    public TipoComunicacion? ObtenerTipo(string nombre)
    {
        return Tipos.FirstOrDefault(t => t.Nombre == nombre);
    }

    // Canal que rige para un tipo: lo que eligió el cliente o, si no eligió, el del catálogo.
    public string CanalDe(int cuentaId, string tipo)
    {
        var elegida = _preferencias.FirstOrDefault(p => p.CuentaId == cuentaId && p.Tipo == tipo);
        if (elegida is not null)
        {
            return elegida.Canal;
        }

        return ObtenerTipo(tipo)?.CanalPorDefecto ?? Ninguno;
    }

    public List<(TipoComunicacion Tipo, string Canal)> PreferenciasDe(int cuentaId)
    {
        return Tipos.Select(t => (t, CanalDe(cuentaId, t.Nombre))).ToList();
    }

    // RF-CLI-18, RF-CLI-20: el cliente elige el canal por tipo. Un tipo esencial no puede quedar en «No recibir».
    public ResultadoOperacion CambiarPreferencia(int cuentaId, string tipo, string canal)
    {
        var cuenta = _cuentas.ObtenerPorId(cuentaId);
        var definicion = ObtenerTipo(tipo);
        if (cuenta is null || cuenta.Eliminada || definicion is null || !Canales.Contains(canal))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No pudimos guardar esa preferencia." };
        }

        if (definicion.Esencial && canal == Ninguno)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"«{tipo}» es parte del servicio: podés elegir correo o solo la app, pero no dejar de recibirlo." };
        }

        var existente = _preferencias.FirstOrDefault(p => p.CuentaId == cuentaId && p.Tipo == tipo);
        if (existente is null)
        {
            _preferencias.Add(new PreferenciaComunicacion { CuentaId = cuentaId, Tipo = tipo, Canal = canal, Fecha = DateTime.Now });
        }
        else
        {
            existente.Canal = canal;
            existente.Fecha = DateTime.Now;
        }

        return new ResultadoOperacion { Exito = true, Mensaje = $"Listo: «{tipo}» → {canal}." };
    }

    // Genera un mensaje para la cuenta. Devuelve null si no se genera: cuenta inexistente, eliminada o sin verificar,
    // tipo desconocido, el cliente eligió no recibirlo, o ya existe un mensaje con la misma clave (idempotencia, RNF-CON-10).
    public Comunicacion? Encolar(int cuentaId, string tipo, string asunto, string cuerpo, string clave = "", string origen = "Sistema")
    {
        var cuenta = _cuentas.ObtenerPorId(cuentaId);
        if (cuenta is null || cuenta.Eliminada || !cuenta.Verificada || ObtenerTipo(tipo) is null)
        {
            return null;
        }

        if (clave != "" && _comunicaciones.Any(c => c.CuentaId == cuentaId && c.Tipo == tipo && c.Clave == clave && c.ReenvioDeId == 0))
        {
            return null;
        }

        var canal = CanalDe(cuentaId, tipo);
        if (canal == Ninguno)
        {
            return null;
        }

        var comunicacion = new Comunicacion
        {
            Id = ++_ultimoId,
            CuentaId = cuentaId,
            Tipo = tipo,
            Asunto = asunto,
            Cuerpo = cuerpo,
            Clave = clave,
            Origen = origen,
            FechaCreacion = DateTime.Now
        };

        if (canal == SoloApp)
        {
            // Sin correo no hay nada que enviar: queda disponible en la cuenta del cliente.
            comunicacion.Canal = "En la app";
            comunicacion.Estado = Enviado;
            comunicacion.FechaEnvio = DateTime.Now;
        }
        else
        {
            comunicacion.Canal = "Correo";
            comunicacion.Destino = cuenta.Correo;
            Intentar(comunicacion);
        }

        _comunicaciones.Add(comunicacion);
        return comunicacion;
    }

    // Un intento de envío. En la demostración sale bien salvo que se simule una falla.
    private void Intentar(Comunicacion comunicacion)
    {
        comunicacion.Intentos++;
        if (SimularFalla)
        {
            comunicacion.UltimoError = "El servidor de correo no respondió (falla simulada).";
            comunicacion.Estado = comunicacion.Intentos >= MaxIntentos ? Fallido : Pendiente;
            return;
        }

        comunicacion.Estado = Enviado;
        comunicacion.FechaEnvio = DateTime.Now;
        comunicacion.UltimoError = "";
    }

    // RNF-CON-04: el proceso de la cola reintenta los pendientes. Devuelve cuántos intentó.
    public int ProcesarCola()
    {
        var pendientes = _comunicaciones.Where(c => c.Estado == Pendiente && c.Canal == "Correo").ToList();
        foreach (var comunicacion in pendientes)
        {
            Intentar(comunicacion);
        }

        return pendientes.Count;
    }

    private static bool PuedeGestionar(Personal? persona)
    {
        return persona is not null && persona.Activo
            && (persona.Rol == PersonalService.Recepcion || persona.Rol == PersonalService.Administrador);
    }

    // Un mensaje que agotó los intentos vuelve a la cola por decisión del personal.
    public ResultadoOperacion Reintentar(int id, Personal? persona)
    {
        if (!PuedeGestionar(persona))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo Recepción o el Administrador pueden reintentar un envío." };
        }

        var comunicacion = ObtenerPorId(id);
        if (comunicacion is null || comunicacion.Estado != Fallido)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo se reintenta un envío que quedó Fallido." };
        }

        comunicacion.Intentos = 0;
        comunicacion.Estado = Pendiente;
        Intentar(comunicacion);
        _auditoria.Registrar(persona!, BitacoraService.ComunicacionesEntidad, "Reintento de envío", $"Comunicación {id} ({comunicacion.Tipo}).");

        return comunicacion.Estado == Enviado
            ? new ResultadoOperacion { Exito = true, Mensaje = "Enviado." }
            : new ResultadoOperacion { Exito = false, Mensaje = "Sigue sin poder enviarse; quedó en la cola para otro intento." };
    }

    // RF-REC-07: reenvío bajo pedido. Crea un mensaje nuevo con el mismo contenido y siempre por correo,
    // porque el cliente lo pidió; no depende de su preferencia. El original no se modifica.
    public ResultadoOperacion Reenviar(int id, Personal? persona)
    {
        if (!PuedeGestionar(persona))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo Recepción o el Administrador pueden reenviar un mensaje." };
        }

        var original = ObtenerPorId(id);
        var cuenta = original is null ? null : _cuentas.ObtenerPorId(original.CuentaId);
        if (original is null || cuenta is null || cuenta.Eliminada || !cuenta.Verificada)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No se puede reenviar: el mensaje o la cuenta ya no existen." };
        }

        var copia = new Comunicacion
        {
            Id = ++_ultimoId,
            CuentaId = original.CuentaId,
            Tipo = original.Tipo,
            Asunto = original.Asunto,
            Cuerpo = original.Cuerpo,
            Canal = "Correo",
            Destino = cuenta.Correo,
            ReenvioDeId = original.Id,
            Origen = persona!.Nombre,
            FechaCreacion = DateTime.Now
        };
        Intentar(copia);
        _comunicaciones.Add(copia);
        _auditoria.Registrar(persona, BitacoraService.ComunicacionesEntidad, "Reenvío", $"Comunicación {original.Id} ({original.Tipo}) reenviada a la cuenta {original.CuentaId}.");

        return copia.Estado == Enviado
            ? new ResultadoOperacion { Exito = true, Mensaje = $"Reenviado a {cuenta.Correo}." }
            : new ResultadoOperacion { Exito = true, Mensaje = "Quedó en la cola: el envío falló y se reintenta." };
    }

    public Comunicacion? ObtenerPorId(int id)
    {
        return _comunicaciones.FirstOrDefault(c => c.Id == id);
    }

    public List<Comunicacion> Todas()
    {
        return _comunicaciones.OrderByDescending(c => c.Id).ToList();
    }

    // Bandeja de salida para el personal: filtra por estado, tipo y texto (asunto, destino o nombre del cliente).
    public List<Comunicacion> Buscar(string estado = "", string tipo = "", string texto = "")
    {
        return _comunicaciones
            .Where(c => string.IsNullOrWhiteSpace(estado) || c.Estado == estado)
            .Where(c => string.IsNullOrWhiteSpace(tipo) || c.Tipo == tipo)
            .Where(c => string.IsNullOrWhiteSpace(texto) || Coincide(c, texto.Trim()))
            .OrderByDescending(c => c.Id)
            .ToList();
    }

    private bool Coincide(Comunicacion comunicacion, string texto)
    {
        var cuenta = _cuentas.ObtenerPorId(comunicacion.CuentaId);
        var nombre = cuenta is null ? "" : $"{cuenta.Nombre} {cuenta.Apellido}";
        return comunicacion.Asunto.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || comunicacion.Destino.Contains(texto, StringComparison.OrdinalIgnoreCase)
            || nombre.Contains(texto, StringComparison.OrdinalIgnoreCase);
    }

    // Lo que ve el cliente en su cuenta: solo sus mensajes (RN-18), del más nuevo al más viejo.
    public List<Comunicacion> DeCuenta(int cuentaId)
    {
        return _comunicaciones.Where(c => c.CuentaId == cuentaId).OrderByDescending(c => c.Id).ToList();
    }

    public int NoLeidas(int cuentaId)
    {
        return _comunicaciones.Count(c => c.CuentaId == cuentaId && !c.Leida);
    }

    public bool MarcarLeida(int id, int cuentaId)
    {
        var comunicacion = _comunicaciones.FirstOrDefault(c => c.Id == id && c.CuentaId == cuentaId);
        if (comunicacion is null)
        {
            return false;
        }

        comunicacion.Leida = true;
        return true;
    }

    // Derecho de supresión: los mensajes tienen datos personales y se borran con la cuenta. Devuelve cuántos.
    public int EliminarDe(int cuentaId)
    {
        _preferencias.RemoveAll(p => p.CuentaId == cuentaId);
        return _comunicaciones.RemoveAll(c => c.CuentaId == cuentaId);
    }

    // RF-CLI-10: al firmar la consulta se genera el resumen para el responsable. La clave evita duplicarlo.
    public Comunicacion? EncolarResumenClinico(int clienteId, Consulta consulta)
    {
        var cuenta = _cuentas.ObtenerPorId(clienteId);
        var animal = _animales.ObtenerParaPersonal(consulta.AnimalId);
        var nombreAnimal = animal?.Nombre ?? "tu animal";
        var fecha = (consulta.FechaFirma ?? consulta.FechaInicio).ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("es-UY"));

        var cuerpo = new StringBuilder();
        cuerpo.AppendLine($"Hola, {cuenta?.Nombre}. Te dejamos el resumen de la consulta de {nombreAnimal} del {fecha}, atendida por {consulta.AutorNombre}.");
        cuerpo.AppendLine($"Motivo: {consulta.Motivo}");
        cuerpo.AppendLine($"Diagnóstico ({consulta.TipoDiagnostico.ToLowerInvariant()}): {consulta.Diagnostico}");
        if (!string.IsNullOrWhiteSpace(consulta.Tratamiento))
        {
            cuerpo.AppendLine($"Tratamiento: {consulta.Tratamiento}");
        }

        if (!string.IsNullOrWhiteSpace(consulta.Indicaciones))
        {
            cuerpo.AppendLine($"Indicaciones: {consulta.Indicaciones}");
        }

        if (consulta.PesoKg is not null)
        {
            cuerpo.AppendLine($"Peso: {consulta.PesoKg.Value.ToString("N1", CultureInfo.GetCultureInfo("es-UY"))} kg");
        }

        return Encolar(clienteId, ResumenClinico, $"Resumen de la consulta de {nombreAnimal}", cuerpo.ToString().TrimEnd(), $"consulta-{consulta.Id}");
    }

    // RF-REC-07: Recepción envía el comprobante de un cobro al cliente. Una vez enviado, se reenvía desde la bandeja.
    public ResultadoOperacion EnviarComprobante(Pago pago, Cargo cargo, Personal? persona)
    {
        if (!PuedeGestionar(persona))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo Recepción o el Administrador pueden enviar un comprobante." };
        }

        var clave = $"pago-{pago.Id}";
        if (_comunicaciones.Any(c => c.CuentaId == cargo.ClienteId && c.Tipo == Comprobante && c.Clave == clave))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ese comprobante ya se envió. Para mandarlo de nuevo, reenviá el mensaje desde Comunicaciones." };
        }

        var cultura = CultureInfo.GetCultureInfo("es-UY");
        var cuerpo = $"Comprobante {pago.Numero} del {pago.Fecha.ToString("dd/MM/yyyy HH:mm", cultura)}.\n"
            + $"Servicio: {cargo.Descripcion}.\n"
            + $"Importe cobrado: $ {pago.Monto.ToString("N0", cultura)} ({pago.Medio}).";

        var comunicacion = Encolar(cargo.ClienteId, Comprobante, $"Comprobante de cobro {pago.Numero}", cuerpo, clave, persona!.Nombre);
        if (comunicacion is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No se pudo generar el mensaje: la cuenta del cliente no existe o no está verificada." };
        }

        _auditoria.Registrar(persona, BitacoraService.ComunicacionesEntidad, "Envío de comprobante", $"Comprobante {pago.Numero} a la cuenta {cargo.ClienteId}.");
        return new ResultadoOperacion { Exito = true, Mensaje = comunicacion.Estado == Enviado ? "Comprobante enviado al cliente." : "Quedó en la cola: el envío falló y se reintenta." };
    }

    // Alerta sanitaria para los clientes con animales de una especie (o todos si la especie va vacía).
    // La reciben solo quienes la activaron en sus preferencias (RN-43).
    public ResultadoOperacion EnviarAlertaSanitaria(Personal? persona, string titulo, string mensaje, string especie = "")
    {
        if (persona is null || !persona.Activo || (persona.Rol != PersonalService.Administrador && persona.Rol != PersonalService.Veterinario))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo el Administrador o un Veterinario pueden emitir una alerta sanitaria." };
        }

        return Difundir(persona, AlertaSanitaria, titulo, mensaje, especie);
    }

    // Aviso operativo para todos los clientes verificados (cierres, cambios de horario).
    public ResultadoOperacion EnviarAvisoOperativo(Personal? persona, string titulo, string mensaje)
    {
        if (!PuedeGestionar(persona))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo Recepción o el Administrador pueden emitir un aviso de la clínica." };
        }

        return Difundir(persona!, AvisoOperativo, titulo, mensaje, "");
    }

    private ResultadoOperacion Difundir(Personal persona, string tipo, string titulo, string mensaje, string especie)
    {
        titulo = (titulo ?? "").Trim();
        mensaje = (mensaje ?? "").Trim();
        if (titulo.Length == 0 || titulo.Length > 120 || mensaje.Length == 0 || mensaje.Length > 1000)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Completá el título (hasta 120 caracteres) y el mensaje (hasta 1000)." };
        }

        var generados = 0;
        var omitidos = 0;
        foreach (var cuenta in _cuentas.Todas().Where(c => c.Verificada && !c.Eliminada))
        {
            if (especie != "" && !_animales.ObtenerDe(cuenta.Id).Any(a => a.Especie == especie))
            {
                continue;
            }

            if (Encolar(cuenta.Id, tipo, titulo, mensaje, "", persona.Nombre) is null)
            {
                omitidos++;
            }
            else
            {
                generados++;
            }
        }

        var destino = especie == "" ? "todos los clientes" : $"clientes con {especie.ToLowerInvariant()}";
        _auditoria.Registrar(persona, BitacoraService.ComunicacionesEntidad, $"Emisión de {tipo.ToLowerInvariant()}", $"«{titulo}» para {destino}: {generados} generados, {omitidos} omitidos por preferencia.");
        return new ResultadoOperacion { Exito = true, Mensaje = $"{generados} mensajes generados. {omitidos} clientes no lo reciben por su preferencia." };
    }
}
