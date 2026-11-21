using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Veterinaria.Api.Controllers;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — internación (A-024, RF-VET-09 a RF-VET-11, RF-CLI-09, RN-21 a RN-25). Reloj fijo: lunes 23/11/2026 10:00.
// Ana (cuenta 10) tiene a Luna (perro, 1) y a un rodeo (grupo, 3); Pedro (11) tiene a Canela (caballo, 2).
public class ApiInternacionTests
{
    private static readonly DateTime Ahora = new(2026, 11, 23, 10, 0, 0);
    private const int Recepcion = 1;
    private const int Ferreira = 2;
    private const int Admin = 5;
    private const int Ana = 10;
    private const int Pedro = 11;

    private class Reloj : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Ahora, DateTimeKind.Utc));
    }

    private class InternacionesEnMemoria : IInternacionRepository
    {
        public List<Animal> Animales { get; } = new();
        public List<Internacion> Internaciones { get; } = new();
        public List<Box> Boxes { get; } = new();
        public List<NovedadInternacion> Novedades { get; } = new();
        public List<Auditoria> Auditorias { get; } = new();
        public List<string> Bloqueos { get; } = new();

        private static bool Abierta(Internacion i) => i.Estado is "Admitida" or "Internado";

        public Task<Animal?> ObtenerAnimalAsync(int animalId) => Task.FromResult(Animales.FirstOrDefault(a => a.Id == animalId && a.Activo));
        public Task<bool> EsResponsableAsync(int animalId, int cuentaId)
            => Task.FromResult(Animales.Any(a => a.Id == animalId && a.Responsables.Any(r => r.CuentaId == cuentaId && r.Hasta == null)));
        public Task<Internacion?> ObtenerAbiertaDeAnimalAsync(int animalId) => Task.FromResult(Internaciones.FirstOrDefault(i => i.AnimalId == animalId && Abierta(i)));

        private Internacion ConDatos(Internacion i)
        {
            i.Animal = Animales.First(a => a.Id == i.AnimalId);
            i.Box = Boxes.FirstOrDefault(b => b.Id == i.BoxId);
            return i;
        }

        public Task<Internacion?> ObtenerAsync(int id)
        {
            var i = Internaciones.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(i == null ? null : ConDatos(i));
        }
        public Task<List<Internacion>> ObtenerAbiertasAsync() => Task.FromResult(Internaciones.Where(Abierta).Select(ConDatos).ToList());
        public Task<List<Internacion>> ObtenerAltasAsync(int tomar) => Task.FromResult(Internaciones.Where(i => i.Estado == "Alta").Select(ConDatos).Take(tomar).ToList());
        public Task<List<Internacion>> ObtenerDeClienteAsync(int clienteId)
            => Task.FromResult(Internaciones.Where(i => i.Estado != "Anulada" && Animales.Any(a => a.Id == i.AnimalId && a.Responsables.Any(r => r.CuentaId == clienteId && r.Hasta == null))).Select(ConDatos).ToList());

        public Task<List<Box>> ObtenerBoxesAsync() => Task.FromResult(Boxes.ToList());
        public Task<Box?> ObtenerBoxAsync(int boxId) => Task.FromResult(Boxes.FirstOrDefault(b => b.Id == boxId));
        public Task<bool> ExisteBoxConNombreAsync(string nombre) => Task.FromResult(Boxes.Any(b => string.Equals(b.Nombre, nombre, StringComparison.OrdinalIgnoreCase)));
        public Task<Internacion?> ObtenerOcupanteAsync(int boxId) => Task.FromResult(Internaciones.FirstOrDefault(i => i.BoxId == boxId && i.Estado == "Internado"));
        public Task<List<Internacion>> ObtenerOcupantesAsync() => Task.FromResult(Internaciones.Where(i => i.BoxId != null && i.Estado == "Internado").Select(ConDatos).ToList());

        public Task<List<NovedadInternacion>> ObtenerNovedadesAsync(int internacionId, bool soloVisibles)
            => Task.FromResult(Novedades.Where(n => n.InternacionId == internacionId && (!soloVisibles || n.Visible)).OrderByDescending(n => n.Id).ToList());
        public Task<NovedadInternacion?> ObtenerNovedadAsync(int novedadId) => Task.FromResult(Novedades.FirstOrDefault(n => n.Id == novedadId));

        public void Agregar<T>(T entidad) where T : class
        {
            switch (entidad)
            {
                case Internacion i: i.Id = Internaciones.Count + 1; Internaciones.Add(i); break;
                case NovedadInternacion n: n.Id = Novedades.Count + 1; Novedades.Add(n); break;
                case Box b: b.Id = Boxes.Max(x => x.Id) + 1; Boxes.Add(b); break;
                case Auditoria a: Auditorias.Add(a); break;
                default: throw new InvalidOperationException($"Tipo no esperado: {typeof(T).Name}");
            }
        }

        public Task GuardarAsync() => Task.CompletedTask;

        public async Task<T> EnTransaccionConAnimalAsync<T>(int animalId, Func<Task<T>> accion)
        {
            Bloqueos.Add($"Animales:{animalId}");
            return await accion();
        }

        public async Task<T> EnTransaccionConBoxAsync<T>(int boxId, Func<Task<T>> accion)
        {
            Bloqueos.Add($"Boxes:{boxId}");
            return await accion();
        }
    }

    private static Animal NuevoAnimal(int id, string nombre, string especie, int duenio, bool grupo = false)
    {
        var a = new Animal { Id = id, Nombre = nombre, Especie = new Especie { Nombre = especie }, EsGrupo = grupo, Activo = true };
        a.Responsables.Add(new Responsabilidad { CuentaId = duenio, AnimalId = id, Desde = Ahora.AddYears(-1) });
        return a;
    }

    private static InternacionesEnMemoria Datos()
    {
        var d = new InternacionesEnMemoria();
        d.Animales.Add(NuevoAnimal(1, "Luna", "Perro", Ana));
        d.Animales.Add(NuevoAnimal(2, "Canela", "Caballo", Pedro));
        d.Animales.Add(NuevoAnimal(3, "Rodeo norte", "Vaca", Ana, grupo: true));
        d.Boxes.Add(new Box { Id = 1, Nombre = "Box 1", Tipo = InternacionService.PequenosAnimales, Activo = true });
        d.Boxes.Add(new Box { Id = 2, Nombre = "Box 2", Tipo = InternacionService.PequenosAnimales, Activo = false });
        d.Boxes.Add(new Box { Id = 4, Nombre = "Box equino", Tipo = InternacionService.GrandesAnimales, Activo = true });
        return d;
    }

    private static InternacionService Servicio(InternacionesEnMemoria d) => new(d, new Reloj());

    private static AdmisionDto Admision(int animalId) => new() { AnimalId = animalId, Motivo = "Vómitos y decaimiento", TelefonoContacto = "+59899123456" };

    [Fact]
    public async Task Admision_una_abierta_por_animal_y_sin_grupos()
    {
        var d = Datos();
        var s = Servicio(d);

        var ok = await s.AdmitirAsync(Recepcion, Admision(1));
        Assert.Equal(201, ok.Estado);
        Assert.Equal(("Admitida", Ana), (ok.Valor!.Estado, ok.Valor.ClienteId));
        Assert.Equal("Luna quedó admitido. Falta que un veterinario le asigne box y plan.", ok.Valor.Mensaje);
        Assert.Equal("Animales:1", Assert.Single(d.Bloqueos));

        Assert.Equal("INTERNACION_ABIERTA", (await s.AdmitirAsync(Recepcion, Admision(1))).Codigo);
        Assert.Equal("ES_GRUPO", (await s.AdmitirAsync(Recepcion, Admision(3))).Codigo);
        Assert.Equal(404, (await s.AdmitirAsync(Recepcion, Admision(99))).Estado);
        Assert.Single(d.Internaciones);
    }

    [Fact]
    public async Task Anular_solo_antes_del_ingreso_y_libera_al_animal()
    {
        var d = Datos();
        var s = Servicio(d);
        await s.AdmitirAsync(Recepcion, Admision(1));

        var anulada = await s.AnularAdmisionAsync(Recepcion, 1, new AnulacionAdmisionDto { Motivo = "El responsable se lo llevó" });
        Assert.Equal("Anulada", anulada.Valor!.Estado);
        Assert.Equal("NO_ES_ADMISION", (await s.AnularAdmisionAsync(Recepcion, 1, new AnulacionAdmisionDto { Motivo = "Otra vez" })).Codigo);
        Assert.Equal(201, (await s.AdmitirAsync(Recepcion, Admision(1))).Estado);
    }

    [Fact]
    public async Task Ingreso_con_box_libre_en_servicio_y_del_tipo_de_la_especie()
    {
        var d = Datos();
        var s = Servicio(d);
        await s.AdmitirAsync(Recepcion, Admision(1)); // Luna → 1
        await s.AdmitirAsync(Recepcion, Admision(2)); // Canela → 2

        Assert.Equal("BOX_FUERA_DE_SERVICIO", (await s.IngresarAsync(Ferreira, 1, new IngresoClinicoDto { BoxId = 2, PlanTerapeutico = "Suero" })).Codigo);
        Assert.Equal("BOX_INADECUADO", (await s.IngresarAsync(Ferreira, 1, new IngresoClinicoDto { BoxId = 4, PlanTerapeutico = "Suero" })).Codigo);
        Assert.Equal(new[] { 1 }, (await s.BoxesLibresAsync(1)).Valor!.Select(b => b.Id));
        Assert.Equal(new[] { 4 }, (await s.BoxesLibresAsync(2)).Valor!.Select(b => b.Id));

        var ok = await s.IngresarAsync(Ferreira, 1, new IngresoClinicoDto { BoxId = 1, PlanTerapeutico = "Fluidoterapia 24 h" });
        Assert.Equal(("Internado", "Box 1"), (ok.Valor!.Estado, ok.Valor.Box));
        Assert.Equal("Luna internado en Box 1.", ok.Valor.Mensaje);
        Assert.Contains("Boxes:1", d.Bloqueos);
        Assert.Equal("NO_ESPERA_INGRESO", (await s.IngresarAsync(Ferreira, 1, new IngresoClinicoDto { BoxId = 1, PlanTerapeutico = "Otro" })).Codigo);

        // Otro perro no puede usar el Box 1 mientras Luna está ahí (RN-22).
        d.Animales.Add(NuevoAnimal(5, "Toby", "Perro", Ana));
        await s.AdmitirAsync(Recepcion, Admision(5));
        Assert.Equal("BOX_OCUPADO", (await s.IngresarAsync(Ferreira, 3, new IngresoClinicoDto { BoxId = 1, PlanTerapeutico = "Suero" })).Codigo);
        Assert.True((await s.BoxesAsync()).Single(b => b.Id == 1).Ocupado);
    }

    [Fact]
    public async Task Novedades_visibles_o_internas_y_rectificacion_que_conserva_la_original()
    {
        var d = Datos();
        var s = Servicio(d);
        await s.AdmitirAsync(Recepcion, Admision(1));

        Assert.Equal("NO_EN_CURSO", (await s.PublicarAsync(Ferreira, 1, new NovedadCreateDto { Texto = "Hola", Visible = true })).Codigo);
        await s.IngresarAsync(Ferreira, 1, new IngresoClinicoDto { BoxId = 1, PlanTerapeutico = "Fluidoterapia" });

        Assert.Equal("TIPO_INVALIDO", (await s.PublicarAsync(Ferreira, 1, new NovedadCreateDto { Tipo = "Chisme", Texto = "x" })).Codigo);
        var visible = await s.PublicarAsync(Ferreira, 1, new NovedadCreateDto { Texto = "Luna comió bien.", Visible = true });
        Assert.Equal("Publicado: el cliente lo ve.", visible.Valor!.Mensaje);
        var interna = await s.PublicarAsync(Ferreira, 1, new NovedadCreateDto { Tipo = "Evolución", Texto = "FC 90, mucosas rosadas." });
        Assert.Equal("Nota interna guardada: el cliente no la ve.", interna.Valor!.Mensaje);

        var rect = await s.RectificarAsync(Ferreira, 1, 1, new RectificacionDto { Texto = "Corrección: comió la mitad." });
        Assert.Equal(201, rect.Estado);
        Assert.Equal(3, d.Novedades.Count);
        Assert.Equal("Luna comió bien.", d.Novedades[0].Texto);
        Assert.True(d.Novedades[2].Visible);
        Assert.Equal(1, d.Novedades[2].RectificaId);
        Assert.Equal(404, (await s.RectificarAsync(Ferreira, 1, 99, new RectificacionDto { Texto = "x" })).Estado);

        // El veterinario ve las 3; Recepción y el cliente, solo las 2 visibles, y el cliente sin el plan.
        Assert.Equal(3, (await s.DetalleAsync(1, esVeterinario: true)).Valor!.Novedades.Count);
        Assert.Equal(2, (await s.DetalleAsync(1, esVeterinario: false)).Valor!.Novedades.Count);
        var cliente = (await s.MiaAsync(Ana, 1)).Valor!;
        Assert.Equal(2, cliente.Novedades.Count);
        Assert.Equal("", cliente.PlanTerapeutico);
        Assert.Equal(404, (await s.MiaAsync(Pedro, 1)).Estado);
    }

    [Fact]
    public async Task Alta_pide_evolucion_e_indicaciones_y_libera_el_box()
    {
        var d = Datos();
        var s = Servicio(d);
        await s.AdmitirAsync(Recepcion, Admision(1));
        Assert.Equal("NO_INTERNADO", (await s.DarAltaAsync(Ferreira, 1, new AltaDto { EvolucionFinal = "Bien", IndicacionesDomicilio = "Reposo" })).Codigo);
        await s.IngresarAsync(Ferreira, 1, new IngresoClinicoDto { BoxId = 1, PlanTerapeutico = "Fluidoterapia" });

        Assert.Equal("ALTA_INCOMPLETA", (await s.DarAltaAsync(Ferreira, 1, new AltaDto { EvolucionFinal = "Bien", IndicacionesDomicilio = " " })).Codigo);
        var alta = await s.DarAltaAsync(Ferreira, 1, new AltaDto { EvolucionFinal = "Recuperada", IndicacionesDomicilio = "Dieta blanda 3 días" });
        Assert.Equal("Alta", alta.Valor!.Estado);
        Assert.Equal(Ahora, alta.Valor.FechaAlta);
        Assert.False((await s.BoxesAsync()).Single(b => b.Id == 1).Ocupado);
        Assert.Single(await s.AltasAsync());
        Assert.Empty(await s.AbiertasAsync());
        Assert.Equal("Dieta blanda 3 días", Assert.Single(await s.MiasAsync(Ana)).IndicacionesDomicilio);
        Assert.Empty(await s.MiasAsync(Pedro));
    }

    [Fact]
    public async Task Boxes_sin_nombre_repetido_y_no_se_saca_de_servicio_uno_ocupado()
    {
        var d = Datos();
        var s = Servicio(d);

        Assert.Equal("BOX_INVALIDO", (await s.AgregarBoxAsync(Admin, new BoxCreateDto { Nombre = "Box 5", Tipo = "Gigantes" })).Codigo);
        Assert.Equal("BOX_REPETIDO", (await s.AgregarBoxAsync(Admin, new BoxCreateDto { Nombre = "box 1", Tipo = InternacionService.PequenosAnimales })).Codigo);
        Assert.Equal(201, (await s.AgregarBoxAsync(Admin, new BoxCreateDto { Nombre = "Box 5", Tipo = InternacionService.PequenosAnimales })).Estado);

        await s.AdmitirAsync(Recepcion, Admision(1));
        await s.IngresarAsync(Ferreira, 1, new IngresoClinicoDto { BoxId = 1, PlanTerapeutico = "Fluidoterapia" });
        Assert.Equal("BOX_OCUPADO", (await s.CambiarServicioBoxAsync(Admin, 1, false)).Codigo);
        Assert.True((await s.CambiarServicioBoxAsync(Admin, 2, true)).Valor!.Activo);
        Assert.Equal(2, d.Auditorias.Count);
    }

    [Fact]
    public void Controladores_tienen_los_roles_correctos()
    {
        Assert.Equal("Recepcionista,Veterinario,Administrador", typeof(InternacionesController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Recepcionista", typeof(InternacionesController).GetMethod(nameof(InternacionesController.Admitir))!.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Veterinario", typeof(InternacionesController).GetMethod(nameof(InternacionesController.Ingresar))!.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Veterinario", typeof(InternacionesController).GetMethod(nameof(InternacionesController.Alta))!.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Cliente", typeof(MisInternacionesController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Administrador", typeof(BoxesController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }
}
