namespace Veterinaria.Api.DTOs;

// Respuesta paginada de toda la API (D-CAT-03): items, page, pageSize y totalItems.
public class PaginaDto<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
}
