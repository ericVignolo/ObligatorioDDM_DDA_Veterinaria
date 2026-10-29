using Veterinaria.Contracts.Catalogo;
using Veterinaria.Contracts.Comun;

namespace Veterinaria.Blazor.Datos;

/// <summary>
/// Acceso de Blazor al catálogo público. En la sección A lo implementa
/// <see cref="Demo.CatalogoServiciosDemo"/>; en la sección C se reemplaza por
/// un cliente HTTP de la API sin cambiar las pantallas.
/// </summary>
public interface ICatalogoServicios
{
    /// <exception cref="ArgumentOutOfRangeException">Página o tamaño fuera de rango (equivale a un 400 de la API).</exception>
    Task<PaginaDto<ServicioPublicoDto>> ListarAsync(int page, int pageSize, CancellationToken ct = default);

    /// <returns>El servicio, o <c>null</c> si no existe o no es público (equivale a un 404).</returns>
    Task<ServicioPublicoDto?> ObtenerAsync(Guid id, CancellationToken ct = default);
}
