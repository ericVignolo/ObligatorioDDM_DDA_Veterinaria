using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Asistencia, igual a AsistenciaService de Blazor (decisiones D-ASI-01 a 11 de la especificación).
// Recepción gestiona la cola y asigna; el Veterinario acepta o rechaza, cambia el nivel con motivo y avanza el
// traslado. Los rojos se ofrecen a la vez a los elegibles y a la guardia; si nadie acepta en 10 minutos pasan a
// Recepción. Cada cambio sube la versión: dos aceptaciones a la vez no se pisan (la segunda da 409).
public class AsistenciaService : IAsistenciaService
{
    public const string Rojo = "Rojo";
    public const string Naranja = "Naranja";
    public const string Amarillo = "Amarillo";
    public const string Verde = "Verde";
    public static readonly string[] Niveles = { Rojo, Naranja, Amarillo, Verde };

    public const string Visita = "Visita";
    public const string Orientacion = "Orientación y traslado";
    public static readonly string[] Modalidades = { Visita, Orientacion };

    public const string Pendiente = "Pendiente de asignación";
    public const string Ofrecida = "Ofrecida";
    public const string Coordinacion = "Requiere coordinación de Recepción";
    public const string Aceptada = "Aceptada";
    public const string EnCamino = "En camino";
    public const string EnElLugar = "En el lugar";
    public const string EnAtencion = "En atención";
    public const string Finalizada = "Finalizada";
    public const string CanceladaClinica = "Cancelada por la clínica";
    public const string NoConcretada = "No concretada";

    // Parámetros por defecto (Eric, 2026-10-31; D-ASI-02 y D-ASI-03). El Administrador los cambia en Parametros.
    public const int CoberturaPorDefecto = 30;
    public const int MinutosRojoPorDefecto = 10;
    public const int HorasParaReevaluar = 24;
    public const decimal TarifaBasePorDefecto = 1500;
    public const decimal TarifaKmPorDefecto = 40;
    public const decimal TarifaAnimalExtraPorDefecto = 300;
    public const int GuardiaPorDefecto = 2; // Dra. Ferreira, como en Blazor
    public const string ClaveGuardia = "asistencia.guardia_cuenta_id";

    private readonly IAsistenciaRepository _repository;
    private readonly TimeProvider _reloj;

    public AsistenciaService(IAsistenciaRepository repository, TimeProvider reloj)
    {
        _repository = repository;
        _reloj = reloj;
    }

    private DateTime Ahora => _reloj.GetLocalNow().DateTime;

    // ===================== Reglas puras =====================

    public static int Prioridad(string nivel)
    {
        var i = Array.IndexOf(Niveles, nivel);
        return i < 0 ? Niveles.Length : i;
    }

    public static string Descripcion(string nivel) => nivel switch
    {
        Rojo => "Posible riesgo vital: guardia inmediata",
        Naranja => "Urgencia alta: en el horario de la clínica, primero",
        Amarillo => "Prioritaria: tolera una espera controlada",
        _ => "Estable o programable"
    };

    public static bool EstaActiva(SolicitudAsistencia s) => s.Estado != Finalizada && s.Estado != CanceladaClinica && s.Estado != NoConcretada;

    public static bool SinAceptar(SolicitudAsistencia s) => s.Estado == Pendiente || s.Estado == Ofrecida || s.Estado == Coordinacion;

    // RN-48: lun–vie 9–19 y sáb 9–13.
    public static bool EnHorarioClinica(DateTime momento)
    {
        var hora = momento.TimeOfDay;
        return momento.DayOfWeek switch
        {
            DayOfWeek.Sunday => false,
            DayOfWeek.Saturday => hora >= TimeSpan.FromHours(9) && hora < TimeSpan.FromHours(13),
            _ => hora >= TimeSpan.FromHours(9) && hora < TimeSpan.FromHours(19)
        };
    }

    public static string? SiguienteEstado(SolicitudAsistencia s) => s.Estado switch
    {
        Aceptada => s.Modalidad == Visita ? EnCamino : EnAtencion,
        EnCamino => EnElLugar,
        EnElLugar => EnAtencion,
        _ => null
    };

    private record Parametros(int Cobertura, int MinutosRojo, decimal Base, decimal PorKm, decimal PorAnimal, int GuardiaId);

    private async Task<Parametros> LeerParametrosAsync()
    {
        var p = await _repository.ObtenerParametrosAsync();
        int Entero(string clave, int defecto) => p.TryGetValue(clave, out var v) && int.TryParse(v, out var n) && n >= 0 ? n : defecto;
        return new Parametros(
            Entero("asistencia.cobertura_km", CoberturaPorDefecto),
            Entero("asistencia.escalado_rojo_minutos", MinutosRojoPorDefecto),
            Entero("asistencia.tarifa_base", (int)TarifaBasePorDefecto),
            Entero("asistencia.tarifa_km", (int)TarifaKmPorDefecto),
            Entero("asistencia.tarifa_animal_extra", (int)TarifaAnimalExtraPorDefecto),
            Entero(ClaveGuardia, GuardiaPorDefecto));
    }

    // RN-58: base + distancia + animales extra; la orientación no tiene costo.
    private static decimal Costo(Parametros p, string modalidad, int km, int cantidad)
        => modalidad == Visita ? p.Base + p.PorKm * km + p.PorAnimal * Math.Max(0, cantidad - 1) : 0;

    // ===================== Creación =====================

    public async Task<Resultado<SolicitudAsistenciaReadDto>> CrearDesdeAppAsync(int cuentaId, SolicitudAppDto datos)
    {
        if (datos.UbicacionGps && (datos.Latitud == null || datos.Longitud == null))
        {
            return Error(400, "GPS_INCOMPLETO", "Si compartís el GPS, faltan las coordenadas. Si no, escribí la dirección.");
        }

        return await CrearAsync(cuentaId, datos, "App", cuentaId, datos.UbicacionGps ? datos.Latitud : null, datos.UbicacionGps ? datos.Longitud : null);
    }

    public Task<Resultado<SolicitudAsistenciaReadDto>> CrearTelefonicaAsync(int personaId, SolicitudTelefonicaDto datos)
        => CrearAsync(datos.ClienteId, datos, "Teléfono", personaId, null, null);

    private async Task<Resultado<SolicitudAsistenciaReadDto>> CrearAsync(int clienteId, SolicitudAsistenciaBaseDto datos, string canal, int autorId, decimal? lat, decimal? lon)
    {
        var clave = string.IsNullOrWhiteSpace(datos.ClaveIdempotencia) ? null : datos.ClaveIdempotencia.Trim();
        if (clave != null)
        {
            var repetida = await _repository.ObtenerPorClaveAsync(clienteId, clave);
            if (repetida != null)
            {
                var dto = await DetalleDtoAsync(repetida, paraPersonal: canal == "Teléfono");
                dto.Mensaje = $"La solicitud n.º {repetida.Id} ya estaba registrada.";
                return Resultado<SolicitudAsistenciaReadDto>.Ok(dto);
            }
        }

        if (!Niveles.Contains(datos.Nivel) || !Modalidades.Contains(datos.Modalidad))
        {
            return Error(400, "DATOS_INVALIDOS", "Corregí los campos marcados: cliente, síntomas, nivel, modalidad, cantidad y teléfono.");
        }

        if (await _repository.ObtenerClienteAsync(clienteId) == null)
        {
            return Error(404, "CLIENTE_NO_ENCONTRADO", "Ese cliente no existe.");
        }

        // Animal registrado: la especie sale de su ficha y tiene que ser de ese cliente.
        string especie;
        if (datos.AnimalId is int animalId && animalId > 0)
        {
            var animal = await _repository.ObtenerAnimalAsync(animalId);
            if (animal == null || !await _repository.EsResponsableAsync(animalId, clienteId))
            {
                return Error(404, "ANIMAL_AJENO", "Ese animal no es de este cliente.");
            }

            especie = animal.Especie?.Nombre ?? "";
        }
        else
        {
            especie = datos.EspecieId is int especieId ? (await _repository.ObtenerEspecieAsync(especieId))?.Nombre ?? "" : "";
        }

        if (string.IsNullOrWhiteSpace(especie))
        {
            return Error(400, "FALTA_ESPECIE", "Indicá la especie del animal o del grupo.");
        }

        // RN-56: una visita necesita destino.
        if (datos.Modalidad == Visita && string.IsNullOrWhiteSpace(datos.Direccion) && lat == null)
        {
            return Error(400, "VISITA_SIN_DESTINO", "Una visita necesita la dirección. Sin ubicación se puede dar orientación o coordinar el traslado (RN-56).");
        }

        var p = await LeerParametrosAsync();
        var ahora = Ahora;
        var km = datos.Modalidad == Visita ? datos.DistanciaKm : 0;
        var s = new SolicitudAsistencia
        {
            ClienteId = clienteId,
            AnimalId = datos.AnimalId is > 0 ? datos.AnimalId : null,
            Especie = especie,
            Cantidad = datos.Cantidad,
            Sintomas = datos.Sintomas.Trim(),
            Modalidad = datos.Modalidad,
            Direccion = (datos.Direccion ?? "").Trim(),
            Referencias = (datos.Referencias ?? "").Trim(),
            UbicacionGps = lat != null,
            Latitud = lat,
            Longitud = lon,
            DistanciaKm = km,
            TelefonoContacto = datos.TelefonoContacto.Trim(),
            Canal = canal,
            ClaveIdempotencia = clave,
            // RN-46 y D-ASI-01: sin cuestionario aprobado, el nivel preliminar es el que indica el cliente.
            NivelCliente = datos.Nivel,
            Nivel = datos.Nivel,
            Estado = Pendiente,
            FechaCreacion = ahora,
            Costo = Costo(p, datos.Modalidad, km, datos.Cantidad)
        };
        _repository.Agregar(s);
        Evento(s, ahora, "Recibida", $"{(canal == "App" ? "Desde la app" : "Por teléfono")}. Nivel indicado: {s.Nivel}. Costo informado: $ {s.Costo:N0}.", autorId);

        if (s.Modalidad == Visita && s.DistanciaKm > p.Cobertura)
        {
            AEscalar(s, ahora, $"Fuera de cobertura ({s.DistanciaKm} km, máximo {p.Cobertura} km): la visita no está confirmada.");
        }
        else if (s.Nivel == Rojo)
        {
            await ActivarGuardiaAsync(s, ahora, p);
        }

        await _repository.GuardarAsync();
        var resultado = await DetalleDtoAsync(s, paraPersonal: canal == "Teléfono");
        resultado.Mensaje = $"Solicitud n.º {s.Id} registrada ({s.Nivel}, {s.Estado}).";
        return new Resultado<SolicitudAsistenciaReadDto> { Exito = true, Estado = 201, Valor = resultado };
    }

    // ===================== Elegibles, guardia y escalamiento =====================

    // RN-54, sección 12: matrícula vigente y especie; para rojo, además, de guardia o en su jornada en este momento.
    private async Task<List<(Profesional Ficha, bool DeGuardia)>> ElegiblesDeAsync(SolicitudAsistencia s, DateTime ahora, Parametros p)
    {
        var resultado = new List<(Profesional, bool)>();
        foreach (var ficha in await _repository.ObtenerVeterinariosAsync())
        {
            if (ficha.MatriculaVigenteHasta is not DateTime hasta || hasta.Date < ahora.Date)
            {
                continue;
            }

            if (!ficha.Especies.Any(e => e.Especie?.Nombre == s.Especie))
            {
                continue;
            }

            var deGuardia = ficha.CuentaId == p.GuardiaId;
            if (s.Nivel == Rojo && !deGuardia && !TrabajaAhora(ficha, ahora))
            {
                continue;
            }

            resultado.Add((ficha, deGuardia));
        }

        return resultado;
    }

    private static bool TrabajaAhora(Profesional ficha, DateTime ahora)
    {
        var hora = ahora.TimeOfDay;
        return ficha.Franjas.Any(f => f.Tipo == "Jornada" && f.Dia == ahora.DayOfWeek && hora >= f.Inicio && hora < f.Fin);
    }

    // RN-49: el rojo se ofrece enseguida a todos los elegibles; sin nadie, a Recepción (RN-55).
    private async Task ActivarGuardiaAsync(SolicitudAsistencia s, DateTime ahora, Parametros p)
    {
        var elegibles = await ElegiblesDeAsync(s, ahora, p);
        if (elegibles.Count == 0)
        {
            AEscalar(s, ahora, "Rojo sin veterinario elegible de guardia o en jornada.");
            return;
        }

        foreach (var (ficha, _) in elegibles)
        {
            Ofrecer(s, ficha.CuentaId!.Value, ahora);
        }

        s.Estado = Ofrecida;
        s.FechaOferta = ahora;
        Evento(s, ahora, "Ofrecida", "Ofrecida a " + string.Join(", ", elegibles.Select(e => e.Ficha.Nombre)) + ".", null);
    }

    // Una oferta por veterinario: si ya la tuvo (la rechazó o venció), se vuelve a abrir.
    private void Ofrecer(SolicitudAsistencia s, int cuentaId, DateTime ahora)
    {
        var oferta = s.Ofertas.FirstOrDefault(o => o.VeterinarioId == cuentaId);
        if (oferta == null)
        {
            s.Ofertas.Add(new OfertaAsistencia { SolicitudAsistencia = s, VeterinarioId = cuentaId, Fecha = ahora, Respuesta = "Pendiente" });
        }
        else
        {
            oferta.Respuesta = "Pendiente";
            oferta.Fecha = ahora;
        }
    }

    private static IEnumerable<OfertaAsistencia> Abiertas(SolicitudAsistencia s) => s.Ofertas.Where(o => o.Respuesta == "Pendiente");

    private void AEscalar(SolicitudAsistencia s, DateTime ahora, string motivo)
    {
        foreach (var o in Abiertas(s).ToList())
        {
            o.Respuesta = "Vencida";
        }

        s.Estado = Coordinacion;
        s.FechaOferta = null;
        s.MotivoCoordinacion = motivo;
        Evento(s, ahora, "Escalada a Recepción", motivo, null);
    }

    // Rojos sin aceptar en 10 minutos → Recepción (RN-55); 24 horas sin aceptar → reevaluación (RN-50).
    public async Task<PlazosRevisadosDto> RevisarPlazosAsync()
    {
        var ahora = Ahora;
        var p = await LeerParametrosAsync();
        var resultado = new PlazosRevisadosDto();
        foreach (var s in await _repository.ObtenerActivasAsync())
        {
            var cambio = false;
            if (s.Nivel == Rojo && s.Estado == Ofrecida && s.FechaOferta is DateTime oferta && ahora - oferta >= TimeSpan.FromMinutes(p.MinutosRojo))
            {
                AEscalar(s, ahora, $"Nadie aceptó el rojo en {p.MinutosRojo} minutos: contactar al cliente.");
                resultado.RojosEscalados++;
                cambio = true;
            }

            if (SinAceptar(s) && !s.ReevaluacionPedida && ahora - s.FechaCreacion >= TimeSpan.FromHours(HorasParaReevaluar))
            {
                // Sin respuesta no se cierra ni se baja el nivel.
                s.ReevaluacionPedida = true;
                Evento(s, ahora, "Reevaluación pedida", "Pasaron 24 horas: se pregunta al cliente si el estado se mantuvo o empeoró.", null);
                resultado.ReevaluacionesPedidas++;
                cambio = true;
            }

            if (cambio)
            {
                s.Version++;
            }
        }

        await _repository.GuardarAsync();
        return resultado;
    }

    // ===================== Recepción =====================

    // Activas, rojo → naranja → amarillo → verde y, dentro del nivel, por antigüedad (RN-47, RN-48).
    public async Task<List<SolicitudAsistenciaReadDto>> ColaAsync()
    {
        await RevisarPlazosAsync();
        return (await _repository.ObtenerActivasAsync())
            .OrderBy(s => Prioridad(s.Nivel)).ThenBy(s => s.FechaCreacion).ThenBy(s => s.Id)
            .Select(s => ADto(s, paraPersonal: true))
            .ToList();
    }

    public async Task<List<SolicitudAsistenciaReadDto>> CerradasAsync()
        => (await _repository.ObtenerCerradasAsync(50)).Select(s => ADto(s, paraPersonal: true)).ToList();

    public async Task<Resultado<SolicitudAsistenciaReadDto>> DetalleAsync(int id)
    {
        var s = await _repository.ObtenerAsync(id);
        return s == null ? NoEncontrada() : Resultado<SolicitudAsistenciaReadDto>.Ok(await DetalleDtoAsync(s, paraPersonal: true));
    }

    public async Task<Resultado<List<VeterinarioElegibleDto>>> ElegiblesAsync(int id)
    {
        var s = await _repository.ObtenerAsync(id);
        if (s == null)
        {
            return Resultado<List<VeterinarioElegibleDto>>.Error(404, "SOLICITUD_NO_ENCONTRADA", "No encontrado", "Esa solicitud no existe.");
        }

        var elegibles = await ElegiblesDeAsync(s, Ahora, await LeerParametrosAsync());
        return Resultado<List<VeterinarioElegibleDto>>.Ok(elegibles
            .Select(e => new VeterinarioElegibleDto { CuentaId = e.Ficha.CuentaId!.Value, Nombre = e.Ficha.Nombre, DeGuardia = e.DeGuardia })
            .ToList());
    }

    // RF-REC-08: Recepción ofrece una solicitud a un veterinario elegible.
    public async Task<Resultado<SolicitudAsistenciaReadDto>> AsignarAsync(int personaId, int id, int veterinarioId)
    {
        var s = await _repository.ObtenerAsync(id);
        if (s == null)
        {
            return NoEncontrada();
        }

        if (s.Estado != Pendiente && s.Estado != Coordinacion)
        {
            return Error(409, "NO_ASIGNABLE", "Esta solicitud ya está ofrecida, aceptada o cerrada.");
        }

        var ahora = Ahora;
        var veterinario = (await ElegiblesDeAsync(s, ahora, await LeerParametrosAsync())).FirstOrDefault(e => e.Ficha.CuentaId == veterinarioId).Ficha;
        if (veterinario == null)
        {
            return Error(400, "NO_ELEGIBLE", "Ese veterinario no es elegible para esta especie o disponibilidad (RN-54).");
        }

        Ofrecer(s, veterinarioId, ahora);
        s.Estado = Ofrecida;
        s.FechaOferta = ahora;
        s.Version++;
        Evento(s, ahora, "Ofrecida", $"Recepción la ofreció a {veterinario.Nombre}.", personaId);
        await _repository.GuardarAsync();
        return Con(s, $"Ofrecida a {veterinario.Nombre}. Queda esperando que acepte.");
    }

    public async Task<Resultado<SolicitudAsistenciaReadDto>> EmpeoramientoTelefonicoAsync(int personaId, int id, CambioNivelDto datos)
    {
        var s = await _repository.ObtenerAsync(id);
        return s == null ? NoEncontrada() : await RegistrarEmpeoramientoAsync(s, datos, personaId, "(por teléfono)");
    }

    // Recepción cierra sin atención. Un rojo solo después de escalar a Recepción (sección 6).
    public async Task<Resultado<SolicitudAsistenciaReadDto>> CerrarAsync(int personaId, int id, CierreAsistenciaDto datos)
    {
        var s = await _repository.ObtenerAsync(id);
        if (s == null)
        {
            return NoEncontrada();
        }

        if (!(SinAceptar(s) || s.Estado == Aceptada))
        {
            return Error(409, "NO_CERRABLE", "Solo se cierra antes de que el veterinario salga.");
        }

        if (datos.Estado != CanceladaClinica && datos.Estado != NoConcretada)
        {
            return Error(400, "CIERRE_INVALIDO", "Elegí cómo se cierra.");
        }

        if (s.Nivel == Rojo && s.Estado != Coordinacion)
        {
            return Error(409, "ROJO_SIN_ESCALAR", "Un rojo se cierra solo después de pasar a Recepción y contactar al cliente.");
        }

        var ahora = Ahora;
        foreach (var o in Abiertas(s).ToList())
        {
            o.Respuesta = "Vencida";
        }

        s.Estado = datos.Estado;
        s.MotivoCierre = datos.Motivo.Trim();
        s.Version++;
        Evento(s, ahora, datos.Estado, s.MotivoCierre, personaId);
        await _repository.GuardarAsync();
        return Con(s, $"Solicitud cerrada: {datos.Estado}.");
    }

    // ===================== Veterinario =====================

    // Ofrecidas a él y las que aceptó y siguen activas.
    public async Task<List<SolicitudAsistenciaReadDto>> DeVeterinarioAsync(int cuentaId)
    {
        await RevisarPlazosAsync();
        return (await _repository.ObtenerActivasAsync())
            .Where(s => (s.Estado == Ofrecida && Abiertas(s).Any(o => o.VeterinarioId == cuentaId)) || s.VeterinarioId == cuentaId)
            .OrderBy(s => Prioridad(s.Nivel)).ThenBy(s => s.FechaCreacion)
            .Select(s => ADto(s, paraPersonal: true))
            .ToList();
    }

    // RF-VET-16: el primero que acepta se la queda. Repetir la aceptación no duplica nada.
    public async Task<Resultado<SolicitudAsistenciaReadDto>> AceptarAsync(int cuentaId, int id)
    {
        var s = await _repository.ObtenerAsync(id);
        if (s == null)
        {
            return NoEncontrada();
        }

        if (s.VeterinarioId == cuentaId && !SinAceptar(s))
        {
            return Con(s, "Ya la habías aceptado.");
        }

        if (s.Estado != Ofrecida || !Abiertas(s).Any(o => o.VeterinarioId == cuentaId))
        {
            return Error(409, "NO_OFRECIDA", "Esta solicitud ya no está ofrecida a vos.");
        }

        var ahora = Ahora;
        foreach (var o in Abiertas(s).ToList())
        {
            o.Respuesta = o.VeterinarioId == cuentaId ? "Aceptada" : "Vencida";
        }

        s.Estado = Aceptada;
        s.VeterinarioId = cuentaId;
        s.FechaAceptacion = ahora;
        // RN-53 y D-ASI-10: la estimación recién ahora; con el mapa la calcula el proveedor.
        s.EtaMinutos = s.Modalidad == Visita ? 15 + s.DistanciaKm * 2 : null;
        s.Version++;
        Evento(s, ahora, "Aceptada", s.EtaMinutos == null ? "Orientación: el veterinario contacta al cliente." : $"Llegada estimada en {s.EtaMinutos} minutos.", cuentaId);
        await _repository.GuardarAsync();
        return Con(s, "Aceptaste la asistencia.");
    }

    // El rechazo libera la oferta; si nadie más la tiene, vuelve a Recepción.
    public async Task<Resultado<SolicitudAsistenciaReadDto>> RechazarAsync(int cuentaId, int id, string motivo)
    {
        var s = await _repository.ObtenerAsync(id);
        var oferta = s == null ? null : Abiertas(s).FirstOrDefault(o => o.VeterinarioId == cuentaId);
        if (s == null || s.Estado != Ofrecida || oferta == null)
        {
            return Error(409, "NO_OFRECIDA", "Esta solicitud no está ofrecida a vos.");
        }

        var ahora = Ahora;
        oferta.Respuesta = "Rechazada";
        Evento(s, ahora, "Rechazada", motivo.Trim(), cuentaId);
        if (!Abiertas(s).Any())
        {
            if (s.Nivel == Rojo)
            {
                AEscalar(s, ahora, "Todos los veterinarios ofrecidos rechazaron el rojo.");
            }
            else
            {
                s.Estado = Pendiente;
                s.FechaOferta = null;
            }
        }

        s.Version++;
        await _repository.GuardarAsync();
        return Con(s, "Rechazo registrado.");
    }

    // RF-VET-17: estados del traslado. La orientación no tiene traslado.
    public async Task<Resultado<SolicitudAsistenciaReadDto>> AvanzarAsync(int cuentaId, int id)
    {
        var s = await _repository.ObtenerAsync(id);
        if (s == null || s.VeterinarioId != cuentaId)
        {
            return Error(403, "NO_ES_SUYA", "Solo el veterinario que aceptó actualiza el estado.");
        }

        var siguiente = SiguienteEstado(s);
        if (siguiente == null)
        {
            return Error(409, "NO_AVANZA", s.Estado == EnAtencion ? "Para terminar, registrá el resultado y finalizá." : "Esta solicitud no se puede avanzar.");
        }

        s.Estado = siguiente;
        s.Version++;
        Evento(s, Ahora, siguiente, siguiente == EnCamino ? "El cliente ve la ubicación del veterinario hasta la llegada (RN-57)." : "", cuentaId);
        await _repository.GuardarAsync();
        return Con(s, $"Estado: {siguiente}.");
    }

    // Termina la asistencia, deja de compartir ubicación y genera el cargo (RN-58). Finalizar dos veces no repite.
    public async Task<Resultado<SolicitudAsistenciaReadDto>> FinalizarAsync(int cuentaId, int id, string resultado)
    {
        var s = await _repository.ObtenerAsync(id);
        if (s == null || s.VeterinarioId != cuentaId)
        {
            return Error(403, "NO_ES_SUYA", "Solo el veterinario que aceptó finaliza la asistencia.");
        }

        if (s.Estado == Finalizada)
        {
            return Con(s, "La asistencia ya estaba finalizada.");
        }

        if (s.Estado != EnAtencion)
        {
            return Error(409, "NO_EN_ATENCION", "Se finaliza después de estar «En atención».");
        }

        var ahora = Ahora;
        s.Estado = Finalizada;
        s.Resultado = resultado.Trim();
        s.Version++;
        Evento(s, ahora, "Finalizada", s.Resultado, cuentaId);

        var mensaje = "Asistencia finalizada.";
        if (s.Costo > 0 && !await _repository.ExisteCargoAsync(s.Id))
        {
            _repository.Agregar(new Cargo
            {
                SolicitudAsistencia = s,
                ClienteId = s.ClienteId,
                AnimalId = s.AnimalId,
                Descripcion = $"Asistencia a domicilio ({s.Especie}, {s.DistanciaKm} km)",
                Monto = ReglasClinicas.RedondearPesos(s.Costo),
                Fecha = ahora
            });
            mensaje = $"Asistencia finalizada. Cargo de $ {s.Costo:N0} pendiente en Recepción.";
        }

        await _repository.GuardarAsync();
        return Con(s, mensaje);
    }

    // RN-52: un veterinario confirma o cambia el nivel con motivo; puede bajarlo.
    public async Task<Resultado<SolicitudAsistenciaReadDto>> CambiarNivelAsync(int cuentaId, int id, CambioNivelDto datos)
    {
        var s = await _repository.ObtenerAsync(id);
        if (s == null || !EstaActiva(s))
        {
            return Error(409, "CERRADA", "Esta solicitud ya está cerrada.");
        }

        if (!Niveles.Contains(datos.Nivel))
        {
            return Error(400, "NIVEL_INVALIDO", "Elegí el nivel y escribí el motivo.");
        }

        var ahora = Ahora;
        if (datos.Nivel == s.Nivel)
        {
            Evento(s, ahora, "Nivel confirmado", datos.Motivo.Trim(), cuentaId);
            await _repository.GuardarAsync();
            return Con(s, $"Nivel {s.Nivel} confirmado.");
        }

        await AplicarNivelAsync(s, datos.Nivel, "Nivel cambiado por el veterinario", datos.Motivo.Trim(), cuentaId, ahora);
        await _repository.GuardarAsync();
        return Con(s, $"Nivel cambiado a {s.Nivel}.");
    }

    // ===================== Cliente =====================

    public async Task<List<SolicitudAsistenciaReadDto>> MiasAsync(int cuentaId)
        => (await _repository.ObtenerDeClienteAsync(cuentaId)).Select(s => ADto(s, paraPersonal: false)).ToList();

    public async Task<Resultado<SolicitudAsistenciaReadDto>> MiaAsync(int cuentaId, int id)
    {
        var s = await _repository.ObtenerAsync(id);
        if (s == null || s.ClienteId != cuentaId)
        {
            return Error(404, "SOLICITUD_NO_ENCONTRADA", "No encontramos esa solicitud entre las tuyas.");
        }

        return Resultado<SolicitudAsistenciaReadDto>.Ok(await DetalleDtoAsync(s, paraPersonal: false));
    }

    public async Task<Resultado<SolicitudAsistenciaReadDto>> EmpeoramientoClienteAsync(int cuentaId, int id, CambioNivelDto datos)
    {
        var s = await _repository.ObtenerAsync(id);
        if (s == null || s.ClienteId != cuentaId)
        {
            return Error(404, "SOLICITUD_NO_ENCONTRADA", "No encontramos esa solicitud entre las tuyas.");
        }

        return await RegistrarEmpeoramientoAsync(s, datos, cuentaId, "(cliente)", paraPersonal: false);
    }

    // Sección 10 (RN-51): el evento queda siempre; el nivel nuevo es el mayor entre el actual y el informado.
    private async Task<Resultado<SolicitudAsistenciaReadDto>> RegistrarEmpeoramientoAsync(SolicitudAsistencia s, CambioNivelDto datos, int autorId, string origen, bool paraPersonal = true)
    {
        if (!EstaActiva(s))
        {
            return Error(409, "CERRADA", "Esta solicitud ya está cerrada.");
        }

        if (!Niveles.Contains(datos.Nivel))
        {
            return Error(400, "NIVEL_INVALIDO", "Elegí el nivel y describí qué cambió.");
        }

        var ahora = Ahora;
        Evento(s, ahora, "Empeoramiento informado", $"Nivel informado: {datos.Nivel} {origen}. {datos.Motivo.Trim()}", autorId);
        string mensaje;
        if (Prioridad(datos.Nivel) > Prioridad(s.Nivel))
        {
            mensaje = "Registrado. El nivel no baja con un aviso del cliente: solo un veterinario puede reducirlo (RN-52).";
        }
        else if (datos.Nivel != s.Nivel)
        {
            await AplicarNivelAsync(s, datos.Nivel, "Nivel elevado por empeoramiento", datos.Motivo.Trim(), autorId, ahora);
            mensaje = $"Empeoramiento registrado: la solicitud pasó a {s.Nivel}.";
        }
        else
        {
            mensaje = "Empeoramiento registrado; el nivel se mantiene.";
        }

        await _repository.GuardarAsync();
        return Con(s, mensaje, paraPersonal);
    }

    private async Task AplicarNivelAsync(SolicitudAsistencia s, string nuevo, string tipo, string motivo, int autorId, DateTime ahora)
    {
        var anterior = s.Nivel;
        s.Nivel = nuevo;
        s.Version++;
        _repository.Agregar(new EventoAsistencia
        {
            SolicitudAsistencia = s,
            Fecha = ahora,
            Tipo = tipo,
            Texto = motivo,
            AutorId = autorId,
            NivelAnterior = anterior,
            NivelNuevo = nuevo
        });

        // Si sube a rojo y nadie la aceptó, la guardia se activa de inmediato (RN-49, RN-51). Fuera de cobertura sigue en Recepción.
        if (nuevo == Rojo && SinAceptar(s) && !(s.Estado == Coordinacion && s.MotivoCoordinacion.StartsWith("Fuera de cobertura")))
        {
            await ActivarGuardiaAsync(s, ahora, await LeerParametrosAsync());
        }
        else if (anterior == Rojo && s.Estado == Ofrecida)
        {
            // Si un veterinario lo baja de rojo, vuelve a la cola de Recepción.
            foreach (var o in Abiertas(s).ToList())
            {
                o.Respuesta = "Vencida";
            }

            s.Estado = Pendiente;
            s.FechaOferta = null;
        }
    }

    // ===================== Administrador =====================

    public async Task<Resultado<VeterinarioElegibleDto>> CambiarGuardiaAsync(int adminId, int cuentaId)
    {
        var ficha = (await _repository.ObtenerVeterinariosAsync()).FirstOrDefault(f => f.CuentaId == cuentaId);
        if (ficha == null)
        {
            return Resultado<VeterinarioElegibleDto>.Error(400, "GUARDIA_INVALIDA", "Datos no válidos", "La guardia tiene que ser un veterinario activo.");
        }

        var parametro = await _repository.ObtenerParametroAsync(ClaveGuardia);
        if (parametro == null)
        {
            parametro = new Parametro { Clave = ClaveGuardia, Descripcion = "Cuenta del veterinario de guardia (RF-ADM-13)." };
            _repository.Agregar(parametro);
        }

        parametro.Valor = cuentaId.ToString();
        _repository.Agregar(new Auditoria
        {
            Fecha = Ahora,
            CuentaId = adminId,
            Rol = "Administrador",
            Accion = "Guardia asignada",
            Entidad = "Parámetro",
            EntidadId = ClaveGuardia,
            Origen = "API",
            Detalle = ficha.Nombre
        });
        await _repository.GuardarAsync();
        return Resultado<VeterinarioElegibleDto>.Ok(new VeterinarioElegibleDto { CuentaId = cuentaId, Nombre = ficha.Nombre, DeGuardia = true });
    }

    // ===================== Apoyo =====================

    private void Evento(SolicitudAsistencia s, DateTime fecha, string tipo, string texto, int? autorId)
    {
        _repository.Agregar(new EventoAsistencia { SolicitudAsistencia = s, SolicitudAsistenciaId = s.Id, Fecha = fecha, Tipo = tipo, Texto = texto.Length > 1000 ? texto[..1000] : texto, AutorId = autorId });
    }

    private Resultado<SolicitudAsistenciaReadDto> Con(SolicitudAsistencia s, string mensaje, bool paraPersonal = true)
    {
        var dto = ADto(s, paraPersonal);
        dto.Mensaje = mensaje;
        return Resultado<SolicitudAsistenciaReadDto>.Ok(dto);
    }

    private async Task<SolicitudAsistenciaReadDto> DetalleDtoAsync(SolicitudAsistencia s, bool paraPersonal)
    {
        var dto = ADto(s, paraPersonal);
        dto.Eventos = (await _repository.ObtenerEventosAsync(s.Id)).Select(e => new EventoAsistenciaReadDto
        {
            Id = e.Id,
            Fecha = e.Fecha,
            Tipo = e.Tipo,
            Texto = e.Texto,
            Autor = e.Autor == null ? "Sistema" : $"{e.Autor.Nombre} {e.Autor.Apellido}".Trim(),
            NivelAnterior = e.NivelAnterior,
            NivelNuevo = e.NivelNuevo
        }).ToList();
        return dto;
    }

    private SolicitudAsistenciaReadDto ADto(SolicitudAsistencia s, bool paraPersonal) => new()
    {
        Id = s.Id,
        ClienteId = s.ClienteId,
        Cliente = s.Cliente == null ? "" : $"{s.Cliente.Nombre} {s.Cliente.Apellido}".Trim(),
        AnimalId = s.AnimalId,
        Animal = s.Animal?.Nombre ?? "",
        Especie = s.Especie,
        Cantidad = s.Cantidad,
        Sintomas = s.Sintomas,
        Modalidad = s.Modalidad,
        Direccion = s.Direccion,
        Referencias = s.Referencias,
        UbicacionGps = s.UbicacionGps,
        Latitud = s.Latitud,
        Longitud = s.Longitud,
        DistanciaKm = s.DistanciaKm,
        TelefonoContacto = s.TelefonoContacto,
        Canal = s.Canal,
        NivelCliente = s.NivelCliente,
        Nivel = s.Nivel,
        DescripcionNivel = Descripcion(s.Nivel),
        Estado = s.Estado,
        FechaCreacion = s.FechaCreacion,
        VeterinarioId = s.VeterinarioId,
        Veterinario = s.Veterinario == null ? "" : $"{s.Veterinario.Nombre} {s.Veterinario.Apellido}".Trim(),
        FechaAceptacion = s.FechaAceptacion,
        EtaMinutos = SinAceptar(s) ? null : s.EtaMinutos,
        Costo = s.Costo,
        ReevaluacionPedida = s.ReevaluacionPedida,
        MotivoCoordinacion = paraPersonal ? s.MotivoCoordinacion : "",
        MotivoCierre = s.MotivoCierre,
        Resultado = s.Resultado,
        ParaProximoDia = s.Nivel != Rojo && SinAceptar(s) && !EnHorarioClinica(Ahora),
        CompartiendoUbicacion = s.Estado == EnCamino,
        OfrecidaA = paraPersonal ? Abiertas(s).Select(o => o.Veterinario == null ? o.VeterinarioId.ToString() : $"{o.Veterinario.Nombre} {o.Veterinario.Apellido}".Trim()).ToList() : new(),
        Version = s.Version
    };

    private static Resultado<SolicitudAsistenciaReadDto> NoEncontrada()
        => Resultado<SolicitudAsistenciaReadDto>.Error(404, "SOLICITUD_NO_ENCONTRADA", "No encontrado", "Esa solicitud no existe.");

    private static Resultado<SolicitudAsistenciaReadDto> Error(int estado, string codigo, string mensaje)
        => Resultado<SolicitudAsistenciaReadDto>.Error(estado, codigo, estado switch { 403 => "Sin permiso", 404 => "No encontrado", 409 => "No se puede", _ => "Datos no válidos" }, mensaje);
}
