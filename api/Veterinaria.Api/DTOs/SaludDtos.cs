using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs;

// ---------- Carnet y avisos de refuerzo (A-021, RF-CLI-06, RF-CLI-07) ----------

// Última aplicación de cada producto, con el estado de su refuerzo.
public class ItemCarnetDto
{
    public int AplicacionId { get; set; }
    public int AnimalId { get; set; }
    public string Animal { get; set; } = "";
    public string Producto { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Laboratorio { get; set; } = "";
    public string Lote { get; set; } = "";
    public DateOnly FechaAplicacion { get; set; }
    public DateOnly FechaRefuerzo { get; set; }

    // "Vigente", "Refuerzo próximo" o "Vencida".
    public string Estado { get; set; } = "";
    public int DiasParaRefuerzo { get; set; }
}

public class AvisosGeneradosDto
{
    public int Generados { get; set; }
    public int YaExistian { get; set; }
    public int SinRecibir { get; set; }
}

// ---------- Inventario (A-022, RF-ADM-05) ----------

public class ArticuloStockDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Unidad { get; set; } = "";
    public int StockMinimo { get; set; }

    // Físico en lotes usables (incluye lo apartado) y lo que queda para usar.
    public int Stock { get; set; }
    public int Disponible { get; set; }
    public bool BajoMinimo { get; set; }
    public int LotesProximosAVencer { get; set; }
}

public class LoteReadDto
{
    public int Id { get; set; }
    public int ArticuloId { get; set; }
    public string Numero { get; set; } = "";
    public DateOnly Vencimiento { get; set; }
    public int Cantidad { get; set; }
    public int Apartado { get; set; }
    public int Disponible { get; set; }
    public bool Inhabilitado { get; set; }
    public bool Vencido { get; set; }
}

public class MovimientoReadDto
{
    public long Id { get; set; }
    public int ArticuloId { get; set; }
    public string Articulo { get; set; } = "";
    public string Lote { get; set; } = "";
    public string Tipo { get; set; } = "";
    public int Cantidad { get; set; }
    public string Origen { get; set; } = "";
    public string Motivo { get; set; } = "";
    public DateTime Fecha { get; set; }
    public string Persona { get; set; } = "";
}

public class AlertaStockReadDto
{
    public int Id { get; set; }
    public int ArticuloId { get; set; }
    public string Articulo { get; set; } = "";
    public int StockQueQuedo { get; set; }
    public int StockMinimo { get; set; }
    public string Origen { get; set; } = "";
    public DateTime Fecha { get; set; }
    public bool Atendida { get; set; }
}

// Compra: un lote nuevo con su vencimiento.
public class CompraDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí un artículo.")]
    public int ArticuloId { get; set; }

    [Required(ErrorMessage = "Ingresá el número de lote.")]
    [RegularExpression(@"^[A-Za-z0-9\-]{1,40}$", ErrorMessage = "El lote puede tener hasta 40 letras, números o guiones.")]
    public string Numero { get; set; } = "";

    [Required(ErrorMessage = "Ingresá el vencimiento.")]
    public DateOnly? Vencimiento { get; set; }

    [Range(1, 100000, ErrorMessage = "La cantidad tiene que estar entre 1 y 100.000.")]
    public int Cantidad { get; set; }
}

// Ajuste (positivo o negativo) o baja de un lote, siempre con motivo (RN-31).
public class AjusteDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí un lote.")]
    public int LoteId { get; set; }

    [Range(-100000, 100000, ErrorMessage = "La cantidad no es válida.")]
    public int Cantidad { get; set; }

    [Required(ErrorMessage = "Escribí el motivo (RN-31).")]
    [StringLength(250, MinimumLength = 3, ErrorMessage = "El motivo tiene que tener entre 3 y 250 caracteres.")]
    public string Motivo { get; set; } = "";
}

public class VencidosProcesadosDto
{
    public int LotesInhabilitados { get; set; }
    public int UnidadesDadasDeBaja { get; set; }
}

// Lo que el veterinario puede registrar en una consulta.
public class InsumoDisponibleDto
{
    public int ArticuloId { get; set; }
    public string Nombre { get; set; } = "";
    public string Unidad { get; set; } = "";
    public int Disponible { get; set; }
}

public class ProductoDisponibleDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Laboratorio { get; set; } = "";
    public int IntervaloRefuerzoDias { get; set; }
    public int Disponible { get; set; }

    // Lote que se usaría (FEFO); vacío si no hay stock vigente.
    public string LoteQueSeUsara { get; set; } = "";
}

// ---------- Consentimiento informado (A-025, RF-CLI-11, RN-10, P-RC-06) ----------

public class ConsentimientoEstadoDto
{
    public int TurnoId { get; set; }
    public string Animal { get; set; } = "";
    public string Prestacion { get; set; } = "";
    public DateTime Inicio { get; set; }

    // "No requiere", "Pendiente", "Firmado", "Rechazado" o "Versión nueva".
    public string Estado { get; set; } = "";
    public int? DocumentoId { get; set; }
    public string Version { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Texto { get; set; } = "";

    // Al responder: lo que se le muestra al cliente.
    public string Mensaje { get; set; } = "";
}

public class RespuestaConsentimientoDto
{
    // true = acepta; false = rechaza.
    public bool Acepta { get; set; }

    // Al aceptar: el nombre completo, como figura en la cuenta.
    [StringLength(160, ErrorMessage = "El nombre es demasiado largo.")]
    public string NombreCompleto { get; set; } = "";
}

public class DocumentoConsentimientoCreateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí el servicio.")]
    public int PrestacionId { get; set; }

    [Required(ErrorMessage = "Indicá una versión nueva y el texto.")]
    [StringLength(20, ErrorMessage = "La versión puede tener hasta 20 caracteres.")]
    public string Version { get; set; } = "";

    [Required(ErrorMessage = "Ingresá el título.")]
    [StringLength(200, ErrorMessage = "El título puede tener hasta 200 caracteres.")]
    public string Titulo { get; set; } = "";

    [Required(ErrorMessage = "Indicá una versión nueva y el texto.")]
    [StringLength(8000, MinimumLength = 50, ErrorMessage = "El texto tiene que tener entre 50 y 8000 caracteres.")]
    public string Texto { get; set; } = "";
}

public class DocumentoConsentimientoReadDto
{
    public int Id { get; set; }
    public int PrestacionId { get; set; }
    public string Prestacion { get; set; } = "";
    public string Version { get; set; } = "";
    public string Titulo { get; set; } = "";
    public string Texto { get; set; } = "";
    public DateTime FechaVigencia { get; set; }
    public bool Vigente { get; set; }
}
