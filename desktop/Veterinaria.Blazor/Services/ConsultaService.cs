using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Consultas e historia clínica de demostración en memoria (A-019, A-020, CU-VET-01).
// En la sección C lo reemplaza ConsultaApiService: la API hace iniciar y firmar en una transacción,
// con control de versión, clave idempotente, auditoría protegida, consumos de stock y cargos.
// Los insumos y las dosis de vacunas se apartan del inventario mientras la consulta está abierta y se
// descuentan al firmar, en el mismo paso (RF-VET-06, RNF-CON-03, A-022).
// Fuera de esta versión: adjuntos (RF-VET-03) y cargos (cobro de Recepción).
public class ConsultaService
{
    private readonly AgendaService _agenda;
    private readonly PersonalService _personal;
    private readonly ProfesionalService _profesionales;
    private readonly InventarioService _inventario;
    private readonly List<Consulta> _consultas = new();
    private readonly List<LecturaHistoria> _lecturas = new();
    private int _ultimoId;
    private int _ultimaEnmiendaId;

    public ConsultaService(AgendaService agenda, PersonalService personal, ProfesionalService profesionales, InventarioService inventario)
    {
        _agenda = agenda;
        _personal = personal;
        _profesionales = profesionales;
        _inventario = inventario;
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

    // Borrador cuyo turno sigue En consulta: la única consulta a la que se le agregan cosas (A-021).
    // Un borrador de turno cancelado sin atención se conserva como evidencia, pero ya no se toca.
    public bool EstaAbierta(Consulta consulta)
    {
        return consulta.Estado == "Borrador" && _agenda.ObtenerPorId(consulta.TurnoId)?.Estado == "En curso";
    }

    // RN-13: matrícula vigente del profesional para firmar.
    public bool MatriculaVigente(int profesionalId)
    {
        var profesional = _profesionales.ObtenerFicha(profesionalId);
        return profesional?.MatriculaVigenteHasta is DateOnly hasta && hasta >= DateOnly.FromDateTime(DateTime.Today);
    }

    // CU-VET-01, paso 3: crea la única consulta del turno o retoma la que ya existe (A-RC-01).
    // La consulta se obtiene después con ObtenerPorTurno.
    public ResultadoOperacion Iniciar(int turnoId, Personal? veterinario)
    {
        var turno = _agenda.ObtenerPorId(turnoId);

        if (turno is null || !EsDelTurno(veterinario, turno))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Este turno no está asignado a vos." };
        }

        if (ObtenerPorTurno(turnoId) is not null)
        {
            return new ResultadoOperacion { Exito = true, Mensaje = "Retomaste la consulta de este turno." };
        }

        if (turno.Estado != "En espera")
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo se inicia la atención de un paciente En espera (Recepción registra la llegada)." };
        }

        _agenda.IniciarAtencion(turnoId);
        var consulta = new Consulta
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

        if (!DatosValidos(datos))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Corregí los campos marcados (rangos de signos vitales o largos)." };
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

        if (!DatosValidos(datos))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Corregí los campos marcados antes de firmar. La consulta sigue en borrador." };
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

        // RN-26, RN-29: lo apartado del inventario tiene que seguir disponible.
        var errorStock = _inventario.ValidarConsumo(consulta.Id);
        if (errorStock is not null)
        {
            return errorStock;
        }

        // Desde acá no puede fallar: se aplican todos los cambios juntos (historia y stock, RNF-CON-03).
        CopiarDatos(datos, consulta);
        consulta.Version++;
        consulta.Estado = "Firmada";
        consulta.FechaFirma = DateTime.Now;
        consulta.HashContenido = Huella(Contenido(consulta));
        _agenda.Finalizar(consulta.TurnoId);
        _inventario.ConfirmarConsumo(consulta.Id, veterinario);

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

        // P-RC-04: si se registraron insumos o vacunas hubo atención; primero se quitan.
        if (_inventario.TieneConsumos(consulta.Id))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Registraste insumos o vacunas en esta consulta: si no se usaron, quitalos antes de cancelar sin atención." };
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

    // RF-VET-06: insumo usado en la consulta abierta. El lote lo elige el inventario por vencimiento (RN-27)
    // y queda apartado hasta la firma. Si no alcanza el stock, no se registra nada (RN-26).
    public ResultadoOperacion RegistrarInsumo(int consultaId, ConsumoFormulario datos, Personal? veterinario)
    {
        var consulta = ObtenerPorId(consultaId);
        if (consulta is null || !PuedeEditar(consulta, veterinario) || !EstaAbierta(consulta))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Esta consulta ya no se puede editar." };
        }

        if (!Validator.TryValidateObject(datos, new ValidationContext(datos), new List<ValidationResult>(), validateAllProperties: true))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Elegí un insumo y una cantidad entre 1 y 100." };
        }

        var articulo = _inventario.ObtenerArticulo(datos.ArticuloId);
        if (articulo is null || articulo.Tipo != "Insumo")
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Elegí un insumo. Las vacunas y antiparasitarios se registran en su sección." };
        }

        if (_inventario.ReservasDe(consultaId).Any(r => r.ArticuloId == articulo.Id && r.AplicacionId == 0))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"Ya registraste {articulo.Nombre}: quitalo y cargalo con la cantidad total." };
        }

        if (_inventario.Reservar(consultaId, articulo.Id, datos.Cantidad, 0) is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"No alcanza el stock de {articulo.Nombre}: hay {_inventario.StockDisponible(articulo.Id)} {articulo.Unidad} disponibles (RN-26)." };
        }

        return new ResultadoOperacion { Exito = true, Mensaje = $"{articulo.Nombre}: {datos.Cantidad} {articulo.Unidad} registrados. Se descuentan al firmar." };
    }

    public ResultadoOperacion QuitarInsumo(int consultaId, int articuloId, Personal? veterinario)
    {
        var consulta = ObtenerPorId(consultaId);
        if (consulta is null || !PuedeEditar(consulta, veterinario) || !EstaAbierta(consulta))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Este insumo ya no se puede quitar." };
        }

        _inventario.Liberar(consultaId, articuloId, 0);
        return new ResultadoOperacion { Exito = true, Mensaje = "Insumo quitado de la consulta." };
    }

    // Insumos de la consulta: artículo, cantidad total y lotes que se usan.
    public List<(Articulo Articulo, int Cantidad, string Lotes)> InsumosDe(int consultaId)
    {
        return _inventario.ReservasDe(consultaId)
            .Where(r => r.AplicacionId == 0)
            .GroupBy(r => r.ArticuloId)
            .Select(g => (_inventario.ObtenerArticulo(g.Key)!, g.Sum(r => r.Cantidad),
                string.Join(", ", g.Select(r => _inventario.ObtenerLote(r.LoteId)?.Numero ?? ""))))
            .ToList();
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

    // Todas las consultas abiertas: Recepción ve la lista (sin texto clínico) al final del día (P-RC-04).
    public List<Consulta> Abiertas()
    {
        return _consultas
            .Where(c => c.Estado == "Borrador" && _agenda.ObtenerPorId(c.TurnoId)?.Estado == "En curso")
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

    // El servicio no confía en la pantalla: vuelve a aplicar las DataAnnotations del formulario.
    private static bool DatosValidos(ConsultaFormulario datos)
    {
        return Validator.TryValidateObject(datos, new ValidationContext(datos), new List<ValidationResult>(), validateAllProperties: true);
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
        // Los números en cultura invariante: la huella no depende de la configuración regional.
        var inv = CultureInfo.InvariantCulture;
        return string.Join("|", c.AnimalId, c.AutorId, c.FechaFirma?.ToString("O"), c.Motivo, c.Anamnesis, c.ExamenFisico,
            c.TemperaturaC?.ToString(inv), c.FrecuenciaCardiaca, c.FrecuenciaRespiratoria, c.PesoKg?.ToString(inv), c.Alergias,
            c.Diagnostico, c.TipoDiagnostico, c.Tratamiento, c.Indicaciones);
    }

    private static string Huella(string texto)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));
    }
}
