using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Consentimiento informado de demostración en memoria (A-025, CU-CLI-10, RF-CLI-11, RN-10, RN-43, P-RC-06).
// El responsable lo acepta en su cuenta marcando la casilla y escribiendo su nombre completo; se guarda la versión,
// el procedimiento, el animal, la fecha, el canal y una huella. Es independiente de Términos y Privacidad.
// No se presenta como firma electrónica avanzada. En la sección C lo reemplaza ConsentimientoApiService.
public class ConsentimientoService
{
    public const string Aceptado = "Aceptado";
    public const string Rechazado = "Rechazado";

    // Estados que muestra la pantalla.
    public const string NoRequiere = "No requiere";
    public const string Pendiente = "Pendiente";
    public const string Firmado = "Firmado";
    public const string Desactualizado = "Versión nueva";

    private readonly AgendaService _agenda;
    private readonly PrestacionService _prestaciones;
    private readonly List<DocumentoConsentimiento> _documentos = new();
    private readonly List<Consentimiento> _respuestas = new();
    private int _ultimoDocumento;
    private int _ultimaRespuesta;

    public ConsentimientoService(AgendaService agenda, PrestacionService prestaciones)
    {
        _agenda = agenda;
        _prestaciones = prestaciones;

        _documentos.Add(new DocumentoConsentimiento
        {
            Id = ++_ultimoDocumento,
            PrestacionId = 8,
            Version = "1.0",
            Titulo = "Consentimiento informado para castración con anestesia general",
            Texto = "Autorizo al equipo veterinario de VITALIA a realizar la castración del animal indicado, bajo anestesia general. "
                + "Me explicaron en qué consiste el procedimiento, los cuidados previos (ayuno de 8 horas de comida y 2 de agua) y los posteriores. "
                + "Entiendo que toda anestesia y cirugía tienen riesgos, poco frecuentes pero posibles: reacciones a los fármacos, sangrado, "
                + "infección de la herida y, excepcionalmente, complicaciones graves o la muerte. "
                + "Autorizo al veterinario a tomar las medidas necesarias si surge una complicación durante la cirugía y me comprometo a "
                + "informar cualquier enfermedad, medicación o alergia del animal. Puedo hacer preguntas antes de firmar y retirar este "
                + "consentimiento avisando a la clínica antes del procedimiento.",
            FechaVigencia = new DateTime(2026, 11, 1)
        });
    }

    // El turno es de una prestación que exige consentimiento.
    public bool Requiere(Turno turno)
    {
        return _prestaciones.ObtenerPorId(turno.PrestacionId)?.RequiereConsentimiento == true;
    }

    public DocumentoConsentimiento? DocumentoVigente(int prestacionId)
    {
        return _documentos.FirstOrDefault(d => d.PrestacionId == prestacionId && d.Vigente);
    }

    public DocumentoConsentimiento? ObtenerDocumento(int id)
    {
        return _documentos.FirstOrDefault(d => d.Id == id);
    }

    // Respuestas de un turno, de la más reciente a la más antigua (historial: nada se borra).
    public List<Consentimiento> RespuestasDe(int turnoId)
    {
        return _respuestas.Where(r => r.TurnoId == turnoId).OrderByDescending(r => r.Id).ToList();
    }

    public Consentimiento? UltimaRespuesta(int turnoId)
    {
        return RespuestasDe(turnoId).FirstOrDefault();
    }

    // RN-10: vale si la última respuesta es una aceptación de la versión vigente.
    public bool EsValido(int turnoId)
    {
        var turno = _agenda.ObtenerPorId(turnoId);
        if (turno is null)
        {
            return false;
        }

        var ultima = UltimaRespuesta(turnoId);
        var vigente = DocumentoVigente(turno.PrestacionId);
        return ultima is not null && vigente is not null && ultima.Respuesta == Aceptado && ultima.DocumentoId == vigente.Id;
    }

    public string Estado(Turno turno)
    {
        if (!Requiere(turno))
        {
            return NoRequiere;
        }

        if (EsValido(turno.Id))
        {
            return Firmado;
        }

        var ultima = UltimaRespuesta(turno.Id);
        if (ultima is null)
        {
            return Pendiente;
        }

        return ultima.Respuesta == Rechazado ? Rechazado : Desactualizado;
    }

    // Se puede responder mientras el turno no empezó.
    private static bool Abierto(Turno turno)
    {
        return turno.Estado == "Confirmado" || turno.Estado == "En espera";
    }

    // CU-CLI-10, paso 1: turnos próximos del cliente que necesitan su consentimiento.
    public List<Turno> PendientesDe(int clienteId)
    {
        return _agenda.ProximosDe(clienteId)
            .Where(t => Abierto(t) && Requiere(t) && !EsValido(t.Id))
            .ToList();
    }

    // CU-CLI-10, pasos 4 a 6. Solo el responsable del turno; casilla marcada y su nombre completo.
    public ResultadoOperacion Firmar(int turnoId, ConsentimientoFormulario datos, Cuenta? cliente)
    {
        var (turno, documento, error) = Preparar(turnoId, cliente);
        if (error is not null)
        {
            return error;
        }

        if (!Validator.TryValidateObject(datos, new ValidationContext(datos), new List<ValidationResult>(), validateAllProperties: true))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Marcá que leíste y aceptás, y escribí tu nombre completo." };
        }

        if (Normalizar(datos.NombreCompleto) != Normalizar($"{cliente!.Nombre} {cliente.Apellido}"))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "El nombre no coincide con el de tu cuenta. Escribilo como figura en «Mis datos»." };
        }

        if (EsValido(turnoId))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ya firmaste este consentimiento." };
        }

        Registrar(turno!, documento!, cliente, Aceptado, datos.NombreCompleto.Trim());
        return new ResultadoOperacion { Exito = true, Mensaje = "Consentimiento firmado. Queda guardado junto al turno." };
    }

    // CU-CLI-10, alternativa «rechazo»: queda registrado y la cirugía no se puede empezar.
    public ResultadoOperacion Rechazar(int turnoId, Cuenta? cliente)
    {
        var (turno, documento, error) = Preparar(turnoId, cliente);
        if (error is not null)
        {
            return error;
        }

        if (UltimaRespuesta(turnoId)?.Respuesta == Rechazado)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ya rechazaste este consentimiento." };
        }

        Registrar(turno!, documento!, cliente!, Rechazado, "");
        return new ResultadoOperacion { Exito = true, Mensaje = "Rechazaste el consentimiento: la cirugía no se hará. Podés cancelar el turno o llamar a la clínica." };
    }

    // Una versión nueva del texto reemplaza a la vigente; lo firmado con la anterior deja de valer (CU-CLI-10, «documento reemplazado»).
    // La pantalla de administración llega con A-027; por ahora se usa en las pruebas.
    public ResultadoOperacion PublicarVersion(int prestacionId, string version, string texto, Personal? persona)
    {
        if (persona is null || persona.Rol != PersonalService.Administrador)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo el Administrador publica versiones del consentimiento." };
        }

        var anterior = DocumentoVigente(prestacionId);
        if (anterior is null || string.IsNullOrWhiteSpace(version) || string.IsNullOrWhiteSpace(texto) || version.Trim() == anterior.Version)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Indicá una versión nueva y el texto." };
        }

        anterior.Vigente = false;
        _documentos.Add(new DocumentoConsentimiento
        {
            Id = ++_ultimoDocumento,
            PrestacionId = prestacionId,
            Version = version.Trim(),
            Titulo = anterior.Titulo,
            Texto = texto.Trim(),
            FechaVigencia = DateTime.Now
        });
        return new ResultadoOperacion { Exito = true, Mensaje = $"Versión {version.Trim()} publicada." };
    }

    private (Turno? Turno, DocumentoConsentimiento? Documento, ResultadoOperacion? Error) Preparar(int turnoId, Cuenta? cliente)
    {
        var turno = _agenda.ObtenerPorId(turnoId);

        // RN-18: si no es su turno no se distingue de que no exista.
        if (cliente is null || turno is null || turno.ClienteId != cliente.Id)
        {
            return (null, null, new ResultadoOperacion { Exito = false, Mensaje = "No encontramos ese turno entre los tuyos." });
        }

        if (!Requiere(turno))
        {
            return (null, null, new ResultadoOperacion { Exito = false, Mensaje = "Este turno no necesita consentimiento." });
        }

        if (!Abierto(turno))
        {
            return (null, null, new ResultadoOperacion { Exito = false, Mensaje = "Este turno ya empezó o terminó: el consentimiento no se puede cambiar." });
        }

        var documento = DocumentoVigente(turno.PrestacionId);
        if (documento is null)
        {
            return (null, null, new ResultadoOperacion { Exito = false, Mensaje = "El documento no está disponible. Consultá en la clínica." });
        }

        return (turno, documento, null);
    }

    private void Registrar(Turno turno, DocumentoConsentimiento documento, Cuenta cliente, string respuesta, string nombre)
    {
        var fecha = DateTime.Now;
        _respuestas.Add(new Consentimiento
        {
            Id = ++_ultimaRespuesta,
            TurnoId = turno.Id,
            ClienteId = cliente.Id,
            AnimalId = turno.AnimalId,
            PrestacionId = turno.PrestacionId,
            DocumentoId = documento.Id,
            Version = documento.Version,
            Respuesta = respuesta,
            NombreEscrito = nombre,
            Canal = "En línea",
            Fecha = fecha,
            Hash = Huella(string.Join("|", turno.Id, cliente.Id, turno.AnimalId, turno.PrestacionId, documento.Version, documento.Texto, respuesta, nombre, fecha.ToString("O")))
        });
    }

    // Sin mayúsculas ni espacios de más, para comparar el nombre escrito con el de la cuenta.
    private static string Normalizar(string texto)
    {
        return string.Join(' ', (texto ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
    }

    private static string Huella(string texto)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
    }
}
