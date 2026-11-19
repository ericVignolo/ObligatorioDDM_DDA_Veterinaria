using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.Controllers;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — profesionales públicos y datos de la clínica (RF-VIS-02, RF-VIS-03, RF-VIS-06).
// Los repositorios de prueba aplican la misma regla ReglasProfesionales.EsPublico que la consulta a MySQL.
public class ApiProfesionalesTests
{
    private class ProfesionalesEnMemoria : IProfesionalRepository
    {
        private readonly List<Profesional> _datos;
        private readonly Func<Profesional, bool> _esPublico = ReglasProfesionales.EsPublico.Compile();

        public ProfesionalesEnMemoria(params Profesional[] datos)
        {
            _datos = datos.ToList();
        }

        public Task<int> ContarPublicosAsync() => Task.FromResult(_datos.Count(_esPublico));

        public Task<List<Profesional>> ObtenerPublicosAsync(int saltar, int tomar)
            => Task.FromResult(_datos.Where(_esPublico).OrderBy(p => p.Id).Skip(saltar).Take(tomar).ToList());

        public Task<Profesional?> ObtenerPublicoPorIdAsync(int id)
            => Task.FromResult(_datos.Where(_esPublico).FirstOrDefault(p => p.Id == id));
    }

    private class ClinicaEnMemoria : IClinicaRepository
    {
        private readonly Clinica? _clinica;
        public ClinicaEnMemoria(Clinica? clinica) { _clinica = clinica; }
        public Task<Clinica?> ObtenerAsync() => Task.FromResult(_clinica);
    }

    private static readonly Prestacion ConsultaGeneral = new() { Id = 1, Nombre = "Consulta clínica general", Publicado = true, PrecioDesde = 1200 };
    private static readonly Prestacion Guarderia = new() { Id = 7, Nombre = "Guardería", Publicado = false, PrecioDesde = 700 };

    private static FranjaHoraria Franja(DayOfWeek dia, string tipo, int hIni, int mIni, int hFin, int mFin)
        => new() { Dia = dia, Tipo = tipo, Inicio = new TimeSpan(hIni, mIni, 0), Fin = new TimeSpan(hFin, mFin, 0) };

    private static Profesional Ferreira() => new()
    {
        Id = 1,
        CuentaId = 2,
        Nombre = "Dra. Lucía Ferreira",
        Especialidad = "Clínica de pequeños animales",
        Matricula = "VET-4521",
        MatriculaVigenteHasta = new DateTime(2027, 11, 1),
        Publicado = true,
        Activo = true,
        Especies =
        {
            new ProfesionalEspecie { EspecieId = 2, Especie = new Especie { Id = 2, Nombre = "Gato" } },
            new ProfesionalEspecie { EspecieId = 1, Especie = new Especie { Id = 1, Nombre = "Perro" } }
        },
        Prestaciones =
        {
            new ProfesionalPrestacion { PrestacionId = 7, Prestacion = Guarderia },
            new ProfesionalPrestacion { PrestacionId = 1, Prestacion = ConsultaGeneral }
        },
        Franjas =
        {
            // Desordenadas a propósito: el horario sale de lunes a domingo y por hora.
            Franja(DayOfWeek.Saturday, "Jornada", 9, 0, 13, 0),
            Franja(DayOfWeek.Monday, "Descanso", 11, 30, 12, 0),
            Franja(DayOfWeek.Monday, "Jornada", 14, 30, 18, 0),
            Franja(DayOfWeek.Monday, "Jornada", 9, 0, 14, 0),
            Franja(DayOfWeek.Wednesday, "Jornada", 9, 0, 14, 0)
        },
        Ausencias = { new Ausencia { Desde = new DateTime(2026, 11, 18), Hasta = new DateTime(2026, 11, 18), Motivo = "Capacitación" } }
    };

    private static Profesional Otro(int id, bool publicado = true, bool activo = true)
        => new() { Id = id, Nombre = $"Profesional {id}", Publicado = publicado, Activo = activo };

    private static ProfesionalesController Controlador(params Profesional[] datos)
        => new(new ProfesionalService(new ProfesionalesEnMemoria(datos)));

    [Fact]
    public async Task Solo_aparecen_los_publicados_y_activos()
    {
        var servicio = new ProfesionalService(new ProfesionalesEnMemoria(Ferreira(), Otro(2, publicado: false), Otro(3, activo: false), Otro(4)));

        var pagina = await servicio.ListarPublicosAsync(1, 20);

        Assert.Equal(new[] { 1, 4 }, pagina.Items.Select(p => p.Id));
        Assert.Equal(2, pagina.TotalItems);
        Assert.Null(await servicio.ObtenerPublicoAsync(2));
        Assert.Null(await servicio.ObtenerPublicoAsync(3));
    }

    [Fact]
    public async Task Muestra_especies_y_solo_los_servicios_publicos()
    {
        var detalle = await new ProfesionalService(new ProfesionalesEnMemoria(Ferreira())).ObtenerPublicoAsync(1);

        Assert.NotNull(detalle);
        Assert.Equal(new[] { "Gato", "Perro" }, detalle!.Especies);
        var servicio = Assert.Single(detalle.Servicios);
        Assert.Equal("Consulta clínica general", servicio.Nombre);
    }

    [Fact]
    public async Task El_horario_sale_por_dia_de_lunes_a_domingo()
    {
        var detalle = await new ProfesionalService(new ProfesionalesEnMemoria(Ferreira())).ObtenerPublicoAsync(1);

        Assert.Equal(new[] { "Lunes", "Miércoles", "Sábado" }, detalle!.Horario.Select(d => d.Dia));

        var lunes = detalle.Horario[0];
        Assert.Equal(new[] { "09:00-14:00", "14:30-18:00" }, lunes.Jornadas.Select(t => $"{t.Desde}-{t.Hasta}"));
        Assert.Equal("11:30", Assert.Single(lunes.Descansos).Desde);
        Assert.Empty(detalle.Horario[1].Descansos);
    }

    [Fact]
    public void Un_dia_con_solo_descanso_no_aparece()
    {
        var horario = ProfesionalService.ArmarHorario(new[] { Franja(DayOfWeek.Tuesday, "Descanso", 12, 0, 13, 0) });

        Assert.Empty(horario);
    }

    [Fact]
    public async Task No_expone_matricula_cuenta_ni_ausencias()
    {
        var detalle = await new ProfesionalService(new ProfesionalesEnMemoria(Ferreira())).ObtenerPublicoAsync(1);

        var json = JsonSerializer.Serialize(detalle);
        Assert.DoesNotContain("VET-4521", json);
        Assert.DoesNotContain("Matricula", json);
        Assert.DoesNotContain("CuentaId", json);
        Assert.DoesNotContain("Capacitación", json);
    }

    [Fact]
    public async Task Profesional_inexistente_o_no_publicado_es_404()
    {
        var controlador = Controlador(Ferreira(), Otro(2, publicado: false));

        foreach (var id in new[] { 2, 99 })
        {
            var resultado = await controlador.Obtener(id);
            var noEncontrado = Assert.IsType<NotFoundObjectResult>(resultado.Result);
            var problema = Assert.IsType<ProblemDetails>(noEncontrado.Value);
            Assert.Equal("PROFESIONAL_NO_ENCONTRADO", problema.Extensions["code"]);
        }
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 51)]
    public async Task Paginacion_invalida_es_400(int page, int pageSize)
    {
        var resultado = await Controlador(Ferreira()).Listar(page, pageSize);

        var malo = Assert.IsType<BadRequestObjectResult>(resultado.Result);
        Assert.Equal("PAGINACION_INVALIDA", Assert.IsType<ProblemDetails>(malo.Value).Extensions["code"]);
    }

    [Fact]
    public async Task Clinica_devuelve_sus_datos_de_contacto()
    {
        var controlador = new ClinicaController(new ClinicaService(new ClinicaEnMemoria(new Clinica
        {
            Id = 1, Nombre = "VITALIA", Telefono = "+598 4544 0000", TelefonoGuardia = "+598 99 000 000", Cobertura = "Hasta 30 km"
        })));

        var resultado = await controlador.Obtener();

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var clinica = Assert.IsType<ClinicaReadDto>(ok.Value);
        Assert.Equal("VITALIA", clinica.Nombre);
        Assert.Equal("+598 99 000 000", clinica.TelefonoGuardia);
    }

    [Fact]
    public async Task Clinica_sin_datos_es_404()
    {
        var resultado = await new ClinicaController(new ClinicaService(new ClinicaEnMemoria(null))).Obtener();

        var noEncontrado = Assert.IsType<NotFoundObjectResult>(resultado.Result);
        Assert.Equal("CLINICA_NO_CONFIGURADA", Assert.IsType<ProblemDetails>(noEncontrado.Value).Extensions["code"]);
    }
}
