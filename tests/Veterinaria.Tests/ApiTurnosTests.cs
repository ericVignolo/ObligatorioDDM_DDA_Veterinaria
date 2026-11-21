using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Veterinaria.Api.Controllers;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — turnos (CU-CLI-02, A-014 a A-018, RF-REC-01 a 04, P-RC-02, RN-01 a RN-09).
// Reloj fijo: lunes 23/11/2026 08:00. Mismos profesionales y horarios que la semilla.
public class ApiTurnosTests
{
    private static readonly DateTime Lunes = new(2026, 11, 23, 8, 0, 0);
    private static readonly DateOnly Miercoles = new(2026, 11, 25);
    private static DateTime Mie(int hora, int minuto = 0) => Miercoles.ToDateTime(new TimeOnly(hora, minuto));

    private const int Ana = 10;
    private const int Bruno = 11;
    private const int Recepcion = 1;

    // ---------- Dobles de prueba ----------

    private class Reloj : TimeProvider
    {
        public DateTime Ahora { get; set; } = Lunes;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Ahora, DateTimeKind.Utc));
    }

    private class TurnosEnMemoria : ITurnoRepository
    {
        public List<Profesional> Profesionales { get; } = new();
        public List<Prestacion> Prestaciones { get; } = new();
        public List<Animal> Animales { get; } = new();
        public List<Turno> Turnos { get; } = new();
        public List<Cuenta> Cuentas { get; } = new();
        public List<Auditoria> Auditorias { get; } = new();
        public HashSet<DateOnly> Feriados { get; } = new();
        public Dictionary<string, string> Parametros { get; } = new();

        public Task<Profesional?> ObtenerProfesionalAsync(int id) => Task.FromResult(Profesionales.FirstOrDefault(p => p.Id == id));
        public Task<int?> ObtenerProfesionalIdDeCuentaAsync(int cuentaId)
            => Task.FromResult(Profesionales.Where(p => p.CuentaId == cuentaId && p.Activo).Select(p => (int?)p.Id).FirstOrDefault());
        public Task<Prestacion?> ObtenerPrestacionAsync(int id) => Task.FromResult(Prestaciones.FirstOrDefault(p => p.Id == id));
        public Task<Animal?> ObtenerAnimalDeClienteAsync(int animalId, int clienteId)
            => Task.FromResult(Animales.FirstOrDefault(a => a.Id == animalId && a.Activo && a.Responsables.Any(r => r.CuentaId == clienteId && r.Hasta == null)));
        public Task<List<Turno>> ObtenerTurnosAsync(int profesionalId, int animalId, DateTime desde, DateTime hasta)
            => Task.FromResult(Turnos.Where(t => (t.ProfesionalId == profesionalId || t.AnimalId == animalId) && t.Inicio < hasta && t.Fin > desde && CalculoAgenda.Ocupa(t)).ToList());
        public Task<HashSet<DateOnly>> ObtenerFeriadosAsync(DateOnly desde, DateOnly hasta) => Task.FromResult(Feriados.Where(f => f >= desde && f <= hasta).ToHashSet());
        public Task<Dictionary<string, string>> ObtenerParametrosAsync() => Task.FromResult(new Dictionary<string, string>(Parametros));
        public Task<int> ContarAusenciasAsync(int clienteId, DateTime desde) => Task.FromResult(Turnos.Count(t => t.ClienteId == clienteId && t.Estado == "Ausente" && t.Inicio >= desde));

        private static readonly string[] Terminados = { "Cancelado", "Finalizado", "Ausente" };
        private IEnumerable<Turno> DeCliente(int clienteId, bool proximos, DateTime ahora)
            => Turnos.Where(t => t.ClienteId == clienteId && (proximos ? t.Fin > ahora && !Terminados.Contains(t.Estado) : t.Fin <= ahora || Terminados.Contains(t.Estado)));
        public Task<List<Turno>> ObtenerDeClienteAsync(int clienteId, bool proximos, DateTime ahora, int saltar, int tomar)
            => Task.FromResult(DeCliente(clienteId, proximos, ahora).OrderBy(t => t.Inicio).Skip(saltar).Take(tomar).ToList());
        public Task<int> ContarDeClienteAsync(int clienteId, bool proximos, DateTime ahora) => Task.FromResult(DeCliente(clienteId, proximos, ahora).Count());
        public Task<Turno?> ObtenerAsync(int id) => Task.FromResult(Turnos.FirstOrDefault(t => t.Id == id));
        public Task<List<Turno>> ObtenerDelDiaAsync(DateOnly fecha, int? profesionalId, string? estado)
            => Task.FromResult(Turnos.Where(t => DateOnly.FromDateTime(t.Inicio) == fecha && (profesionalId == null || t.ProfesionalId == profesionalId) && (estado == null || t.Estado == estado)).OrderBy(t => t.Inicio).ToList());
        public Task<List<Cuenta>> BuscarClientesAsync(string texto, int saltar, int tomar)
            => Task.FromResult(Cuentas.Where(c => c.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) || c.Apellido.Contains(texto, StringComparison.OrdinalIgnoreCase)).Skip(saltar).Take(tomar).ToList());
        public Task<int> ContarClientesAsync(string texto) => Task.FromResult(Cuentas.Count(c => c.Nombre.Contains(texto, StringComparison.OrdinalIgnoreCase) || c.Apellido.Contains(texto, StringComparison.OrdinalIgnoreCase)));
        public Task<List<Animal>> ObtenerAnimalesDeClienteAsync(int clienteId)
            => Task.FromResult(Animales.Where(a => a.Activo && a.Responsables.Any(r => r.CuentaId == clienteId && r.Hasta == null)).ToList());

        public void Agregar(Turno turno)
        {
            turno.Id = Turnos.Count + 1;
            turno.Animal = Animales.First(a => a.Id == turno.AnimalId);
            turno.Prestacion = Prestaciones.First(p => p.Id == turno.PrestacionId);
            turno.Profesional = Profesionales.First(p => p.Id == turno.ProfesionalId);
            Turnos.Add(turno);
        }

        public void AgregarAuditoria(Auditoria registro) => Auditorias.Add(registro);
        public Task GuardarAsync() => Task.CompletedTask;
        public Task<T> EnTransaccionConBloqueoAsync<T>(int profesionalId, int animalId, Func<Task<T>> accion) => accion();
    }

    // ---------- Datos: los de la semilla ----------

    private static FranjaHoraria F(string tipo, DayOfWeek dia, int hIni, int mIni, int hFin, int mFin)
        => new() { Tipo = tipo, Dia = dia, Inicio = new TimeSpan(hIni, mIni, 0), Fin = new TimeSpan(hFin, mFin, 0) };

    private static readonly DayOfWeek[] Semana = { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };

    private static Profesional Ferreira()
    {
        var p = new Profesional { Id = 1, CuentaId = 2, Nombre = "Dra. Lucía Ferreira", Publicado = true, Activo = true };
        foreach (var d in Semana)
        {
            p.Franjas.Add(F("Jornada", d, 9, 0, 14, 0));
            p.Franjas.Add(F("Descanso", d, 11, 30, 12, 0));
        }

        p.Prestaciones.AddRange(new[] { 1, 2, 3 }.Select(id => new ProfesionalPrestacion { ProfesionalId = 1, PrestacionId = id }));
        p.Especies.AddRange(new[] { 1, 2 }.Select(id => new ProfesionalEspecie { ProfesionalId = 1, EspecieId = id }));
        return p;
    }

    private static Profesional Silveira()
    {
        var p = new Profesional { Id = 2, CuentaId = 3, Nombre = "Dr. Martín Silveira", Publicado = true, Activo = true };
        foreach (var d in Semana)
        {
            p.Franjas.Add(F("Jornada", d, 14, 0, 19, 0));
        }

        p.Prestaciones.Add(new ProfesionalPrestacion { ProfesionalId = 2, PrestacionId = 1 });
        p.Especies.AddRange(new[] { 7, 8 }.Select(id => new ProfesionalEspecie { ProfesionalId = 2, EspecieId = id }));
        return p;
    }

    private static Animal Animal(int id, int duenio, int especieId = 1, string nombre = "Luna")
    {
        var a = new Animal { Id = id, Nombre = nombre, EspecieId = especieId, Especie = new Especie { Id = especieId, Nombre = especieId == 1 ? "Perro" : "Oveja" }, Activo = true };
        a.Responsables.Add(new Responsabilidad { CuentaId = duenio, AnimalId = id });
        return a;
    }

    private class Escenario
    {
        public TurnosEnMemoria Repo { get; } = new();
        public Reloj Reloj { get; } = new();
        public TurnoService Servicio { get; }

        public Escenario()
        {
            Repo.Profesionales.Add(Ferreira());
            Repo.Profesionales.Add(Silveira());
            Repo.Prestaciones.Add(new Prestacion { Id = 1, Nombre = "Consulta clínica general", DuracionMinutos = 30, ReservableEnLinea = true, Publicado = true, PrecioDesde = 1200 });
            Repo.Prestaciones.Add(new Prestacion { Id = 2, Nombre = "Vacunación", DuracionMinutos = 15, ReservableEnLinea = true, Publicado = true, PrecioDesde = 600 });
            Repo.Prestaciones.Add(new Prestacion { Id = 4, Nombre = "Revisión de ovinos y bovinos", DuracionMinutos = 0, ReservableEnLinea = false, Publicado = true, PrecioDesde = 1500 });
            Repo.Animales.Add(Animal(100, Ana));
            Repo.Animales.Add(Animal(101, Bruno, nombre: "Toby"));
            Repo.Animales.Add(Animal(102, Ana, especieId: 8, nombre: "Majada"));
            Repo.Cuentas.Add(new Cuenta { Id = Ana, Nombre = "Ana", Apellido = "Pérez" });
            Servicio = new TurnoService(Repo, Reloj);
        }

        public Task<Resultado<TurnoReadDto>> ReservarAsync(int cliente, int animal, DateTime inicio, int prestacion = 1, int profesional = 1)
            => Servicio.ReservarAsync(cliente, new ReservaCreateDto { AnimalId = animal, PrestacionId = prestacion, ProfesionalId = profesional, Inicio = inicio });

        public Task<Resultado<TurnoReadDto>> MostradorAsync(int cliente, int animal, DateTime inicio, bool sobreturno = false, string motivo = "")
            => Servicio.ReservarPorMostradorAsync(Recepcion, "Recepcionista", new ReservaMostradorDto
            {
                ClienteId = cliente, AnimalId = animal, PrestacionId = 1, ProfesionalId = 1, Inicio = inicio, Sobreturno = sobreturno, Motivo = motivo
            });

        public void TurnoExistente(int animal, int cliente, DateTime inicio, string estado, int profesional = 1, int minutos = 30)
            => Repo.Agregar(new Turno { AnimalId = animal, ClienteId = cliente, PrestacionId = 1, ProfesionalId = profesional, Inicio = inicio, Fin = inicio.AddMinutes(minutos), Estado = estado });
    }

    private static List<DateTime> Libres(Profesional p, DateOnly fecha, IEnumerable<Turno>? ocupados = null, ISet<DateOnly>? feriados = null, DateTime? ahora = null, int animalId = 100, int duracion = 30)
        => CalculoAgenda.HorariosLibres(p, animalId, duracion, fecha, ocupados ?? new List<Turno>(), feriados ?? new HashSet<DateOnly>(), new ReglasAgenda(), ahora ?? Lunes);

    // ---------- Cálculo de horarios ----------

    [Fact]
    public void Horarios_cada_15_minutos_sin_pisar_el_descanso()
    {
        var libres = Libres(Ferreira(), Miercoles);

        Assert.Equal(Mie(9), libres.First());
        Assert.Equal(Mie(13, 30), libres.Last()); // la última consulta de 30 min termina a las 14:00
        Assert.Contains(Mie(11), libres);          // 11:00–11:30 no pisa el descanso
        Assert.DoesNotContain(Mie(11, 15), libres);
        Assert.DoesNotContain(Mie(11, 30), libres);
        Assert.DoesNotContain(Mie(11, 45), libres);
        Assert.Contains(Mie(12), libres);
        Assert.Equal(16, libres.Count);
    }

    [Fact]
    public void Un_turno_ocupa_toda_su_duracion()
    {
        var ocupado = new Turno { Id = 1, ProfesionalId = 1, AnimalId = 999, Inicio = Mie(9), Fin = Mie(9, 30), Estado = "Confirmado" };

        var libres = Libres(Ferreira(), Miercoles, new[] { ocupado });

        Assert.DoesNotContain(Mie(9), libres);
        Assert.DoesNotContain(Mie(9, 15), libres);
        Assert.Contains(Mie(9, 30), libres);
    }

    [Fact]
    public void El_animal_no_puede_estar_en_dos_turnos_aunque_sea_con_otro_profesional()
    {
        var conOtro = new Turno { Id = 1, ProfesionalId = 2, AnimalId = 100, Inicio = Mie(10), Fin = Mie(10, 30), Estado = "Confirmado" };

        var libres = Libres(Ferreira(), Miercoles, new[] { conOtro });

        Assert.DoesNotContain(Mie(10), libres);
        Assert.DoesNotContain(Mie(9, 45), libres);
    }

    [Fact]
    public void Turnos_cancelados_no_ocupan_y_el_que_se_reprograma_tampoco()
    {
        var cancelado = new Turno { Id = 1, ProfesionalId = 1, AnimalId = 999, Inicio = Mie(9), Fin = Mie(9, 30), Estado = "Cancelado" };
        var propio = new Turno { Id = 2, ProfesionalId = 1, AnimalId = 100, Inicio = Mie(10), Fin = Mie(10, 30), Estado = "Confirmado" };

        var libres = CalculoAgenda.HorariosLibres(Ferreira(), 100, 30, Miercoles, new[] { cancelado, propio }, new HashSet<DateOnly>(), new ReglasAgenda(), Lunes, excluirTurnoId: 2);

        Assert.Contains(Mie(9), libres);
        Assert.Contains(Mie(10), libres);
    }

    [Fact]
    public void RN05_anticipacion_minima_de_2_horas()
    {
        var hoy = Libres(Ferreira(), DateOnly.FromDateTime(Lunes));

        Assert.Equal(Lunes.Date.AddHours(10), hoy.First()); // ahora 08:00 → desde las 10:00
    }

    [Fact]
    public void Sin_horarios_en_feriado_ausencia_fin_de_semana_o_pasado_el_horizonte()
    {
        var conAusencia = Ferreira();
        conAusencia.Ausencias.Add(new Ausencia { Desde = Mie(0), Hasta = Mie(0) });

        Assert.Empty(Libres(Ferreira(), Miercoles, feriados: new HashSet<DateOnly> { Miercoles }));
        Assert.Empty(Libres(conAusencia, Miercoles));
        Assert.Empty(Libres(Ferreira(), new DateOnly(2026, 11, 28))); // sábado
        Assert.Empty(Libres(Ferreira(), DateOnly.FromDateTime(Lunes.AddDays(61))));
    }

    [Fact]
    public void Las_reglas_se_leen_de_los_parametros()
    {
        var reglas = ReglasAgenda.Desde(new Dictionary<string, string> { ["agenda.anticipacion_minima_horas"] = "4", ["agenda.horizonte_dias"] = "abc" });

        Assert.Equal(4, reglas.AnticipacionHoras);
        Assert.Equal(60, reglas.HorizonteDias); // valor inválido → el de siempre
    }

    // ---------- Reserva del cliente ----------

    [Fact]
    public async Task Reservar_crea_el_turno_confirmado_con_la_duracion_del_servicio()
    {
        var e = new Escenario();

        var r = await e.ReservarAsync(Ana, 100, Mie(9));

        Assert.Equal(201, r.Estado);
        Assert.Equal("Confirmado", r.Valor!.Estado);
        Assert.Equal(Mie(9, 30), r.Valor.Fin);
        Assert.Equal("Luna", r.Valor.Animal);
        Assert.True(r.Valor.PuedeGestionar);
    }

    [Fact]
    public async Task CA_RC01_el_segundo_que_reserva_el_mismo_horario_recibe_409()
    {
        var e = new Escenario();
        await e.ReservarAsync(Ana, 100, Mie(9));

        var segundo = await e.ReservarAsync(Bruno, 101, Mie(9, 15));

        Assert.Equal(409, segundo.Estado);
        Assert.Equal("HORARIO_NO_DISPONIBLE", segundo.Codigo);
        Assert.Single(e.Repo.Turnos);
    }

    [Fact]
    public async Task RN18_no_se_reserva_para_un_animal_ajeno()
    {
        var e = new Escenario();

        Assert.Equal("ANIMAL_NO_ENCONTRADO", (await e.ReservarAsync(Bruno, 100, Mie(9))).Codigo);
    }

    [Fact]
    public async Task Combinaciones_no_validas()
    {
        var e = new Escenario();

        Assert.Equal("SERVICIO_NO_RESERVABLE", (await e.ReservarAsync(Ana, 100, Mie(9), prestacion: 4)).Codigo);
        Assert.Equal("PROFESIONAL_SIN_ESPECIE", (await e.ReservarAsync(Ana, 100, Mie(15), profesional: 2)).Codigo);
        Assert.Equal("PROFESIONAL_SIN_SERVICIO", (await e.ReservarAsync(Ana, 102, Mie(15), prestacion: 2, profesional: 2)).Codigo);
    }

    [Fact]
    public async Task Un_horario_fuera_de_la_grilla_no_se_reserva()
    {
        var e = new Escenario();

        Assert.Equal(409, (await e.ReservarAsync(Ana, 100, Mie(9, 7))).Estado);
        Assert.Equal(409, (await e.ReservarAsync(Ana, 100, Mie(11, 30))).Estado); // descanso
    }

    [Fact]
    public async Task RN08_con_3_ausencias_se_reserva_por_Recepcion_y_con_2_se_avisa()
    {
        var e = new Escenario();
        e.TurnoExistente(100, Ana, Lunes.AddDays(-30), "Ausente");
        e.TurnoExistente(100, Ana, Lunes.AddDays(-20), "Ausente");

        var aviso = await e.Servicio.DisponibilidadClienteAsync(Ana, 1, 1, 100, Miercoles);
        Assert.Contains("Si llegás a 3", aviso.Valor!.AvisoAusencias);

        e.TurnoExistente(100, Ana, Lunes.AddDays(-10), "Ausente");
        var bloqueado = await e.ReservarAsync(Ana, 100, Mie(9));
        Assert.Equal(403, bloqueado.Estado);
        Assert.Equal("RESERVA_POR_RECEPCION", bloqueado.Codigo);

        // Recepción sí puede darle el turno (no aplica RN-08).
        Assert.True((await e.MostradorAsync(Ana, 100, Mie(9))).Exito);
    }

    [Fact]
    public async Task Disponibilidad_sin_horarios_sugiere_el_proximo_dia()
    {
        var e = new Escenario();

        var sabado = await e.Servicio.DisponibilidadClienteAsync(Ana, 1, 1, 100, new DateOnly(2026, 11, 28));

        Assert.Empty(sabado.Valor!.Horarios);
        Assert.Equal(new DateOnly(2026, 11, 30), sabado.Valor.ProximoDiaConHorarios); // lunes siguiente
    }

    // ---------- Cancelar y reprogramar ----------

    [Fact]
    public async Task RN06_el_cliente_cancela_hasta_24_horas_antes()
    {
        var e = new Escenario();
        var turno = (await e.ReservarAsync(Ana, 100, Mie(9))).Valor!;

        e.Reloj.Ahora = Mie(9).AddHours(-23);
        var tarde = await e.Servicio.CancelarAsync(Ana, turno.Id, "");
        Assert.Equal("CANCELAR_POR_RECEPCION", tarde.Codigo);

        e.Reloj.Ahora = Lunes;
        var aTiempo = await e.Servicio.CancelarAsync(Ana, turno.Id, "Viaje");
        Assert.Equal("Cancelado", aTiempo.Valor!.Estado);
        Assert.Equal("Cliente", aTiempo.Valor.CanceladoPor);

        // El horario quedó libre.
        Assert.True((await e.ReservarAsync(Bruno, 101, Mie(9))).Exito);
    }

    [Fact]
    public async Task Un_cliente_no_cancela_ni_ve_turnos_ajenos()
    {
        var e = new Escenario();
        var turno = (await e.ReservarAsync(Ana, 100, Mie(9))).Valor!;

        Assert.Equal(404, (await e.Servicio.CancelarAsync(Bruno, turno.Id, "")).Estado);
        Assert.Equal(404, (await e.Servicio.ObtenerMioAsync(Bruno, turno.Id)).Estado);
    }

    [Fact]
    public async Task Reprogramar_conserva_el_numero_y_libera_el_horario_anterior()
    {
        var e = new Escenario();
        var turno = (await e.ReservarAsync(Ana, 100, Mie(9))).Valor!;

        var r = await e.Servicio.ReprogramarAsync(Ana, turno.Id, Mie(10));

        Assert.True(r.Exito);
        Assert.Equal(turno.Id, r.Valor!.Id);
        Assert.Equal(Mie(10), r.Valor.Inicio);
        Assert.Equal(Mie(9), r.Valor.InicioAnterior);
        Assert.True((await e.ReservarAsync(Bruno, 101, Mie(9))).Exito);
    }

    [Fact]
    public async Task Reprogramar_a_un_horario_ocupado_deja_el_turno_como_estaba()
    {
        var e = new Escenario();
        var turno = (await e.ReservarAsync(Ana, 100, Mie(9))).Valor!;
        await e.ReservarAsync(Bruno, 101, Mie(10));

        var r = await e.Servicio.ReprogramarAsync(Ana, turno.Id, Mie(10));

        Assert.Equal("HORARIO_NO_DISPONIBLE", r.Codigo);
        Assert.Equal(Mie(9), e.Repo.Turnos[0].Inicio);
    }

    [Fact]
    public async Task Mis_turnos_separa_proximos_e_historial()
    {
        var e = new Escenario();
        await e.ReservarAsync(Ana, 100, Mie(9));
        e.TurnoExistente(100, Ana, Lunes.AddDays(-7), "Finalizado");

        Assert.Single((await e.Servicio.MisTurnosAsync(Ana, true, 1, 20)).Items);
        Assert.Single((await e.Servicio.MisTurnosAsync(Ana, false, 1, 20)).Items);
        Assert.Empty((await e.Servicio.MisTurnosAsync(Bruno, true, 1, 20)).Items);
    }

    // ---------- Recepción ----------

    [Fact]
    public async Task RN09_sobreturno_con_motivo_sobre_un_horario_tomado_y_queda_auditado()
    {
        var e = new Escenario();
        await e.ReservarAsync(Bruno, 101, Mie(9));

        var sinMotivo = await e.MostradorAsync(Ana, 100, Mie(9), sobreturno: true, motivo: "");
        var normal = await e.MostradorAsync(Ana, 100, Mie(9));
        var sobreturno = await e.MostradorAsync(Ana, 100, Mie(9), sobreturno: true, motivo: "Urgencia leve");

        Assert.Equal("MOTIVO_SOBRETURNO", sinMotivo.Codigo);
        Assert.Equal("HORARIO_NO_DISPONIBLE", normal.Codigo);
        Assert.True(sobreturno.Exito);
        Assert.True(sobreturno.Valor!.EsSobreturno);
        var auditoria = Assert.Single(e.Repo.Auditorias);
        Assert.Equal("Sobreturno", auditoria.Accion);
        Assert.DoesNotContain("Urgencia", auditoria.Detalle); // el motivo puede ser clínico
        Assert.Equal(Recepcion, e.Repo.Turnos[^1].CreadoPorId);
    }

    [Fact]
    public async Task Sobreturno_fuera_de_la_jornada_o_con_el_animal_ocupado_se_rechaza()
    {
        var e = new Escenario();
        await e.ReservarAsync(Ana, 100, Mie(10));

        var deNoche = await e.MostradorAsync(Ana, 100, Mie(20), sobreturno: true, motivo: "Urgencia leve");
        var animalOcupado = await e.MostradorAsync(Ana, 100, Mie(10, 15), sobreturno: true, motivo: "Urgencia leve");

        Assert.Equal("FUERA_DE_JORNADA", deNoche.Codigo);
        Assert.Equal("ANIMAL_OCUPADO", animalOcupado.Codigo);
    }

    [Fact]
    public async Task Llegada_solo_el_mismo_dia_y_ausencia_pasados_15_minutos()
    {
        var e = new Escenario();
        var turno = (await e.ReservarAsync(Ana, 100, Mie(9))).Valor!;

        Assert.Equal("OTRO_DIA", (await e.Servicio.RegistrarLlegadaAsync(turno.Id)).Codigo);

        e.Reloj.Ahora = Mie(9, 10);
        Assert.Equal("ANTES_DE_TOLERANCIA", (await e.Servicio.RegistrarAusenciaAsync(turno.Id)).Codigo);

        var llegada = await e.Servicio.RegistrarLlegadaAsync(turno.Id);
        Assert.Equal("En espera", llegada.Valor!.Estado);
        Assert.Equal(Mie(9, 10), llegada.Valor.HoraLlegada);

        e.TurnoExistente(101, Bruno, Mie(9, 30), "Confirmado");
        var otro = e.Repo.Turnos[^1];
        e.Reloj.Ahora = Mie(9, 46);
        Assert.Equal("Ausente", (await e.Servicio.RegistrarAusenciaAsync(otro.Id)).Valor!.Estado);
    }

    [Fact]
    public async Task Recepcion_cancela_dentro_de_las_24_horas_con_motivo()
    {
        var e = new Escenario();
        var turno = (await e.ReservarAsync(Ana, 100, Mie(9))).Valor!;
        e.Reloj.Ahora = Mie(8);

        Assert.Equal("FALTA_MOTIVO", (await e.Servicio.CancelarPorRecepcionAsync(turno.Id, " ")).Codigo);
        var r = await e.Servicio.CancelarPorRecepcionAsync(turno.Id, "Avisó por teléfono");

        Assert.Equal("Cancelado", r.Valor!.Estado);
        Assert.Equal("Recepción", r.Valor.CanceladoPor);
    }

    [Fact]
    public async Task Buscar_clientes_trae_sus_animales_y_ausencias()
    {
        var e = new Escenario();

        var r = await e.Servicio.BuscarClientesAsync("pér", 1, 20);

        var ana = Assert.Single(r.Items);
        Assert.Equal(new[] { "Luna", "Majada" }, ana.Animales.Select(a => a.Nombre).OrderBy(n => n));
    }

    // ---------- Veterinario ----------

    [Fact]
    public async Task El_veterinario_ve_solo_su_agenda()
    {
        var e = new Escenario();
        await e.ReservarAsync(Ana, 100, Mie(9));
        await e.ReservarAsync(Ana, 102, Mie(15), profesional: 2);

        var ferreira = await e.Servicio.AgendaDelVeterinarioAsync(2, Miercoles);
        var sinProfesional = await e.Servicio.AgendaDelVeterinarioAsync(99, Miercoles);

        Assert.Equal(new[] { "Luna" }, ferreira.Valor!.Select(t => t.Animal));
        Assert.Equal("SIN_PROFESIONAL", sinProfesional.Codigo);
    }

    // ---------- Permisos ----------

    [Fact]
    public void Cada_controller_exige_su_rol()
    {
        Assert.Equal("Cliente", typeof(TurnosController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Recepcionista,Administrador", typeof(RecepcionController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Veterinario", typeof(VeterinarioController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);

        foreach (var metodo in new[] { "RegistrarLlegada", "RegistrarAusencia", "Cancelar" })
        {
            Assert.Equal("Recepcionista", typeof(RecepcionController).GetMethod(metodo)!.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        }
    }
}
