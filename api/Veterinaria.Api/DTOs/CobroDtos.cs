using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs;

// ---------- Cobro (A-023, RF-REC-05, RN-32 a RN-36) ----------

public class CargoReadDto
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string Cliente { get; set; } = "";
    public int? AnimalId { get; set; }
    public string Animal { get; set; } = "";
    public int? TurnoId { get; set; }
    public int? ConsultaId { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal Monto { get; set; }
    public decimal Pagado { get; set; }
    public decimal Saldo { get; set; }

    // "Pendiente", "Pago parcial" o "Pagado".
    public string Estado { get; set; } = "";
    public DateTime Fecha { get; set; }
    public bool EsSena { get; set; }

    // Solo en el detalle.
    public List<PagoReadDto> Pagos { get; set; } = new();
}

public class PagoReadDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = "";
    public int CargoId { get; set; }
    public string Descripcion { get; set; } = "";
    public string Cliente { get; set; } = "";
    public decimal Monto { get; set; }
    public string Medio { get; set; } = "";
    public string Referencia { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string CobradoPor { get; set; } = "";
    public bool Anulado { get; set; }
    public string NotaCredito { get; set; } = "";
}

public class NotaCreditoReadDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = "";
    public int PagoId { get; set; }
    public string Comprobante { get; set; } = "";
    public decimal Monto { get; set; }
    public string Motivo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string EmitidaPor { get; set; } = "";
}

public class CobroCreateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí el cargo.")]
    public int CargoId { get; set; }

    [Range(typeof(decimal), "1", "1000000", ErrorMessage = "El monto tiene que estar entre $ 1 y $ 1.000.000.")]
    public decimal Monto { get; set; }

    [Required(ErrorMessage = "Elegí un medio de pago de la lista.")]
    public string Medio { get; set; } = "";

    // N.º de operación o de voucher; obligatorio en transferencias.
    [StringLength(60, ErrorMessage = "La referencia puede tener hasta 60 caracteres.")]
    public string Referencia { get; set; } = "";
}

public class CobroRegistradoDto
{
    public PagoReadDto Pago { get; set; } = new();
    public CargoReadDto Cargo { get; set; } = new();
    public string Mensaje { get; set; } = "";
}

public class AnulacionDto
{
    [Required(ErrorMessage = "Escribí el motivo de la anulación (hasta 200 caracteres).")]
    [StringLength(200, MinimumLength = 3, ErrorMessage = "Escribí el motivo de la anulación (hasta 200 caracteres).")]
    public string Motivo { get; set; } = "";
}

public class AnulacionRegistradaDto
{
    public NotaCreditoReadDto Nota { get; set; } = new();
    public string Mensaje { get; set; } = "";
}

public class ResumenCajaDto
{
    public DateOnly Fecha { get; set; }
    public decimal CobradoEnElDia { get; set; }
    public int Comprobantes { get; set; }
    public decimal TotalPendiente { get; set; }
}

// RN-36: aviso al agendar; nunca bloquea.
public class DeudaClienteDto
{
    public int ClienteId { get; set; }
    public decimal DeudaVencida { get; set; }
    public string Aviso { get; set; } = "";
}
