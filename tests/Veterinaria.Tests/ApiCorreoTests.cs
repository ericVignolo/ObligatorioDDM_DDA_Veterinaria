using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — correo real por SMTP: textos de los códigos, cuándo se considera configurado y qué pasa si el servidor falla.
public class ApiCorreoTests
{
    [Fact]
    public void Codigo_de_verificacion_y_de_recuperacion_con_su_hora_de_vencimiento()
    {
        var vence = new DateTime(2026, 11, 22, 18, 45, 0, DateTimeKind.Local);

        var (asunto, cuerpo) = CorreoSmtp.ArmarCodigo(CuentaService.TipoVerificacion, "473775", vence);
        Assert.Equal("Verificá tu correo en VITALIA", asunto);
        Assert.Contains("473775", cuerpo);
        Assert.Contains("18:45", cuerpo);

        var (asuntoRec, cuerpoRec) = CorreoSmtp.ArmarCodigo(CuentaService.TipoRecuperacion, "120934", vence);
        Assert.Equal("Recuperá tu contraseña de VITALIA", asuntoRec);
        Assert.Contains("tu contraseña sigue igual", cuerpoRec);
    }

    [Fact]
    public void Sin_servidor_o_sin_remitente_no_esta_configurado()
    {
        Assert.False(new OpcionesCorreo().Configurado);
        Assert.False(new OpcionesCorreo { Servidor = "smtp.gmail.com" }.Configurado);
        Assert.True(new OpcionesCorreo { Servidor = "smtp.gmail.com", Remitente = "vitalia@gmail.com" }.Configurado);
    }

    [Fact]
    public async Task Si_el_servidor_no_responde_devuelve_el_motivo_sin_romper()
    {
        var smtp = new CorreoSmtp(Options.Create(new OpcionesCorreo { Servidor = "127.0.0.1", Puerto = 1, UsarSsl = false, Remitente = "vitalia@correo.uy" }), NullLogger<CorreoSmtp>.Instance);

        var error = await smtp.EnviarAsync("ana@correo.uy", "Prueba", "Hola");
        Assert.NotNull(error);
        Assert.DoesNotContain("ana@correo.uy", error);

        // El código no lanza: el cliente lo pide de nuevo con «Reenviar código».
        await smtp.EnviarCodigoAsync("ana@correo.uy", CuentaService.TipoVerificacion, "111111", DateTime.UtcNow.AddMinutes(15));
    }
}
