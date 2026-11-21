using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Artículo del inventario: vacuna, antiparasitario o insumo (A-022, RF-ADM-05).
// El stock no se guarda acá: es la suma de los lotes habilitados.
public class Articulo
{
    public int Id { get; set; }
    [MaxLength(120)] public string Nombre { get; set; } = "";

    // "Vacuna", "Antiparasitario" o "Insumo".
    [MaxLength(20)] public string Tipo { get; set; } = "Insumo";

    // "dosis", "unidad", "par", "sobre"…
    [MaxLength(20)] public string Unidad { get; set; } = "unidad";

    // Por debajo de este stock se avisa al Administrador (RN-28).
    public int StockMinimo { get; set; }

    public bool Activo { get; set; } = true;
}

// Lote de un artículo con su vencimiento (RN-15, RN-27, RN-29).
// La cantidad solo cambia con un movimiento (RN-30) y nunca queda negativa (RN-26, restricción CHECK).
public class LoteProducto
{
    public int Id { get; set; }

    public int ArticuloId { get; set; }
    public Articulo? Articulo { get; set; }

    [MaxLength(40)] public string Numero { get; set; } = "";

    // Columna DATE.
    public DateTime Vencimiento { get; set; }

    public int Cantidad { get; set; }

    // Un lote vencido queda inhabilitado para consumo (RN-29).
    public bool Inhabilitado { get; set; }

    public DateTime FechaIngreso { get; set; }
}

// Movimiento del inventario con origen trazable (RN-30): Compra, Consumo, Ajuste o Baja.
public class MovimientoStock
{
    public long Id { get; set; }

    public int ArticuloId { get; set; }
    public Articulo? Articulo { get; set; }

    public int LoteId { get; set; }
    public LoteProducto? Lote { get; set; }

    [MaxLength(20)] public string Tipo { get; set; } = "";

    // Positiva si entra stock, negativa si sale.
    public int Cantidad { get; set; }

    // De dónde viene: "Consulta 5", "Compra", "Vencimiento automático"…
    [MaxLength(80)] public string Origen { get; set; } = "";
    [MaxLength(250)] public string Motivo { get; set; } = "";

    public DateTime Fecha { get; set; }

    // Null = el sistema (por ejemplo, la baja automática por vencimiento).
    public int? PersonaId { get; set; }
    public Cuenta? Persona { get; set; }
}

// Lo que una consulta en borrador va a consumir de un lote (RF-VET-06).
// Aparta el lote mientras la consulta está abierta; al firmar se descuenta (RNF-CON-03).
public class ReservaStock
{
    public int Id { get; set; }

    public int ConsultaId { get; set; }
    public Consulta? Consulta { get; set; }

    public int ArticuloId { get; set; }
    public Articulo? Articulo { get; set; }

    public int LoteId { get; set; }
    public LoteProducto? Lote { get; set; }

    public int Cantidad { get; set; }

    // Si es la dosis de una vacuna o antiparasitario; null si es un insumo.
    public int? AplicacionSanitariaId { get; set; }
    public AplicacionSanitaria? AplicacionSanitaria { get; set; }

    // true cuando la consulta se firmó y el stock ya se descontó.
    public bool Confirmada { get; set; }
}

// Aviso al Administrador cuando un artículo baja del mínimo (RN-28).
public class AlertaStock
{
    public int Id { get; set; }

    public int ArticuloId { get; set; }
    public Articulo? Articulo { get; set; }

    public int StockQueQuedo { get; set; }
    public int StockMinimo { get; set; }
    [MaxLength(80)] public string Origen { get; set; } = "";
    public DateTime Fecha { get; set; }
    public bool Atendida { get; set; }
}

// Vacuna o antiparasitario del catálogo sanitario (A-021, RF-VET-04).
// El intervalo de refuerzo vive en el producto: la fecha de refuerzo se calcula (RN-19).
public class ProductoSanitario
{
    public int Id { get; set; }
    [MaxLength(120)] public string Nombre { get; set; } = "";

    // "Vacuna" o "Antiparasitario".
    [MaxLength(20)] public string Tipo { get; set; } = "Vacuna";

    [MaxLength(120)] public string Laboratorio { get; set; } = "";

    // Artículo del inventario con sus lotes. Cada aplicación descuenta una dosis.
    public int ArticuloId { get; set; }
    public Articulo? Articulo { get; set; }

    public int IntervaloRefuerzoDias { get; set; }

    // RN-16: días mínimos desde la aplicación anterior; antes, adelantar exige justificación. 0 = sin mínimo.
    public int IntervaloMinimoDias { get; set; }

    public List<ProductoSanitarioEspecie> Especies { get; set; } = new();
}

// Especies para las que está indicado un producto.
public class ProductoSanitarioEspecie
{
    public int ProductoSanitarioId { get; set; }
    public ProductoSanitario? ProductoSanitario { get; set; }

    public int EspecieId { get; set; }
    public Especie? Especie { get; set; }
}

// Aplicación de una vacuna o un antiparasitario (A-021, RF-VET-04). Cuenta para el carnet cuando la consulta se firma.
// Copia nombre, laboratorio y lote para que el carnet no cambie si cambia el catálogo.
public class AplicacionSanitaria
{
    public int Id { get; set; }

    public int AnimalId { get; set; }
    public Animal? Animal { get; set; }

    // Null = registro anterior al sistema, sin consulta de origen.
    public int? ConsultaId { get; set; }
    public Consulta? Consulta { get; set; }

    public int ProductoSanitarioId { get; set; }
    public ProductoSanitario? ProductoSanitario { get; set; }

    [MaxLength(120)] public string Producto { get; set; } = "";
    [MaxLength(20)] public string Tipo { get; set; } = "";
    [MaxLength(120)] public string Laboratorio { get; set; } = "";

    public int? LoteId { get; set; }
    public LoteProducto? Lote { get; set; }
    [MaxLength(40)] public string NumeroLote { get; set; } = "";

    // Columnas DATE.
    public DateTime? LoteVence { get; set; }
    public DateTime FechaAplicacion { get; set; }
    public DateTime FechaRefuerzo { get; set; }

    public int? AplicadaPorId { get; set; }
    public Cuenta? AplicadaPor { get; set; }

    // RN-16: justificación cuando la dosis se adelantó al intervalo mínimo.
    [MaxLength(500)] public string JustificacionAdelanto { get; set; } = "";
}
