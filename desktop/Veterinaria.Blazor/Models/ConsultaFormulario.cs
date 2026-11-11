using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Lo que el veterinario completa en la consulta (A-019). El borrador se puede guardar incompleto;
// motivo y diagnóstico se exigen recién al firmar, en ConsultaService (P-RC-06, RN-20).
public class ConsultaFormulario
{
    [StringLength(200, ErrorMessage = "El motivo puede tener hasta 200 caracteres.")]
    public string Motivo { get; set; } = "";

    [StringLength(2000, ErrorMessage = "La anamnesis puede tener hasta 2000 caracteres.")]
    public string Anamnesis { get; set; } = "";

    [StringLength(2000, ErrorMessage = "El examen físico puede tener hasta 2000 caracteres.")]
    public string ExamenFisico { get; set; } = "";

    // Los límites se escriben con punto: se leen en cultura invariante, no en es-UY (donde el decimal es coma).
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

    public string TipoDiagnostico { get; set; } = "Presuntivo";

    [StringLength(2000, ErrorMessage = "El tratamiento puede tener hasta 2000 caracteres.")]
    public string Tratamiento { get; set; } = "";

    [StringLength(2000, ErrorMessage = "Las indicaciones pueden tener hasta 2000 caracteres.")]
    public string Indicaciones { get; set; } = "";
}
