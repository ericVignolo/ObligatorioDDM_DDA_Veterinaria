using System.Text.Encodings.Web;
using System.Text.Json;
using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Derechos del titular sobre sus datos personales de demostración (A-028, RF-CLI-19, RNF-LEG-01, RNF-LEG-02, RNF-LEG-06).
// Exportar entrega los datos del cliente en formato abierto (JSON). Eliminar reemplaza los datos personales por valores neutros
// y conserva los animales, la historia clínica, los cargos y los consentimientos sin identificar al titular.
// En la sección C la API genera el archivo, valida las condiciones y anonimiza en una transacción con auditoría.
public class DerechosService
{
    public const string Exportacion = "Exportación";
    public const string Eliminacion = "Eliminación";

    public const string Recibida = "Recibida";
    public const string Completada = "Completada";
    public const string Rechazada = "Rechazada";
    public const string Cancelada = "Cancelada";

    private readonly CuentaService _cuentas;
    private readonly AnimalService _animales;
    private readonly AgendaService _agenda;
    private readonly CobroService _cobros;
    private readonly ConsentimientoService _consentimientos;
    private readonly InternacionService _internaciones;
    private readonly ComunicacionesService _comunicaciones;
    private readonly AuditoriaService _auditoria;
    private readonly List<SolicitudDerecho> _solicitudes = new();
    private int _ultimoId;

    public DerechosService(CuentaService cuentas, AnimalService animales, AgendaService agenda, CobroService cobros,
        ConsentimientoService consentimientos, InternacionService internaciones, ComunicacionesService comunicaciones, AuditoriaService auditoria)
    {
        _cuentas = cuentas;
        _animales = animales;
        _agenda = agenda;
        _cobros = cobros;
        _consentimientos = consentimientos;
        _internaciones = internaciones;
        _comunicaciones = comunicaciones;
        _auditoria = auditoria;
    }

    // RNF-LEG-06: finalidad, acceso, conservación y destino de cada categoría. Los plazos legales no se inventan:
    // quedan «a confirmar» hasta la revisión de normativa previa a producción (RNF-LEG-04, RNF-LEG-08).
    public List<PoliticaConservacion> Politicas()
    {
        return new List<PoliticaConservacion>
        {
            new() { Categoria = "Datos de la cuenta (nombre, contacto, documento, domicilio)", Finalidad = "Identificarte, contactarte y atender tus animales.", Acceso = "Vos, Recepción y el Administrador.", Conservacion = "Mientras la cuenta esté activa.", AlEliminar = "Se reemplazan por valores neutros." },
            new() { Categoria = "Animales e historia clínica", Finalidad = "Continuidad de la atención veterinaria y obligaciones sanitarias.", Acceso = "Vos (solo lectura), el personal clínico y el Administrador; toda lectura queda auditada.", Conservacion = "El plazo que fije la normativa veterinaria aplicable (a confirmar en la revisión legal previa a producción).", AlEliminar = "Se conserva, sin identificar al titular." },
            new() { Categoria = "Consentimientos informados", Finalidad = "Constancia de tu decisión antes de un procedimiento.", Acceso = "Vos, el personal clínico y el Administrador.", Conservacion = "Junto con la historia clínica.", AlEliminar = "Se conservan, sin identificar al titular." },
            new() { Categoria = "Cargos, pagos y comprobantes", Finalidad = "Registro de lo cobrado en la clínica.", Acceso = "Vos, Recepción y el Administrador.", Conservacion = "El plazo que fijen las obligaciones contables y tributarias aplicables (a confirmar).", AlEliminar = "Se conservan, sin identificar al titular." },
            new() { Categoria = "Comunicaciones enviadas y preferencias", Finalidad = "Entregarte resúmenes, avisos y comprobantes por el canal que elegiste.", Acceso = "Vos, Recepción y el Administrador.", Conservacion = "Mientras la cuenta esté activa.", AlEliminar = "Se borran." },
            new() { Categoria = "Ubicación en una asistencia a domicilio", Finalidad = "Despachar al profesional durante la asistencia (RNF-LEG-09).", Acceso = "El personal asignado.", Conservacion = "Se deja de usar al terminar la asistencia.", AlEliminar = "No aplica: no se conserva el seguimiento." },
            new() { Categoria = "Aceptaciones de términos y privacidad", Finalidad = "Probar qué versión aceptaste y cuándo (RN-43).", Acceso = "El Administrador.", Conservacion = "Mientras la cuenta esté activa.", AlEliminar = "Se reemplazan junto con la cuenta." }
        };
    }

    // RNF-LEG-02: archivo en formato abierto con los datos personales del cliente. No incluye el texto de las consultas:
    // la historia clínica de cada animal se consulta en Mi cuenta y se conserva por la normativa aplicable.
    public string Exportar(Cuenta cuenta)
    {
        var turnos = _agenda.ProximosDe(cuenta.Id).Concat(_agenda.HistorialDe(cuenta.Id)).OrderBy(t => t.Inicio)
            .Select(t => new
            {
                fecha = t.Inicio.ToString("s"),
                estado = t.Estado,
                animal = _animales.ObtenerPorId(t.AnimalId, cuenta.Id)?.Nombre ?? "",
                servicioId = t.PrestacionId
            }).ToList();

        var cargos = _cobros.CargosDe(cuenta.Id).Select(c => new
        {
            fecha = c.Fecha.ToString("s"),
            descripcion = c.Descripcion,
            monto = c.Monto,
            estado = _cobros.Estado(c),
            saldo = _cobros.Saldo(c),
            pagos = _cobros.PagosDe(c.Id).Select(p => new
            {
                numero = p.Numero,
                fecha = p.Fecha.ToString("s"),
                monto = p.Monto,
                medio = p.Medio,
                anulado = _cobros.EstaAnulado(p.Id)
            }).ToList()
        }).ToList();

        var datos = new
        {
            generado = DateTime.Now.ToString("s"),
            titular = new
            {
                nombre = cuenta.Nombre,
                apellido = cuenta.Apellido,
                correo = cuenta.Correo,
                telefono = cuenta.Telefono,
                documento = $"{cuenta.TipoDocumento} {cuenta.NumeroDocumento}".Trim(),
                domicilio = new
                {
                    calle = cuenta.Calle,
                    numero = cuenta.NumeroPuerta,
                    apartamento = cuenta.Apartamento,
                    localidad = cuenta.Localidad,
                    departamento = cuenta.Departamento,
                    referencias = cuenta.Referencias
                },
                cuentaCreada = cuenta.FechaCreacion.ToString("s")
            },
            aceptaciones = new
            {
                versionTerminos = cuenta.VersionTerminosAceptada,
                versionPrivacidad = cuenta.VersionPrivacidadAceptada,
                fecha = cuenta.FechaAceptacion.ToString("s")
            },
            animales = _animales.ObtenerDe(cuenta.Id).Select(a => new
            {
                nombre = a.Nombre,
                especie = a.Especie,
                raza = a.Raza,
                sexo = a.Sexo,
                nacimiento = a.FechaNacimiento?.ToString("yyyy-MM-dd") ?? "",
                microchip = a.Microchip,
                senasParticulares = a.SenasParticulares
            }).ToList(),
            turnos,
            cargos,
            consentimientos = _consentimientos.Respuestas().Where(r => r.ClienteId == cuenta.Id).Select(r => new
            {
                fecha = r.Fecha.ToString("s"),
                animal = _animales.ObtenerPorId(r.AnimalId, cuenta.Id)?.Nombre ?? "",
                respuesta = r.Respuesta,
                version = r.Version,
                canal = r.Canal
            }).ToList(),
            comunicaciones = _comunicaciones.DeCuenta(cuenta.Id).Select(c => new
            {
                fecha = c.FechaCreacion.ToString("s"),
                tipo = c.Tipo,
                asunto = c.Asunto,
                cuerpo = c.Cuerpo,
                canal = c.Canal,
                estado = c.Estado
            }).ToList(),
            preferenciasDeComunicacion = _comunicaciones.PreferenciasDe(cuenta.Id).Select(p => new { tipo = p.Tipo.Nombre, canal = p.Canal }).ToList(),
            solicitudesDeDatos = DeCuenta(cuenta.Id).Select(s => new { fecha = s.Fecha.ToString("s"), tipo = s.Tipo, estado = s.Estado }).ToList(),
            nota = "Este archivo no incluye el texto de las consultas. La historia clínica de tus animales se conserva y la ves en Mi cuenta."
        };

        return JsonSerializer.Serialize(datos, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
    }

    // RF-CLI-19: el pedido de exportación se atiende en el acto y queda registrado.
    public ResultadoOperacion SolicitarExportacion(Cuenta? cuenta)
    {
        if (cuenta is null || cuenta.Eliminada)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Iniciá sesión para pedir tus datos." };
        }

        _solicitudes.Add(new SolicitudDerecho
        {
            Id = ++_ultimoId,
            CuentaId = cuenta.Id,
            Tipo = Exportacion,
            Estado = Completada,
            Fecha = DateTime.Now,
            FechaResolucion = DateTime.Now,
            ResueltaPor = "Sistema"
        });

        return new ResultadoOperacion { Exito = true, Mensaje = "Tus datos están listos para descargar." };
    }

    // Motivos por los que hoy no se puede eliminar la cuenta: se atiende primero lo pendiente.
    public List<string> ObstaculosEliminacion(int cuentaId)
    {
        var obstaculos = new List<string>();

        if (_cobros.CargosDe(cuentaId).Any(c => _cobros.Saldo(c) > 0))
        {
            obstaculos.Add("Tenés cargos con saldo pendiente: se cobran en Recepción antes de eliminar la cuenta.");
        }

        if (_agenda.ProximosDe(cuentaId).Count > 0)
        {
            obstaculos.Add("Tenés turnos próximos: cancelalos o esperá a que pasen.");
        }

        if (_animales.ObtenerDe(cuentaId).Any(a => _internaciones.AbiertaDe(a.Id) is not null))
        {
            obstaculos.Add("Alguno de tus animales está internado: la cuenta se puede eliminar después del alta.");
        }

        return obstaculos;
    }

    // RF-CLI-19: pedido de eliminación. Hace falta confirmar de forma explícita y no tener pendientes.
    public ResultadoOperacion SolicitarEliminacion(Cuenta? cuenta, bool confirmado)
    {
        if (cuenta is null || cuenta.Eliminada)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Iniciá sesión para pedir la eliminación." };
        }

        if (!confirmado)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Confirmá que entendés que la eliminación no se puede deshacer." };
        }

        if (_solicitudes.Any(s => s.CuentaId == cuenta.Id && s.Tipo == Eliminacion && s.Estado == Recibida))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ya tenés un pedido de eliminación en curso." };
        }

        var obstaculos = ObstaculosEliminacion(cuenta.Id);
        if (obstaculos.Count > 0)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = obstaculos[0] };
        }

        _solicitudes.Add(new SolicitudDerecho { Id = ++_ultimoId, CuentaId = cuenta.Id, Tipo = Eliminacion, Estado = Recibida, Fecha = DateTime.Now });
        return new ResultadoOperacion { Exito = true, Mensaje = "Recibimos tu pedido. El Administrador lo resuelve y te lo muestra acá." };
    }

    // El cliente puede arrepentirse mientras nadie resolvió su pedido.
    public ResultadoOperacion Cancelar(int id, int cuentaId)
    {
        var solicitud = _solicitudes.FirstOrDefault(s => s.Id == id && s.CuentaId == cuentaId);
        if (solicitud is null || solicitud.Estado != Recibida)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ese pedido ya no se puede cancelar." };
        }

        solicitud.Estado = Cancelada;
        solicitud.FechaResolucion = DateTime.Now;
        solicitud.ResueltaPor = "Titular";
        return new ResultadoOperacion { Exito = true, Mensaje = "Cancelaste tu pedido de eliminación." };
    }

    // El Administrador aprueba (anonimiza la cuenta) o rechaza con motivo.
    public ResultadoOperacion Resolver(int id, bool aprobar, string motivo, Personal? persona)
    {
        if (persona is null || !persona.Activo || persona.Rol != PersonalService.Administrador)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo el Administrador resuelve pedidos de eliminación." };
        }

        var solicitud = _solicitudes.FirstOrDefault(s => s.Id == id);
        if (solicitud is null || solicitud.Tipo != Eliminacion || solicitud.Estado != Recibida)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ese pedido ya no está pendiente." };
        }

        if (!aprobar)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                return new ResultadoOperacion { Exito = false, Mensaje = "Escribí el motivo del rechazo." };
            }

            solicitud.Estado = Rechazada;
            solicitud.Detalle = motivo.Trim();
        }
        else
        {
            // Entre el pedido y la resolución puede haber aparecido algo pendiente.
            var obstaculos = ObstaculosEliminacion(solicitud.CuentaId);
            if (obstaculos.Count > 0)
            {
                return new ResultadoOperacion { Exito = false, Mensaje = $"No se puede aprobar: {obstaculos[0]} Podés rechazar el pedido con ese motivo." };
            }

            _comunicaciones.EliminarDe(solicitud.CuentaId);
            _cuentas.Anonimizar(solicitud.CuentaId);
            solicitud.Estado = Completada;
            solicitud.Detalle = "Datos personales reemplazados. Animales, historia clínica, cargos y consentimientos se conservan sin identificar al titular.";
        }

        solicitud.FechaResolucion = DateTime.Now;
        solicitud.ResueltaPor = persona.Nombre;
        _auditoria.Registrar(persona, BitacoraService.DatosPersonales, aprobar ? "Eliminación aprobada" : "Eliminación rechazada", $"Pedido {solicitud.Id} de la cuenta {solicitud.CuentaId}.");
        return new ResultadoOperacion { Exito = true, Mensaje = aprobar ? "Cuenta anonimizada." : "Pedido rechazado." };
    }

    public List<SolicitudDerecho> DeCuenta(int cuentaId)
    {
        return _solicitudes.Where(s => s.CuentaId == cuentaId).OrderByDescending(s => s.Id).ToList();
    }

    public List<SolicitudDerecho> Todas()
    {
        return _solicitudes.OrderByDescending(s => s.Id).ToList();
    }
}
