using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

// Animales del cliente (A-012, A-013, RF-CLI-01). La cuenta sale siempre del token, nunca del pedido (RN-18).
public interface IAnimalService
{
    Task<List<EspecieReadDto>> ListarEspeciesAsync();

    // Antes de llamarlo, el Controller valida page y pageSize con Paginacion.Validar.
    Task<PaginaDto<AnimalReadDto>> ListarAsync(int cuentaId, int page, int pageSize);

    Task<Resultado<AnimalReadDto>> ObtenerAsync(int id, int cuentaId);
    Task<Resultado<AnimalReadDto>> CrearAsync(AnimalCreateDto datos, int cuentaId);
    Task<Resultado<AnimalReadDto>> EditarAsync(int id, AnimalUpdateDto datos, int cuentaId);

    // Baja lógica: el animal y su historia se conservan (RN-14).
    Task<Resultado<MensajeDto>> DarDeBajaAsync(int id, int cuentaId);
}
