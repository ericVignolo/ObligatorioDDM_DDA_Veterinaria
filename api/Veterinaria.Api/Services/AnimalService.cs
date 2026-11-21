using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Animales del cliente (A-012, A-013, RF-CLI-01). Mensajes iguales a los de AnimalService de Blazor.
public class AnimalService : IAnimalService
{
    private readonly IAnimalRepository _repository;
    private readonly TimeProvider _reloj;

    public AnimalService(IAnimalRepository repository, TimeProvider reloj)
    {
        _repository = repository;
        _reloj = reloj;
    }

    public async Task<List<EspecieReadDto>> ListarEspeciesAsync()
    {
        return (await _repository.ObtenerEspeciesAsync())
            .Select(e => new EspecieReadDto { Id = e.Id, Nombre = e.Nombre })
            .ToList();
    }

    public async Task<PaginaDto<AnimalReadDto>> ListarAsync(int cuentaId, int page, int pageSize)
    {
        var total = await _repository.ContarDeClienteAsync(cuentaId);
        var animales = await _repository.ObtenerDeClienteAsync(cuentaId, (page - 1) * pageSize, pageSize);

        return new PaginaDto<AnimalReadDto>
        {
            Items = animales.Select(ADto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<Resultado<AnimalReadDto>> ObtenerAsync(int id, int cuentaId)
    {
        var animal = await _repository.ObtenerDeClienteAsync(id, cuentaId);
        return animal == null ? NoEncontrado<AnimalReadDto>() : Resultado<AnimalReadDto>.Ok(ADto(animal));
    }

    public async Task<Resultado<AnimalReadDto>> CrearAsync(AnimalCreateDto datos, int cuentaId)
    {
        var ahora = _reloj.GetLocalNow().DateTime;
        var animal = new Animal { FechaAlta = ahora, Activo = true };

        var error = await CopiarAsync(datos, animal, null);
        if (error != null)
        {
            return error;
        }

        // El cliente que lo registra queda como responsable (RN-18).
        animal.Responsables.Add(new Responsabilidad { CuentaId = cuentaId, Desde = ahora });
        _repository.Agregar(animal);

        try
        {
            await _repository.GuardarAsync();
        }
        catch (MicrochipDuplicadoException)
        {
            return MicrochipRepetido();
        }

        return new Resultado<AnimalReadDto> { Exito = true, Estado = 201, Valor = ADto(animal) };
    }

    public async Task<Resultado<AnimalReadDto>> EditarAsync(int id, AnimalUpdateDto datos, int cuentaId)
    {
        var animal = await _repository.ObtenerDeClienteAsync(id, cuentaId);
        if (animal == null)
        {
            return NoEncontrado<AnimalReadDto>();
        }

        var error = await CopiarAsync(datos, animal, animal.Id);
        if (error != null)
        {
            return error;
        }

        try
        {
            await _repository.GuardarAsync();
        }
        catch (MicrochipDuplicadoException)
        {
            return MicrochipRepetido();
        }

        return Resultado<AnimalReadDto>.Ok(ADto(animal));
    }

    public async Task<Resultado<MensajeDto>> DarDeBajaAsync(int id, int cuentaId)
    {
        var animal = await _repository.ObtenerDeClienteAsync(id, cuentaId);
        if (animal == null)
        {
            return NoEncontrado<MensajeDto>();
        }

        // Con un turno por delante, primero hay que cancelarlo (así no queda una agenda con un animal dado de baja).
        if (await _repository.TieneTurnosPendientesAsync(animal.Id, _reloj.GetLocalNow().DateTime))
        {
            return Resultado<MensajeDto>.Error(409, "ANIMAL_CON_TURNOS", "No se pudo dar de baja",
                $"{animal.Nombre} tiene turnos pendientes. Cancelalos antes de darlo de baja.");
        }

        animal.Activo = false;
        await _repository.GuardarAsync();
        return Resultado<MensajeDto>.Ok(new MensajeDto { Mensaje = $"{animal.Nombre} quedó dado de baja. Su historia clínica se conserva." });
    }

    // Copia los datos del formulario al animal y revisa especie y microchip.
    private async Task<Resultado<AnimalReadDto>?> CopiarAsync(AnimalCreateDto datos, Animal animal, int? excepto)
    {
        var especie = await _repository.ObtenerEspecieAsync(datos.EspecieId);
        if (especie == null)
        {
            return Resultado<AnimalReadDto>.Error(400, "ESPECIE_INVALIDA", "Especie no válida", "Elegí la especie.");
        }

        var microchip = string.IsNullOrWhiteSpace(datos.Microchip) ? null : datos.Microchip.Trim();
        if (microchip != null && await _repository.ExisteMicrochipAsync(microchip, excepto))
        {
            return MicrochipRepetido();
        }

        animal.Nombre = datos.Nombre.Trim();
        animal.EspecieId = especie.Id;
        animal.Especie = especie;
        animal.Raza = (datos.Raza ?? "").Trim();
        animal.Sexo = datos.Sexo;
        animal.FechaNacimiento = datos.FechaNacimiento?.ToDateTime(TimeOnly.MinValue);
        animal.Microchip = microchip;
        animal.Caravana = (datos.Caravana ?? "").Trim().ToUpperInvariant();
        animal.EsGrupo = datos.EsGrupo;
        animal.CantidadGrupo = datos.EsGrupo ? datos.CantidadGrupo : null;
        animal.SenasParticulares = (datos.SenasParticulares ?? "").Trim();
        animal.Antecedentes = (datos.Antecedentes ?? "").Trim();
        return null;
    }

    private static AnimalReadDto ADto(Animal a)
    {
        return new AnimalReadDto
        {
            Id = a.Id,
            Nombre = a.Nombre,
            EspecieId = a.EspecieId,
            Especie = a.Especie?.Nombre ?? "",
            Raza = a.Raza,
            Sexo = a.Sexo,
            FechaNacimiento = a.FechaNacimiento == null ? null : DateOnly.FromDateTime(a.FechaNacimiento.Value),
            Microchip = a.Microchip ?? "",
            Caravana = a.Caravana,
            EsGrupo = a.EsGrupo,
            CantidadGrupo = a.CantidadGrupo,
            SenasParticulares = a.SenasParticulares,
            Antecedentes = a.Antecedentes,
            FechaAlta = a.FechaAlta
        };
    }

    // Ajeno o inexistente se responden igual, para no revelar animales de otros clientes (RN-18).
    private static Resultado<T> NoEncontrado<T>()
        => Resultado<T>.Error(404, "ANIMAL_NO_ENCONTRADO", "Animal no encontrado", "No encontramos ese animal entre los tuyos.");

    private static Resultado<AnimalReadDto> MicrochipRepetido()
        => Resultado<AnimalReadDto>.Error(409, "MICROCHIP_REPETIDO", "Microchip repetido",
            "Ese microchip ya figura en el sistema. Consultá en la clínica para revisarlo.");
}
