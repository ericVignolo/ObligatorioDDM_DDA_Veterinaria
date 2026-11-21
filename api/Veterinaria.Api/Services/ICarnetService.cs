using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

// Carnet sanitario y avisos de refuerzo de vacunas y desparasitaciones (A-021, RF-CLI-06, RF-CLI-07).
public interface ICarnetService
{
    // Última aplicación de cada producto, primero lo más urgente. Solo de un animal propio (RN-18).
    Task<Resultado<List<ItemCarnetDto>>> CarnetAsync(int cuentaId, int animalId);

    // Refuerzos próximos o vencidos de los animales del cliente.
    Task<List<ItemCarnetDto>> AlertasAsync(int cuentaId);

    // Encola en Comunicaciones el aviso de los refuerzos que vencen en los próximos días (5 por defecto), una vez por
    // aplicación y responsable, por el canal elegido. Pensado para correr una vez por día.
    Task<AvisosGeneradosDto> GenerarAvisosAsync();
}
