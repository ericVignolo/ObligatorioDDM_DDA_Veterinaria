using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

// Reglas del inventario, iguales a InventarioService de Blazor: stock nunca negativo (RN-26), primero el lote
// que vence antes (FEFO, RN-27), lotes vencidos o inhabilitados no se usan (RN-15, RN-29). Puras: sin base.
public static class ReglasStock
{
    public static bool Usable(LoteProducto lote, DateTime hoy) => !lote.Inhabilitado && lote.Vencimiento.Date >= hoy.Date;

    // Lo que queda para usar en un lote: lo físico menos lo apartado por consultas sin firmar.
    public static int Disponible(LoteProducto lote, IReadOnlyDictionary<int, int> reservado, DateTime hoy)
        => Usable(lote, hoy) ? Math.Max(0, lote.Cantidad - reservado.GetValueOrDefault(lote.Id)) : 0;

    // Stock físico: lo que hay en los lotes usables (incluye lo apartado).
    public static int Stock(IEnumerable<LoteProducto> lotes, DateTime hoy) => lotes.Where(l => Usable(l, hoy)).Sum(l => l.Cantidad);

    public static int StockDisponible(IEnumerable<LoteProducto> lotes, IReadOnlyDictionary<int, int> reservado, DateTime hoy)
        => lotes.Sum(l => Disponible(l, reservado, hoy));

    // RN-27: lotes y cantidades para cubrir `cantidad`, primero el que vence antes. Null si no alcanza (RN-26).
    public static List<(LoteProducto Lote, int Cantidad)>? PlanFefo(IEnumerable<LoteProducto> lotes, IReadOnlyDictionary<int, int> reservado, int cantidad, DateTime hoy)
    {
        if (cantidad <= 0)
        {
            return null;
        }

        var plan = new List<(LoteProducto, int)>();
        var falta = cantidad;
        foreach (var lote in lotes.Where(l => Disponible(l, reservado, hoy) > 0).OrderBy(l => l.Vencimiento).ThenBy(l => l.Id))
        {
            var toma = Math.Min(falta, Disponible(lote, reservado, hoy));
            plan.Add((lote, toma));
            falta -= toma;
            if (falta == 0)
            {
                return plan;
            }
        }

        return null;
    }

    // RN-28: hay que avisar si el stock cruzó el mínimo con este movimiento (estaba en o sobre el mínimo y quedó debajo).
    public static bool CruzaMinimo(int antes, int despues, int minimo) => antes >= minimo && despues < minimo;
}
