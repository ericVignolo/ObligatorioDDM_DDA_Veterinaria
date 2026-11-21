using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Agenda y turnos. Reglas y mensajes iguales a AgendaService de Blazor (P-RC-02, RN-01 a RN-09).
// Hora local de la clínica (la del servidor). Reservar, reprogramar y el mostrador revalidan dentro de la
// transacción con bloqueo del profesional y del animal (DT-BD-02): la consulta previa de horarios no alcanza.
public class TurnoService : ITurnoService
{
    private const string HorarioOcupado = "Ese horario ya no está disponible. Te mostramos los horarios actualizados.";

    private readonly ITurnoRepository _repository;
    private readonly TimeProvider _reloj;

    public TurnoService(ITurnoRepository repository, TimeProvider reloj)
    {
        _repository = repository;
        _reloj = reloj;
    }

    private DateTime Ahora => _reloj.GetLocalNow().DateTime;

    // ===================== Cliente =====================

    public async Task<Resultado<DisponibilidadDto>> DisponibilidadClienteAsync(int cuentaId, int profesionalId, int prestacionId, int animalId, DateOnly fecha)
    {
        var animal = await _repository.ObtenerAnimalDeClienteAsync(animalId, cuentaId);
        if (animal == null)
        {
            return AnimalNoEncontrado<DisponibilidadDto>();
        }

        var (profesional, prestacion, error) = await CombinacionAsync<DisponibilidadDto>(profesionalId, prestacionId, animal, enLinea: true);
        if (error != null)
        {
            return error;
        }

        var reglas = await ReglasAsync();
        var disponibilidad = await DisponibilidadAsync(profesional!, prestacion!, animal.Id, fecha, reglas, 0);
        disponibilidad.AvisoAusencias = await MensajeAusenciasAsync(cuentaId, reglas);
        return Resultado<DisponibilidadDto>.Ok(disponibilidad);
    }

    public async Task<Resultado<TurnoReadDto>> ReservarAsync(int cuentaId, ReservaCreateDto datos)
    {
        var inicio = datos.Inicio!.Value;
        var animal = await _repository.ObtenerAnimalDeClienteAsync(datos.AnimalId, cuentaId);
        if (animal == null)
        {
            return AnimalNoEncontrado<TurnoReadDto>();
        }

        var reglas = await ReglasAsync();

        // RN-08: con demasiadas ausencias sin aviso, el turno se pide en Recepción.
        if (await AusenciasAsync(cuentaId) >= reglas.LimiteAusencias)
        {
            return Resultado<TurnoReadDto>.Error(403, "RESERVA_POR_RECEPCION", "Reservá por Recepción", await MensajeAusenciasAsync(cuentaId, reglas));
        }

        var (profesional, prestacion, error) = await CombinacionAsync<TurnoReadDto>(datos.ProfesionalId, datos.PrestacionId, animal, enLinea: true);
        if (error != null)
        {
            return error;
        }

        return await _repository.EnTransaccionConBloqueoAsync(profesional!.Id, animal.Id, async () =>
        {
            // Revalida con el bloqueo tomado: el horario pudo ocuparse mientras el cliente elegía (CU-CLI-02, paso 5).
            if (!(await HorariosLibresAsync(profesional, prestacion!, animal.Id, DateOnly.FromDateTime(inicio), reglas, 0)).Contains(inicio))
            {
                return Resultado<TurnoReadDto>.Error(409, "HORARIO_NO_DISPONIBLE", "Horario no disponible", HorarioOcupado);
            }

            var turno = NuevoTurno(cuentaId, animal, prestacion!, profesional, inicio);
            _repository.Agregar(turno);
            await _repository.GuardarAsync();
            return new Resultado<TurnoReadDto> { Exito = true, Estado = 201, Valor = ADto(ConDatos(turno, animal, prestacion!, profesional), reglas, Ahora) };
        });
    }

    public async Task<PaginaDto<TurnoReadDto>> MisTurnosAsync(int cuentaId, bool proximos, int page, int pageSize)
    {
        var ahora = Ahora;
        var reglas = await ReglasAsync();
        var total = await _repository.ContarDeClienteAsync(cuentaId, proximos, ahora);
        var turnos = await _repository.ObtenerDeClienteAsync(cuentaId, proximos, ahora, (page - 1) * pageSize, pageSize);

        return new PaginaDto<TurnoReadDto>
        {
            Items = turnos.Select(t => ADto(t, reglas, ahora)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<Resultado<TurnoReadDto>> ObtenerMioAsync(int cuentaId, int turnoId)
    {
        var turno = await _repository.ObtenerAsync(turnoId);
        if (turno == null || turno.ClienteId != cuentaId)
        {
            return TurnoNoEncontrado();
        }

        return Resultado<TurnoReadDto>.Ok(ADto(turno, await ReglasAsync(), Ahora));
    }

    public async Task<Resultado<TurnoReadDto>> CancelarAsync(int cuentaId, int turnoId, string motivo)
    {
        var reglas = await ReglasAsync();
        var turno = await _repository.ObtenerAsync(turnoId);
        if (turno == null || turno.ClienteId != cuentaId)
        {
            return TurnoNoEncontrado();
        }

        if (turno.Estado != "Confirmado" && turno.Estado != CalculoAgenda.PendienteSena)
        {
            return Resultado<TurnoReadDto>.Error(409, "TURNO_NO_CANCELABLE", "No se puede cancelar", "Ese turno ya no se puede cancelar.");
        }

        // RN-06: el cliente cancela hasta 24 h antes; después, solo Recepción.
        if (!PuedeGestionar(turno, reglas, Ahora))
        {
            return Resultado<TurnoReadDto>.Error(409, "CANCELAR_POR_RECEPCION", "Cancelá por Recepción",
                $"Faltan menos de {reglas.CancelacionHoras} horas: la cancelación se hace por Recepción.");
        }

        Cancelar(turno, motivo, "Cliente");
        await _repository.GuardarAsync();
        return Resultado<TurnoReadDto>.Ok(ADto(turno, reglas, Ahora));
    }

    public async Task<Resultado<TurnoReadDto>> ReprogramarAsync(int cuentaId, int turnoId, DateTime nuevoInicio)
    {
        var reglas = await ReglasAsync();
        var previo = await _repository.ObtenerAsync(turnoId);
        if (previo == null || previo.ClienteId != cuentaId)
        {
            return TurnoNoEncontrado();
        }

        return await _repository.EnTransaccionConBloqueoAsync(previo.ProfesionalId, previo.AnimalId, async () =>
        {
            // Se vuelve a leer dentro de la transacción (si hubo un reintento, el seguimiento de EF se limpió).
            var turno = await _repository.ObtenerAsync(turnoId);
            if (turno == null || !PuedeGestionar(turno, reglas, Ahora))
            {
                return Resultado<TurnoReadDto>.Error(409, "TURNO_NO_REPROGRAMABLE", "No se puede reprogramar",
                    $"Este turno ya no se puede reprogramar. Si faltan menos de {reglas.CancelacionHoras} horas, consultá en Recepción.");
            }

            var profesional = await _repository.ObtenerProfesionalAsync(turno.ProfesionalId);
            var libres = await HorariosLibresAsync(profesional!, turno.Prestacion!, turno.AnimalId, DateOnly.FromDateTime(nuevoInicio), reglas, turno.Id);
            if (!libres.Contains(nuevoInicio))
            {
                return Resultado<TurnoReadDto>.Error(409, "HORARIO_NO_DISPONIBLE", "Horario no disponible",
                    "Ese horario ya no está disponible. Tu turno sigue como estaba; te mostramos los horarios actualizados.");
            }

            // Mismo turno (conserva el número), con el horario anterior guardado (A-016).
            var duracion = turno.Fin - turno.Inicio;
            turno.InicioAnterior = turno.Inicio;
            turno.Inicio = nuevoInicio;
            turno.Fin = nuevoInicio + duracion;
            turno.Version++;
            await _repository.GuardarAsync();
            return Resultado<TurnoReadDto>.Ok(ADto(turno, reglas, Ahora));
        });
    }

    // ===================== Recepción =====================

    public async Task<PaginaDto<ClienteBusquedaDto>> BuscarClientesAsync(string texto, int page, int pageSize)
    {
        var total = await _repository.ContarClientesAsync(texto);
        var clientes = await _repository.BuscarClientesAsync(texto, (page - 1) * pageSize, pageSize);
        var items = new List<ClienteBusquedaDto>();

        foreach (var c in clientes)
        {
            var animales = await _repository.ObtenerAnimalesDeClienteAsync(c.Id);
            items.Add(new ClienteBusquedaDto
            {
                Id = c.Id,
                Nombre = $"{c.Nombre} {c.Apellido}".Trim(),
                Correo = c.Correo,
                Telefono = c.Telefono,
                Documento = $"{c.TipoDocumento} {c.NumeroDocumento}".Trim(),
                AusenciasRecientes = await AusenciasAsync(c.Id),
                Animales = animales.Select(a => new AnimalResumenDto { Id = a.Id, Nombre = a.Nombre, Especie = a.Especie?.Nombre ?? "" }).ToList()
            });
        }

        return new PaginaDto<ClienteBusquedaDto> { Items = items, Page = page, PageSize = pageSize, TotalItems = total };
    }

    public async Task<Resultado<DisponibilidadDto>> DisponibilidadMostradorAsync(int clienteId, int profesionalId, int prestacionId, int animalId, DateOnly fecha)
    {
        var animal = await _repository.ObtenerAnimalDeClienteAsync(animalId, clienteId);
        if (animal == null)
        {
            return Resultado<DisponibilidadDto>.Error(404, "ANIMAL_NO_ENCONTRADO", "Animal no encontrado", "Ese animal no es de ese cliente.");
        }

        var (profesional, prestacion, error) = await CombinacionAsync<DisponibilidadDto>(profesionalId, prestacionId, animal, enLinea: false);
        if (error != null)
        {
            return error;
        }

        var reglas = await ReglasAsync();
        var disponibilidad = await DisponibilidadAsync(profesional!, prestacion!, animal.Id, fecha, reglas, 0);
        disponibilidad.AvisoAusencias = await MensajeAusenciasAsync(clienteId, reglas);
        return Resultado<DisponibilidadDto>.Ok(disponibilidad);
    }

    public async Task<List<TurnoReadDto>> AgendaDelDiaAsync(DateOnly fecha, int? profesionalId, string? estado)
    {
        var reglas = await ReglasAsync();
        var ahora = Ahora;
        return (await _repository.ObtenerDelDiaAsync(fecha, profesionalId, estado)).Select(t => ADto(t, reglas, ahora)).ToList();
    }

    // RF-REC-02 y RN-09. Sin sobreturno respeta los horarios libres; con sobreturno puede tomar un horario ya ocupado
    // del profesional o fuera de la grilla, dentro de su jornada, con motivo. El animal nunca tiene dos turnos a la vez.
    // No aplica RN-08 (lo da Recepción).
    public async Task<Resultado<TurnoReadDto>> ReservarPorMostradorAsync(int personaId, string rol, ReservaMostradorDto datos)
    {
        var inicio = datos.Inicio!.Value;
        var motivo = (datos.Motivo ?? "").Trim();

        var animal = await _repository.ObtenerAnimalDeClienteAsync(datos.AnimalId, datos.ClienteId);
        if (animal == null)
        {
            return Resultado<TurnoReadDto>.Error(404, "ANIMAL_NO_ENCONTRADO", "Animal no encontrado", "Ese animal no es de ese cliente.");
        }

        var (profesional, prestacion, error) = await CombinacionAsync<TurnoReadDto>(datos.ProfesionalId, datos.PrestacionId, animal, enLinea: false);
        if (error != null)
        {
            return error;
        }

        if (datos.Sobreturno && motivo.Length < 5)
        {
            return Resultado<TurnoReadDto>.Error(400, "MOTIVO_SOBRETURNO", "Falta el motivo", "Para un sobreturno escribí el motivo (al menos 5 caracteres).");
        }

        var reglas = await ReglasAsync();
        var fecha = DateOnly.FromDateTime(inicio);
        var fin = inicio.AddMinutes(prestacion!.DuracionMinutos);

        return await _repository.EnTransaccionConBloqueoAsync(profesional!.Id, animal.Id, async () =>
        {
            if (!datos.Sobreturno)
            {
                if (!(await HorariosLibresAsync(profesional, prestacion, animal.Id, fecha, reglas, 0)).Contains(inicio))
                {
                    return Resultado<TurnoReadDto>.Error(409, "HORARIO_NO_DISPONIBLE", "Horario no disponible",
                        "Ese horario no está libre. Elegí otro o marcá sobreturno con su motivo.");
                }
            }
            else
            {
                if (inicio < Ahora.AddMinutes(-reglas.IntervaloMinutos))
                {
                    return Resultado<TurnoReadDto>.Error(400, "SOBRETURNO_PASADO", "Horario pasado", "No se da un sobreturno en un horario que ya pasó.");
                }

                var feriados = await _repository.ObtenerFeriadosAsync(fecha, fecha);
                if (feriados.Contains(fecha) || CalculoAgenda.EstaAusente(profesional, fecha) || !CalculoAgenda.AtiendeA(profesional, inicio))
                {
                    return Resultado<TurnoReadDto>.Error(409, "FUERA_DE_JORNADA", "Fuera de la jornada",
                        $"{profesional.Nombre} no atiende en ese horario: el sobreturno tiene que caer dentro de su jornada.");
                }

                var cruzados = await _repository.ObtenerTurnosAsync(profesional.Id, animal.Id, inicio, fin);
                if (cruzados.Any(t => CalculoAgenda.Ocupa(t) && t.AnimalId == animal.Id))
                {
                    return Resultado<TurnoReadDto>.Error(409, "ANIMAL_OCUPADO", "Animal con otro turno", "Ese animal ya tiene un turno en ese horario.");
                }
            }

            var turno = NuevoTurno(datos.ClienteId, animal, prestacion, profesional, inicio);
            turno.EsSobreturno = datos.Sobreturno;
            turno.MotivoSobreturno = datos.Sobreturno ? motivo : "";
            turno.CreadoPorId = personaId;
            _repository.Agregar(turno);

            // RN-09: el sobreturno queda auditado, sin el motivo (puede ser clínico).
            if (datos.Sobreturno)
            {
                _repository.AgregarAuditoria(new Auditoria
                {
                    Fecha = Ahora,
                    CuentaId = personaId,
                    Rol = rol,
                    Accion = "Sobreturno",
                    Entidad = "Turno",
                    Origen = "API",
                    Detalle = $"Con {profesional.Nombre}, {inicio:dd/MM/yyyy HH:mm}"
                });
            }

            await _repository.GuardarAsync();
            return new Resultado<TurnoReadDto> { Exito = true, Estado = 201, Valor = ADto(ConDatos(turno, animal, prestacion, profesional), reglas, Ahora) };
        });
    }

    // Confirmado → En espera, el mismo día del turno.
    public async Task<Resultado<TurnoReadDto>> RegistrarLlegadaAsync(int turnoId)
    {
        var turno = await _repository.ObtenerAsync(turnoId);
        if (turno == null)
        {
            return TurnoNoEncontrado();
        }

        if (turno.Estado != "Confirmado")
        {
            return Resultado<TurnoReadDto>.Error(409, "ESTADO_INVALIDO", "No se puede registrar", "Solo se registra la llegada de un turno Confirmado.");
        }

        if (turno.Inicio.Date != Ahora.Date)
        {
            return Resultado<TurnoReadDto>.Error(409, "OTRO_DIA", "No se puede registrar", "La llegada se registra el mismo día del turno.");
        }

        turno.Estado = "En espera";
        turno.HoraLlegada = Ahora;
        turno.Version++;
        await _repository.GuardarAsync();
        return Resultado<TurnoReadDto>.Ok(ADto(turno, await ReglasAsync(), Ahora));
    }

    // Confirmado → Ausente, recién pasada la tolerancia. Nunca automático.
    public async Task<Resultado<TurnoReadDto>> RegistrarAusenciaAsync(int turnoId)
    {
        var reglas = await ReglasAsync();
        var turno = await _repository.ObtenerAsync(turnoId);
        if (turno == null)
        {
            return TurnoNoEncontrado();
        }

        if (turno.Estado != "Confirmado")
        {
            return Resultado<TurnoReadDto>.Error(409, "ESTADO_INVALIDO", "No se puede registrar", "Solo se marca ausente un turno Confirmado.");
        }

        if (Ahora < turno.Inicio.AddMinutes(reglas.ToleranciaAusenciaMinutos))
        {
            return Resultado<TurnoReadDto>.Error(409, "ANTES_DE_TOLERANCIA", "Todavía no",
                $"Todavía no pasaron {reglas.ToleranciaAusenciaMinutos} minutos desde la hora del turno.");
        }

        turno.Estado = "Ausente";
        turno.Version++;
        await _repository.GuardarAsync();
        return Resultado<TurnoReadDto>.Ok(ADto(turno, reglas, Ahora));
    }

    // Recepción cancela en cualquier momento (incluso dentro de las 24 h) o registra el retiro de alguien En espera.
    public async Task<Resultado<TurnoReadDto>> CancelarPorRecepcionAsync(int turnoId, string motivo)
    {
        var turno = await _repository.ObtenerAsync(turnoId);
        if (turno == null)
        {
            return TurnoNoEncontrado();
        }

        if (turno.Estado != "Confirmado" && turno.Estado != "En espera" && turno.Estado != CalculoAgenda.PendienteSena)
        {
            return Resultado<TurnoReadDto>.Error(409, "TURNO_NO_CANCELABLE", "No se puede cancelar", "Este turno ya no se puede cancelar.");
        }

        if (string.IsNullOrWhiteSpace(motivo))
        {
            return Resultado<TurnoReadDto>.Error(400, "FALTA_MOTIVO", "Falta el motivo", "Escribí el motivo de la cancelación.");
        }

        Cancelar(turno, motivo, "Recepción");
        await _repository.GuardarAsync();
        return Resultado<TurnoReadDto>.Ok(ADto(turno, await ReglasAsync(), Ahora));
    }

    // ===================== Veterinario =====================

    public async Task<Resultado<List<TurnoReadDto>>> AgendaDelVeterinarioAsync(int cuentaId, DateOnly fecha)
    {
        var profesionalId = await _repository.ObtenerProfesionalIdDeCuentaAsync(cuentaId);
        if (profesionalId == null)
        {
            return Resultado<List<TurnoReadDto>>.Error(404, "SIN_PROFESIONAL", "Sin agenda", "Tu cuenta no está asociada a un profesional activo.");
        }

        return Resultado<List<TurnoReadDto>>.Ok(await AgendaDelDiaAsync(fecha, profesionalId, null));
    }

    // ===================== Apoyo =====================

    private async Task<ReglasAgenda> ReglasAsync() => ReglasAgenda.Desde(await _repository.ObtenerParametrosAsync());

    private Task<int> AusenciasAsync(int clienteId) => _repository.ContarAusenciasAsync(clienteId, Ahora.AddMonths(-12));

    // Texto de RN-08 para el cliente: vacío si no corresponde avisar.
    private async Task<string> MensajeAusenciasAsync(int clienteId, ReglasAgenda reglas)
    {
        var ausencias = await AusenciasAsync(clienteId);
        if (ausencias >= reglas.LimiteAusencias)
        {
            return $"Tenés {ausencias} turnos sin asistir ni avisar en los últimos 12 meses: para reservar comunicate con Recepción.";
        }

        if (ausencias >= reglas.AvisoAusencias)
        {
            return $"Tenés {ausencias} turnos sin asistir ni avisar en los últimos 12 meses. Si llegás a {reglas.LimiteAusencias}, vas a tener que reservar por Recepción. Si no podés venir, cancelá con anticipación.";
        }

        return "";
    }

    // Revisa que profesional y servicio existan y vayan juntos: el profesional activo realiza el servicio y atiende
    // la especie del animal. En línea, además, el servicio se reserva en línea y el profesional está publicado.
    private async Task<(Profesional?, Prestacion?, Resultado<T>?)> CombinacionAsync<T>(int profesionalId, int prestacionId, Animal animal, bool enLinea)
    {
        var prestacion = await _repository.ObtenerPrestacionAsync(prestacionId);
        if (prestacion == null || !prestacion.Publicado || prestacion.DuracionMinutos <= 0 || (enLinea && !prestacion.ReservableEnLinea))
        {
            return (null, null, Resultado<T>.Error(400, "SERVICIO_NO_RESERVABLE", "Servicio no disponible",
                enLinea ? "Ese servicio no se reserva en línea. Consultá o llamá a la clínica." : "Elegí un servicio con turno."));
        }

        var profesional = await _repository.ObtenerProfesionalAsync(profesionalId);
        if (profesional == null || !profesional.Activo || (enLinea && !profesional.Publicado))
        {
            return (null, null, Resultado<T>.Error(400, "PROFESIONAL_NO_DISPONIBLE", "Profesional no disponible", "Elegí un profesional disponible."));
        }

        if (!profesional.Prestaciones.Any(pp => pp.PrestacionId == prestacion.Id))
        {
            return (null, null, Resultado<T>.Error(400, "PROFESIONAL_SIN_SERVICIO", "Combinación no válida", $"{profesional.Nombre} no realiza ese servicio."));
        }

        if (!profesional.Especies.Any(pe => pe.EspecieId == animal.EspecieId))
        {
            return (null, null, Resultado<T>.Error(400, "PROFESIONAL_SIN_ESPECIE", "Combinación no válida",
                $"{profesional.Nombre} no atiende {animal.Especie?.Nombre.ToLowerInvariant() ?? "esa especie"}."));
        }

        return (profesional, prestacion, null);
    }

    private async Task<List<DateTime>> HorariosLibresAsync(Profesional profesional, Prestacion prestacion, int animalId, DateOnly fecha, ReglasAgenda reglas, int excluirTurnoId)
    {
        var desde = fecha.ToDateTime(TimeOnly.MinValue);
        var ocupados = await _repository.ObtenerTurnosAsync(profesional.Id, animalId, desde, desde.AddDays(1));
        var feriados = await _repository.ObtenerFeriadosAsync(fecha, fecha);
        return CalculoAgenda.HorariosLibres(profesional, animalId, prestacion.DuracionMinutos, fecha, ocupados, feriados, reglas, Ahora, excluirTurnoId);
    }

    // Horarios del día pedido y, si no hay, el primer día con horarios (evita «sin horarios» sin salida, RF-CLI-04).
    private async Task<DisponibilidadDto> DisponibilidadAsync(Profesional profesional, Prestacion prestacion, int animalId, DateOnly fecha, ReglasAgenda reglas, int excluirTurnoId)
    {
        var ahora = Ahora;
        var primero = CalculoAgenda.PrimerDia(ahora, reglas);
        var ultimo = CalculoAgenda.UltimoDia(ahora, reglas);
        var desde = primero.ToDateTime(TimeOnly.MinValue);
        var ocupados = await _repository.ObtenerTurnosAsync(profesional.Id, animalId, desde, ultimo.ToDateTime(TimeOnly.MinValue).AddDays(1));
        var feriados = await _repository.ObtenerFeriadosAsync(primero, ultimo);

        List<DateTime> Libres(DateOnly dia)
            => CalculoAgenda.HorariosLibres(profesional, animalId, prestacion.DuracionMinutos, dia, ocupados, feriados, reglas, ahora, excluirTurnoId);

        var resultado = new DisponibilidadDto { Fecha = fecha, Horarios = Libres(fecha) };
        if (resultado.Horarios.Count == 0)
        {
            for (var dia = fecha < primero ? primero : fecha.AddDays(1); dia <= ultimo; dia = dia.AddDays(1))
            {
                if (Libres(dia).Count > 0)
                {
                    resultado.ProximoDiaConHorarios = dia;
                    break;
                }
            }
        }

        return resultado;
    }

    // Solo con los ids: si la transacción se reintenta, EF ya no sigue a esas entidades y no hay que volver a insertarlas.
    private Turno NuevoTurno(int clienteId, Animal animal, Prestacion prestacion, Profesional profesional, DateTime inicio)
    {
        return new Turno
        {
            ClienteId = clienteId,
            AnimalId = animal.Id,
            PrestacionId = prestacion.Id,
            ProfesionalId = profesional.Id,
            Inicio = inicio,
            Fin = inicio.AddMinutes(prestacion.DuracionMinutos),
            // La seña de las cirugías (RN-35) llega con el módulo de cobro (Diego, 2026-11-21): hasta entonces, Confirmado.
            Estado = "Confirmado",
            FechaCreacion = Ahora
        };
    }

    // Completa los datos de la respuesta después de guardar.
    private static Turno ConDatos(Turno turno, Animal animal, Prestacion prestacion, Profesional profesional)
    {
        turno.Animal ??= animal;
        turno.Prestacion ??= prestacion;
        turno.Profesional ??= profesional;
        return turno;
    }

    private void Cancelar(Turno turno, string motivo, string quien)
    {
        turno.Estado = "Cancelado";
        turno.MotivoCancelacion = (motivo ?? "").Trim();
        turno.FechaCancelacion = Ahora;
        turno.CanceladoPor = quien;
        turno.Version++;
    }

    // RN-06: el cliente gestiona su turno hasta 24 h antes.
    private static bool PuedeGestionar(Turno turno, ReglasAgenda reglas, DateTime ahora)
    {
        return (turno.Estado == "Confirmado" || turno.Estado == CalculoAgenda.PendienteSena)
            && turno.Inicio - ahora > TimeSpan.FromHours(reglas.CancelacionHoras);
    }

    private static TurnoReadDto ADto(Turno t, ReglasAgenda reglas, DateTime ahora)
    {
        return new TurnoReadDto
        {
            Id = t.Id,
            Estado = t.Estado,
            Inicio = t.Inicio,
            Fin = t.Fin,
            InicioAnterior = t.InicioAnterior,
            ClienteId = t.ClienteId,
            Cliente = t.Cliente == null ? "" : $"{t.Cliente.Nombre} {t.Cliente.Apellido}".Trim(),
            AnimalId = t.AnimalId,
            Animal = t.Animal?.Nombre ?? "",
            Especie = t.Animal?.Especie?.Nombre ?? "",
            PrestacionId = t.PrestacionId,
            Prestacion = t.Prestacion?.Nombre ?? "",
            ProfesionalId = t.ProfesionalId,
            Profesional = t.Profesional?.Nombre ?? "",
            EsSobreturno = t.EsSobreturno,
            HoraLlegada = t.HoraLlegada,
            MotivoCancelacion = t.MotivoCancelacion,
            CanceladoPor = t.CanceladoPor,
            PuedeGestionar = PuedeGestionar(t, reglas, ahora)
        };
    }

    private static Resultado<T> AnimalNoEncontrado<T>()
        => Resultado<T>.Error(404, "ANIMAL_NO_ENCONTRADO", "Animal no encontrado", "No encontramos ese animal entre los tuyos.");

    private static Resultado<TurnoReadDto> TurnoNoEncontrado()
        => Resultado<TurnoReadDto>.Error(404, "TURNO_NO_ENCONTRADO", "Turno no encontrado", "No encontramos ese turno.");
}
