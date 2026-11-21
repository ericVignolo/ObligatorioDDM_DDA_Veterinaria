using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Veterinaria.Api.Models;

namespace Veterinaria.Api.Services;

// Reglas de la consulta clínica iguales a ConsultaService de Blazor. Puras: sin base.
public static class ReglasClinicas
{
    private static readonly string[] SinAlergia = { "ninguna", "ninguno", "no", "sin alergias", "sin alergias conocidas", "no conocidas", "niega", "nada" };

    // RN-17: alergias registradas (consultas firmadas) más las que se escriben en la consulta en curso.
    public static List<string> Alergias(IEnumerable<string> registradas, string enCurso = "")
    {
        return registradas.Append(enCurso ?? "")
            .SelectMany(t => Regex.Split(t ?? "", @"[,;/\n]| y ", RegexOptions.IgnoreCase))
            .Select(a => a.Trim().TrimEnd('.'))
            .Where(a => a.Length >= 3 && !SinAlergia.Contains(Normalizar(a)))
            .DistinctBy(Normalizar)
            .ToList();
    }

    // Alergias que aparecen como palabra completa en el tratamiento o las indicaciones, sin distinguir mayúsculas ni tildes.
    public static List<string> Choques(IEnumerable<string> alergias, string tratamiento, string indicaciones)
    {
        var texto = Normalizar($"{tratamiento} {indicaciones}");
        return alergias.Where(a => Regex.IsMatch(texto, $@"\b{Regex.Escape(Normalizar(a))}\b")).ToList();
    }

    public static string Normalizar(string texto)
    {
        var descompuesto = (texto ?? "").ToLowerInvariant().Normalize(NormalizationForm.FormD);
        return new string(descompuesto.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray());
    }

    // RN-13: matrícula vigente para firmar.
    public static bool MatriculaVigente(Profesional? profesional, DateTime hoy)
        => profesional?.MatriculaVigenteHasta is DateTime hasta && hasta.Date >= hoy.Date;

    // Texto que se firma: todos los datos clínicos, el autor y la fecha. Números en cultura invariante.
    public static string Contenido(Consulta c)
    {
        var inv = CultureInfo.InvariantCulture;
        return string.Join("|", c.AnimalId, c.AutorId, c.FechaFirma?.ToString("O"), c.Motivo, c.Anamnesis, c.ExamenFisico,
            c.TemperaturaC?.ToString(inv), c.FrecuenciaCardiaca, c.FrecuenciaRespiratoria, c.PesoKg?.ToString(inv), c.Alergias,
            c.Diagnostico, c.TipoDiagnostico, c.Tratamiento, c.Indicaciones);
    }

    public static string Huella(string texto) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(texto)));

    // P-RC-07: el total del cargo se redondea a peso entero, mitad hacia arriba.
    public static decimal RedondearPesos(decimal monto) => Math.Round(monto, 0, MidpointRounding.AwayFromZero);
}
