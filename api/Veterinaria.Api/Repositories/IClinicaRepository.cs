using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

// Acceso a la fila única de Clinicas.
public interface IClinicaRepository
{
    Task<Clinica?> ObtenerAsync();
}
