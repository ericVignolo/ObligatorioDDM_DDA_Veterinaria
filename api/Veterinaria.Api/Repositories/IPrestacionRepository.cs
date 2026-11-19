using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

// Acceso a la tabla Prestaciones.
public interface IPrestacionRepository
{
    Task<int> ContarPublicasAsync();
    Task<List<Prestacion>> ObtenerPublicasAsync(int saltar, int tomar);
    Task<Prestacion?> ObtenerPublicaPorIdAsync(int id);
}
