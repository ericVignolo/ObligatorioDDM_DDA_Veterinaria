using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Carnet y avisos de refuerzo, iguales a VacunaService de Blazor. Decisión de Eric (2026-10-31): aviso por la bandeja
// de la app y por correo, 5 días antes por defecto (parámetro avisos.refuerzo_dias_antes).
public class CarnetService : ICarnetService
{
    public const string Vigente = "Vigente";
    public const string RefuerzoProximo = "Refuerzo próximo";
    public const string Vencida = "Vencida";

    // Tipo de comunicación «Aviso de refuerzo» de la semilla.
    public const int TipoAvisoRefuerzo = 4;

    private readonly ISaludRepository _repository;
    private readonly TimeProvider _reloj;

    public CarnetService(ISaludRepository repository, TimeProvider reloj)
    {
        _repository = repository;
        _reloj = reloj;
    }

    private DateTime Hoy => _reloj.GetLocalNow().DateTime.Date;

    public static string Estado(AplicacionSanitaria aplicacion, DateTime hoy, int diasAviso)
    {
        var dias = (aplicacion.FechaRefuerzo.Date - hoy.Date).Days;
        return dias < 0 ? Vencida : dias <= diasAviso ? RefuerzoProximo : Vigente;
    }

    public async Task<Resultado<List<ItemCarnetDto>>> CarnetAsync(int cuentaId, int animalId)
    {
        var animal = await _repository.ObtenerAnimalAsync(animalId);
        if (animal == null || !animal.Activo || !await _repository.EsResponsableAsync(animalId, cuentaId))
        {
            return Resultado<List<ItemCarnetDto>>.Error(404, "ANIMAL_NO_ENCONTRADO", "Animal no encontrado", "No encontramos ese animal entre los tuyos.");
        }

        var dias = await DiasAvisoAsync();
        var aplicaciones = await _repository.ObtenerAplicacionesQueCuentanAsync(new[] { animalId });
        return Resultado<List<ItemCarnetDto>>.Ok(Carnet(new[] { animal }, aplicaciones, dias));
    }

    public async Task<List<ItemCarnetDto>> AlertasAsync(int cuentaId)
    {
        var animales = await _repository.ObtenerAnimalesDeClienteAsync(cuentaId);
        var aplicaciones = await _repository.ObtenerAplicacionesQueCuentanAsync(animales.Select(a => a.Id));
        return Carnet(animales, aplicaciones, await DiasAvisoAsync())
            .Where(i => i.Estado != Vigente)
            .OrderBy(i => i.DiasParaRefuerzo)
            .ToList();
    }

    public async Task<AvisosGeneradosDto> GenerarAvisosAsync()
    {
        var resultado = new AvisosGeneradosDto();
        var hoy = Hoy;
        var dias = await DiasAvisoAsync();
        var tipo = await _repository.ObtenerTipoComunicacionAsync(TipoAvisoRefuerzo);
        if (tipo == null)
        {
            return resultado;
        }

        var animales = await _repository.ObtenerAnimalesConRefuerzoHastaAsync(hoy.AddDays(dias));
        var aplicaciones = await _repository.ObtenerAplicacionesQueCuentanAsync(animales.Select(a => a.Id));

        // Solo los que vencen de hoy en adelante: lo ya vencido se ve en el carnet, sin repetir avisos viejos.
        foreach (var item in Carnet(animales, aplicaciones, dias).Where(i => i.Estado == RefuerzoProximo))
        {
            var animal = animales.First(a => a.Id == item.AnimalId);
            foreach (var responsable in animal.Responsables.Where(r => r.Hasta == null))
            {
                var cuenta = await _repository.ObtenerCuentaAsync(responsable.CuentaId);
                if (cuenta == null || !cuenta.Activa || cuenta.Eliminada)
                {
                    continue;
                }

                var canal = await _repository.ObtenerCanalPreferidoAsync(cuenta.Id, tipo.Id) ?? tipo.CanalPorDefecto;
                if (canal == "No recibir")
                {
                    resultado.SinRecibir++;
                    continue;
                }

                var clave = $"refuerzo-{item.AplicacionId}";
                if (await _repository.ExisteComunicacionAsync(cuenta.Id, tipo.Id, clave))
                {
                    resultado.YaExistian++;
                    continue;
                }

                _repository.Agregar(new Comunicacion
                {
                    CuentaId = cuenta.Id,
                    TipoComunicacionId = tipo.Id,
                    Asunto = $"{item.Animal}: refuerzo de {item.Producto}",
                    Cuerpo = $"El refuerzo de {item.Producto} de {item.Animal} vence el {item.FechaRefuerzo:dd/MM/yyyy}. Reservá un turno en la app o llamá a la clínica.",
                    Canal = canal == "Correo" ? "Correo" : "En la app",
                    Estado = "Pendiente",
                    Destino = canal == "Correo" ? cuenta.Correo : "",
                    FechaCreacion = _reloj.GetLocalNow().DateTime,
                    Clave = clave
                });
                resultado.Generados++;
            }
        }

        await _repository.GuardarAsync();
        return resultado;
    }

    private async Task<int> DiasAvisoAsync()
    {
        var parametros = await _repository.ObtenerParametrosAsync();
        return parametros.TryGetValue("avisos.refuerzo_dias_antes", out var valor) && int.TryParse(valor, out var dias) && dias >= 0 ? dias : 5;
    }

    // La última aplicación de cada producto por animal, con su estado; primero lo que vence antes.
    private List<ItemCarnetDto> Carnet(IEnumerable<Animal> animales, List<AplicacionSanitaria> aplicaciones, int dias)
    {
        var hoy = Hoy;
        var nombres = animales.ToDictionary(a => a.Id, a => a.Nombre);
        return aplicaciones
            .Where(a => nombres.ContainsKey(a.AnimalId))
            .GroupBy(a => (a.AnimalId, a.ProductoSanitarioId))
            .Select(g => g.OrderByDescending(a => a.FechaAplicacion).ThenByDescending(a => a.Id).First())
            .Select(a => new ItemCarnetDto
            {
                AplicacionId = a.Id,
                AnimalId = a.AnimalId,
                Animal = nombres[a.AnimalId],
                Producto = a.Producto,
                Tipo = a.Tipo,
                Laboratorio = a.Laboratorio,
                Lote = a.NumeroLote,
                FechaAplicacion = DateOnly.FromDateTime(a.FechaAplicacion),
                FechaRefuerzo = DateOnly.FromDateTime(a.FechaRefuerzo),
                Estado = Estado(a, hoy, dias),
                DiasParaRefuerzo = (a.FechaRefuerzo.Date - hoy).Days
            })
            .OrderBy(i => i.FechaRefuerzo)
            .ToList();
    }
}
