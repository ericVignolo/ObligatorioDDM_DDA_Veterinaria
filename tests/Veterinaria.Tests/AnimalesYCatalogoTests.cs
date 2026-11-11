namespace Veterinaria.Tests;

// A-003, A-004, A-012, A-013: catálogo, profesionales y animales.
public class AnimalesYCatalogoTests
{
    [Fact]
    public void Catalogo_no_muestra_lo_no_publicado()
    {
        var f = new Fabrica();

        Assert.DoesNotContain(f.Prestaciones.ObtenerPublicas(), p => p.Nombre == "Guardería");
        Assert.Null(f.Prestaciones.ObtenerPorId(7));
        Assert.Contains(f.Prestaciones.ObtenerReservables(), p => p.Nombre == "Baño y estética" && p.DuracionMinutos == 60);
    }

    [Theory]
    [InlineData(6, "Perro", new[] { 5 })]
    [InlineData(1, "Caballo", new[] { 3 })]
    [InlineData(2, "Caballo", new int[0])]
    [InlineData(1, "Oveja", new[] { 2 })]
    public void Solo_aparecen_profesionales_del_servicio_y_la_especie(int prestacionId, string especie, int[] esperados)
    {
        var f = new Fabrica();

        var ids = f.Profesionales.ObtenerPara(prestacionId, especie).Select(p => p.Id).ToArray();

        Assert.Equal(esperados, ids);
    }

    [Fact]
    public void Profesional_no_publicado_no_aparece()
    {
        var f = new Fabrica();

        Assert.Null(f.Profesionales.ObtenerPorId(4));
    }

    [Fact]
    public void Microchip_repetido_se_rechaza()
    {
        var f = new Fabrica();

        var resultado = f.Animales.Crear(new() { Nombre = "Otro", Especie = "Perro", Sexo = "Macho", Microchip = "858000000000001" }, 1);

        Assert.False(resultado.Exito);
    }

    [Fact]
    public void Cliente_no_ve_ni_edita_animales_ajenos()
    {
        var f = new Fabrica();

        Assert.Null(f.Animales.ObtenerPorId(1, 999));
        Assert.False(f.Animales.Editar(1, new() { Nombre = "Robado", Especie = "Perro", Sexo = "Macho" }, 999).Exito);
        Assert.All(f.Animales.ObtenerDe(100), a => Assert.Equal(100, a.ResponsableId));
    }

    [Fact]
    public void Grupo_no_guarda_microchip()
    {
        var f = new Fabrica();

        f.Animales.Crear(new() { Nombre = "Majada", Especie = "Oveja", Sexo = "Hembra", EsGrupo = true, CantidadGrupo = 10, Microchip = "858000000999999" }, 1);
        var majada = f.Animales.ObtenerDe(1).Single();

        Assert.Equal("", majada.Microchip);
        Assert.Equal(10, majada.CantidadGrupo);
    }
}
