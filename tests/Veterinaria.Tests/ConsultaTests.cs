using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// A-019 y A-020: CU-VET-01, P-RC-04 a P-RC-06, RN-12, RN-13, RN-20, RN-40.
public class ConsultaTests
{
    private const string Ferreira = "ferreira@vitalia.uy";

    private static (Fabrica f, Personal vet, Turno turno, Consulta consulta) ConsultaIniciada()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var turno = f.TurnoEnEspera();
        f.Consultas.Iniciar(turno.Id, vet, out var consulta);
        return (f, vet, turno, consulta!);
    }

    [Fact]
    public void Iniciar_crea_un_borrador_y_pasa_el_turno_a_en_curso()
    {
        var (_, vet, turno, consulta) = ConsultaIniciada();

        Assert.Equal("Borrador", consulta.Estado);
        Assert.Equal("En curso", turno.Estado);
        Assert.Equal(vet.Id, consulta.AutorId);
    }

    [Fact]
    public void Iniciar_dos_veces_devuelve_la_misma_consulta()
    {
        var (f, vet, turno, consulta) = ConsultaIniciada();

        var segundo = f.Consultas.Iniciar(turno.Id, vet, out var otra);

        Assert.True(segundo.Exito);
        Assert.Same(consulta, otra);
    }

    [Fact]
    public void Otro_veterinario_no_inicia_un_turno_ajeno()
    {
        var f = new Fabrica();
        var turno = f.TurnoEnEspera();

        var resultado = f.Consultas.Iniciar(turno.Id, f.PersonalDe("silveira@vitalia.uy"), out var consulta);

        Assert.False(resultado.Exito);
        Assert.Null(consulta);
        Assert.Equal("En espera", turno.Estado);
    }

    [Fact]
    public void Sin_llegada_registrada_no_se_inicia()
    {
        var f = new Fabrica();
        var inicio = DateTime.Today.AddHours(7);
        f.Agenda.OcuparComoOtroCliente(1, 1, inicio);
        var turno = f.Agenda.TurnosDelDia(DateOnly.FromDateTime(inicio), 1).Last(t => t.Inicio == inicio);

        var resultado = f.Consultas.Iniciar(turno.Id, f.PersonalDe(Ferreira), out _);

        Assert.False(resultado.Exito);
        Assert.Equal("Confirmado", turno.Estado);
    }

    [Fact]
    public void Guardar_con_version_vieja_no_sobrescribe()
    {
        var (f, vet, _, consulta) = ConsultaIniciada();
        var version = consulta.Version;
        f.Consultas.Guardar(consulta.Id, version, new ConsultaFormulario { Motivo = "Primera" }, vet);

        var segundo = f.Consultas.Guardar(consulta.Id, version, new ConsultaFormulario { Motivo = "Pisada" }, vet);

        Assert.False(segundo.Exito);
        Assert.Equal("Primera", consulta.Motivo);
    }

    [Fact]
    public void Sin_diagnostico_no_se_firma_y_queda_en_borrador()
    {
        var (f, vet, turno, consulta) = ConsultaIniciada();

        var resultado = f.Consultas.Firmar(consulta.Id, consulta.Version, new ConsultaFormulario { Motivo = "Control" }, PersonalService.ContrasenaDemo, vet);

        Assert.False(resultado.Exito);
        Assert.Equal("Borrador", consulta.Estado);
        Assert.Equal("En curso", turno.Estado);
    }

    [Fact]
    public void Con_contrasena_incorrecta_no_se_firma()
    {
        var (f, vet, _, consulta) = ConsultaIniciada();

        var resultado = f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), "otra", vet);

        Assert.False(resultado.Exito);
        Assert.Equal("Borrador", consulta.Estado);
        Assert.Equal("", consulta.Motivo);
    }

    [Fact]
    public void Con_matricula_vencida_no_se_firma()
    {
        var (f, vet, _, consulta) = ConsultaIniciada();
        f.Profesionales.ObtenerPorId(1)!.MatriculaVigenteHasta = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

        var resultado = f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet);

        Assert.False(resultado.Exito);
        Assert.Equal("Borrador", consulta.Estado);
    }

    [Fact]
    public void Firmar_finaliza_el_turno_y_publica_en_la_historia()
    {
        var (f, vet, turno, consulta) = ConsultaIniciada();

        var resultado = f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet);

        Assert.True(resultado.Exito);
        Assert.Equal("Firmada", consulta.Estado);
        Assert.Equal("Finalizado", turno.Estado);
        Assert.NotNull(consulta.FechaFirma);
        Assert.Equal(64, consulta.HashContenido.Length);
        Assert.Contains(consulta, f.Consultas.HistoriaDe(1));
    }

    [Fact]
    public void Una_consulta_firmada_no_se_edita()
    {
        var (f, vet, _, consulta) = ConsultaIniciada();
        f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet);

        var resultado = f.Consultas.Guardar(consulta.Id, consulta.Version, new ConsultaFormulario { Motivo = "Cambiado", Diagnostico = "Otro" }, vet);

        Assert.False(resultado.Exito);
        Assert.Equal("Vómitos", consulta.Motivo);
    }

    [Fact]
    public void El_borrador_no_aparece_en_la_historia()
    {
        var (f, _, _, consulta) = ConsultaIniciada();

        Assert.DoesNotContain(consulta, f.Consultas.HistoriaDe(1));
        Assert.Contains(consulta, f.Consultas.AbiertasDe(consulta.AutorId));
    }

    [Fact]
    public void La_enmienda_se_agrega_sin_cambiar_el_original()
    {
        var (f, vet, _, consulta) = ConsultaIniciada();
        f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet);
        var huella = consulta.HashContenido;

        var resultado = f.Consultas.Enmendar(consulta.Id, new EnmiendaFormulario { Motivo = "Error de tipeo", Texto = "Peso 18,4 kg", Contrasena = PersonalService.ContrasenaDemo }, vet);

        Assert.True(resultado.Exito);
        Assert.Single(consulta.Enmiendas);
        Assert.Equal(huella, consulta.HashContenido);
        Assert.Equal(18.2m, consulta.PesoKg);
    }

    [Fact]
    public void Solo_el_autor_enmienda_y_solo_si_esta_firmada()
    {
        var (f, vet, _, consulta) = ConsultaIniciada();
        var datos = new EnmiendaFormulario { Motivo = "x", Texto = "y", Contrasena = PersonalService.ContrasenaDemo };

        Assert.False(f.Consultas.Enmendar(consulta.Id, datos, vet).Exito);

        f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet);

        Assert.False(f.Consultas.Enmendar(consulta.Id, datos, f.PersonalDe("silveira@vitalia.uy")).Exito);
        Assert.Empty(consulta.Enmiendas);
    }

    [Fact]
    public void Cancelar_sin_atencion_exige_motivo_y_conserva_el_borrador()
    {
        var (f, vet, turno, consulta) = ConsultaIniciada();

        Assert.False(f.Consultas.CancelarSinAtencion(consulta.Id, " ", vet).Exito);
        Assert.True(f.Consultas.CancelarSinAtencion(consulta.Id, "Se retiró antes de ser atendido", vet).Exito);
        Assert.Equal("Cancelado", turno.Estado);
        Assert.Equal("Veterinario", turno.CanceladoPor);
        Assert.Equal("Borrador", consulta.Estado);
        Assert.DoesNotContain(consulta, f.Consultas.HistoriaDe(1));
    }

    [Fact]
    public void Sin_turno_activo_la_historia_pide_motivo_y_registra_la_lectura()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var animal = f.AnimalDe(1);

        Assert.True(f.Consultas.NecesitaMotivo(animal.Id));
        Assert.False(f.Consultas.RegistrarLectura(vet, animal.Id, "").Exito);
        Assert.True(f.Consultas.RegistrarLectura(vet, animal.Id, "Interconsulta").Exito);
        Assert.Single(f.Consultas.LecturasDe(animal.Id));
    }

    [Fact]
    public void Recepcion_no_lee_la_historia_completa()
    {
        var f = new Fabrica();

        Assert.False(f.Consultas.RegistrarLectura(f.PersonalDe("recepcion@vitalia.uy"), 1, "Consulta del cliente").Exito);
        Assert.False(f.Consultas.RegistrarLectura(f.PersonalDe("admin@vitalia.uy"), 1, "Revisión").Exito);
    }

    [Fact]
    public void La_historia_de_demostracion_viene_firmada_y_ordenada()
    {
        var f = new Fabrica();

        var historia = f.Consultas.HistoriaDe(1);

        Assert.NotEmpty(historia);
        Assert.All(historia, c => Assert.Equal("Firmada", c.Estado));
        Assert.Single(historia[0].Enmiendas);
    }
}
