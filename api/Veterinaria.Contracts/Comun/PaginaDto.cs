namespace Veterinaria.Contracts.Comun;

/// <summary>
/// Página de resultados paginada en el servidor (D-CAT-03, RNF-REN-03).
/// </summary>
public sealed record PaginaDto<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems)
{
    public const int PageSizePorDefecto = 20;
    public const int PageSizeMaximo = 50;

    public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public bool HayAnterior => Page > 1;

    public bool HaySiguiente => Page < TotalPages;
}
