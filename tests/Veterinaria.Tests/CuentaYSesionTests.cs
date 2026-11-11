using Veterinaria.Blazor.Models;
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

    // A-009: «Mi cuenta».
    [Fact]
    public void Perfil_guarda_datos_editables_y_normaliza_el_telefono()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        var perfil = f.Cuentas.ParaEditarPerfil(cuenta);
        perfil.Nombre = "  Juana María ";
        perfil.Telefono = "098 765 432";
        perfil.Localidad = "Carmelo";

        var resultado = f.Cuentas.ActualizarPerfil(cuenta.Id, perfil);

        Assert.True(resultado.Exito);
        Assert.Equal("Juana María", cuenta.Nombre);
        Assert.Equal("+59898765432", cuenta.Telefono);
        Assert.Equal("Carmelo", cuenta.Localidad);
        Assert.Equal("cliente@prueba.uy", cuenta.Correo);
    }

    [Theory]
    [InlineData("telefono")]
    [InlineData("nombre")]
    [InlineData("departamento")]
    public void Perfil_invalido_no_cambia_nada(string campo)
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        var perfil = f.Cuentas.ParaEditarPerfil(cuenta);
        perfil.Localidad = "Carmelo";
        if (campo == "telefono") perfil.Telefono = "12";
        if (campo == "nombre") perfil.Nombre = "   ";
        if (campo == "departamento") perfil.Departamento = "Buenos Aires";

        var resultado = f.Cuentas.ActualizarPerfil(cuenta.Id, perfil);

        Assert.False(resultado.Exito);
        Assert.Equal("Nueva Palmira", cuenta.Localidad);
        Assert.Equal("+59899123456", cuenta.Telefono);
    }

    [Fact]
    public void Perfil_de_cuenta_sin_verificar_no_se_guarda()
    {
        var f = new Fabrica();
        f.Cuentas.Registrar(Fabrica.RegistroValido());
        var cuenta = f.Cuentas.ObtenerPorCorreo("cliente@prueba.uy")!;
        var perfil = f.Cuentas.ParaEditarPerfil(cuenta);
        perfil.Localidad = "Carmelo";

        Assert.False(f.Cuentas.ActualizarPerfil(cuenta.Id, perfil).Exito);
        Assert.Equal("Nueva Palmira", cuenta.Localidad);
    }

    // D-PER-04: cambio de correo con verificación.
    [Fact]
    public void Cambio_de_correo_espera_el_codigo_y_despues_se_ingresa_con_el_nuevo()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();

        var pedido = f.Cuentas.SolicitarCambioCorreo(cuenta.Id, new CambioCorreoFormulario { CorreoNuevo = " Nuevo@Prueba.uy ", ContrasenaActual = "clave-segura-1" });

        Assert.True(pedido.Exito);
        Assert.Equal("cliente@prueba.uy", cuenta.Correo);
        Assert.Equal("nuevo@prueba.uy", cuenta.CorreoNuevoPendiente);

        var confirmado = f.Cuentas.ConfirmarCambioCorreo(cuenta.Id, cuenta.CodigoCorreoDemo);

        Assert.True(confirmado.Exito);
        Assert.Equal("nuevo@prueba.uy", cuenta.Correo);
        Assert.Equal("", cuenta.CorreoNuevoPendiente);
        Assert.Null(f.Cuentas.ValidarCredenciales("cliente@prueba.uy", "clave-segura-1"));
        Assert.NotNull(f.Cuentas.ValidarCredenciales("nuevo@prueba.uy", "clave-segura-1"));
    }

    [Fact]
    public void Cambio_de_correo_exige_contrasena_y_correo_libre()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.ClienteVerificado("otro@prueba.uy", "2.345.678-3");

        var sinClave = f.Cuentas.SolicitarCambioCorreo(cuenta.Id, new CambioCorreoFormulario { CorreoNuevo = "nuevo@prueba.uy", ContrasenaActual = "otra-clave" });
        var ocupado = f.Cuentas.SolicitarCambioCorreo(cuenta.Id, new CambioCorreoFormulario { CorreoNuevo = "OTRO@prueba.uy", ContrasenaActual = "clave-segura-1" });
        var mismo = f.Cuentas.SolicitarCambioCorreo(cuenta.Id, new CambioCorreoFormulario { CorreoNuevo = "cliente@prueba.uy", ContrasenaActual = "clave-segura-1" });

        Assert.False(sinClave.Exito);
        Assert.False(ocupado.Exito);
        Assert.False(mismo.Exito);
        Assert.Equal("", cuenta.CorreoNuevoPendiente);
    }

    [Fact]
    public void Codigo_de_cambio_de_correo_incorrecto_se_invalida_al_quinto()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.Cuentas.SolicitarCambioCorreo(cuenta.Id, new CambioCorreoFormulario { CorreoNuevo = "nuevo@prueba.uy", ContrasenaActual = "clave-segura-1" });
        var correcto = cuenta.CodigoCorreoDemo;
        var incorrecto = correcto == "000000" ? "111111" : "000000";

        for (var i = 0; i < 5; i++)
        {
            Assert.False(f.Cuentas.ConfirmarCambioCorreo(cuenta.Id, incorrecto).Exito);
        }

        Assert.False(f.Cuentas.ConfirmarCambioCorreo(cuenta.Id, correcto).Exito);
        Assert.Equal("cliente@prueba.uy", cuenta.Correo);
    }

    // D-PER-05: cambio de contraseña con la actual.
    [Fact]
    public void Cambio_de_contrasena_exige_la_actual_y_reemplaza_la_anterior()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();

        var malaActual = f.Cuentas.CambiarContrasena(cuenta.Id, new CambioContrasenaFormulario { ContrasenaActual = "otra-clave", Contrasena = "clave-nueva-2", RepetirContrasena = "clave-nueva-2" });
        var igual = f.Cuentas.CambiarContrasena(cuenta.Id, new CambioContrasenaFormulario { ContrasenaActual = "clave-segura-1", Contrasena = "clave-segura-1", RepetirContrasena = "clave-segura-1" });
        var bien = f.Cuentas.CambiarContrasena(cuenta.Id, new CambioContrasenaFormulario { ContrasenaActual = "clave-segura-1", Contrasena = "clave-nueva-2", RepetirContrasena = "clave-nueva-2" });

        Assert.False(malaActual.Exito);
        Assert.False(igual.Exito);
        Assert.True(bien.Exito);
        Assert.Null(f.Cuentas.ValidarCredenciales("cliente@prueba.uy", "clave-segura-1"));
        Assert.NotNull(f.Cuentas.ValidarCredenciales("cliente@prueba.uy", "clave-nueva-2"));
    }
}
