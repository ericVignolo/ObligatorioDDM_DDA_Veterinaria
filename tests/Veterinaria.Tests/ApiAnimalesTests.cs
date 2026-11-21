using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Veterinaria.Api.Controllers;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — animales del cliente (A-012, A-013, RF-CLI-01, RN-18 solo los propios, RN-41 microchip único).
public class ApiAnimalesTests
{
    private const int Ana = 10;
    private const int Bruno = 11;

    private class AnimalesEnMemoria : IAnimalRepository
    {
        public List<Animal> Animales { get; } = new();
        public List<Turno> Turnos { get; } = new();
        public List<Especie> Especies { get; } = new()
        {
            new Especie { Id = 1, Nombre = "Perro" },
            new Especie { Id = 8, Nombre = "Oveja" }
        };

        private IEnumerable<Animal> DeCliente(int cuentaId)
            => Animales.Where(a => a.Activo && a.Responsables.Any(r => r.CuentaId == cuentaId && r.Hasta == null));

        public Task<int> ContarDeClienteAsync(int cuentaId) => Task.FromResult(DeCliente(cuentaId).Count());
        public Task<List<Animal>> ObtenerDeClienteAsync(int cuentaId, int saltar, int tomar)
            => Task.FromResult(DeCliente(cuentaId).OrderBy(a => a.Nombre).ThenBy(a => a.Id).Skip(saltar).Take(tomar).ToList());
        public Task<Animal?> ObtenerDeClienteAsync(int id, int cuentaId) => Task.FromResult(DeCliente(cuentaId).FirstOrDefault(a => a.Id == id));
        public Task<bool> ExisteMicrochipAsync(string microchip, int? excepto)
            => Task.FromResult(Animales.Any(a => a.Microchip == microchip && a.Id != excepto));
        public Task<Especie?> ObtenerEspecieAsync(int id) => Task.FromResult(Especies.FirstOrDefault(e => e.Id == id));
        public Task<List<Especie>> ObtenerEspeciesAsync() => Task.FromResult(Especies.ToList());
        public Task<bool> TieneTurnosPendientesAsync(int animalId, DateTime ahora)
            => Task.FromResult(Turnos.Any(t => t.AnimalId == animalId && t.Fin > ahora && t.Estado is not ("Cancelado" or "Finalizado" or "Ausente")));

        public void Agregar(Animal animal)
        {
            animal.Id = Animales.Count + 1;
            Animales.Add(animal);
        }

        public Task GuardarAsync() => Task.CompletedTask;
    }

    private static AnimalCreateDto Datos(string nombre = "Luna", string microchip = "", int especieId = 1) => new()
    {
        Nombre = nombre,
        EspecieId = especieId,
        Sexo = "Hembra",
        Microchip = microchip,
        FechaNacimiento = new DateOnly(2021, 3, 10)
    };

    private static AnimalUpdateDto Edicion(string nombre, string microchip = "") => new()
    {
        Nombre = nombre,
        EspecieId = 1,
        Sexo = "Hembra",
        Microchip = microchip
    };

    private static List<ValidationResult> Validar(object dto)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), errores, validateAllProperties: true);
        return errores;
    }

    private static (AnimalService Servicio, AnimalesEnMemoria Repo) Nuevo()
    {
        var repo = new AnimalesEnMemoria();
        return (new AnimalService(repo, TimeProvider.System), repo);
    }

    [Fact]
    public async Task Alta_deja_al_cliente_como_responsable_y_normaliza_los_datos()
    {
        var (servicio, repo) = Nuevo();

        var r = await servicio.CrearAsync(new AnimalCreateDto { Nombre = "  Luna ", EspecieId = 1, Sexo = "Hembra", Caravana = "uy-123", Microchip = "" }, Ana);

        Assert.Equal(201, r.Estado);
        Assert.Equal("Luna", r.Valor!.Nombre);
        Assert.Equal("Perro", r.Valor.Especie);
        Assert.Equal("UY-123", r.Valor.Caravana);
        var animal = Assert.Single(repo.Animales);
        Assert.Null(animal.Microchip); // vacío se guarda como NULL para no chocar con el índice único
        Assert.Equal(Ana, Assert.Single(animal.Responsables).CuentaId);
    }

    [Fact]
    public async Task RN18_cada_cliente_ve_solo_sus_animales()
    {
        var (servicio, _) = Nuevo();
        var deAna = (await servicio.CrearAsync(Datos("Luna"), Ana)).Valor!;
        await servicio.CrearAsync(Datos("Toby"), Bruno);

        var lista = await servicio.ListarAsync(Ana, 1, 20);

        Assert.Equal(new[] { "Luna" }, lista.Items.Select(a => a.Nombre));
        Assert.Equal(1, lista.TotalItems);
        Assert.Equal("ANIMAL_NO_ENCONTRADO", (await servicio.ObtenerAsync(deAna.Id, Bruno)).Codigo);
    }

    [Fact]
    public async Task RN18_no_se_puede_editar_ni_dar_de_baja_un_animal_ajeno()
    {
        var (servicio, repo) = Nuevo();
        var deAna = (await servicio.CrearAsync(Datos("Luna"), Ana)).Valor!;

        var editar = await servicio.EditarAsync(deAna.Id, Edicion("Robada"), Bruno);
        var baja = await servicio.DarDeBajaAsync(deAna.Id, Bruno);

        Assert.Equal(404, editar.Estado);
        Assert.Equal(404, baja.Estado);
        Assert.Equal("Luna", repo.Animales[0].Nombre);
        Assert.True(repo.Animales[0].Activo);
    }

    [Fact]
    public async Task RN41_microchip_repetido_en_alta_y_en_edicion()
    {
        var (servicio, _) = Nuevo();
        await servicio.CrearAsync(Datos("Luna", microchip: "858000012345678"), Ana);
        var toby = (await servicio.CrearAsync(Datos("Toby"), Bruno)).Valor!;

        var alta = await servicio.CrearAsync(Datos("Otra", microchip: "858000012345678"), Bruno);
        var edicion = await servicio.EditarAsync(toby.Id, Edicion("Toby", microchip: "858000012345678"), Bruno);

        Assert.Equal("MICROCHIP_REPETIDO", alta.Codigo);
        Assert.Equal("MICROCHIP_REPETIDO", edicion.Codigo);
    }

    [Fact]
    public async Task Editar_conservando_su_propio_microchip_no_es_repetido()
    {
        var (servicio, _) = Nuevo();
        var luna = (await servicio.CrearAsync(Datos("Luna", microchip: "858000012345678"), Ana)).Valor!;

        var r = await servicio.EditarAsync(luna.Id, Edicion("Luna Blanca", microchip: "858000012345678"), Ana);

        Assert.True(r.Exito);
        Assert.Equal("Luna Blanca", r.Valor!.Nombre);
    }

    [Fact]
    public async Task Especie_inexistente_se_rechaza()
    {
        var (servicio, _) = Nuevo();

        Assert.Equal("ESPECIE_INVALIDA", (await servicio.CrearAsync(Datos(especieId: 99), Ana)).Codigo);
    }

    [Fact]
    public async Task Un_animal_individual_no_guarda_cantidad_de_grupo()
    {
        var (servicio, repo) = Nuevo();
        var dto = Datos();
        dto.EsGrupo = false;
        dto.CantidadGrupo = 30;

        await servicio.CrearAsync(dto, Ana);

        Assert.Null(repo.Animales[0].CantidadGrupo);
    }

    [Fact]
    public void Validaciones_del_formulario()
    {
        var grupoSinCantidad = Datos();
        grupoSinCantidad.EsGrupo = true;
        var chipCorto = Datos(microchip: "12345");
        var futuro = Datos();
        futuro.FechaNacimiento = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        var sexoRaro = Datos();
        sexoRaro.Sexo = "Otro";
        var grupo = Datos("Majada", especieId: 8);
        grupo.EsGrupo = true;
        grupo.CantidadGrupo = 40;

        Assert.Contains(Validar(grupoSinCantidad), v => v.ErrorMessage!.Contains("cuántos animales"));
        Assert.Contains(Validar(chipCorto), v => v.ErrorMessage!.Contains("15 números"));
        Assert.Contains(Validar(futuro), v => v.ErrorMessage!.Contains("futura"));
        Assert.NotEmpty(Validar(sexoRaro));
        Assert.Empty(Validar(grupo));
    }

    [Fact]
    public async Task Baja_logica_lo_saca_de_la_lista_sin_borrarlo()
    {
        var (servicio, repo) = Nuevo();
        var luna = (await servicio.CrearAsync(Datos("Luna"), Ana)).Valor!;

        var r = await servicio.DarDeBajaAsync(luna.Id, Ana);

        Assert.True(r.Exito);
        Assert.Single(repo.Animales);
        Assert.False(repo.Animales[0].Activo);
        Assert.Empty((await servicio.ListarAsync(Ana, 1, 20)).Items);
        Assert.Equal(404, (await servicio.ObtenerAsync(luna.Id, Ana)).Estado);
    }

    [Fact]
    public async Task No_se_da_de_baja_con_turnos_pendientes()
    {
        var (servicio, repo) = Nuevo();
        var luna = (await servicio.CrearAsync(Datos("Luna"), Ana)).Valor!;
        repo.Turnos.Add(new Turno { AnimalId = luna.Id, Inicio = DateTime.Now.AddDays(2), Fin = DateTime.Now.AddDays(2).AddMinutes(30), Estado = "Confirmado" });
        repo.Turnos.Add(new Turno { AnimalId = luna.Id, Inicio = DateTime.Now.AddDays(3), Fin = DateTime.Now.AddDays(3).AddMinutes(30), Estado = "Cancelado" });

        var r = await servicio.DarDeBajaAsync(luna.Id, Ana);

        Assert.Equal(409, r.Estado);
        Assert.Equal("ANIMAL_CON_TURNOS", r.Codigo);
        Assert.True(repo.Animales[0].Activo);
    }

    [Fact]
    public async Task Especies_para_el_formulario()
    {
        var (servicio, _) = Nuevo();

        Assert.Equal(new[] { "Perro", "Oveja" }, (await servicio.ListarEspeciesAsync()).Select(e => e.Nombre));
    }

    [Fact]
    public void Animales_exige_token_de_cliente_y_especies_es_publico()
    {
        var autorizacion = typeof(AnimalesController).GetCustomAttribute<AuthorizeAttribute>();

        Assert.NotNull(autorizacion);
        Assert.Equal("Cliente", autorizacion!.Roles);
        Assert.Null(typeof(EspeciesController).GetCustomAttribute<AuthorizeAttribute>());
    }
}
