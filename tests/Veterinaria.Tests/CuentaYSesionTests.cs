using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// A-006 a A-008 y A-010: registro, verificación, recuperación e ingreso.
public class CuentaYSesionTests
{
    [Fact]
    public void Registro_crea_cuenta_sin_verificar_y_guarda_versiones_aceptadas()
    {
        var f = new Fabrica();

        var resultado = f.Cuentas.Registrar(Fabrica.RegistroValido());
        var cuenta = f.Cuentas.ObtenerPorCorreo("cliente@prueba.uy")!;

        Assert.True(resultado.Exito);
        Assert.False(cuenta.Verificada);
        Assert.Equal("+59899123456", cuenta.Telefono);
        Assert.Equal("1.0", cuenta.VersionTerminosAceptada);
        Assert.NotEqual("clave-segura-1", cuenta.HashContrasena);
    }

    [Fact]
    public void No_permite_correo_ni_documento_repetidos()
    {
        var f = new Fabrica();
        f.Cuentas.Registrar(Fabrica.RegistroValido());

        var mismoCorreo = f.Cuentas.Registrar(Fabrica.RegistroValido(correo: "CLIENTE@prueba.uy", cedula: "2.345.678-3"));
        var mismoDocumento = f.Cuentas.Registrar(Fabrica.RegistroValido(correo: "otro@prueba.uy"));

        Assert.False(mismoCorreo.Exito);
        Assert.False(mismoDocumento.Exito);
    }

    [Fact]
    public void Codigo_incorrecto_descuenta_intentos_y_al_quinto_se_invalida()
    {
        var f = new Fabrica();
        f.Cuentas.Registrar(Fabrica.RegistroValido());
        var cuenta = f.Cuentas.ObtenerPorCorreo("cliente@prueba.uy")!;
        var incorrecto = cuenta.CodigoDemo == "000000" ? "111111" : "000000";

        for (var i = 0; i < 5; i++)
        {
            f.Cuentas.Verificar(cuenta.Correo, incorrecto);
        }

        var conElCorrecto = f.Cuentas.Verificar(cuenta.Correo, cuenta.CodigoDemo);

        Assert.False(conElCorrecto.Exito);
        Assert.False(cuenta.Verificada);
    }

    [Fact]
    public void Codigo_correcto_verifica_la_cuenta()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();

        Assert.True(cuenta.Verificada);
    }

    [Fact]
    public void Reenvio_limitado_a_tres_por_hora()
    {
        var f = new Fabrica();
        f.Cuentas.Registrar(Fabrica.RegistroValido());

        for (var i = 0; i < 3; i++)
        {
            Assert.True(f.Cuentas.ReenviarCodigo("cliente@prueba.uy").Exito);
        }

        Assert.False(f.Cuentas.ReenviarCodigo("cliente@prueba.uy").Exito);
    }

    [Fact]
    public void Cuenta_sin_verificar_no_puede_ingresar()
    {
        var f = new Fabrica();
        f.Cuentas.Registrar(Fabrica.RegistroValido());

        var resultado = f.Sesion.Ingresar(new() { Correo = "cliente@prueba.uy", Contrasena = "clave-segura-1" });

        Assert.False(resultado.Exito);
        Assert.False(f.Sesion.Iniciada);
    }

    [Fact]
    public void Contrasena_incorrecta_da_mensaje_generico()
    {
        var f = new Fabrica();
        f.ClienteVerificado();

        var mala = f.Sesion.Ingresar(new() { Correo = "cliente@prueba.uy", Contrasena = "otra" });
        var inexistente = f.Sesion.Ingresar(new() { Correo = "nadie@prueba.uy", Contrasena = "otra" });

        Assert.False(mala.Exito);
        Assert.Equal(mala.Mensaje, inexistente.Mensaje);
    }

    [Fact]
    public void Recuperacion_cambia_la_contrasena()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();

        var respuesta = f.Cuentas.SolicitarRecuperacion("cliente@prueba.uy");
        var cambio = f.Cuentas.Restablecer("cliente@prueba.uy", cuenta.CodigoRecuperacionDemo, "nueva-clave-2");

        Assert.Equal(f.Cuentas.SolicitarRecuperacion("nadie@prueba.uy").Mensaje, respuesta.Mensaje);
        Assert.True(cambio.Exito);
        Assert.False(f.Sesion.Ingresar(new() { Correo = "cliente@prueba.uy", Contrasena = "clave-segura-1" }).Exito);
        Assert.True(f.Sesion.Ingresar(new() { Correo = "cliente@prueba.uy", Contrasena = "nueva-clave-2" }).Exito);
    }

    [Theory]
    [InlineData("recepcion@vitalia.uy", PersonalService.Recepcion)]
    [InlineData("ferreira@vitalia.uy", PersonalService.Veterinario)]
    [InlineData("admin@vitalia.uy", PersonalService.Administrador)]
    public void Personal_ingresa_con_su_rol(string correo, string rol)
    {
        var f = new Fabrica();

        var resultado = f.Sesion.Ingresar(new() { Correo = correo, Contrasena = PersonalService.ContrasenaDemo });

        Assert.True(resultado.Exito);
        Assert.True(f.Sesion.EsPersonal);
        Assert.False(f.Sesion.Iniciada);
        Assert.True(f.Sesion.TieneRol(rol));
    }
}
