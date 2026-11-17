using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// G-009: conjunto grande de datos de demostración (ESPECIFICACION_CONJUNTO_DEMO.md, CD-01 a CD-08).
public class ConjuntoDemoTests
{
    private static Fabrica Cargada()
    {
        var f = new Fabrica();
        Assert.True(f.ConjuntoDemo.Cargar().Exito);
        return f;
    }

    [Fact]
    public void CD01_sin_cargar_la_demostracion_queda_como_antes()
    {
        var f = new Fabrica();

        Assert.False(f.ConjuntoDemo.Cargado);
        Assert.Equal(2, f.Cuentas.Todas().Count);
        Assert.Equal(4, f.Animales.ObtenerTodos().Count);
        Assert.Equal(2, f.Consultas.Firmadas().Count);
    }

    [Fact]
    public void CD02_cargado_hay_30_clientes_50_animales_y_200_consultas()
    {
        var f = Cargada();

        Assert.Equal(ConjuntoDemoService.ClientesTotales, f.Cuentas.Todas().Count);
        Assert.Equal(ConjuntoDemoService.AnimalesTotales, f.Animales.ObtenerTodos().Count);
        Assert.Equal(ConjuntoDemoService.ConsultasTotales, f.Consultas.Firmadas().Count);
    }

    [Fact]
    public void CD03_cargar_dos_veces_no_duplica()
    {
        var f = Cargada();

        var r = f.ConjuntoDemo.Cargar();

        Assert.True(r.Exito);
        Assert.Equal(30, f.Cuentas.Todas().Count);
        Assert.Equal(50, f.Animales.ObtenerTodos().Count);
        Assert.Equal(200, f.Consultas.Firmadas().Count);
    }

    [Fact]
    public void CD04_hay_casos_de_caballo_y_oveja_en_grupo()
    {
        var f = Cargada();
        var animales = f.Animales.ObtenerTodos();

        Assert.True(animales.Count(a => a.Especie == "Caballo") >= 5);
        var ovejas = animales.Where(a => a.Especie == "Oveja").ToList();
        Assert.True(ovejas.Count >= 3);
        Assert.All(ovejas, o => { Assert.True(o.EsGrupo); Assert.True(o.CantidadGrupo > 1); Assert.NotEqual("", o.Caravana); });
    }

    [Fact]
    public void CD05_los_datos_son_unicos_y_validos()
    {
        var f = Cargada();
        var cuentas = f.Cuentas.Todas();
        var animales = f.Animales.ObtenerTodos();

        Assert.Equal(cuentas.Count, cuentas.Select(c => c.Id).Distinct().Count());
        Assert.Equal(cuentas.Count, cuentas.Select(c => c.Correo).Distinct().Count());
        Assert.Equal(animales.Count, animales.Select(a => a.Id).Distinct().Count());
        var chips = animales.Where(a => a.Microchip != "").Select(a => a.Microchip).ToList();
        Assert.Equal(chips.Count, chips.Distinct().Count());
        Assert.All(chips, c => Assert.Equal(15, c.Length));
        Assert.All(animales, a => Assert.NotNull(f.Cuentas.ObtenerPorId(a.ResponsableId)));
        Assert.All(cuentas, c => Assert.True(c.Verificada));
    }

    [Fact]
    public void CD06_cada_consulta_es_firmada_del_veterinario_que_atiende_la_especie()
    {
        var f = Cargada();
        var porId = f.Animales.ObtenerTodos().ToDictionary(a => a.Id);

        foreach (var c in f.Consultas.Firmadas())
        {
            Assert.Equal("Firmada", c.Estado);
            Assert.NotNull(c.FechaFirma);
            Assert.True(c.FechaInicio < DateTime.Now);
            Assert.False(string.IsNullOrWhiteSpace(c.Motivo));
            Assert.False(string.IsNullOrWhiteSpace(c.Diagnostico));
            var especie = porId[c.AnimalId].Especie;
            var profesional = f.Profesionales.ObtenerFicha(c.ProfesionalId)!;
            Assert.Contains(especie, profesional.Especies);
        }
    }

    [Fact]
    public void CD07_los_clientes_nuevos_entran_con_la_contrasena_de_demostracion()
    {
        var f = Cargada();

        var cuenta = f.Cuentas.ValidarCredenciales("cliente01.demo@vitalia.uy", PersonalService.ContrasenaDemo);

        Assert.NotNull(cuenta);
        Assert.Contains(f.Animales.ObtenerDe(cuenta!.Id).Count, new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void CD08_cada_animal_nuevo_tiene_historia_y_los_de_Ana_siguen_igual()
    {
        var f = Cargada();

        foreach (var animal in f.Animales.ObtenerTodos().Where(a => a.Id > 4))
        {
            Assert.NotEmpty(f.Consultas.HistoriaDe(animal.Id));
        }

        Assert.Equal(1, f.Consultas.HistoriaDe(1).Count);
        Assert.Equal(2, f.Animales.ObtenerDe(100).Count);
    }
}
