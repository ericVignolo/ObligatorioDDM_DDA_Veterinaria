using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// A-025: RF-CLI-11, RN-10, RN-18, RN-43, P-RC-06. La castración (prestación 8) pide consentimiento.
public class ConsentimientoTests
{
    private const string Ferreira = "ferreira@vitalia.uy";
    private const string Admin = "admin@vitalia.uy";

    private static ConsentimientoFormulario Firma(string nombre = "Juana Pérez", bool acepta = true)
    {
        return new ConsentimientoFormulario { LeyoYAcepta = acepta, NombreCompleto = nombre };
    }

    // Castración futura reservada por un cliente nuevo (Juana Pérez) para su perro.
    private static (Fabrica f, Cuenta cliente, Turno turno) CastracionReservada()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id, "Perro", "Firulais");
        var reserva = f.Agenda.Reservar(cliente.Id, animal.Id, 8, 1, Fabrica.A(f.DiaQueAtiende(1), 9), out var turno);
        Assert.True(reserva.Exito, reserva.Mensaje);
        return (f, cliente, turno!);
    }

    [Fact]
    public void La_castracion_pide_consentimiento_y_arranca_pendiente()
    {
        var (f, cliente, turno) = CastracionReservada();

        Assert.True(f.Consentimientos.Requiere(turno));
        Assert.Equal(ConsentimientoService.Pendiente, f.Consentimientos.Estado(turno));
        Assert.Contains(f.Consentimientos.PendientesDe(cliente.Id), t => t.Id == turno.Id);
    }

    [Fact]
    public void Firmar_guarda_version_nombre_canal_y_huella()
    {
        var (f, cliente, turno) = CastracionReservada();

        var resultado = f.Consentimientos.Firmar(turno.Id, Firma("  juana   PÉREZ "), cliente);

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.Equal(ConsentimientoService.Firmado, f.Consentimientos.Estado(turno));
        var registro = f.Consentimientos.UltimaRespuesta(turno.Id)!;
        Assert.Equal(ConsentimientoService.Aceptado, registro.Respuesta);
        Assert.Equal("1.0", registro.Version);
        Assert.Equal(turno.AnimalId, registro.AnimalId);
        Assert.Equal(8, registro.PrestacionId);
        Assert.Equal("En línea", registro.Canal);
        Assert.Equal(64, registro.Hash.Length);
        Assert.DoesNotContain(f.Consentimientos.PendientesDe(cliente.Id), t => t.Id == turno.Id);
    }

    [Fact]
    public void Sin_marcar_la_casilla_o_con_otro_nombre_no_se_firma()
    {
        var (f, cliente, turno) = CastracionReservada();

        Assert.False(f.Consentimientos.Firmar(turno.Id, Firma(acepta: false), cliente).Exito);
        Assert.False(f.Consentimientos.Firmar(turno.Id, Firma("Juana"), cliente).Exito);
        Assert.False(f.Consentimientos.Firmar(turno.Id, Firma(""), cliente).Exito);
        Assert.Empty(f.Consentimientos.RespuestasDe(turno.Id));
    }

    [Fact]
    public void Otro_cliente_no_firma_un_turno_ajeno()
    {
        var (f, _, turno) = CastracionReservada();
        var otro = f.ClienteVerificado("otra@prueba.uy", "2.345.678-3");

        Assert.False(f.Consentimientos.Firmar(turno.Id, Firma(), otro).Exito);
        Assert.False(f.Consentimientos.Firmar(turno.Id, Firma(), null).Exito);
        Assert.Empty(f.Consentimientos.RespuestasDe(turno.Id));
    }

    [Fact]
    public void No_se_firma_dos_veces()
    {
        var (f, cliente, turno) = CastracionReservada();
        f.Consentimientos.Firmar(turno.Id, Firma(), cliente);

        Assert.False(f.Consentimientos.Firmar(turno.Id, Firma(), cliente).Exito);
        Assert.Single(f.Consentimientos.RespuestasDe(turno.Id));
    }

    [Fact]
    public void Rechazar_queda_registrado_y_despues_se_puede_firmar_con_un_registro_nuevo()
    {
        var (f, cliente, turno) = CastracionReservada();

        Assert.True(f.Consentimientos.Rechazar(turno.Id, cliente).Exito);
        Assert.Equal(ConsentimientoService.Rechazado, f.Consentimientos.Estado(turno));
        Assert.False(f.Consentimientos.Rechazar(turno.Id, cliente).Exito);

        Assert.True(f.Consentimientos.Firmar(turno.Id, Firma(), cliente).Exito);

        var respuestas = f.Consentimientos.RespuestasDe(turno.Id);
        Assert.Equal(2, respuestas.Count);
        Assert.Equal(ConsentimientoService.Rechazado, respuestas[1].Respuesta);
        Assert.Equal(ConsentimientoService.Firmado, f.Consentimientos.Estado(turno));
    }

    [Fact]
    public void Una_version_nueva_invalida_lo_firmado()
    {
        var (f, cliente, turno) = CastracionReservada();
        f.Consentimientos.Firmar(turno.Id, Firma(), cliente);

        var publicar = f.Consentimientos.PublicarVersion(8, "1.1", "Texto actualizado.", f.PersonalDe(Admin));

        Assert.True(publicar.Exito, publicar.Mensaje);
        Assert.False(f.Consentimientos.EsValido(turno.Id));
        Assert.Equal(ConsentimientoService.Desactualizado, f.Consentimientos.Estado(turno));

        Assert.True(f.Consentimientos.Firmar(turno.Id, Firma(), cliente).Exito);
        Assert.Equal("1.1", f.Consentimientos.UltimaRespuesta(turno.Id)!.Version);
    }

    [Fact]
    public void Solo_el_Administrador_publica_una_version_distinta()
    {
        var f = new Fabrica();

        Assert.False(f.Consentimientos.PublicarVersion(8, "1.1", "Texto", f.PersonalDe(Ferreira)).Exito);
        Assert.False(f.Consentimientos.PublicarVersion(8, "1.0", "Texto", f.PersonalDe(Admin)).Exito);
        Assert.Equal("1.0", f.Consentimientos.DocumentoVigente(8)!.Version);
    }

    [Fact]
    public void Un_turno_que_no_lo_pide_no_admite_firma()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id);
        f.Agenda.Reservar(cliente.Id, animal.Id, 1, 1, Fabrica.A(f.DiaQueAtiende(1), 9), out var turno);

        Assert.False(f.Consentimientos.Requiere(turno!));
        Assert.Equal(ConsentimientoService.NoRequiere, f.Consentimientos.Estado(turno!));
        Assert.False(f.Consentimientos.Firmar(turno!.Id, Firma(), cliente).Exito);
    }

    [Fact]
    public void RN10_sin_consentimiento_la_cirugia_no_pasa_a_en_curso()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id);
        var turno = f.CirugiaEnEspera(cliente.Id, animal.Id);

        var resultado = f.Consultas.Iniciar(turno.Id, f.PersonalDe(Ferreira));

        Assert.False(resultado.Exito);
        Assert.Equal("En espera", turno.Estado);
        Assert.Null(f.Consultas.ObtenerPorTurno(turno.Id));
    }

    [Fact]
    public void RN10_con_el_consentimiento_firmado_la_cirugia_empieza()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id);
        var turno = f.CirugiaEnEspera(cliente.Id, animal.Id);
        Assert.True(f.Consentimientos.Firmar(turno.Id, Firma(), cliente).Exito);

        var resultado = f.Consultas.Iniciar(turno.Id, f.PersonalDe(Ferreira));

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.Equal("En curso", turno.Estado);
    }

    [Fact]
    public void Con_el_turno_en_curso_ya_no_se_responde()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id);
        var turno = f.CirugiaEnEspera(cliente.Id, animal.Id);
        f.Consentimientos.Firmar(turno.Id, Firma(), cliente);
        f.Consultas.Iniciar(turno.Id, f.PersonalDe(Ferreira));

        Assert.False(f.Consentimientos.Rechazar(turno.Id, cliente).Exito);
        Assert.Single(f.Consentimientos.RespuestasDe(turno.Id));
    }
}
