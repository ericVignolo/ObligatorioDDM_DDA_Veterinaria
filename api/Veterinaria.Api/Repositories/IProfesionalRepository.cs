using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

// Acceso a Profesionales con sus especies, servicios y franjas horarias.
public interface IProfesionalRepository
{
    Task<int> ContarPublicosAsync();
    Task<List<Profesional>> ObtenerPublicosAsync(int saltar, int tomar);
    Task<Profesional?> ObtenerPublicoPorIdAsync(int id);
}
