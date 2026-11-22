using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Veterinaria.Api.Controllers;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — asistencia (A-026, ESPECIFICACION_ALERTAS_ASISTENCIA §17, RN-46 a RN-59). Reloj: lunes 23/11/2026 10:00.
// Ferreira (cuenta 2, perros, lun 9–14) es la guardia por defecto; Silveira (cuenta 3, vacas, lun 14–19).
// Ana (10) tiene a Luna (perro, 1); Pedro (11) tiene un rodeo sin registrar.
public class ApiAsistenciaTests
{
    private static readonly DateTime Lunes10 = new(2026, 11, 23, 10, 0, 0);
    private const int Recepcion = 1;
    private const int Ferreira = 2;
    private const int Silveira = 3;
    private const int Admin = 5;
    private const int Ana = 10;
    private const int Pedro = 11;
    private const int Perro = 1;
    private const int Vaca = 7;

    private class Reloj : TimeProvider
    {
        public DateTime Ahora { get; set; } = Lunes10;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Ahora, DateTimeKind.Utc));
    }

    private class AsistenciasEnMemoria : IAsistenciaRepository
    {
        public List<SolicitudAsistencia> Solicitudes { get; } = new();
        public List<EventoAsistencia> Eventos { get; } = new();
        public List<Animal> Animales { get; } = new();
        public List<Especie> Especies { get; } = new();
        public List<Cuenta> Clientes { get; } = new();
        public List<Profesional> Veterinarios { get; } = new();
        public List<Parametro> Parametros { get; } = new();
        public List<Cargo> Cargos { get; } = new();
        public List<Auditoria> Auditorias { get; } = new();

        private static readonly string[] Cerradas = { "Finalizada", "Cancelada por la clínica", "No concretada" };

        public Task<SolicitudAsistencia?> ObtenerAsync(int id) => Task.FromResult(Solicitudes.FirstOrDefault(s => s.Id == id));
        public Task<List<SolicitudAsistencia>> ObtenerActivasAsync() => Task.FromResult(Solicitudes.Where(s => !Cerradas.Contains(s.Estado)).ToList());
        public Task<List<SolicitudAsistencia>> ObtenerCerradasAsync(int tomar) => Task.FromResult(Solicitudes.Where(s => Cerradas.Contains(s.Estado)).Take(tomar).ToList());
        public Task<List<SolicitudAsistencia>> ObtenerDeClienteAsync(int clienteId) => Task.FromResult(Solicitudes.Where(s => s.ClienteId == clienteId).ToList());
        public Task<SolicitudAsistencia?> ObtenerPorClaveAsync(int clienteId, string clave)
            => Task.FromResult(Solicitudes.FirstOrDefault(s => s.ClienteId == clienteId && s.ClaveIdempotencia == clave));
        public Task<List<EventoAsistencia>> ObtenerEventosAsync(int solicitudId)
            => Task.FromResult(Eventos.Where(e => (e.SolicitudAsistencia?.Id ?? e.SolicitudAsistenciaId) == solicitudId).OrderByDescending(e => e.Id).ToList());
        public Task<Animal?> ObtenerAnimalAsync(int animalId) => Task.FromResult(Animales.FirstOrDefault(a => a.Id == animalId));
        public Task<bool> EsResponsableAsync(int animalId, int cuentaId)
            => Task.FromResult(Animales.Any(a => a.Id == animalId && a.Responsables.Any(r => r.CuentaId == cuentaId && r.Hasta == null)));
        public Task<Especie?> ObtenerEspecieAsync(int especieId) => Task.FromResult(Especies.FirstOrDefault(e => e.Id == especieId));
        public Task<Cuenta?> ObtenerClienteAsync(int cuentaId) => Task.FromResult(Clientes.FirstOrDefault(c => c.Id == cuentaId));
        public Task<List<Profesional>> ObtenerVeterinariosAsync() => Task.FromResult(Veterinarios.ToList());
        public Task<Dictionary<string, string>> ObtenerParametrosAsync() => Task.FromResult(Parametros.ToDictionary(p => p.Clave, p => p.Valor));
        public Task<Parametro?> ObtenerParametroAsync(string clave) => Task.FromResult(Parametros.FirstOrDefault(p => p.Clave == clave));
        public Task<bool> ExisteCargoAsync(int solicitudId) => Task.FromResult(Cargos.Any(c => c.SolicitudAsistencia?.Id == solicitudId));

        public void Agregar<T>(T entidad) where T : class
        {
            switch (entidad)
            {
                case SolicitudAsistencia s: s.Id = Solicitudes.Count + 1; Solicitudes.Add(s); break;
                case EventoAsistencia e: e.Id = Eventos.Count + 1; Eventos.Add(e); break;
                case Cargo c: c.Id = Cargos.Count + 1; Cargos.Add(c); break;
                case Parametro p: Parametros.Add(p); break;
                case Auditoria a: Auditorias.Add(a); break;
                default: throw new InvalidOperationException($"Tipo no esperado: {typeof(T).Name}");
            }
        }

        public Task GuardarAsync() => Task.CompletedTask;
    }

    private static Profesional Ficha(int id, int cuentaId, string nombre, string especie, int hIni, int hFin) => new()
    {
        Id = id,
        CuentaId = cuentaId,
        Cuenta = new Cuenta { Id = cuentaId, Nombre = nombre, Activa = true },
        Nombre = nombre,
        Activo = true,
        MatriculaVigenteHasta = new DateTime(2027, 11, 1),
        Especies = new List<ProfesionalEspecie> { new() { ProfesionalId = id, Especie = new Especie { Nombre = especie } } },
        Franjas = new List<FranjaHoraria> { new() { ProfesionalId = id, Tipo = "Jornada", Dia = DayOfWeek.Monday, Inicio = TimeSpan.FromHours(hIni), Fin = TimeSpan.FromHours(hFin) } }
    };

    private static (AsistenciaService S, AsistenciasEnMemoria D, Reloj R) Escenario()
    {
        var d = new AsistenciasEnMemoria();
        d.Especies.Add(new Especie { Id = Perro, Nombre = "Perro" });
        d.Especies.Add(new Especie { Id = Vaca, Nombre = "Vaca" });
        d.Clientes.Add(new Cuenta { Id = Ana, Nombre = "Ana", Apellido = "Pérez" });
        d.Clientes.Add(new Cuenta { Id = Pedro, Nombre = "Pedro", Apellido = "Gómez" });
        var luna = new Animal { Id = 1, Nombre = "Luna", Especie = d.Especies[0], Activo = true };
        luna.Responsables.Add(new Responsabilidad { CuentaId = Ana, AnimalId = 1 });
        d.Animales.Add(luna);
        d.Veterinarios.Add(Ficha(1, Ferreira, "Dra. Lucía Ferreira", "Perro", 9, 14));
        d.Veterinarios.Add(Ficha(2, Silveira, "Dr. Martín Silveira", "Vaca", 14, 19));
        var reloj = new Reloj();
        return (new AsistenciaService(d, reloj), d, reloj);
    }

    private static SolicitudTelefonicaDto Telefonica(int cliente, string nivel, int? animal = null, int? especie = null, string modalidad = AsistenciaService.Visita,
        string direccion = "Rivera 1234, Nueva Palmira", int km = 6, int cantidad = 1, string clave = "") => new()
    {
        ClienteId = cliente, AnimalId = animal, EspecieId = especie, Nivel = nivel, Modalidad = modalidad, Direccion = direccion,
        DistanciaKm = km, Cantidad = cantidad, Sintomas = "Síntomas", TelefonoContacto = "+59899111222", ClaveIdempotencia = clave
    };

    private static CambioNivelDto Nivel(string nivel) => new() { Nivel = nivel, Motivo = "Motivo" };

    [Fact]
    public async Task Cola_rojo_primero_y_dentro_del_nivel_por_antiguedad()
    {
        var (s, d, r) = Escenario();
        await s.CrearTelefonicaAsync(Recepcion, Telefonica(Pedro, "Amarillo", especie: Vaca, cantidad: 20));
        r.Ahora = Lunes10.AddMinutes(1);
        await s.CrearTelefonicaAsync(Recepcion, Telefonica(Ana, "Naranja", animal: 1));
        r.Ahora = Lunes10.AddMinutes(2);
        await s.CrearTelefonicaAsync(Recepcion, Telefonica(Ana, "Naranja", animal: 1));
        r.Ahora = Lunes10.AddMinutes(3);
        var rojo = await s.CrearTelefonicaAsync(Recepcion, Telefonica(Ana, "Rojo", animal: 1));

        Assert.Equal(("Ofrecida", "Perro"), (rojo.Valor!.Estado, rojo.Valor.Especie));
        Assert.Equal(new[] { 4, 2, 3, 1 }, (await s.ColaAsync()).Select(x => x.Id));
    }

    [Fact]
    public async Task Costo_visita_y_orientacion_y_visita_sin_destino()
    {
        var (s, _, _) = Escenario();
        var visita = await s.CrearTelefonicaAsync(Recepcion, Telefonica(Pedro, "Verde", especie: Vaca, km: 6, cantidad: 2));
        Assert.Equal(2040m, visita.Valor!.Costo); // 1500 + 6 × 40 + 1 × 300

        Assert.Equal("VISITA_SIN_DESTINO", (await s.CrearTelefonicaAsync(Recepcion, Telefonica(Ana, "Verde", animal: 1, direccion: ""))).Codigo);
        var orientacion = await s.CrearTelefonicaAsync(Recepcion, Telefonica(Ana, "Verde", animal: 1, modalidad: AsistenciaService.Orientacion, direccion: ""));
        Assert.Equal((0m, 0), (orientacion.Valor!.Costo, orientacion.Valor.DistanciaKm));

        Assert.Equal("FALTA_ESPECIE", (await s.CrearTelefonicaAsync(Recepcion, Telefonica(Pedro, "Verde"))).Codigo);
        Assert.Equal("ANIMAL_AJENO", (await s.CrearTelefonicaAsync(Recepcion, Telefonica(Pedro, "Verde", animal: 1))).Codigo);
    }

    [Fact]
    public async Task Reintento_con_la_misma_clave_no_duplica()
    {
        var (s, d, _) = Escenario();
        var primera = await s.CrearDesdeAppAsync(Ana, new SolicitudAppDto { AnimalId = 1, Nivel = "Amarillo", Modalidad = AsistenciaService.Visita, Direccion = "Rivera 1234", DistanciaKm = 6, Sintomas = "Vómitos", TelefonoContacto = "099", ClaveIdempotencia = "abc" });
        var segunda = await s.CrearDesdeAppAsync(Ana, new SolicitudAppDto { AnimalId = 1, Nivel = "Amarillo", Modalidad = AsistenciaService.Visita, Direccion = "Rivera 1234", DistanciaKm = 6, Sintomas = "Vómitos", TelefonoContacto = "099", ClaveIdempotencia = "abc" });

        Assert.Equal((201, 200), (primera.Estado, segunda.Estado));
        Assert.Equal(primera.Valor!.Id, segunda.Valor!.Id);
        Assert.Single(d.Solicitudes);
        Assert.Equal("App", d.Solicitudes[0].Canal);
    }

    [Fact]
    public async Task Rojo_sin_elegibles_va_a_Recepcion_y_con_guardia_se_ofrece()
    {
        var (s, d, _) = Escenario();

        // A las 10 Silveira no trabaja y no es la guardia: nadie atiende vacas → Recepción (prueba 6).
        var sinNadie = await s.CrearTelefonicaAsync(Recepcion, Telefonica(Pedro, "Rojo", especie: Vaca));
        Assert.Equal(AsistenciaService.Coordinacion, sinNadie.Valor!.Estado);
        Assert.Null(sinNadie.Valor.VeterinarioId);
        Assert.Null(sinNadie.Valor.EtaMinutos);

        Assert.True((await s.CambiarGuardiaAsync(Admin, Silveira)).Exito);
        var conGuardia = await s.CrearTelefonicaAsync(Recepcion, Telefonica(Pedro, "Rojo", especie: Vaca));
        Assert.Equal(AsistenciaService.Ofrecida, conGuardia.Valor!.Estado);
        Assert.Single(d.Solicitudes[1].Ofertas, o => o.VeterinarioId == Silveira && o.Respuesta == "Pendiente");
        Assert.Equal("GUARDIA_INVALIDA", (await s.CambiarGuardiaAsync(Admin, 99)).Codigo);
    }

    [Fact]
    public async Task Fuera_de_cobertura_va_a_Recepcion_y_un_rojo_no_lo_saca()
    {
        var (s, _, _) = Escenario();
        var lejos = await s.CrearTelefonicaAsync(Recepcion, Telefonica(Ana, "Amarillo", animal: 1, km: 45));
        Assert.Equal(AsistenciaService.Coordinacion, lejos.Valor!.Estado);
        Assert.StartsWith("Fuera de cobertura", lejos.Valor.MotivoCoordinacion);

        var rojo = await s.EmpeoramientoTelefonicoAsync(Recepcion, 1, Nivel("Rojo"));
        Assert.Equal(("Rojo", AsistenciaService.Coordinacion), (rojo.Valor!.Nivel, rojo.Valor.Estado));
    }

    [Fact]
    public async Task Empeoramiento_sube_a_rojo_y_activa_la_guardia_pero_nunca_baja()
    {
        var (s, d, _) = Escenario();
        await s.CrearDesdeAppAsync(Ana, new SolicitudAppDto { AnimalId = 1, Nivel = "Amarillo", Modalidad = AsistenciaService.Visita, Direccion = "Rivera 1234", DistanciaKm = 6, Sintomas = "Vómitos", TelefonoContacto = "099" });

        var baja = await s.EmpeoramientoClienteAsync(Ana, 1, Nivel("Verde"));
        Assert.Equal("Amarillo", baja.Valor!.Nivel);
        Assert.StartsWith("Registrado. El nivel no baja", baja.Valor.Mensaje);

        var rojo = await s.EmpeoramientoClienteAsync(Ana, 1, Nivel("Rojo"));
        Assert.Equal(("Rojo", AsistenciaService.Ofrecida), (rojo.Valor!.Nivel, rojo.Valor.Estado));
        Assert.Empty(rojo.Valor.OfrecidaA); // el cliente no ve a quién se ofreció
        Assert.Equal(404, (await s.EmpeoramientoClienteAsync(Pedro, 1, Nivel("Rojo"))).Estado);

        // Repetir el mismo nivel deja el evento pero no vuelve a ofrecer.
        var eventos = d.Eventos.Count;
        await s.EmpeoramientoClienteAsync(Ana, 1, Nivel("Rojo"));
        Assert.Equal(eventos + 1, d.Eventos.Count);
        Assert.Single(d.Solicitudes[0].Ofertas);
    }

    [Fact]
    public async Task Plazos_rojo_a_los_10_minutos_y_reevaluacion_a_las_24_horas_sin_bajar_el_nivel()
    {
        var (s, _, r) = Escenario();
        await s.CrearTelefonicaAsync(Recepcion, Telefonica(Ana, "Rojo", animal: 1));
        await s.CrearTelefonicaAsync(Recepcion, Telefonica(Pedro, "Amarillo", especie: Vaca));

        r.Ahora = Lunes10.AddMinutes(9);
        Assert.Equal(0, (await s.RevisarPlazosAsync()).RojosEscalados);
        r.Ahora = Lunes10.AddMinutes(10);
        Assert.Equal(1, (await s.RevisarPlazosAsync()).RojosEscalados);
        Assert.Equal(AsistenciaService.Coordinacion, (await s.DetalleAsync(1)).Valor!.Estado);

        r.Ahora = Lunes10.AddHours(24);
        Assert.Equal(2, (await s.RevisarPlazosAsync()).ReevaluacionesPedidas);
        Assert.Equal(0, (await s.RevisarPlazosAsync()).ReevaluacionesPedidas);
        var amarilla = (await s.DetalleAsync(2)).Valor!;
        Assert.True(amarilla.ReevaluacionPedida);
        Assert.Equal(("Amarillo", AsistenciaService.Pendiente), (amarilla.Nivel, amarilla.Estado));
    }

    [Fact]
    public async Task Asignar_aceptar_con_estimacion_recien_al_aceptar_y_el_primero_se_la_queda()
    {
        var (s, _, _) = Escenario();
        await s.CrearTelefonicaAsync(Recepcion, Telefonica(Ana, "Amarillo", animal: 1, km: 6));

        Assert.Equal("NO_ELEGIBLE", (await s.AsignarAsync(Recepcion, 1, Silveira)).Codigo);
        Assert.Equal(new[] { Ferreira }, (await s.ElegiblesAsync(1)).Valor!.Select(e => e.CuentaId));
        var ofrecida = await s.AsignarAsync(Recepcion, 1, Ferreira);
        Assert.Null(ofrecida.Valor!.EtaMinutos); // prueba 7
        Assert.Equal("NO_ASIGNABLE", (await s.AsignarAsync(Recepcion, 1, Ferreira)).Codigo);

        Assert.Equal("NO_OFRECIDA", (await s.AceptarAsync(Silveira, 1)).Codigo);
        var aceptada = await s.AceptarAsync(Ferreira, 1);
        Assert.Equal((AsistenciaService.Aceptada, 27), (aceptada.Valor!.Estado, aceptada.Valor.EtaMinutos!.Value)); // 15 + 6 × 2
        Assert.Equal("Ya la habías aceptado.", (await s.AceptarAsync(Ferreira, 1)).Valor!.Mensaje);
        Assert.Contains(ofrecida.Valor.Version, new[] { 1, 2 });
    }

    [Fact]
    public async Task Rechazo_vuelve_a_la_cola_y_en_un_rojo_va_a_Recepcion()
    {
        var (s, _, _) = Escenario();
        await s.CrearTelefonicaAsync(Recepcion, Telefonica(Ana, "Verde", animal: 1));
        await s.AsignarAsync(Recepcion, 1, Ferreira);
        var rechazo = await s.RechazarAsync(Ferreira, 1, "Estoy en cirugía");
        Assert.Equal(AsistenciaService.Pendiente, rechazo.Valor!.Estado);
        Assert.Equal("NO_OFRECIDA", (await s.RechazarAsync(Ferreira, 1, "Otra vez")).Codigo);
        Assert.True((await s.AsignarAsync(Recepcion, 1, Ferreira)).Exito); // se le puede volver a ofrecer

        await s.CrearTelefonicaAsync(Recepcion, Telefonica(Ana, "Rojo", animal: 1));
        var rojo = await s.RechazarAsync(Ferreira, 2, "Sin movilidad");
        Assert.Equal(AsistenciaService.Coordinacion, rojo.Valor!.Estado);
    }

    [Fact]
    public async Task Traslado_comparte_ubicacion_solo_en_camino_y_finalizar_genera_un_solo_cargo()
    {
        var (s, d, _) = Escenario();
        await s.CrearTelefonicaAsync(Recepcion, Telefonica(Ana, "Naranja", animal: 1, km: 6));
        await s.AsignarAsync(Recepcion, 1, Ferreira);
        await s.AceptarAsync(Ferreira, 1);

        Assert.Equal(403, (await s.AvanzarAsync(Silveira, 1)).Estado);
        Assert.True((await s.AvanzarAsync(Ferreira, 1)).Valor!.CompartiendoUbicacion);
        Assert.False((await s.AvanzarAsync(Ferreira, 1)).Valor!.CompartiendoUbicacion); // En el lugar
        Assert.Equal("NO_CERRABLE", (await s.CerrarAsync(Recepcion, 1, new CierreAsistenciaDto { Estado = AsistenciaService.NoConcretada, Motivo = "x" })).Codigo);
        Assert.Equal(AsistenciaService.EnAtencion, (await s.AvanzarAsync(Ferreira, 1)).Valor!.Estado);
        Assert.Equal("NO_AVANZA", (await s.AvanzarAsync(Ferreira, 1)).Codigo);

        var fin = await s.FinalizarAsync(Ferreira, 1, "Gastroenteritis leve, dieta.");
        Assert.Equal(AsistenciaService.Finalizada, fin.Valor!.Estado);
        Assert.Equal(1740m, Assert.Single(d.Cargos).Monto); // 1500 + 6 × 40
        Assert.Equal("La asistencia ya estaba finalizada.", (await s.FinalizarAsync(Ferreira, 1, "Otra vez")).Valor!.Mensaje);
        Assert.Single(d.Cargos);
        Assert.Single(await s.CerradasAsync());
    }

    [Fact]
    public async Task Orientacion_salta_a_atencion_y_no_cobra()
    {
        var (s, d, _) = Escenario();
        await s.CrearTelefonicaAsync(Recepcion, Telefonica(Ana, "Verde", animal: 1, modalidad: AsistenciaService.Orientacion, direccion: ""));
        await s.AsignarAsync(Recepcion, 1, Ferreira);
        var aceptada = await s.AceptarAsync(Ferreira, 1);
        Assert.Null(aceptada.Valor!.EtaMinutos);
        Assert.Equal(AsistenciaService.EnAtencion, (await s.AvanzarAsync(Ferreira, 1)).Valor!.Estado);
        await s.FinalizarAsync(Ferreira, 1, "Se coordinó el traslado.");
        Assert.Empty(d.Cargos);
    }

    [Fact]
    public async Task Cierre_de_un_rojo_solo_despues_de_escalar_y_el_veterinario_puede_bajar_el_nivel()
    {
        var (s, _, r) = Escenario();
        await s.CrearTelefonicaAsync(Recepcion, Telefonica(Ana, "Rojo", animal: 1));
        var cierre = new CierreAsistenciaDto { Estado = AsistenciaService.NoConcretada, Motivo = "No atiende el teléfono" };
        Assert.Equal("ROJO_SIN_ESCALAR", (await s.CerrarAsync(Recepcion, 1, cierre)).Codigo);

        r.Ahora = Lunes10.AddMinutes(10);
        await s.RevisarPlazosAsync();
        Assert.Equal(AsistenciaService.NoConcretada, (await s.CerrarAsync(Recepcion, 1, cierre)).Valor!.Estado);
        Assert.Equal("CERRADA", (await s.CambiarNivelAsync(Ferreira, 1, Nivel("Verde"))).Codigo);

        // Otro rojo ofrecido: la veterinaria lo baja a naranja y vuelve a la cola de Recepción.
        await s.CrearTelefonicaAsync(Recepcion, Telefonica(Ana, "Rojo", animal: 1));
        var bajado = await s.CambiarNivelAsync(Ferreira, 2, Nivel("Naranja"));
        Assert.Equal(("Naranja", AsistenciaService.Pendiente), (bajado.Valor!.Nivel, bajado.Valor.Estado));
        Assert.Equal("Nivel Naranja confirmado.", (await s.CambiarNivelAsync(Ferreira, 2, Nivel("Naranja"))).Valor!.Mensaje);
    }

    [Fact]
    public async Task Cliente_ve_solo_lo_suyo_y_fuera_de_horario_queda_para_el_proximo_dia()
    {
        var (s, _, r) = Escenario();
        r.Ahora = new DateTime(2026, 11, 22, 20, 0, 0); // domingo a la noche
        await s.CrearDesdeAppAsync(Ana, new SolicitudAppDto { AnimalId = 1, Nivel = "Amarillo", Modalidad = AsistenciaService.Visita, UbicacionGps = true, Latitud = -33.87m, Longitud = -58.41m, DistanciaKm = 3, Sintomas = "Cojea", TelefonoContacto = "099" });

        var mia = (await s.MiaAsync(Ana, 1)).Valor!;
        Assert.True(mia.ParaProximoDia);
        Assert.True(mia.UbicacionGps);
        Assert.Equal("Recibida", Assert.Single(mia.Eventos).Tipo);
        Assert.Equal(404, (await s.MiaAsync(Pedro, 1)).Estado);
        Assert.Single(await s.MiasAsync(Ana));
        Assert.Equal("GPS_INCOMPLETO", (await s.CrearDesdeAppAsync(Ana, new SolicitudAppDto { AnimalId = 1, Nivel = "Verde", Modalidad = AsistenciaService.Visita, UbicacionGps = true, Sintomas = "x", TelefonoContacto = "099" })).Codigo);
    }

    [Fact]
    public async Task Tiempo_real_avisa_al_cliente_a_Recepcion_y_a_los_veterinarios_ofrecidos_sin_ubicacion()
    {
        var (_, d, r) = Escenario();
        var aviso = new NotificadorDePrueba();
        var s = new AsistenciaService(d, r, aviso);

        await s.CrearDesdeAppAsync(Ana, new SolicitudAppDto { AnimalId = 1, Nivel = "Rojo", Modalidad = AsistenciaService.Visita, UbicacionGps = true, Latitud = -33.87m, Longitud = -58.41m, DistanciaKm = 3, Sintomas = "Convulsiona", TelefonoContacto = "099" });

        var destinos = aviso.Enviados.Select(e => e.Destino).ToList();
        Assert.Contains($"cuenta-{Ana}", destinos);
        Assert.Contains("rol-Recepcionista", destinos);
        Assert.Contains("rol-Administrador", destinos);
        Assert.Contains(aviso.Enviados, e => e.Destino == $"cuenta-{Ferreira}" && e.Evento == AsistenciaService.EventoOferta);
        Assert.All(aviso.Enviados, e => Assert.DoesNotContain("Latitud", NotificadorDePrueba.Campos(e.Datos)));
        Assert.All(aviso.Enviados, e => Assert.DoesNotContain("Sintomas", NotificadorDePrueba.Campos(e.Datos)));

        // Al escalar por plazo también se avisa.
        aviso.Enviados.Clear();
        r.Ahora = Lunes10.AddMinutes(10);
        await s.RevisarPlazosAsync();
        Assert.Contains(aviso.Enviados, e => e.Destino == "rol-Recepcionista");
    }

    [Fact]
    public void Controladores_tienen_los_roles_correctos()
    {
        Assert.Equal("Cliente", typeof(AsistenciasController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Recepcionista,Administrador", typeof(AsistenciaRecepcionController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Recepcionista", typeof(AsistenciaRecepcionController).GetMethod(nameof(AsistenciaRecepcionController.Crear))!.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Veterinario", typeof(AsistenciaVeterinarioController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Administrador", typeof(GuardiaController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }
}
