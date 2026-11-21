using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

public interface IClinicaService
{
    // Null si todavía no se cargaron los datos de la clínica.
    Task<ClinicaReadDto?> ObtenerAsync();
}
