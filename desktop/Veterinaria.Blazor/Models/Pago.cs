namespace Veterinaria.Blazor.Models;

// Cobro registrado por Recepción y su comprobante interno (A-023, RF-REC-05).
// Un comprobante emitido no se modifica: se anula con una nota de crédito (RN-34).
public class Pago
{
    public int Id { get; set; }
    public int CargoId { get; set; }

    // Número correlativo del comprobante, por ejemplo C-000001.
    public string Numero { get; set; } = "";
    public decimal Monto { get; set; }

    // Efectivo, Débito, Crédito o Transferencia.
    public string Medio { get; set; } = "";

    // N.º de operación o de voucher; obligatorio en transferencias.
    public string Referencia { get; set; } = "";
    public DateTime Fecha { get; set; }
    public int PersonalId { get; set; }
    public string PersonalNombre { get; set; } = "";
}
