using System.Security.Cryptography;
using System.Text;
using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Consultas e historia clínica de demostración en memoria (A-019, A-020, CU-VET-01).
// En la sección C lo reemplaza ConsultaApiService: la API hace iniciar y firmar en una transacción,
// con control de versión, clave idempotente, auditoría protegida, consumos de stock y cargos.
// Fuera de esta versión: adjuntos (RF-VET-03), insumos (A-022) y cargos (cobro de Recepción).
public class ConsultaService
{
    private readonly AgendaService _agenda;
    private readonly PersonalService _personal;
    private readonly ProfesionalService _profesionales;
    private readonly List<Consulta> _consultas = new();
    private readonly List<LecturaHistoria> _lecturas = new();
    private int _ultimoId;
    private int _ultimaEnmiendaId;

    public ConsultaService(AgendaService agenda, PersonalService personal, ProfesionalService profesionales)
    {
        _agenda = agenda;
        _personal = personal;
        _profesionales = profesionales;
        CargarHistoriaDemo();
    }

    // Consultas ya firmadas de los animales de demostración, para que la historia no aparezca vacía.
    private void CargarHistoriaDemo()
    {
        var toby = AgregarFirmadaDemo(animalId: 1, profesionalId: 1, autorId: 2, autor: "Dra. Lucía Ferreira", haceDias: 60,
            motivo: "Control anual y vacunación", diagnostico: "Paciente sano. Sobrepeso leve.", tipo: "Definitivo",
            tratamiento: "Vacuna polivalente aplicada. Dieta con 10 % menos de ración.", peso: 18.4m, alergias: "");
        toby.Enmiendas.Add(new Enmienda
        {
            Id = ++_ultimaEnmiendaId,
            ConsultaId = toby.Id,
            Motivo = "Dato omitido",
            Texto = "El responsable informó después de la consulta que tuvo una reacción leve a una pipeta antiparasitaria en 2025.",
            AutorId = 2,
            AutorNombre = "Dra. Lucía Ferreira",
            Fecha = toby.FechaFirma!.Value.AddDays(2)
        });
        toby.Enmiendas[0].HashContenido = Huella(toby.Enmiendas[0].Motivo + "|" + toby.Enmiendas[0].Texto);

        AgregarFirmadaDemo(animalId: 4, profesionalId: 3, autorId: 4, autor: "Dra. Valentina Acosta", haceDias: 30,
            motivo: "Claudicación del miembro anterior izquierdo", diagnostico: "Tendinitis leve del flexor digital superficial.",
            tipo: "Presuntivo", tratamiento: "Reposo en box 10 días, frío local dos veces por día. Control en dos semanas.",
            peso: 430m, alergias: "Penicilina");
    }

    private Consulta AgregarFirmadaDemo(int animalId, int profesionalId, int autorId, string autor, int haceDias,
        string motivo, string diagnostico, string tipo, string tratamiento, decimal peso, string alergias)
    {
        var fecha = DateTime.Today.AddDays(-haceDias).AddHours(10);
        var consulta = new Consulta
        {
            Id = ++_ultimoId,
            AnimalId = animalId,
            ProfesionalId = profesionalId,
            AutorId = autorId,
            AutorNombre = autor,
            Estado = "Firmada",
            FechaInicio = fecha,
            Motivo = motivo,
            Diagnostico = diagnostico,
            TipoDiagnostico = tipo,
            Tratamiento = tratamiento,
            PesoKg = peso,
            Alergias = alergias,
            FechaFirma = fecha.AddMinutes(25)
        };
        consulta.HashContenido = Huella(Contenido(consulta));
        _consultas.Add(consulta);
        return consulta;
    }

    public Consulta? ObtenerPorId(int id)
    {
        return _consultas.FirstOrDefault(c => c.Id == id);
    }

    public Consulta? ObtenerPorTurno(int turnoId)
    {
        return _consultas.FirstOrDefault(c => c.TurnoId == turnoId && turnoId != 0);
    }

    // El veterinario de la sesión es el profesional asignado al turno.
    private static bool EsDelTurno(Personal? veterinario, Turno turno)
    {
        return veterinario is not null
            && veterinario.Rol == PersonalService.Veterinario
            && veterinario.ProfesionalId == turno.ProfesionalId;
    }

    // Solo el autor edita su borrador (matriz de permisos, FLUJO_RESERVA_CONSULTA §5).
    public bool PuedeEditar(Consulta consulta, Personal? veterinario)
    {
        return consulta.Estado == "Borrador"
            && veterinario is not null
            && veterinario.Rol == PersonalService.Veterinario
            && veterinario.Id == consulta.AutorId;
    }

    // RN-13: matrícula vigente del profesional para firmar.
    public bool MatriculaVigente(int profesionalId)
    {
        var profesional = _profesionales.ObtenerPorId(profesionalId);
        return profesional?.MatriculaVigenteHasta is DateOnly hasta && hasta >= DateOnly.FromDateTime(DateTime.Today);
    }

    // CU-VET-01, paso 3: crea la única consulta del turno o devuelve la que ya existe (A-RC-01).
    public ResultadoOperacion Iniciar(int turnoId, Personal? veterinario, out Consulta? consulta)
    {
        consulta = null;
        var turno = _agenda.ObtenerPorId(turnoId);

        if (turno is null || !EsDelTurno(veterinario, turno))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Este turno no está asignado a vos." };
        }

        var existente = ObtenerPorTurno(turnoId);
        if (existente is not null)
        {
            consulta = existente;
            return new ResultadoOperacion { Exito = true, Mensaje = "Retomaste la consulta de este turno." };
        }

        if (turno.Estado != "En espera")
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo se inicia la atención de un paciente En espera (Recepción registra la llegada)." };
        }

        _agenda.IniciarAtencion(turnoId);
        consulta = new Consulta
        {
            Id = ++_ultimoId,
            TurnoId = turnoId,
            AnimalId = turno.AnimalId,
            ProfesionalId = turno.ProfesionalId,
            AutorId = veterinario!.Id,
            AutorNombre = veterinario.Nombre,
            FechaInicio = DateTime.Now
        };
        _consultas.Add(consulta);
        return new ResultadoOperacion { Exito = true, Mensaje = "Atención iniciada: el turno está En consulta." };
    }

    public ConsultaFormulario ParaEditar(Consulta consulta)
    {
        return new ConsultaFormulario
        {
            Motivo = consulta.Motivo,
            Anamnesis = consulta.Anamnesis,
            ExamenFisico = consulta.ExamenFisico,
            TemperaturaC = consulta.TemperaturaC,
            FrecuenciaCardiaca = consulta.FrecuenciaCardiaca,
            FrecuenciaRespiratoria = consulta.FrecuenciaRespiratoria,
            PesoKg = consulta.PesoKg,
            Alergias = consulta.Alergias,
            Diagnostico = consulta.Diagnostico,
            TipoDiagnostico = consulta.TipoDiagnostico,
            Tratamiento = consulta.Tratamiento,
            Indicaciones = consulta.Indicaciones
        };
    }

    // Guarda avances sin firmar, sin cargos y sin avisar al cliente (HU-VET-02).
    public ResultadoOperacion Guardar(int consultaId, int versionEsperada, ConsultaFormulario datos, Personal? veterinario)
    {
        var consulta = ObtenerPorId(consultaId);
        var error = ValidarEdicion(consulta, versionEsperada, veterinario);
        if (error is not null)
        {
            return error;
        }

        CopiarDatos(datos, consulta!);
        consulta!.Version++;
        return new ResultadoOperacion { Exito = true, Mensaje = $"Borrador guardado a las {DateTime.Now:HH:mm}." };
    }

    // CU-VET-01, pasos 9 a 11: valida todo antes de cambiar nada; si algo falla, el borrador queda como estaba.
    public ResultadoOperacion Firmar(int consultaId, int versionEsperada, ConsultaFormulario datos, string contrasena, Personal? veterinario)
    {
        var consulta = ObtenerPorId(consultaId);
        var error = ValidarEdicion(consulta, versionEsperada, veterinario);
        if (error is not null)
        {
            return error;
        }

        if (_personal.ValidarCredenciales(veterinario!.Correo, contrasena) is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "La contraseña no es correcta. La consulta sigue en borrador." };
        }

        if (!MatriculaVigente(consulta!.ProfesionalId))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Tu matrícula no está vigente: no podés firmar (RN-13)." };
        }

        if (string.IsNullOrWhiteSpace(datos.Motivo) || string.IsNullOrWhiteSpace(datos.Diagnostico))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Para firmar completá el motivo y al menos un diagnóstico presuntivo o definitivo." };
        }

        var turno = _agenda.ObtenerPorId(consulta.TurnoId);
        if (turno is null || turno.Estado != "En curso")
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "El turno de esta consulta ya no está En consulta." };
        }

        // Desde acá no puede fallar: se aplican todos los cambios juntos.
        CopiarDatos(datos, consulta);
        consulta.Version++;
        consulta.Estado = "Firmada";
        consulta.FechaFirma = DateTime.Now;
        consulta.HashContenido = Huella(Contenido(consulta));
        _agenda.Finalizar(consulta.TurnoId);

        return new ResultadoOperacion { Exito = true, Mensaje = "Consulta firmada y turno finalizado. El resumen al cliente y el cobro llegan con la API." };
    }

    // P-RC-04: sin atención ni consumos, el autor cancela con motivo. El borrador se conserva como evidencia.
    public ResultadoOperacion CancelarSinAtencion(int consultaId, string motivo, Personal? veterinario)
    {
        var consulta = ObtenerPorId(consultaId);
        if (consulta is null || !PuedeEditar(consulta, veterinario))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No podés cancelar esta consulta." };
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Escribí el motivo de la cancelación." };
        }

        if (!_agenda.CancelarSinAtencion(consulta.TurnoId, motivo))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "El turno ya no está En consulta." };
        }

        return new ResultadoOperacion { Exito = true, Mensaje = "Turno cancelado sin atención. El borrador queda guardado y no se publica." };
    }

    // A-020, RN-12: la enmienda es un registro nuevo, firmado con la contraseña; el original no cambia.
    public ResultadoOperacion Enmendar(int consultaId, EnmiendaFormulario datos, Personal? veterinario)
    {
        var consulta = ObtenerPorId(consultaId);
        if (consulta is null || consulta.Estado != "Firmada")
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo se enmienda una consulta firmada." };
        }

        if (veterinario is null || veterinario.Rol != PersonalService.Veterinario || veterinario.Id != consulta.AutorId)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo el veterinario que firmó la consulta puede enmendarla." };
        }

        if (string.IsNullOrWhiteSpace(datos.Motivo) || string.IsNullOrWhiteSpace(datos.Texto))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Completá el motivo y el texto de la enmienda." };
        }

        if (_personal.ValidarCredenciales(veterinario.Correo, datos.Contrasena) is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "La contraseña no es correcta." };
        }

        if (!MatriculaVigente(consulta.ProfesionalId))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Tu matrícula no está vigente: no podés firmar (RN-13)." };
        }

        var enmienda = new Enmienda
        {
            Id = ++_ultimaEnmiendaId,
            ConsultaId = consulta.Id,
            Motivo = datos.Motivo.Trim(),
            Texto = datos.Texto.Trim(),
            AutorId = veterinario.Id,
            AutorNombre = veterinario.Nombre,
            Fecha = DateTime.Now
        };
        enmienda.HashContenido = Huella(enmienda.Motivo + "|" + enmienda.Texto);
        consulta.Enmiendas.Add(enmienda);

        return new ResultadoOperacion { Exito = true, Mensaje = "Enmienda firmada. El registro original quedó intacto." };
    }

    // Historia clínica: solo consultas firmadas, de la más reciente a la más antigua (RF-CLI-05).
    // Los borradores nunca se muestran como atención finalizada (A-RC-14).
    public List<Consulta> HistoriaDe(int animalId)
    {
        return _consultas
            .Where(c => c.AnimalId == animalId && c.Estado == "Firmada")
            .OrderByDescending(c => c.FechaFirma)
            .ToList();
    }

    // Borradores de un veterinario: «consultas abiertas» para resolver al final del día (P-RC-04).
    public List<Consulta> AbiertasDe(int autorId)
    {
        return _consultas
            .Where(c => c.AutorId == autorId && c.Estado == "Borrador" && _agenda.ObtenerPorId(c.TurnoId)?.Estado == "En curso")
            .OrderBy(c => c.FechaInicio)
            .ToList();
    }

    // P-RC-05: sin turno activo, el personal necesita dar un motivo para leer la historia.
    public bool NecesitaMotivo(int animalId)
    {
        return !_agenda.TieneTurnoActivo(animalId);
    }

    // RN-40: toda lectura del personal queda registrada, sin copiar texto clínico.
    public ResultadoOperacion RegistrarLectura(Personal? persona, int animalId, string motivo)
    {
        if (persona is null || persona.Rol != PersonalService.Veterinario)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "La historia clínica completa es solo para veterinarios." };
        }

        if (NecesitaMotivo(animalId) && string.IsNullOrWhiteSpace(motivo))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Este animal no tiene un turno activo: indicá el motivo del acceso." };
        }

        _lecturas.Add(new LecturaHistoria { PersonalId = persona.Id, AnimalId = animalId, Fecha = DateTime.Now, Motivo = (motivo ?? "").Trim() });
        return new ResultadoOperacion { Exito = true, Mensaje = "" };
    }

    public List<LecturaHistoria> LecturasDe(int animalId)
    {
        return _lecturas.Where(l => l.AnimalId == animalId).OrderByDescending(l => l.Fecha).ToList();
    }

    private ResultadoOperacion? ValidarEdicion(Consulta? consulta, int versionEsperada, Personal? veterinario)
    {
        if (consulta is null || !PuedeEditar(consulta, veterinario))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Esta consulta ya no se puede editar." };
        }

        if (consulta.Version != versionEsperada)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Otra pantalla guardó cambios en esta consulta. Recargá para ver la versión actual." };
        }

        return null;
    }

    private static void CopiarDatos(ConsultaFormulario datos, Consulta consulta)
    {
        consulta.Motivo = datos.Motivo.Trim();
        consulta.Anamnesis = datos.Anamnesis.Trim();
        consulta.ExamenFisico = datos.ExamenFisico.Trim();
        consulta.TemperaturaC = datos.TemperaturaC;
        consulta.FrecuenciaCardiaca = datos.FrecuenciaCardiaca;
        consulta.FrecuenciaRespiratoria = datos.FrecuenciaRespiratoria;
        consulta.PesoKg = datos.PesoKg;
        consulta.Alergias = datos.Alergias.Trim();
        consulta.Diagnostico = datos.Diagnostico.Trim();
        consulta.TipoDiagnostico = datos.TipoDiagnostico == "Definitivo" ? "Definitivo" : "Presuntivo";
        consulta.Tratamiento = datos.Tratamiento.Trim();
        consulta.Indicaciones = datos.Indicaciones.Trim();
    }

    // Texto que se firma: todos los datos clínicos, el autor y la fecha.
    private static string Contenido(Consulta c)
    {
        return string.Join("|", c.AnimalId, c.AutorId, c.FechaFirma?.ToString("O"), c.Motivo, c.Anamnesis, c.ExamenFisico,
            c.TemperaturaC, c.FrecuenciaCardiaca, c.FrecuenciaRespiratoria, c.PesoKg, c.Alergias,
            c.Diagnostico, c.TipoDiagnostico, c.Tratamiento, c.Indicaciones);
    }

    private static string Huella(string texto)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
    }
}
