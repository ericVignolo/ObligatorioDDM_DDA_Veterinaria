namespace Veterinaria.Blazor.Models;

// Lo que una consulta en borrador va a consumir de un lote (A-022, RF-VET-06).
// Mientras la consulta está abierta el lote queda apartado; al firmar se descuenta de verdad (RNF-CON-03)
// y al quitarlo se libera. Así el lote que ve el veterinario es el que se descuenta.
public class ReservaStock
{
    public int Id { get; set; }
    public int ConsultaId { get; set; }
    public int ArticuloId { get; set; }
    public int LoteId { get; set; }
    public int Cantidad { get; set; }

    // Si es la dosis de una vacuna o antiparasitario (AplicacionSanitaria.Id); 0 = insumo.
    public int AplicacionId { get; set; }

    // true cuando la consulta se firmó y el stock ya se descontó.
    public bool Confirmada { get; set; }
}
