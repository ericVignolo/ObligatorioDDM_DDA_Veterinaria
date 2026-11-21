using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Consentimiento informado, igual a ConsentimientoService de Blazor. Vale solo si la última respuesta del turno es
// «Aceptado» y es de la versión vigente del documento: si se publica una versión nueva, hay que firmar de nuevo.
// Se guarda el nombre escrito, la versión, el canal, la fecha y una huella del texto aceptado (RN-10).
public class ConsentimientoService : IConsentimientoService
{
    public const string NoRequiere = "No requiere";
    public const string Pendiente = "Pendiente";
    public const string Firmado = "Firmado";
    public const string Rechazado = "Rechazado";
    public const string VersionNueva = "Versión nueva";

    private static readonly string[] TurnoAbierto = { "Confirmado", "En espera", "Pendiente de seña" };

    private readonly ISaludRepository _repository;
    private readonly TimeProvider _reloj;

    public ConsentimientoService(ISaludRepository repository, TimeProvider reloj)
    {
        _repository = repository;
        _reloj = reloj;
    }

    private DateTime Ahora => _reloj.GetLocalNow().DateTime;

    public static string Estado(bool requiere, DocumentoConsentimiento? vigente, Consentimiento? ultima)
    {
        if (!requiere)
        {
            return NoRequiere;
        }

        if (ultima == null)
        {
            return Pendiente;
        }

        if (vigente != null && ultima.DocumentoConsentimientoId != vigente.Id)
        {
            return VersionNueva;
        }

        return ultima.Respuesta == "Aceptado" ? Firmado : Rechazado;
    }

    public async Task<Resultado<ConsentimientoEstadoDto>> EstadoAsync(int cuentaId, int turnoId)
    {
        var turno = await _repository.ObtenerTurnoAsync(turnoId);
        if (turno == null || turno.ClienteId != cuentaId)
        {
            return NoEncontrado();
        }

        return Resultado<ConsentimientoEstadoDto>.Ok(await ArmarAsync(turno));
    }

    public async Task<List<ConsentimientoEstadoDto>> PendientesAsync(int cuentaId)
    {
        var lista = new List<ConsentimientoEstadoDto>();
        foreach (var turno in await _repository.ObtenerTurnosConConsentimientoAsync(cuentaId, Ahora))
        {
            var estado = await ArmarAsync(turno);
            if (estado.Estado != Firmado)
            {
                lista.Add(estado);
            }
        }

        return lista;
    }

    public async Task<Resultado<ConsentimientoEstadoDto>> ResponderAsync(int cuentaId, int turnoId, RespuestaConsentimientoDto datos)
    {
        var turno = await _repository.ObtenerTurnoAsync(turnoId);
        if (turno == null || turno.ClienteId != cuentaId)
        {
            return NoEncontrado();
        }

        if (turno.Prestacion == null || !turno.Prestacion.RequiereConsentimiento)
        {
            return Error(400, "NO_REQUIERE_CONSENTIMIENTO", "Este turno no necesita consentimiento.");
        }

        if (!TurnoAbierto.Contains(turno.Estado) || turno.Inicio <= Ahora)
        {
            return Error(409, "TURNO_CERRADO", "Este turno ya empezó o terminó: el consentimiento no se puede cambiar.");
        }

        var documento = await _repository.ObtenerDocumentoVigenteAsync(turno.PrestacionId);
        if (documento == null)
        {
            return Error(409, "SIN_DOCUMENTO", "Todavía no hay un consentimiento publicado para este servicio. Llamá a la clínica.");
        }

        var ultima = await _repository.ObtenerUltimaRespuestaAsync(turno.Id);
        var estado = Estado(true, documento, ultima);
        var cuenta = await _repository.ObtenerCuentaAsync(cuentaId);
        var nombre = (datos.NombreCompleto ?? "").Trim();

        if (datos.Acepta)
        {
            if (nombre.Length == 0)
            {
                return Error(400, "FALTA_NOMBRE", "Marcá que leíste y aceptás, y escribí tu nombre completo.");
            }

            if (cuenta == null || !MismoNombre(nombre, $"{cuenta.Nombre} {cuenta.Apellido}"))
            {
                return Error(400, "NOMBRE_NO_COINCIDE", "El nombre no coincide con el de tu cuenta. Escribilo como figura en «Mis datos».");
            }

            if (estado == Firmado)
            {
                return Error(409, "YA_FIRMADO", "Ya firmaste este consentimiento.");
            }
        }
        else if (estado == Rechazado)
        {
            return Error(409, "YA_RECHAZADO", "Ya rechazaste este consentimiento.");
        }

        var respuesta = datos.Acepta ? "Aceptado" : "Rechazado";
        var fecha = Ahora;
        _repository.Agregar(new Consentimiento
        {
            TurnoId = turno.Id,
            ClienteId = cuentaId,
            AnimalId = turno.AnimalId,
            PrestacionId = turno.PrestacionId,
            DocumentoConsentimientoId = documento.Id,
            Version = documento.Version,
            Respuesta = respuesta,
            NombreEscrito = datos.Acepta ? nombre : "",
            Canal = "En línea",
            Fecha = fecha,
            Hash = ReglasClinicas.Huella($"{turno.Id}|{documento.Id}|{documento.Version}|{respuesta}|{nombre}|{fecha:O}|{documento.Texto}")
        });
        await _repository.GuardarAsync();

        var resultado = await ArmarAsync(turno);
        resultado.Mensaje = datos.Acepta
            ? "Consentimiento firmado. Queda guardado junto al turno."
            : "Rechazaste el consentimiento: la cirugía no se hará. Podés cancelar el turno o llamar a la clínica.";
        return Resultado<ConsentimientoEstadoDto>.Ok(resultado);
    }

    public async Task<Resultado<List<DocumentoConsentimientoReadDto>>> DocumentosAsync(int prestacionId)
    {
        var prestacion = await _repository.ObtenerPrestacionAsync(prestacionId);
        if (prestacion == null)
        {
            return Resultado<List<DocumentoConsentimientoReadDto>>.Error(404, "SERVICIO_NO_ENCONTRADO", "No encontrado", "No encontramos ese servicio.");
        }

        return Resultado<List<DocumentoConsentimientoReadDto>>.Ok((await _repository.ObtenerDocumentosAsync(prestacionId))
            .OrderByDescending(d => d.FechaVigencia).ThenByDescending(d => d.Id)
            .Select(d => DocumentoADto(d, prestacion)).ToList());
    }

    public async Task<Resultado<DocumentoConsentimientoReadDto>> PublicarAsync(DocumentoConsentimientoCreateDto datos)
    {
        var prestacion = await _repository.ObtenerPrestacionAsync(datos.PrestacionId);
        if (prestacion == null || !prestacion.RequiereConsentimiento)
        {
            return Resultado<DocumentoConsentimientoReadDto>.Error(400, "SERVICIO_SIN_CONSENTIMIENTO", "Datos no válidos", "Elegí un servicio que pida consentimiento.");
        }

        var version = datos.Version.Trim();
        var documentos = await _repository.ObtenerDocumentosAsync(prestacion.Id);
        if (documentos.Any(d => string.Equals(d.Version, version, StringComparison.OrdinalIgnoreCase)))
        {
            return Resultado<DocumentoConsentimientoReadDto>.Error(409, "VERSION_REPETIDA", "No se puede", $"Ya existe la versión {version} de este consentimiento. Indicá una versión nueva y el texto.");
        }

        foreach (var anterior in documentos.Where(d => d.Vigente))
        {
            anterior.Vigente = false;
        }

        var nuevo = new DocumentoConsentimiento
        {
            PrestacionId = prestacion.Id,
            Version = version,
            Titulo = datos.Titulo.Trim(),
            Texto = datos.Texto.Trim(),
            FechaVigencia = Ahora,
            Vigente = true
        };
        _repository.Agregar(nuevo);
        await _repository.GuardarAsync();

        return new Resultado<DocumentoConsentimientoReadDto> { Exito = true, Estado = 201, Valor = DocumentoADto(nuevo, prestacion) };
    }

    private async Task<ConsentimientoEstadoDto> ArmarAsync(Turno turno)
    {
        var requiere = turno.Prestacion?.RequiereConsentimiento == true;
        var documento = requiere ? await _repository.ObtenerDocumentoVigenteAsync(turno.PrestacionId) : null;
        var ultima = requiere ? await _repository.ObtenerUltimaRespuestaAsync(turno.Id) : null;

        return new ConsentimientoEstadoDto
        {
            TurnoId = turno.Id,
            Animal = turno.Animal?.Nombre ?? "",
            Prestacion = turno.Prestacion?.Nombre ?? "",
            Inicio = turno.Inicio,
            Estado = Estado(requiere, documento, ultima),
            DocumentoId = documento?.Id,
            Version = documento?.Version ?? "",
            Titulo = documento?.Titulo ?? "",
            Texto = documento?.Texto ?? ""
        };
    }

    private static DocumentoConsentimientoReadDto DocumentoADto(DocumentoConsentimiento d, Prestacion p) => new()
    {
        Id = d.Id,
        PrestacionId = d.PrestacionId,
        Prestacion = p.Nombre,
        Version = d.Version,
        Titulo = d.Titulo,
        Texto = d.Texto,
        FechaVigencia = d.FechaVigencia,
        Vigente = d.Vigente
    };

    // Sin importar mayúsculas, tildes ni espacios de más.
    public static bool MismoNombre(string escrito, string deLaCuenta)
        => string.Join(' ', ReglasClinicas.Normalizar(escrito).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            == string.Join(' ', ReglasClinicas.Normalizar(deLaCuenta).Split(' ', StringSplitOptions.RemoveEmptyEntries));

    private static Resultado<ConsentimientoEstadoDto> NoEncontrado()
        => Resultado<ConsentimientoEstadoDto>.Error(404, "TURNO_NO_ENCONTRADO", "No encontrado", "No encontramos ese turno entre los tuyos.");

    private static Resultado<ConsentimientoEstadoDto> Error(int estado, string codigo, string mensaje)
        => Resultado<ConsentimientoEstadoDto>.Error(estado, codigo, estado == 409 ? "No se puede" : "Datos no válidos", mensaje);
}
