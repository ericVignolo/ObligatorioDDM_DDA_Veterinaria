using Veterinaria.Api.DTOs;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Datos públicos de contacto de la clínica (RF-VIS-03, RF-VIS-06).
public class ClinicaService : IClinicaService
{
    private readonly IClinicaRepository _repository;

    public ClinicaService(IClinicaRepository repository)
    {
        _repository = repository;
    }

    public async Task<ClinicaReadDto?> ObtenerAsync()
    {
        var c = await _repository.ObtenerAsync();
        if (c == null)
        {
            return null;
        }

        return new ClinicaReadDto
        {
            Nombre = c.Nombre,
            RazonSocial = c.RazonSocial,
            Lema = c.Lema,
            Presentacion = c.Presentacion,
            Direccion = c.Direccion,
            Ciudad = c.Ciudad,
            Telefono = c.Telefono,
            TelefonoGuardia = c.TelefonoGuardia,
            Correo = c.Correo,
            Horario = c.Horario,
            Cobertura = c.Cobertura
        };
    }
}
