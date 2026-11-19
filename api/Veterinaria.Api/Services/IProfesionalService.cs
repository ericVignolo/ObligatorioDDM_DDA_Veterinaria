using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

public interface IProfesionalService
{
    // Antes de llamarlo, el Controller valida page y pageSize con Paginacion.Validar.
    Task<PaginaDto<ProfesionalReadDto>> ListarPublicosAsync(int page, int pageSize);

    // Null si no existe o no es público: el Controller responde 404.
    Task<ProfesionalDetalleDto?> ObtenerPublicoAsync(int id);
}
