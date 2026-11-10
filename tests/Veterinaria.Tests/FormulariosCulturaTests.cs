using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Veterinaria.Blazor.Models;

namespace Veterinaria.Tests;

// La aplicación corre en es-UY (Program.cs). Validar los formularios en esa cultura evita excepciones
// que en el navegador dejan la pantalla en negro (los límites "0.01" con punto, por ejemplo).
public class FormulariosCulturaTests
{
    public static IEnumerable<object[]> Formularios()
    {
        yield return new object[] { new ConsultaFormulario() };
        yield return new object[] { new ConsultaFormulario { PesoKg = 18.4m, TemperaturaC = 38.5m, FrecuenciaCardiaca = 90, FrecuenciaRespiratoria = 20 } };
        yield return new object[] { new EnmiendaFormulario() };
        yield return new object[] { new FirmaConsulta() };
        yield return new object[] { new AnimalFormulario() };
        yield return new object[] { Fabrica.RegistroValido() };
        yield return new object[] { new InicioSesion() };
    }

    [Theory]
    [MemberData(nameof(Formularios))]
    public void Validar_en_es_UY_no_tira_excepcion(object formulario)
    {
        var anterior = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("es-UY");
        try
        {
            var errores = new List<ValidationResult>();
            Validator.TryValidateObject(formulario, new ValidationContext(formulario), errores, validateAllProperties: true);
        }
        finally
        {
            CultureInfo.CurrentCulture = anterior;
        }
    }

    [Theory]
    [InlineData(0.005, false)]
    [InlineData(0.01, true)]
    [InlineData(2000, true)]
    [InlineData(2001, false)]
    public void Rango_de_peso_en_es_UY(double peso, bool valido)
    {
        var anterior = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("es-UY");
        try
        {
            var formulario = new ConsultaFormulario { PesoKg = (decimal)peso };
            var errores = new List<ValidationResult>();
            Validator.TryValidateObject(formulario, new ValidationContext(formulario), errores, validateAllProperties: true);

            Assert.Equal(valido, !errores.Any(e => e.MemberNames.Contains(nameof(ConsultaFormulario.PesoKg))));
        }
        finally
        {
            CultureInfo.CurrentCulture = anterior;
        }
    }
}
