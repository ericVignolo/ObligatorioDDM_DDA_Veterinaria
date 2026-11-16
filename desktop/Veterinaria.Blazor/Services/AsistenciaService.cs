using System.ComponentModel.DataAnnotations;
using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Asistencia veterinaria de demostración en memoria (A-026, ESPECIFICACION_ALERTAS_ASISTENCIA, RN-46 a RN-59).
// Recepción gestiona la cola y asigna; el Veterinario acepta o rechaza, cambia el nivel con motivo y avanza el traslado.
// Los rojos se ofrecen enseguida a los veterinarios elegibles y a la guardia; si nadie acepta en 10 minutos, pasan a Recepción.
// En la sección C lo reemplaza AsistenciaApiService: la API ejecuta los plazos con una tarea programada, calcula
// distancia y ruta con el proveedor de mapas y avisa por notificaciones.
public class AsistenciaService
{
    // Niveles, de mayor a menor prioridad (RN-47).
    public const string Rojo = "Rojo";
    public const string Naranja = "Naranja";
    public const string Amarillo = "Amarillo";
    public const string Verde = "Verde";

    public const string Visita = "Visita";
    public const string Orientacion = "Orientación y traslado";

    // Estados (sección 6 de la especificación).
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

    // Parámetros por defecto (decisiones de Eric del 31/10); los edita el Administrador en A-027.
    public const int CoberturaKm = 30;
    public const int MinutosParaEscalarRojo = 10;
    public const int HorasParaReevaluar = 24;
    public const decimal TarifaBase = 1500;
    public const decimal TarifaPorKm = 40;
    public const decimal TarifaPorAnimalExtra = 300;

    public const string AvisoDemo = "Demostración: las solicitudes viven solo mientras la aplicación está abierta. La distancia se carga a mano; en la versión final la calcula el mapa y los plazos los controla la API.";

    public static readonly string[] Niveles = { Rojo, Naranja, Amarillo, Verde };
    public static readonly string[] Modalidades = { Visita, Orientacion };

    private readonly PersonalService _personal;
    private readonly ProfesionalService _profesionales;
    private readonly AnimalService _animales;
    private readonly CobroService _cobros;
    private readonly List<SolicitudAsistencia> _solicitudes = new();
    private readonly List<EventoAsistencia> _eventos = new();
    private int _ultimaSolicitud;
    private int _ultimoEvento;

    public AsistenciaService(PersonalService personal, ProfesionalService profesionales, AnimalService animales, CobroService cobros)
    {
        _personal = personal;
        _profesionales = profesionales;
        _animales = animales;
        _cobros = cobros;
        CargarDemo();
    }

    // Veterinario de guardia (Personal.Id). Demo: la Dra. Ferreira; lo carga el Administrador en A-027.
    public int GuardiaId { get; private set; } = 2;

    private void CargarDemo()
    {
        var ahora = DateTime.Now;
        var recepcion = new Personal { Id = 0, Nombre = "Sistema (demo)", Rol = PersonalService.Recepcion };

        // Naranja, rodeo de Pedro a 18 km, hace 2 horas.
        Crear(Demo(101, 3, "Vaca", 25, "Varias vacas rengas y una caída que no se levanta.", Naranja, Visita, "Ruta 21 km 230, Establecimiento La Esperanza", 18), recepcion, ahora.AddHours(-2));
        // Amarillo, Toby de Ana a 6 km, hace 26 horas: pide reevaluación (RN-50).
        Crear(Demo(100, 1, "Perro", 1, "Vómitos desde ayer, come poco.", Amarillo, Visita, "Rivera 1234, Nueva Palmira", 6), recepcion, ahora.AddHours(-26));
        // Verde, orientación para Mishi.
        Crear(Demo(100, 2, "Gato", 1, "Consulta si es normal que esté decaída después de la vacuna.", Verde, Orientacion, "", 0), recepcion, ahora.AddHours(-1));
        // Amarillo fuera de cobertura (45 km): pasa a Recepción (RN-55).
        Crear(Demo(101, 0, "Oveja", 1, "Oveja con dificultad para parir.", Amarillo, Visita, "Paraje Las Flores, camino vecinal", 45), recepcion, ahora.AddMinutes(-40));
        // Rojo sin aceptar hace 15 minutos: al revisar los plazos pasa a Recepción (RN-55).
        Crear(Demo(101, 0, "Caballo", 1, "Potrillo con cólico fuerte, se revuelca.", Rojo, Visita, "Chacra Los Álamos, Carmelo", 12), recepcion, ahora.AddMinutes(-15));
    }

    private static SolicitudTelefonicaFormulario Demo(int cliente, int animal, string especie, int cantidad, string sintomas, string nivel, string modalidad, string direccion, int km)
    {
        return new SolicitudTelefonicaFormulario
        {
            ClienteId = cliente, AnimalId = animal, Especie = especie, Cantidad = cantidad, Sintomas = sintomas,
            Nivel = nivel, Modalidad = modalidad, Direccion = direccion, DistanciaKm = km, TelefonoContacto = cliente == 100 ? "+59899111222" : "+59899333444"
        };
    }

    // 0 = máxima prioridad.
    public static int Prioridad(string nivel)
    {
        var indice = Array.IndexOf(Niveles, nivel);
        return indice < 0 ? Niveles.Length : indice;
    }

    public static string Descripcion(string nivel)
    {
        return nivel switch
        {
            Rojo => "Posible riesgo vital: guardia inmediata",
            Naranja => "Urgencia alta: en el horario de la clínica, primero",
            Amarillo => "Prioritaria: tolera una espera controlada",
            _ => "Estable o programable"
        };
    }

    public static bool EstaActiva(SolicitudAsistencia s)
    {
        return s.Estado != Finalizada && s.Estado != CanceladaClinica && s.Estado != NoConcretada;
    }

    // Todavía nadie la aceptó.
    public static bool SinAceptar(SolicitudAsistencia s)
    {
        return s.Estado == Pendiente || s.Estado == Ofrecida || s.Estado == Coordinacion;
    }

    // RN-57: la ubicación del veterinario se comparte solo durante el traslado.
    public static bool CompartiendoUbicacion(SolicitudAsistencia s)
    {
        return s.Estado == EnCamino;
    }

    // RN-58: base + distancia + animales extra; la orientación no tiene costo.
    public static decimal CalcularCosto(string modalidad, int distanciaKm, int cantidad)
    {
        return modalidad == Visita ? TarifaBase + TarifaPorKm * distanciaKm + TarifaPorAnimalExtra * Math.Max(0, cantidad - 1) : 0;
    }

    // Horario de la clínica para verde, amarillo y naranja (RN-48): lun–vie 9–19, sáb 9–13.
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

    // RN-48: fuera del horario, lo no rojo y sin aceptar queda para el próximo día de atención.
    public static bool ParaProximoDia(SolicitudAsistencia s, DateTime ahora)
    {
        return s.Nivel != Rojo && SinAceptar(s) && !EnHorarioClinica(ahora);
    }

    public SolicitudAsistencia? ObtenerPorId(int id)
    {
        return _solicitudes.FirstOrDefault(s => s.Id == id);
    }

    public List<EventoAsistencia> Eventos(int solicitudId)
    {
        return _eventos.Where(e => e.SolicitudId == solicitudId).OrderByDescending(e => e.Id).ToList();
    }

    // Cola de Recepción: activas, rojo → naranja → amarillo → verde y, dentro del nivel, por antigüedad (RN-47, RN-48).
    public List<SolicitudAsistencia> Cola()
    {
        return _solicitudes
            .Where(EstaActiva)
            .OrderBy(s => Prioridad(s.Nivel))
            .ThenBy(s => s.FechaCreacion)
            .ThenBy(s => s.Id)
            .ToList();
    }

    public List<SolicitudAsistencia> Cerradas()
    {
        return _solicitudes.Where(s => !EstaActiva(s)).OrderByDescending(s => s.Id).ToList();
    }

    // Lo que ve un veterinario: ofrecidas a él y las que aceptó y siguen activas.
    public List<SolicitudAsistencia> DeVeterinario(int personalId)
    {
        return Cola()
            .Where(s => (s.Estado == Ofrecida && s.OfrecidaA.Contains(personalId)) || s.VeterinarioId == personalId)
            .ToList();
    }

    // RN-54, sección 12: matrícula vigente, especie y disponibilidad; la cercanía no vuelve elegible a nadie.
    // Para rojo: además, de guardia o dentro de su jornada en este momento. Para el resto, Recepción decide cuándo.
    public List<Personal> Elegibles(SolicitudAsistencia s, DateTime ahora)
    {
        var resultado = new List<Personal>();
        foreach (var persona in _personal.ObtenerTodos().Where(p => p.Rol == PersonalService.Veterinario))
        {
            var ficha = _profesionales.ObtenerFicha(persona.ProfesionalId);
            if (ficha is null || ficha.MatriculaVigenteHasta is not DateOnly hasta || hasta < DateOnly.FromDateTime(ahora))
            {
                continue;
            }

            if (!ficha.Especies.Contains(s.Especie))
            {
                continue;
            }

            if (s.Nivel == Rojo && persona.Id != GuardiaId && !TrabajaAhora(ficha, ahora))
            {
                continue;
            }

            resultado.Add(persona);
        }

        return resultado;
    }

    private static bool TrabajaAhora(Profesional ficha, DateTime ahora)
    {
        var hora = TimeOnly.FromDateTime(ahora);
        return ficha.Jornada.Any(f => f.Dia == ahora.DayOfWeek && hora >= f.Inicio && hora < f.Fin);
    }

    // Recepción registra una solicitud que llega por teléfono (CU, sección 7 y 8). Idempotente por clave.
    public ResultadoOperacion Crear(SolicitudTelefonicaFormulario datos, Personal? persona, DateTime? momento = null)
    {
        var ahora = momento ?? DateTime.Now;
        if (persona is null || persona.Rol != PersonalService.Recepcion)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo Recepción registra solicitudes por teléfono." };
        }

        var repetida = _solicitudes.FirstOrDefault(s => !string.IsNullOrEmpty(datos.ClaveIdempotencia) && s.ClaveIdempotencia == datos.ClaveIdempotencia);
        if (repetida is not null)
        {
            return new ResultadoOperacion { Exito = true, Mensaje = $"La solicitud n.º {repetida.Id} ya estaba registrada." };
        }

        if (!Validator.TryValidateObject(datos, new ValidationContext(datos), new List<ValidationResult>(), validateAllProperties: true)
            || !Niveles.Contains(datos.Nivel) || !Modalidades.Contains(datos.Modalidad))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Corregí los campos marcados: cliente, síntomas, nivel, modalidad, cantidad y teléfono." };
        }

        // Animal registrado: la especie sale de su ficha y tiene que ser de ese cliente.
        var especie = datos.Especie.Trim();
        var cantidad = datos.Cantidad;
        if (datos.AnimalId != 0)
        {
            var animal = _animales.ObtenerParaPersonal(datos.AnimalId);
            if (animal is null || animal.ResponsableId != datos.ClienteId)
            {
                return new ResultadoOperacion { Exito = false, Mensaje = "Ese animal no es de este cliente." };
            }

            especie = animal.Especie;
        }

        if (string.IsNullOrWhiteSpace(especie))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Indicá la especie del animal o del grupo." };
        }

        // RN-56: una visita necesita destino.
        if (datos.Modalidad == Visita && string.IsNullOrWhiteSpace(datos.Direccion))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Una visita necesita la dirección. Sin ubicación se puede dar orientación o coordinar el traslado (RN-56)." };
        }

        var solicitud = new SolicitudAsistencia
        {
            Id = ++_ultimaSolicitud,
            ClienteId = datos.ClienteId,
            AnimalId = datos.AnimalId,
            Especie = especie,
            Cantidad = cantidad,
            Sintomas = datos.Sintomas.Trim(),
            Modalidad = datos.Modalidad,
            Direccion = datos.Direccion.Trim(),
            Referencias = datos.Referencias.Trim(),
            DistanciaKm = datos.Modalidad == Visita ? datos.DistanciaKm : 0,
            TelefonoContacto = datos.TelefonoContacto.Trim(),
            Canal = "Teléfono",
            ClaveIdempotencia = datos.ClaveIdempotencia,
            // RN-46: sin cuestionario aprobado todavía, el nivel preliminar es el que indica el cliente.
            NivelCliente = datos.Nivel,
            Nivel = datos.Nivel,
            Estado = Pendiente,
            FechaCreacion = ahora,
            Costo = CalcularCosto(datos.Modalidad, datos.Modalidad == Visita ? datos.DistanciaKm : 0, cantidad)
        };
        _solicitudes.Add(solicitud);
        Evento(solicitud, ahora, "Recibida", $"Por teléfono. Nivel indicado: {solicitud.Nivel}. Costo informado: $ {solicitud.Costo:N0}.", persona.Nombre);

        if (solicitud.Modalidad == Visita && solicitud.DistanciaKm > CoberturaKm)
        {
            AEscalar(solicitud, ahora, $"Fuera de cobertura ({solicitud.DistanciaKm} km, máximo {CoberturaKm} km): la visita no está confirmada.");
        }
        else if (solicitud.Nivel == Rojo)
        {
            ActivarGuardia(solicitud, ahora);
        }

        return new ResultadoOperacion { Exito = true, Mensaje = $"Solicitud n.º {solicitud.Id} registrada ({solicitud.Nivel}, {solicitud.Estado})." };
    }

    // RN-49: el rojo se ofrece enseguida a los elegibles disponibles y a la guardia; sin nadie, a Recepción (RN-55).
    private void ActivarGuardia(SolicitudAsistencia s, DateTime ahora)
    {
        var elegibles = Elegibles(s, ahora);
        if (elegibles.Count == 0)
        {
            AEscalar(s, ahora, "Rojo sin veterinario elegible de guardia o en jornada.");
            return;
        }

        s.Estado = Ofrecida;
        s.OfrecidaA = elegibles.Select(p => p.Id).ToList();
        s.FechaOferta = ahora;
        Evento(s, ahora, "Ofrecida", "Ofrecida a " + string.Join(", ", elegibles.Select(p => p.Nombre)) + ".", "Sistema");
    }

    private void AEscalar(SolicitudAsistencia s, DateTime ahora, string motivo)
    {
        s.Estado = Coordinacion;
        s.OfrecidaA = new();
        s.FechaOferta = null;
        s.MotivoCoordinacion = motivo;
        Evento(s, ahora, "Escalada a Recepción", motivo, "Sistema");
    }

    // Lo que en la API hace una tarea programada: rojos sin aceptar en 10 minutos y pendientes de 24 horas (RN-50, RN-55).
    public void RevisarPlazos(DateTime? momento = null)
    {
        var ahora = momento ?? DateTime.Now;
        foreach (var s in _solicitudes.Where(EstaActiva).ToList())
        {
            if (s.Nivel == Rojo && s.Estado == Ofrecida && s.FechaOferta is DateTime oferta && ahora - oferta >= TimeSpan.FromMinutes(MinutosParaEscalarRojo))
            {
                AEscalar(s, ahora, $"Nadie aceptó el rojo en {MinutosParaEscalarRojo} minutos: contactar al cliente.");
            }

            if (SinAceptar(s) && !s.ReevaluacionPedida && ahora - s.FechaCreacion >= TimeSpan.FromHours(HorasParaReevaluar))
            {
                // RN-50: se pide la reevaluación; sin respuesta no se cierra ni se baja el nivel.
                s.ReevaluacionPedida = true;
                Evento(s, ahora, "Reevaluación pedida", "Pasaron 24 horas: se pregunta al cliente si el estado se mantuvo o empeoró.", "Sistema");
            }
        }
    }

    // RF-REC-08: Recepción ofrece una solicitud no roja a un veterinario elegible.
    public ResultadoOperacion Asignar(int id, int veterinarioId, Personal? persona, DateTime? momento = null)
    {
        var ahora = momento ?? DateTime.Now;
        if (persona is null || persona.Rol != PersonalService.Recepcion)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo Recepción asigna solicitudes." };
        }

        var s = ObtenerPorId(id);
        if (s is null || (s.Estado != Pendiente && s.Estado != Coordinacion))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Esta solicitud ya está ofrecida, aceptada o cerrada." };
        }

        var veterinario = Elegibles(s, ahora).FirstOrDefault(p => p.Id == veterinarioId);
        if (veterinario is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ese veterinario no es elegible para esta especie o disponibilidad (RN-54)." };
        }

        s.Estado = Ofrecida;
        s.OfrecidaA = new() { veterinario.Id };
        s.FechaOferta = ahora;
        Evento(s, ahora, "Ofrecida", $"Recepción la ofreció a {veterinario.Nombre}.", persona.Nombre);
        return new ResultadoOperacion { Exito = true, Mensaje = $"Ofrecida a {veterinario.Nombre}. Queda esperando que acepte." };
    }

    // RF-VET-16: el primero que acepta se la queda. Repetir la aceptación no duplica nada.
    public ResultadoOperacion Aceptar(int id, Personal? veterinario, DateTime? momento = null)
    {
        var ahora = momento ?? DateTime.Now;
        var s = ObtenerPorId(id);
        if (s is null || veterinario is null || veterinario.Rol != PersonalService.Veterinario)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo un veterinario acepta asistencias." };
        }

        if (s.VeterinarioId == veterinario.Id && !SinAceptar(s))
        {
            return new ResultadoOperacion { Exito = true, Mensaje = "Ya la habías aceptado." };
        }

        if (s.Estado != Ofrecida || !s.OfrecidaA.Contains(veterinario.Id))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Esta solicitud ya no está ofrecida a vos." };
        }

        s.Estado = Aceptada;
        s.VeterinarioId = veterinario.Id;
        s.VeterinarioNombre = veterinario.Nombre;
        s.FechaAceptacion = ahora;
        s.OfrecidaA = new();
        // RN-53: estimación recién ahora; con la API la calcula el mapa.
        s.EtaMinutos = s.Modalidad == Visita ? 15 + s.DistanciaKm * 2 : null;
        Evento(s, ahora, "Aceptada", s.EtaMinutos is null ? "Orientación: el veterinario contacta al cliente." : $"Llegada estimada en {s.EtaMinutos} minutos.", veterinario.Nombre);
        return new ResultadoOperacion { Exito = true, Mensaje = "Aceptaste la asistencia." };
    }

    // El rechazo libera la oferta; si nadie más la tiene, vuelve a Recepción.
    public ResultadoOperacion Rechazar(int id, string motivo, Personal? veterinario, DateTime? momento = null)
    {
        var ahora = momento ?? DateTime.Now;
        var s = ObtenerPorId(id);
        if (s is null || veterinario is null || s.Estado != Ofrecida || !s.OfrecidaA.Contains(veterinario.Id))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Esta solicitud no está ofrecida a vos." };
        }

        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 200)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Escribí el motivo del rechazo (hasta 200 caracteres)." };
        }

        s.OfrecidaA.Remove(veterinario.Id);
        Evento(s, ahora, "Rechazada", motivo.Trim(), veterinario.Nombre);
        if (s.OfrecidaA.Count == 0)
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

        return new ResultadoOperacion { Exito = true, Mensaje = "Rechazo registrado." };
    }

    // RF-VET-17: estados del traslado. La orientación no tiene traslado.
    public ResultadoOperacion Avanzar(int id, Personal? veterinario, DateTime? momento = null)
    {
        var ahora = momento ?? DateTime.Now;
        var s = ObtenerPorId(id);
        if (s is null || veterinario is null || s.VeterinarioId != veterinario.Id)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo el veterinario que aceptó actualiza el estado." };
        }

        var siguiente = SiguienteEstado(s);
        if (siguiente is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = s.Estado == EnAtencion ? "Para terminar, registrá el resultado y finalizá." : "Esta solicitud no se puede avanzar." };
        }

        s.Estado = siguiente;
        Evento(s, ahora, siguiente, siguiente == EnCamino ? "El cliente ve la ubicación del veterinario hasta la llegada (RN-57)." : "", veterinario.Nombre);
        return new ResultadoOperacion { Exito = true, Mensaje = $"Estado: {siguiente}." };
    }

    public static string? SiguienteEstado(SolicitudAsistencia s)
    {
        return s.Estado switch
        {
            Aceptada => s.Modalidad == Visita ? EnCamino : EnAtencion,
            EnCamino => EnElLugar,
            EnElLugar => EnAtencion,
            _ => null
        };
    }

    // Termina la asistencia, deja de compartir ubicación y genera el cargo (RN-58).
    public ResultadoOperacion Finalizar(int id, string resultado, Personal? veterinario, DateTime? momento = null)
    {
        var ahora = momento ?? DateTime.Now;
        var s = ObtenerPorId(id);
        if (s is null || veterinario is null || s.VeterinarioId != veterinario.Id)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo el veterinario que aceptó finaliza la asistencia." };
        }

        if (s.Estado != EnAtencion)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Se finaliza después de estar «En atención»." };
        }

        if (string.IsNullOrWhiteSpace(resultado) || resultado.Trim().Length > 1000)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Escribí el resultado de la atención (hasta 1000 caracteres)." };
        }

        s.Estado = Finalizada;
        s.Resultado = resultado.Trim();
        Evento(s, ahora, "Finalizada", s.Resultado, veterinario.Nombre);
        var cargo = _cobros.GenerarCargoAsistencia(s);
        return new ResultadoOperacion { Exito = true, Mensaje = cargo is null ? "Asistencia finalizada." : $"Asistencia finalizada. Cargo de $ {cargo.Monto:N0} pendiente en Recepción." };
    }

    // RN-52: un veterinario confirma o cambia el nivel con motivo; puede bajarlo. Si sube a rojo sin aceptar, se activa la guardia.
    public ResultadoOperacion CambiarNivel(int id, CambioNivelFormulario datos, Personal? veterinario, DateTime? momento = null)
    {
        var ahora = momento ?? DateTime.Now;
        if (veterinario is null || veterinario.Rol != PersonalService.Veterinario)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo un veterinario cambia la clasificación (RN-52)." };
        }

        var s = ObtenerPorId(id);
        if (s is null || !EstaActiva(s))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Esta solicitud ya está cerrada." };
        }

        if (!Validator.TryValidateObject(datos, new ValidationContext(datos), new List<ValidationResult>(), validateAllProperties: true) || !Niveles.Contains(datos.Nivel))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Elegí el nivel y escribí el motivo." };
        }

        if (datos.Nivel == s.Nivel)
        {
            Evento(s, ahora, "Nivel confirmado", datos.Motivo.Trim(), veterinario.Nombre);
            return new ResultadoOperacion { Exito = true, Mensaje = $"Nivel {s.Nivel} confirmado." };
        }

        AplicarNivel(s, datos.Nivel, "Nivel cambiado por el veterinario", datos.Motivo.Trim(), veterinario.Nombre, ahora);
        return new ResultadoOperacion { Exito = true, Mensaje = $"Nivel cambiado a {s.Nivel}." };
    }

    // RN-51: empeoramiento informado por el cliente (app) o por teléfono a Recepción. Siempre queda el evento; solo puede subir.
    public ResultadoOperacion InformarEmpeoramiento(int id, CambioNivelFormulario datos, Personal? recepcion, DateTime? momento = null)
    {
        if (recepcion is null || recepcion.Rol != PersonalService.Recepcion)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Por teléfono, el empeoramiento lo registra Recepción." };
        }

        return RegistrarEmpeoramiento(id, datos, recepcion.Nombre + " (por teléfono)", momento ?? DateTime.Now);
    }

    // Lo mismo desde la app del cliente (B-014): solo sus solicitudes (RN-18).
    public ResultadoOperacion InformarEmpeoramientoCliente(int id, CambioNivelFormulario datos, Cuenta? cliente, DateTime? momento = null)
    {
        var s = ObtenerPorId(id);
        if (cliente is null || s is null || s.ClienteId != cliente.Id)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No encontramos esa solicitud entre las tuyas." };
        }

        return RegistrarEmpeoramiento(id, datos, $"{cliente.Nombre} {cliente.Apellido} (cliente)", momento ?? DateTime.Now);
    }

    private ResultadoOperacion RegistrarEmpeoramiento(int id, CambioNivelFormulario datos, string autor, DateTime ahora)
    {
        var s = ObtenerPorId(id);
        if (s is null || !EstaActiva(s))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Esta solicitud ya está cerrada." };
        }

        if (!Validator.TryValidateObject(datos, new ValidationContext(datos), new List<ValidationResult>(), validateAllProperties: true) || !Niveles.Contains(datos.Nivel))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Elegí el nivel y describí qué cambió." };
        }

        if (Prioridad(datos.Nivel) > Prioridad(s.Nivel))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Un empeoramiento no baja el nivel: solo un veterinario puede reducirlo (RN-52)." };
        }

        Evento(s, ahora, "Empeoramiento informado", datos.Motivo.Trim(), autor);
        if (datos.Nivel != s.Nivel)
        {
            AplicarNivel(s, datos.Nivel, "Nivel elevado por empeoramiento", datos.Motivo.Trim(), autor, ahora);
            return new ResultadoOperacion { Exito = true, Mensaje = $"Empeoramiento registrado: la solicitud pasó a {s.Nivel}." };
        }

        return new ResultadoOperacion { Exito = true, Mensaje = "Empeoramiento registrado; el nivel se mantiene." };
    }

    private void AplicarNivel(SolicitudAsistencia s, string nuevo, string tipo, string motivo, string autor, DateTime ahora)
    {
        var anterior = s.Nivel;
        s.Nivel = nuevo;
        _eventos.Add(new EventoAsistencia
        {
            Id = ++_ultimoEvento, SolicitudId = s.Id, Fecha = ahora, Tipo = tipo, Texto = motivo, Autor = autor,
            NivelAnterior = anterior, NivelNuevo = nuevo
        });

        // Si sube a rojo y nadie la aceptó, la guardia se activa de inmediato (RN-49, RN-51).
        if (nuevo == Rojo && SinAceptar(s) && !(s.Estado == Coordinacion && s.MotivoCoordinacion.StartsWith("Fuera de cobertura")))
        {
            ActivarGuardia(s, ahora);
        }
        else if (anterior == Rojo && s.Estado == Ofrecida)
        {
            // Si un veterinario lo baja de rojo, vuelve a la cola de Recepción.
            s.Estado = Pendiente;
            s.OfrecidaA = new();
            s.FechaOferta = null;
        }
    }

    // Recepción cierra sin atención. Un rojo solo queda «No concretada» después de escalar a Recepción (sección 6).
    public ResultadoOperacion Cerrar(int id, string estado, string motivo, Personal? persona, DateTime? momento = null)
    {
        var ahora = momento ?? DateTime.Now;
        if (persona is null || persona.Rol != PersonalService.Recepcion)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo Recepción cierra una solicitud sin atención." };
        }

        var s = ObtenerPorId(id);
        if (s is null || !(SinAceptar(s) || s.Estado == Aceptada))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo se cierra antes de que el veterinario salga." };
        }

        if (estado != CanceladaClinica && estado != NoConcretada)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Elegí cómo se cierra." };
        }

        if (string.IsNullOrWhiteSpace(motivo) || motivo.Trim().Length > 300)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Escribí el motivo y el contacto con el cliente (hasta 300 caracteres)." };
        }

        if (s.Nivel == Rojo && s.Estado != Coordinacion)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Un rojo se cierra solo después de pasar a Recepción y contactar al cliente." };
        }

        s.Estado = estado;
        s.MotivoCierre = motivo.Trim();
        s.OfrecidaA = new();
        Evento(s, ahora, estado, s.MotivoCierre, persona.Nombre);
        return new ResultadoOperacion { Exito = true, Mensaje = $"Solicitud cerrada: {estado}." };
    }

    private void Evento(SolicitudAsistencia s, DateTime fecha, string tipo, string texto, string autor)
    {
        _eventos.Add(new EventoAsistencia { Id = ++_ultimoEvento, SolicitudId = s.Id, Fecha = fecha, Tipo = tipo, Texto = texto, Autor = autor });
    }
}
