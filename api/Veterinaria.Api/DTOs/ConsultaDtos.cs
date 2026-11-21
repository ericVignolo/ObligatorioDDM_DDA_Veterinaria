using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs;

// Lo que el veterinario completa en la consulta (A-019, RF-VET-02). Mismas reglas que ConsultaFormulario de Blazor.
// El borrador se guarda incompleto; motivo y diagnóstico se exigen recién al firmar (P-RC-06, RN-20).
public class ConsultaGuardarDto
{
    // Versión que el veterinario tiene en pantalla: si otro guardó antes, se rechaza (RNF-CON-05).
    [Range(1, int.MaxValue, ErrorMessage = "Falta la versión de la consulta.")]
    public int Version { get; set; }

    [StringLength(200, ErrorMessage = "El motivo puede tener hasta 200 caracteres.")]
    public string Motivo { get; set; } = "";

    [StringLength(2000, ErrorMessage = "La anamnesis puede tener hasta 2000 caracteres.")]
    public string Anamnesis { get; set; } = "";

    [StringLength(2000, ErrorMessage = "El examen físico puede tener hasta 2000 caracteres.")]
    public string ExamenFisico { get; set; } = "";

    [Range(typeof(decimal), "30", "45", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "La temperatura debe estar entre 30 y 45 °C.")]
    public decimal? TemperaturaC { get; set; }

    [Range(1, 400, ErrorMessage = "La frecuencia cardíaca debe estar entre 1 y 400 lpm.")]
    public int? FrecuenciaCardiaca { get; set; }

    [Range(1, 200, ErrorMessage = "La frecuencia respiratoria debe estar entre 1 y 200 rpm.")]
    public int? FrecuenciaRespiratoria { get; set; }

    [Range(typeof(decimal), "0.01", "2000", ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true, ErrorMessage = "El peso debe estar entre 0,01 y 2000 kg.")]
    public decimal? PesoKg { get; set; }

    [StringLength(500, ErrorMessage = "Las alergias pueden tener hasta 500 caracteres.")]
    public string Alergias { get; set; } = "";

    [StringLength(1000, ErrorMessage = "El diagnóstico puede tener hasta 1000 caracteres.")]
    public string Diagnostico { get; set; } = "";

    [RegularExpression("^(Presuntivo|Definitivo)$", ErrorMessage = "El diagnóstico es presuntivo o definitivo.")]
    public string TipoDiagnostico { get; set; } = "Presuntivo";

    [StringLength(2000, ErrorMessage = "El tratamiento puede tener hasta 2000 caracteres.")]
    public string Tratamiento { get; set; } = "";

    [StringLength(2000, ErrorMessage = "Las indicaciones pueden tener hasta 2000 caracteres.")]
    public string Indicaciones { get; set; } = "";
}

// Firma: los mismos datos, más la contraseña del veterinario (P-RC-06) y la confirmación de RN-17.
public class ConsultaFirmaDto : ConsultaGuardarDto
{
    [Required(ErrorMessage = "Ingresá tu contraseña para firmar.")]
    public string Contrasena { get; set; } = "";

    // RN-17: indicar algo registrado como alergia del animal exige confirmarlo.
    public bool ConfirmaAlergia { get; set; }
}

public class InsumoDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí un insumo.")]
    public int ArticuloId { get; set; }

    [Range(1, 100, ErrorMessage = "Elegí un insumo y una cantidad entre 1 y 100.")]
    public int Cantidad { get; set; }
}

public class AplicacionDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí un producto.")]
    public int ProductoSanitarioId { get; set; }

    // RN-16: obligatoria si la dosis se adelanta al intervalo mínimo.
    [StringLength(300, ErrorMessage = "La justificación puede tener hasta 300 caracteres.")]
    public string Justificacion { get; set; } = "";
}

public class EnmiendaDto
{
    [Required(ErrorMessage = "Completá el motivo y el texto de la enmienda.")]
    [StringLength(500, ErrorMessage = "El motivo puede tener hasta 500 caracteres.")]
    public string Motivo { get; set; } = "";

    [Required(ErrorMessage = "Completá el motivo y el texto de la enmienda.")]
    [StringLength(2000, ErrorMessage = "El texto puede tener hasta 2000 caracteres.")]
    public string Texto { get; set; } = "";

    [Required(ErrorMessage = "Ingresá tu contraseña para firmar.")]
    public string Contrasena { get; set; } = "";
}

public class ConsultaReadDto
{
    public int Id { get; set; }
    public int? TurnoId { get; set; }
    public int AnimalId { get; set; }
    public string Animal { get; set; } = "";
    public string Especie { get; set; } = "";
    public string Prestacion { get; set; } = "";
    public string Profesional { get; set; } = "";
    public string Estado { get; set; } = "";
    public int Version { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime? FechaFirma { get; set; }

    public string Motivo { get; set; } = "";
    public string Anamnesis { get; set; } = "";
    public string ExamenFisico { get; set; } = "";
    public decimal? TemperaturaC { get; set; }
    public int? FrecuenciaCardiaca { get; set; }
    public int? FrecuenciaRespiratoria { get; set; }
    public decimal? PesoKg { get; set; }
    public string Alergias { get; set; } = "";
    public string Diagnostico { get; set; } = "";
    public string TipoDiagnostico { get; set; } = "";
    public string Tratamiento { get; set; } = "";
    public string Indicaciones { get; set; } = "";
    public string AlergiaConfirmada { get; set; } = "";

    // RN-17: alergias registradas del animal en consultas firmadas, para avisar mientras se escribe.
    public List<string> AlergiasRegistradas { get; set; } = new();

    public List<InsumoReadDto> Insumos { get; set; } = new();
    public List<AplicacionReadDto> Vacunas { get; set; } = new();
    public List<EnmiendaReadDto> Enmiendas { get; set; } = new();
}

public class InsumoReadDto
{
    public int ArticuloId { get; set; }
    public string Articulo { get; set; } = "";
    public int Cantidad { get; set; }
    public string Unidad { get; set; } = "";
    public string Lotes { get; set; } = "";
}

public class AplicacionReadDto
{
    public int Id { get; set; }
    public string Producto { get; set; } = "";
    public string Tipo { get; set; } = "";
    public string Laboratorio { get; set; } = "";
    public string Lote { get; set; } = "";
    public DateOnly FechaAplicacion { get; set; }
    public DateOnly FechaRefuerzo { get; set; }
    public bool Adelantada { get; set; }
}

public class EnmiendaReadDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Autor { get; set; } = "";
    public string Motivo { get; set; } = "";
    public string Texto { get; set; } = "";
}

// Resultado de la firma: la consulta firmada y el cargo que quedó pendiente de cobro.
public class FirmaReadDto
{
    public ConsultaReadDto Consulta { get; set; } = new();
    public int? CargoId { get; set; }
    public decimal Monto { get; set; }
    public string Mensaje { get; set; } = "";
}

// Historia clínica: consultas firmadas con sus enmiendas y las vacunas que cuentan (RF-VET-03, RF-CLI-05).
public class HistoriaDto
{
    public int AnimalId { get; set; }
    public string Animal { get; set; } = "";
    public string Especie { get; set; } = "";
    public List<string> Alergias { get; set; } = new();
    public List<ConsultaReadDto> Consultas { get; set; } = new();
    public List<AplicacionReadDto> Vacunas { get; set; } = new();
}

public class CancelacionSinAtencionDto
{
    [Required(ErrorMessage = "Escribí el motivo de la cancelación.")]
    [StringLength(250, ErrorMessage = "El motivo puede tener hasta 250 caracteres.")]
    public string Motivo { get; set; } = "";
}
