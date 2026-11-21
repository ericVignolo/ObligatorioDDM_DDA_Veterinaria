using Microsoft.AspNetCore.Mvc;
using Veterinaria.Api.Controllers;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — catálogo público GET /api/v1/servicios (RF-VIS-01, D-CAT-02/03/04, pruebas CAT-02 a CAT-06).
// El repositorio de prueba aplica la misma regla ReglasCatalogo.EsPublica que usa la consulta a MySQL.
public class ApiCatalogoTests
{
    private class RepositorioEnMemoria : IPrestacionRepository
    {
        private readonly List<Prestacion> _datos;
        private readonly Func<Prestacion, bool> _esPublica = ReglasCatalogo.EsPublica.Compile();

        public RepositorioEnMemoria(params Prestacion[] datos)
        {
            _datos = datos.ToList();
        }

        public Task<int> ContarPublicasAsync()
            => Task.FromResult(_datos.Count(_esPublica));

        public Task<List<Prestacion>> ObtenerPublicasAsync(int saltar, int tomar)
            => Task.FromResult(_datos.Where(_esPublica).OrderBy(p => p.Id).Skip(saltar).Take(tomar).ToList());

        public Task<Prestacion?> ObtenerPublicaPorIdAsync(int id)
            => Task.FromResult(_datos.Where(_esPublica).FirstOrDefault(p => p.Id == id));
    }

    private static Prestacion Servicio(int id, bool publicado = true, decimal precio = 1000, bool reservable = true)
        => new() { Id = id, Nombre = $"Servicio {id}", Descripcion = "Descripción", Publicado = publicado, PrecioDesde = precio, ReservableEnLinea = reservable, DuracionMinutos = reservable ? 30 : 0 };

    private static ServiciosController Controlador(params Prestacion[] datos)
        => new(new PrestacionService(new RepositorioEnMemoria(datos)));

    [Fact]
    public async Task CAT02_no_aparece_lo_despublicado_ni_sin_precio()
    {
        var servicio = new PrestacionService(new RepositorioEnMemoria(
            Servicio(1),
            Servicio(2, publicado: false),
            Servicio(3, precio: 0)));

        var pagina = await servicio.ListarPublicasAsync(1, 20);

        Assert.Equal(new[] { 1 }, pagina.Items.Select(s => s.Id));
        Assert.Equal(1, pagina.TotalItems);
        Assert.Null(await servicio.ObtenerPublicaAsync(2));
        Assert.Null(await servicio.ObtenerPublicaAsync(3));
    }

    [Fact]
    public async Task CAT02B_no_reservable_aparece_con_su_precio()
    {
        var servicio = new PrestacionService(new RepositorioEnMemoria(Servicio(4, precio: 1500, reservable: false)));

        var detalle = await servicio.ObtenerPublicaAsync(4);

        Assert.NotNull(detalle);
        Assert.False(detalle!.ReservableEnLinea);
        Assert.Equal(1500m, detalle.PrecioReferencia.Importe);
        Assert.Equal("UYU", detalle.PrecioReferencia.Moneda);
    }

    [Fact]
    public async Task CAT03_detalle_inexistente_o_no_publico_es_404_con_problem_details()
    {
        var controlador = Controlador(Servicio(1), Servicio(2, publicado: false));

        foreach (var id in new[] { 2, 99 })
        {
            var resultado = await controlador.Obtener(id);

            var noEncontrado = Assert.IsType<NotFoundObjectResult>(resultado.Result);
            var problema = Assert.IsType<ProblemDetails>(noEncontrado.Value);
            Assert.Equal(404, problema.Status);
            Assert.Equal("SERVICIO_NO_ENCONTRADO", problema.Extensions["code"]);
        }
    }

    [Fact]
    public async Task CAT04_lista_vacia_devuelve_items_vacio()
    {
        var resultado = await Controlador().Listar(1, 20);

        var ok = Assert.IsType<OkObjectResult>(resultado.Result);
        var pagina = Assert.IsType<PaginaDto<ServicioReadDto>>(ok.Value);
        Assert.Empty(pagina.Items);
        Assert.Equal(0, pagina.TotalItems);
    }

    [Fact]
    public async Task CAT05_paginacion_estable_y_total_coherente()
    {
        var datos = Enumerable.Range(1, 25).Select(i => Servicio(i)).Reverse().ToArray();
        var servicio = new PrestacionService(new RepositorioEnMemoria(datos));

        var pagina2 = await servicio.ListarPublicasAsync(2, 10);
        var pagina3 = await servicio.ListarPublicasAsync(3, 10);

        Assert.Equal(Enumerable.Range(11, 10), pagina2.Items.Select(s => s.Id));
        Assert.Equal(Enumerable.Range(21, 5), pagina3.Items.Select(s => s.Id));
        Assert.Equal(25, pagina2.TotalItems);
        Assert.Equal(2, pagina2.Page);
        Assert.Equal(10, pagina2.PageSize);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    [InlineData(-1, 10)]
    public async Task CAT06_paginacion_invalida_es_400(int page, int pageSize)
    {
        var resultado = await Controlador(Servicio(1)).Listar(page, pageSize);

        var malo = Assert.IsType<BadRequestObjectResult>(resultado.Result);
        var problema = Assert.IsType<ProblemDetails>(malo.Value);
        Assert.Equal(400, problema.Status);
        Assert.Equal("PAGINACION_INVALIDA", problema.Extensions["code"]);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1, 50)]
    [InlineData(3, 20)]
    public void Paginacion_valida_dentro_de_los_limites(int page, int pageSize)
    {
        Assert.Null(Paginacion.Validar(page, pageSize));
    }
}
