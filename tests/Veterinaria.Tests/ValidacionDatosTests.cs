using System.ComponentModel.DataAnnotations;
using Veterinaria.Blazor.Models;

namespace Veterinaria.Tests;

// D-REG-01: documento por tipo y teléfono en E.164.
public class ValidacionDatosTests
{
    [Theory]
    [InlineData("1.234.567-2", true)]
    [InlineData("12345672", true)]
    [InlineData("1.234.567-3", false)]
    [InlineData("12345", false)]
    [InlineData("abc", false)]
    public void Cedula_valida_con_digito_verificador(string cedula, bool esperado)
    {
        Assert.Equal(esperado, ValidacionDatos.CedulaValida(cedula));
    }

    [Theory]
    [InlineData("099 123 456", "+59899123456")]
    [InlineData("+598 99 123 456", "+59899123456")]
    [InlineData("4544 0000", "+59845440000")]
    [InlineData("0054 11 1234 5678", "+541112345678")]
    public void Telefono_se_normaliza_a_E164(string entrada, string esperado)
    {
        Assert.Equal(esperado, ValidacionDatos.NormalizarTelefono(entrada));
    }

    [Fact]
    public void Telefono_demasiado_corto_no_es_valido()
    {
        Assert.Null(ValidacionDatos.NormalizarTelefono("12"));
    }

    [Theory]
    [InlineData("Pasaporte", "AB123456", true)]
    [InlineData("Pasaporte", "A1", false)]
    [InlineData("Otro", "X-12", false)]
    public void Documento_extranjero_alfanumerico_de_5_a_20(string tipo, string numero, bool esperado)
    {
        Assert.Equal(esperado, ValidacionDatos.DocumentoValido(tipo, numero));
    }

    [Fact]
    public void Registro_sin_aceptaciones_no_valida()
    {
        var registro = Fabrica.RegistroValido();
        registro.AceptaTerminos = false;
        registro.AceptaPrivacidad = false;

        var errores = Validar(registro);

        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RegistroCliente.AceptaTerminos)));
        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RegistroCliente.AceptaPrivacidad)));
    }

    [Fact]
    public void Registro_con_cedula_invalida_marca_el_documento()
    {
        var registro = Fabrica.RegistroValido(cedula: "1.234.567-3");

        var errores = Validar(registro);

        Assert.Contains(errores, e => e.MemberNames.Contains(nameof(RegistroCliente.NumeroDocumento)));
    }

    [Fact]
    public void Registro_completo_es_valido()
    {
        Assert.Empty(Validar(Fabrica.RegistroValido()));
    }

    private static List<ValidationResult> Validar(object modelo)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(modelo, new ValidationContext(modelo), errores, validateAllProperties: true);
        return errores;
    }
}
