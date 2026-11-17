using System.Text.RegularExpressions;
using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// A-030: recorridos demostrativos y datos de partida repetibles (ESPECIFICACION_RECORRIDOS_DEMO.md, RD-01 a RD-10).
public class RecorridosTests
{
    private static readonly string[] Roles = { "Visitante", "Cliente", "Recepción", "Veterinario", "Administrador" };

    private static string CarpetaDeVistas()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null)
        {
            var candidata = Path.Combine(carpeta.FullName, "desktop", "Veterinaria.Blazor", "Components");
            if (Directory.Exists(candidata))
            {
                return candidata;
            }

            carpeta = carpeta.Parent;
        }

        throw new DirectoryNotFoundException("No se encontró desktop/Veterinaria.Blazor/Components");
    }

    [Fact]
    public void Hay_un_recorrido_por_rol_en_orden()
    {
        var f = new Fabrica();

        Assert.Equal(Roles, f.Recorridos.Todos().Select(r => r.Rol).ToArray());
    }

    [Fact]
    public void Cada_recorrido_tiene_pasos_completos()
    {
        var f = new Fabrica();

        foreach (var r in f.Recorridos.Todos())
        {
            Assert.False(string.IsNullOrWhiteSpace(r.Titulo));
            Assert.False(string.IsNullOrWhiteSpace(r.Resumen));
            Assert.True(r.Pasos.Count >= 5, $"{r.Rol} tiene pocos pasos");
            foreach (var p in r.Pasos)
            {
                Assert.False(string.IsNullOrWhiteSpace(p.Accion));
                Assert.False(string.IsNullOrWhiteSpace(p.Resultado));
                Assert.NotNull(p.Ruta);
            }
        }
    }

    [Fact]
    public void DeRol_devuelve_el_recorrido_o_nulo()
    {
        var f = new Fabrica();

        Assert.Equal("Cliente", f.Recorridos.DeRol("Cliente")!.Rol);
        Assert.Null(f.Recorridos.DeRol("Inexistente"));
    }

    [Fact]
    public void Solo_el_visitante_no_tiene_correo()
    {
        var f = new Fabrica();

        Assert.Equal("", f.Recorridos.DeRol("Visitante")!.Correo);
        Assert.All(f.Recorridos.Todos().Where(r => r.Rol != "Visitante"), r => Assert.False(string.IsNullOrWhiteSpace(r.Correo)));
    }

    [Fact]
    public void Cada_recorrido_con_correo_puede_iniciarse_con_la_contrasena_de_demostracion()
    {
        foreach (var rol in Roles.Where(r => r != "Visitante"))
        {
            var f = new Fabrica();
            var recorrido = f.Recorridos.DeRol(rol)!;

            var resultado = f.Sesion.Ingresar(new InicioSesion { Correo = recorrido.Correo, Contrasena = PersonalService.ContrasenaDemo });

            Assert.True(resultado.Exito, $"{rol}: {resultado.Mensaje}");
            Assert.Equal(rol != "Cliente", f.Sesion.EsPersonal);
            Assert.Equal(rol == "Cliente", f.Sesion.Iniciada);
        }
    }

    [Fact]
    public void Los_clientes_de_demostracion_ingresan_y_una_contrasena_incorrecta_se_rechaza()
    {
        var f = new Fabrica();

        Assert.False(f.Sesion.Ingresar(new InicioSesion { Correo = "pedro.demo@vitalia.uy", Contrasena = "otra" }).Exito);
        Assert.True(f.Sesion.Ingresar(new InicioSesion { Correo = "pedro.demo@vitalia.uy", Contrasena = PersonalService.ContrasenaDemo }).Exito);
        Assert.Equal(101, f.Sesion.CuentaActual!.Id);
    }

    [Fact]
    public void Cada_paso_apunta_a_una_pantalla_existente()
    {
        var patrones = Directory.GetFiles(CarpetaDeVistas(), "*.razor", SearchOption.AllDirectories)
            .SelectMany(File.ReadAllLines)
            .Select(l => Regex.Match(l, @"^@page ""/([^""]*)"""))
            .Where(m => m.Success)
            .Select(m => m.Groups[1].Value)
            .ToList();
        var f = new Fabrica();

        var sinPantalla = f.Recorridos.Todos()
            .SelectMany(r => r.Pasos.Select(p => (r.Rol, p.Ruta)))
            .Where(x => !patrones.Contains(x.Ruta))
            .Select(x => $"{x.Rol}: «{x.Ruta}»")
            .ToList();

        Assert.Empty(sinPantalla);
    }

    [Fact]
    public void Los_datos_de_partida_son_repetibles()
    {
        var a = new Fabrica();
        var b = new Fabrica();

        Assert.Equal(a.Cuentas.Todas().Count, b.Cuentas.Todas().Count);
        Assert.Equal(a.Personal.ObtenerTodos().Count, b.Personal.ObtenerTodos().Count);
        Assert.Equal(a.Prestaciones.ObtenerTodas().Count, b.Prestaciones.ObtenerTodas().Count);
        Assert.Equal(
            a.Animales.ObtenerDe(100).Select(x => x.Nombre).ToArray(),
            b.Animales.ObtenerDe(100).Select(x => x.Nombre).ToArray());
    }

    [Fact]
    public void Cada_cliente_de_demostracion_ve_solo_sus_animales()
    {
        var f = new Fabrica();

        Assert.Equal(new[] { "Mishi", "Toby" }, f.Animales.ObtenerDe(100).Select(a => a.Nombre).ToArray());
        Assert.Equal(new[] { "Canela", "Rodeo La Esperanza" }, f.Animales.ObtenerDe(101).Select(a => a.Nombre).ToArray());
    }
}
