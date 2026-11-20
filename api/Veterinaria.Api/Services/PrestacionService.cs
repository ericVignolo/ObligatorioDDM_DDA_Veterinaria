using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Catálogo público de servicios (RF-VIS-01, CU-VIS-01).
public class PrestacionService : IPrestacionService
{
    private readonly IPrestacionRepository _repository;

    public PrestacionService(IPrestacionRepository repository)
    {
        _repository = repository;
    }

    public async Task<PaginaDto<ServicioReadDto>> ListarPublicasAsync(int page, int pageSize)
    {
        var total = await _repository.ContarPublicasAsync();
        var prestaciones = await _repository.ObtenerPublicasAsync((page - 1) * pageSize, pageSize);

        return new PaginaDto<ServicioReadDto>
        {
            Items = prestaciones.Select(ADto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<ServicioReadDto?> ObtenerPublicaAsync(int id)
    {
        var prestacion = await _repository.ObtenerPublicaPorIdAsync(id);
        return prestacion == null ? null : ADto(prestacion);
    }

    // Mapeo explícito entidad → DTO, como en el curso.
    private static ServicioReadDto ADto(Prestacion p)
    {
        return new ServicioReadDto
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Descripcion = p.Descripcion,
            ReservableEnLinea = p.ReservableEnLinea,
            DuracionMinutos = p.DuracionMinutos,
            PrecioReferencia = new PrecioReferenciaDto { Importe = p.PrecioDesde, Moneda = "UYU" }
        };
    }
}
