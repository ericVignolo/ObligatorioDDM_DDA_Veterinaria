using System.Linq.Expressions;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

// Reglas del catálogo público (D-CAT-02, D-CAT-04).
// Están en una expresión para que el Repository la use dentro de la consulta a MySQL
// (la paginación se hace en el servidor, RNF-REN-03) y las pruebas usen exactamente la misma regla.
public static class ReglasCatalogo
{
    // Aparece si está publicado y tiene importe de referencia.
    // Por ahora el importe es PrecioDesde; las tarifas con vigencia llegan con una migración posterior (Diego, 2026-11-16).
    public static readonly Expression<Func<Prestacion, bool>> EsPublica =
        p => p.Publicado && p.PrecioDesde > 0;
}

// Paginación común de la API (D-CAT-03).
public static class Paginacion
{
    public const int PageSizePorDefecto = 20;
    public const int PageSizeMaximo = 50;

    // Devuelve el motivo si los parámetros son inválidos, o null si están bien.
    public static string? Validar(int page, int pageSize)
    {
        if (page < 1)
        {
            return "El número de página empieza en 1.";
        }

        if (pageSize < 1 || pageSize > PageSizeMaximo)
        {
            return $"El tamaño de página tiene que estar entre 1 y {PageSizeMaximo}.";
        }

        return null;
    }
}
