namespace Veterinaria.Blazor.Models;

// Anula un comprobante emitido sin modificarlo (A-023, RN-34). Solo la emite el Administrador, con motivo.
public class NotaCredito
{
    public int Id { get; set; }

    // Número correlativo, por ejemplo NC-000001.
    public string Numero { get; set; } = "";
    public int PagoId { get; set; }
    public decimal Monto { get; set; }
    public string Motivo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public int PersonalId { get; set; }
    public string PersonalNombre { get; set; } = "";
}
