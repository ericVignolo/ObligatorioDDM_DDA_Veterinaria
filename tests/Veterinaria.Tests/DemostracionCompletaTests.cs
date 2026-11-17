using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// Recorrido de demostración completo: Ana reserva una cirugía con los datos de partida reales y nunca queda
// frente a una pantalla sin horarios (ver ESPECIFICACION_RECORRIDOS_DEMO.md, RD-11 y RD-12).
public class DemostracionCompletaTests
{
    private const int Ana = 100;
    private const int Toby = 1;
    private const int Castracion = 8;
    private const int Ferreira = 1;

    [Fact]
    public void RD11_ProximoDiaConHorarios_devuelve_el_primer_dia_con_lugar_para_la_castracion()
    {
        var f = new Fabrica();

        var dia = f.Agenda.ProximoDiaConHorarios(Ferreira, Castracion, Toby, f.Agenda.PrimerDia);

        Assert.NotNull(dia);
        Assert.NotEmpty(f.Agenda.HorariosLibres(Ferreira, Castracion, Toby, dia!.Value));
        for (var d = f.Agenda.PrimerDia; d < dia.Value; d = d.AddDays(1))
        {
            Assert.Empty(f.Agenda.HorariosLibres(Ferreira, Castracion, Toby, d));
        }
    }

    [Fact]
    public void RD11_ProximoDiaConHorarios_no_devuelve_nada_si_el_profesional_no_hace_el_servicio_o_no_hay_dias()
    {
        var f = new Fabrica();

        Assert.Null(f.Agenda.ProximoDiaConHorarios(Ferreira, 999, Toby, f.Agenda.PrimerDia));
        Assert.Null(f.Agenda.ProximoDiaConHorarios(Ferreira, Castracion, Toby, f.Agenda.UltimoDia.AddDays(1)));
    }

    [Fact]
    public void RD12_Ana_reserva_la_castracion_de_Toby_queda_pendiente_de_sena_y_Recepcion_la_confirma()
    {
        var f = new Fabrica();
        var dia = f.Agenda.ProximoDiaConHorarios(Ferreira, Castracion, Toby, f.Agenda.PrimerDia)!.Value;
        var horario = f.Agenda.HorariosLibres(Ferreira, Castracion, Toby, dia).First();

        var r = f.Agenda.Reservar(Ana, Toby, Castracion, Ferreira, horario, out var turno);

        Assert.True(r.Exito, r.Mensaje);
        Assert.Equal(AgendaService.PendienteSena, turno!.Estado);

        var sena = f.Cobros.GenerarSena(turno)!;
        var recepcion = f.PersonalDe("recepcion@vitalia.uy");
        f.Cobros.Cobrar(new CobroFormulario { CargoId = sena.Id, Monto = (int)sena.Monto, Medio = CobroService.Efectivo }, recepcion);

        Assert.Equal("Confirmado", turno.Estado);
    }

    [Fact]
    public void RD12_El_recorrido_se_puede_repetir_varias_veces_sin_agotar_los_horarios_de_la_castracion()
    {
        var f = new Fabrica();

        for (var i = 0; i < 5; i++)
        {
            var dia = f.Agenda.ProximoDiaConHorarios(Ferreira, Castracion, Toby, f.Agenda.PrimerDia);
            Assert.NotNull(dia);
            var horario = f.Agenda.HorariosLibres(Ferreira, Castracion, Toby, dia!.Value).First();
            var r = f.Agenda.Reservar(Ana, Toby, Castracion, Ferreira, horario, out var turno);
            Assert.True(r.Exito, r.Mensaje);
            f.Agenda.Cancelar(turno!.Id, Ana, "Prueba de repetición");
        }
    }
}
