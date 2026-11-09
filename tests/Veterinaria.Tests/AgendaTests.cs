using Veterinaria.Blazor.Models;

namespace Veterinaria.Tests;

// A-014 a A-017: disponibilidad, reserva, cancelación, reprogramación y acciones de Recepción.
public class AgendaTests
{
    private const int Ferreira = 1;
    private const int Consulta = 1;      // 30 min
    private const int Vacunacion = 2;    // 15 min

    [Fact]
    public void Horarios_cada_15_minutos_dentro_de_la_jornada_y_sin_descanso()
    {
        var f = new Fabrica();
        var dia = f.DiaQueAtiende(Ferreira);
        var animal = f.AnimalDe(1);

        var horarios = f.Agenda.HorariosLibres(Ferreira, Consulta, animal.Id, dia);

        Assert.Equal(Fabrica.A(dia, 9), horarios.First());
        Assert.Equal(Fabrica.A(dia, 13, 30), horarios.Last());
        Assert.Contains(Fabrica.A(dia, 9, 15), horarios);
        // Una consulta de 30 min no puede pisar el descanso de 11:30 a 12:00.
        Assert.DoesNotContain(Fabrica.A(dia, 11, 15), horarios);
        Assert.DoesNotContain(Fabrica.A(dia, 11, 30), horarios);
        Assert.Contains(Fabrica.A(dia, 12), horarios);
    }

    [Fact]
    public void Un_turno_bloquea_toda_su_duracion()
    {
        var f = new Fabrica();
        var dia = f.DiaQueAtiende(Ferreira);
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id);
        var otro = f.AnimalDe(cliente.Id, nombre: "Otro");

        var reserva = f.Agenda.Reservar(cliente.Id, animal.Id, Consulta, Ferreira, Fabrica.A(dia, 9), out _);
        var libres = f.Agenda.HorariosLibres(Ferreira, Vacunacion, otro.Id, dia);

        Assert.True(reserva.Exito);
        Assert.DoesNotContain(Fabrica.A(dia, 9), libres);
        Assert.DoesNotContain(Fabrica.A(dia, 9, 15), libres);
        Assert.Contains(Fabrica.A(dia, 9, 30), libres);
    }

    [Fact]
    public void El_mismo_animal_no_puede_tener_dos_turnos_a_la_vez()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id);
        var dia = f.DiaQueAtiende(Ferreira);

        f.Agenda.Reservar(cliente.Id, animal.Id, Consulta, Ferreira, Fabrica.A(dia, 10), out _);
        // Méndez (baño) atiende de 10 a 18 de lunes a viernes; si ese día atiende, el animal ya está ocupado a las 10.
        var libresBano = f.Agenda.HorariosLibres(5, 6, animal.Id, dia);

        Assert.DoesNotContain(Fabrica.A(dia, 10), libresBano);
    }

    [Fact]
    public void Sin_horarios_en_dia_que_no_atiende_feriado_o_fuera_de_60_dias()
    {
        var f = new Fabrica();
        var animal = f.AnimalDe(1);
        var domingo = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        while (domingo.DayOfWeek != DayOfWeek.Sunday) domingo = domingo.AddDays(1);

        Assert.Empty(f.Agenda.HorariosLibres(Ferreira, Consulta, animal.Id, domingo));
        Assert.Empty(f.Agenda.HorariosLibres(Ferreira, Consulta, animal.Id, new DateOnly(2026, 12, 25)));
        Assert.Empty(f.Agenda.HorariosLibres(Ferreira, Consulta, animal.Id, DateOnly.FromDateTime(DateTime.Today.AddDays(61))));
    }

    [Fact]
    public void Sin_horarios_el_dia_de_ausencia_del_profesional()
    {
        var f = new Fabrica();
        var animal = f.AnimalDe(1);
        var ausencia = f.Profesionales.ObtenerPorId(Ferreira)!.Ausencias.First().Desde;

        Assert.Empty(f.Agenda.HorariosLibres(Ferreira, Consulta, animal.Id, ausencia));
    }

    [Fact]
    public void No_se_reserva_para_un_animal_ajeno()
    {
        var f = new Fabrica();
        var dia = f.DiaQueAtiende(Ferreira);

        var resultado = f.Agenda.Reservar(999, 1, Consulta, Ferreira, Fabrica.A(dia, 9), out var turno);

        Assert.False(resultado.Exito);
        Assert.Null(turno);
    }

    [Fact]
    public void Solo_se_gestiona_con_mas_de_24_horas()
    {
        var f = new Fabrica();

        Assert.True(f.Agenda.PuedeGestionar(new Turno { Estado = "Confirmado", Inicio = DateTime.Now.AddHours(25) }));
        Assert.False(f.Agenda.PuedeGestionar(new Turno { Estado = "Confirmado", Inicio = DateTime.Now.AddHours(23) }));
        Assert.False(f.Agenda.PuedeGestionar(new Turno { Estado = "Cancelado", Inicio = DateTime.Now.AddDays(5) }));
    }

    [Fact]
    public void Cancelar_libera_el_horario()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id);
        var dia = f.DiaQueAtiende(Ferreira);
        f.Agenda.Reservar(cliente.Id, animal.Id, Consulta, Ferreira, Fabrica.A(dia, 9), out var turno);

        var resultado = f.Agenda.Cancelar(turno!.Id, cliente.Id, "Viaje");

        Assert.True(resultado.Exito);
        Assert.Equal("Cancelado", turno.Estado);
        Assert.Equal("Cliente", turno.CanceladoPor);
        Assert.Contains(Fabrica.A(dia, 9), f.Agenda.HorariosLibres(Ferreira, Consulta, animal.Id, dia));
    }

    [Fact]
    public void Reprogramar_conserva_el_numero_y_libera_el_horario_anterior()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id);
        var dia = f.DiaQueAtiende(Ferreira);
        f.Agenda.Reservar(cliente.Id, animal.Id, Consulta, Ferreira, Fabrica.A(dia, 9), out var turno);
        var id = turno!.Id;

        var resultado = f.Agenda.Reprogramar(id, cliente.Id, Fabrica.A(dia, 12));

        Assert.True(resultado.Exito);
        Assert.Equal(id, turno.Id);
        Assert.Equal(Fabrica.A(dia, 12), turno.Inicio);
        Assert.Equal(Fabrica.A(dia, 9), turno.InicioAnterior);
        Assert.Contains(Fabrica.A(dia, 9), f.Agenda.HorariosLibres(Ferreira, Consulta, animal.Id, dia));
    }

    [Fact]
    public void Reprogramar_a_un_horario_ocupado_no_cambia_el_turno()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id);
        var dia = f.DiaQueAtiende(Ferreira);
        f.Agenda.Reservar(cliente.Id, animal.Id, Consulta, Ferreira, Fabrica.A(dia, 9), out var turno);
        f.Agenda.OcuparComoOtroCliente(Ferreira, Consulta, Fabrica.A(dia, 12));

        var resultado = f.Agenda.Reprogramar(turno!.Id, cliente.Id, Fabrica.A(dia, 12));

        Assert.False(resultado.Exito);
        Assert.Equal(Fabrica.A(dia, 9), turno.Inicio);
    }

    [Fact]
    public void Recepcion_no_marca_llegada_ni_ausencia_antes_de_tiempo()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id);
        var dia = f.DiaQueAtiende(Ferreira);
        f.Agenda.Reservar(cliente.Id, animal.Id, Consulta, Ferreira, Fabrica.A(dia, 9), out var turno);

        Assert.False(f.Agenda.RegistrarLlegada(turno!.Id).Exito);
        Assert.False(f.Agenda.RegistrarAusencia(turno.Id).Exito);
        Assert.Equal("Confirmado", turno.Estado);
    }

    [Fact]
    public void Recepcion_cancela_solo_con_motivo()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id);
        var dia = f.DiaQueAtiende(Ferreira);
        f.Agenda.Reservar(cliente.Id, animal.Id, Consulta, Ferreira, Fabrica.A(dia, 9), out var turno);

        Assert.False(f.Agenda.CancelarPorRecepcion(turno!.Id, " ").Exito);
        Assert.True(f.Agenda.CancelarPorRecepcion(turno.Id, "Avisó por teléfono").Exito);
        Assert.Equal("Recepción", turno.CanceladoPor);
    }

    [Fact]
    public void Ocupacion_cero_el_dia_de_ausencia()
    {
        var f = new Fabrica();
        var ausencia = f.Profesionales.ObtenerPorId(Ferreira)!.Ausencias.First().Desde;

        Assert.Equal((0, 0), f.Agenda.Ocupacion(Ferreira, ausencia));
    }
}
