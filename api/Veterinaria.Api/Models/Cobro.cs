using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Precio de una prestación desde una fecha (RF-ADM-02). El cargo usa el vigente en la fecha de la atención (RN-33).
public class TarifaPrestacion
{
    public int Id { get; set; }

    public int PrestacionId { get; set; }
    public Prestacion? Prestacion { get; set; }

    // decimal(12,2), pesos uruguayos.
    public decimal Precio { get; set; }

    // Columna DATE.
    public DateTime VigenteDesde { get; set; }

    // Null = tarifa inicial cargada con la base.
    public int? RegistradoPorId { get; set; }
    public Cuenta? RegistradoPor { get; set; }
}

// Cargo por una prestación (A-023, RN-32, RN-33). Lo genera la firma de la consulta o el cierre de una asistencia.
// El monto no cambia si después se modifica la tarifa.
public class Cargo
{
    public int Id { get; set; }

    // De una consulta (único: no se duplica, RN-32) o de una asistencia a domicilio (RN-58).
    public int? ConsultaId { get; set; }
    public Consulta? Consulta { get; set; }

    public int? SolicitudAsistenciaId { get; set; }
    public SolicitudAsistencia? SolicitudAsistencia { get; set; }

    public int? TurnoId { get; set; }
    public Turno? Turno { get; set; }

    public int ClienteId { get; set; }
    public Cuenta? Cliente { get; set; }

    public int? AnimalId { get; set; }
    public Animal? Animal { get; set; }

    public int? PrestacionId { get; set; }
    public Prestacion? Prestacion { get; set; }

    [MaxLength(250)] public string Descripcion { get; set; } = "";
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }

    // RN-35: seña de una cirugía programada; se descuenta del cargo final.
    public bool EsSena { get; set; }
}

// Cobro registrado por Recepción y su comprobante interno (RF-REC-05). No se modifica: se anula con nota de crédito (RN-34).
public class Pago
{
    public int Id { get; set; }

    public int CargoId { get; set; }
    public Cargo? Cargo { get; set; }

    // Correlativo, por ejemplo C-000001 (tabla Numeradores).
    [MaxLength(20)] public string Numero { get; set; } = "";

    public decimal Monto { get; set; }

    // Efectivo, Débito, Crédito, Transferencia o En línea.
    [MaxLength(20)] public string Medio { get; set; } = "";

    // N.º de operación o de voucher; obligatorio en transferencias.
    [MaxLength(60)] public string Referencia { get; set; } = "";

    public DateTime Fecha { get; set; }

    public int? CobradoPorId { get; set; }
    public Cuenta? CobradoPor { get; set; }
}

// Anula un comprobante sin modificarlo (RN-34). Solo la emite el Administrador, con motivo.
public class NotaCredito
{
    public int Id { get; set; }

    // Correlativo, por ejemplo NC-000001.
    [MaxLength(20)] public string Numero { get; set; } = "";

    // Único: un comprobante se anula una sola vez.
    public int PagoId { get; set; }
    public Pago? Pago { get; set; }

    public decimal Monto { get; set; }
    [MaxLength(250)] public string Motivo { get; set; } = "";
    public DateTime Fecha { get; set; }

    public int EmitidaPorId { get; set; }
    public Cuenta? EmitidaPor { get; set; }
}
