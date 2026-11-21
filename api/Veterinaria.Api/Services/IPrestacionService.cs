using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

public interface IPrestacionService
{
    // Antes de llamarlo, el Controller valida page y pageSize con Paginacion.Validar.
    Task<PaginaDto<ServicioReadDto>> ListarPublicasAsync(int page, int pageSize);

    // Null si no existe o no es pública: el Controller responde 404 sin distinguir los dos casos (CAT-03).
    Task<ServicioReadDto?> ObtenerPublicaAsync(int id);
}
