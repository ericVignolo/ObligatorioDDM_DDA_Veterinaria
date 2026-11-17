using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// Reglas pendientes de la revisión de la sección A (decisiones de Eric, 2026-11-17):
// RN-08 ausencias, RN-09 sobreturno, RN-16 intervalo entre dosis, RN-17 alergias, RN-35 seña, RN-36 deuda vencida.
public class ReglasPendientesTests
{
    private const string Ferreira = "ferreira@vitalia.uy";
    private const string Recepcion = "recepcion@vitalia.uy";
    private const int Castracion = 8;

    private static Turno Reservar(Fabrica f, int clienteId, int animalId, int prestacionId = 1, int hora = 9)
    {
        var r = f.Agenda.Reservar(clienteId, animalId, prestacionId, 1, Fabrica.A(f.DiaQueAtiende(1), hora), out var turno);
        Assert.True(r.Exito, r.Mensaje);
        return turno!;
    }

    private static void MarcarAusente(Turno turno, int haceDias)
    {
        turno.Estado = "Ausente";
        turno.Inicio = DateTime.Now.AddDays(-haceDias);
        turno.Fin = turno.Inicio.AddMinutes(30);
    }

    // ---------- RN-08 ----------

    [Fact]
    public void RN08_con_dos_ausencias_se_avisa_y_con_tres_ya_no_se_reserva_en_linea()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id, "Perro", "Firulais");

        Assert.Equal("", f.Agenda.MensajeAusencias(cliente.Id));
        MarcarAusente(Reservar(f, cliente.Id, animal.Id, hora: 9), 10);
        MarcarAusente(Reservar(f, cliente.Id, animal.Id, hora: 10), 20);
        Assert.Contains("Si llegás a 3", f.Agenda.MensajeAusencias(cliente.Id));
        Assert.False(f.Agenda.BloqueadoPorAusencias(cliente.Id));

        MarcarAusente(Reservar(f, cliente.Id, animal.Id, hora: 11), 30);

        Assert.True(f.Agenda.BloqueadoPorAusencias(cliente.Id));
        var r = f.Agenda.Reservar(cliente.Id, animal.Id, 1, 1, Fabrica.A(f.DiaQueAtiende(1), 12), out var turno);
        Assert.False(r.Exito);
        Assert.Null(turno);
        Assert.Contains("Recepción", r.Mensaje);
    }

    [Fact]
    public void RN08_las_ausencias_de_hace_mas_de_un_anio_no_cuentan_y_Recepcion_igual_puede_dar_turno()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id, "Perro", "Firulais");
        MarcarAusente(Reservar(f, cliente.Id, animal.Id, hora: 9), 400);
        MarcarAusente(Reservar(f, cliente.Id, animal.Id, hora: 10), 5);
        MarcarAusente(Reservar(f, cliente.Id, animal.Id, hora: 11), 6);
        Assert.Equal(2, f.Agenda.AusenciasRecientes(cliente.Id));

        MarcarAusente(Reservar(f, cliente.Id, animal.Id, hora: 12), 7);
        Assert.True(f.Agenda.BloqueadoPorAusencias(cliente.Id));

        var dia = f.DiaQueAtiende(1);
        var libre = f.Agenda.HorariosLibres(1, 1, animal.Id, dia).First();
        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, 1, 1, libre, false, "", f.PersonalDe(Recepcion), out var turno);
        Assert.True(r.Exito, r.Mensaje);
        Assert.Equal("Confirmado", turno!.Estado);
    }

    // ---------- RN-09 ----------

    [Fact]
    public void RN09_sobreturno_solo_lo_da_Recepcion_o_el_Administrador_con_motivo_y_queda_en_la_bitacora()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id, "Perro", "Firulais");
        var ocupado = Reservar(f, cliente.Id, f.AnimalDe(cliente.Id, "Gato", "Michi").Id, hora: 9);

        Assert.False(f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, 1, 1, ocupado.Inicio, true, "Urgencia leve", f.PersonalDe(Ferreira), out _).Exito);
        Assert.False(f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, 1, 1, ocupado.Inicio, true, "", f.PersonalDe(Recepcion), out _).Exito);
        Assert.False(f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, 1, 1, ocupado.Inicio, false, "", f.PersonalDe(Recepcion), out _).Exito);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, 1, 1, ocupado.Inicio, true, "Llegó sin turno", f.PersonalDe(Recepcion), out var sobre);

        Assert.True(r.Exito, r.Mensaje);
        Assert.True(sobre!.EsSobreturno);
        Assert.Equal("Llegó sin turno", sobre.MotivoSobreturno);
        Assert.Contains(f.Auditoria.Buscar(), a => a.Accion == "Sobreturno" && a.Entidad == BitacoraService.Agenda && !a.Detalle.Contains("Llegó sin turno"));
    }

    [Fact]
    public void RN09_el_sobreturno_cae_en_la_jornada_y_no_choca_con_otro_turno_del_mismo_animal()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id, "Perro", "Firulais");
        var propio = Reservar(f, cliente.Id, animal.Id, hora: 9);
        var recepcion = f.PersonalDe(Recepcion);

        Assert.False(f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, 1, 1, propio.Inicio, true, "Otra vez", recepcion, out _).Exito);
        Assert.False(f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, 1, 1, Fabrica.A(f.DiaQueAtiende(1), 3), true, "De madrugada", recepcion, out _).Exito);
        Assert.False(f.Agenda.ReservarPorMostrador(cliente.Id, 2, 1, 1, propio.Inicio, true, "Animal ajeno", recepcion, out _).Exito);
    }

    // ---------- RN-35 ----------

    [Fact]
    public void RN35_la_cirugia_queda_pendiente_de_sena_y_se_confirma_al_pagarla()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id, "Perro", "Firulais");

        var turno = Reservar(f, cliente.Id, animal.Id, Castracion);

        Assert.Equal(AgendaService.PendienteSena, turno.Estado);
        Assert.Empty(f.Agenda.HorariosLibres(1, 1, 0, DateOnly.FromDateTime(turno.Inicio)).Where(h => h == turno.Inicio));

        var sena = f.Cobros.GenerarSena(turno)!;
        Assert.Same(sena, f.Cobros.GenerarSena(turno));
        var precio = f.Prestaciones.PrecioVigente(Castracion, DateOnly.FromDateTime(turno.Inicio));
        Assert.Equal(Math.Round(precio * 0.30m), sena.Monto);

        var pago = f.Cobros.Cobrar(new CobroFormulario { CargoId = sena.Id, Monto = (int)sena.Monto, Medio = CobroService.Efectivo }, f.PersonalDe(Recepcion));

        Assert.True(pago.Exito, pago.Mensaje);
        Assert.Equal("Confirmado", turno.Estado);
    }

    [Fact]
    public void RN35_un_pago_parcial_no_confirma_y_anular_la_sena_vuelve_a_pendiente()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id, "Perro", "Firulais");
        var turno = Reservar(f, cliente.Id, animal.Id, Castracion);
        var sena = f.Cobros.GenerarSena(turno)!;
        var recepcion = f.PersonalDe(Recepcion);

        f.Cobros.Cobrar(new CobroFormulario { CargoId = sena.Id, Monto = 100, Medio = CobroService.Efectivo }, recepcion);
        Assert.Equal(AgendaService.PendienteSena, turno.Estado);

        f.Cobros.Cobrar(new CobroFormulario { CargoId = sena.Id, Monto = (int)sena.Monto - 100, Medio = CobroService.Efectivo }, recepcion);
        Assert.Equal("Confirmado", turno.Estado);

        var ultimo = f.Cobros.PagosDe(sena.Id).Last();
        var anulacion = f.Cobros.Anular(ultimo.Id, new AnulacionFormulario { Motivo = "Error de caja" }, f.PersonalDe("admin@vitalia.uy"));
        Assert.True(anulacion.Exito, anulacion.Mensaje);
        Assert.Equal(AgendaService.PendienteSena, turno.Estado);
    }

    [Fact]
    public void RN35_un_servicio_sin_cirugia_se_confirma_directo_y_no_genera_sena()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var turno = Reservar(f, cliente.Id, f.AnimalDe(cliente.Id, "Perro", "Firulais").Id);

        Assert.Equal("Confirmado", turno.Estado);
        Assert.Null(f.Cobros.GenerarSena(turno));
    }

    [Fact]
    public void RN35_el_cliente_puede_cancelar_una_cirugia_pendiente_de_sena()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var turno = Reservar(f, cliente.Id, f.AnimalDe(cliente.Id, "Perro", "Firulais").Id, Castracion);

        Assert.True(f.Agenda.Cancelar(turno.Id, cliente.Id, "").Exito);
    }

    // ---------- RN-36 ----------

    [Fact]
    public void RN36_la_deuda_vencida_sobre_el_limite_avisa_sin_bloquear()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id, "Perro", "Firulais");
        Assert.Equal("", f.Cobros.AvisoDeuda(cliente.Id));

        var cirugia = Reservar(f, cliente.Id, animal.Id, Castracion);
        var sena = f.Cobros.GenerarSena(cirugia)!;
        sena.Fecha = DateTime.Now.AddDays(-60);
        Assert.Equal(0m, f.Cobros.DeudaVencida(cliente.Id));

        f.Cobros.LimiteDeuda = 0;
        var otro = Reservar(f, cliente.Id, animal.Id, hora: 12);
        // Un cargo viejo con saldo cuenta como deuda vencida.
        var cargo = f.Cobros.GenerarSena(new Turno { Id = otro.Id, ClienteId = cliente.Id, AnimalId = animal.Id, PrestacionId = Castracion, Inicio = otro.Inicio, Estado = AgendaService.PendienteSena })!;
        cargo.EsSena = false;
        cargo.Fecha = DateTime.Now.AddDays(-45);

        Assert.True(f.Cobros.DeudaVencida(cliente.Id) > 0);
        Assert.Contains("Se puede agendar igual", f.Cobros.AvisoDeuda(cliente.Id));
        Assert.True(f.Agenda.Reservar(cliente.Id, animal.Id, 1, 1, Fabrica.A(f.DiaQueAtiende(1), 14), out _).Exito);
    }

    // ---------- Parámetros ----------

    [Fact]
    public void Solo_el_Administrador_cambia_los_parametros_y_dentro_de_rango()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe("admin@vitalia.uy");

        Assert.False(f.Administracion.CambiarParametrosAgenda(4, 40, 5000, f.PersonalDe(Recepcion)).Exito);
        Assert.False(f.Administracion.CambiarParametrosAgenda(1, 40, 5000, admin).Exito);
        Assert.False(f.Administracion.CambiarParametrosAgenda(4, 5, 5000, admin).Exito);
        Assert.True(f.Administracion.CambiarParametrosAgenda(4, 40, 5000, admin).Exito);
        Assert.Equal(4, f.Agenda.LimiteAusencias);
        Assert.Equal(40, f.Cobros.PorcentajeSena);
        Assert.Equal(5000, f.Cobros.LimiteDeuda);
    }

    // ---------- RN-17 ----------

    [Fact]
    public void RN17_indicar_una_alergia_registrada_exige_confirmacion_y_queda_asentado()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var turno = f.TurnoEnEspera();
        f.Consultas.Iniciar(turno.Id, vet);
        var consulta = f.Consultas.ObtenerPorTurno(turno.Id)!;
        var datos = Fabrica.ConsultaCompleta();
        datos.Alergias = "Penicilina";
        datos.Tratamiento = "PENICILÍNA benzatínica una dosis";

        var sinConfirmar = f.Consultas.Firmar(consulta.Id, consulta.Version, datos, PersonalService.ContrasenaDemo, vet);
        Assert.False(sinConfirmar.Exito);
        Assert.Contains("RN-17", sinConfirmar.Mensaje);
        Assert.Equal("Borrador", consulta.Estado);

        datos.ConfirmaAlergia = true;
        var confirmada = f.Consultas.Firmar(consulta.Id, consulta.Version, datos, PersonalService.ContrasenaDemo, vet);
        Assert.True(confirmada.Exito, confirmada.Mensaje);
        Assert.Equal("Penicilina", consulta.AlergiaConfirmada);
    }

    [Fact]
    public void RN17_las_alergias_salen_de_la_historia_y_solo_cuenta_la_palabra_completa()
    {
        var f = new Fabrica();

        Assert.Contains("Penicilina", f.Consultas.AlergiasDe(4));
        Assert.Empty(f.Consultas.AlergiasEnTratamiento(4, new ConsultaFormulario { Tratamiento = "Amoxicilina" }));
        Assert.Empty(f.Consultas.AlergiasEnTratamiento(1, new ConsultaFormulario { Alergias = "Ninguna", Tratamiento = "Ninguna indicación" }));
        Assert.Single(f.Consultas.AlergiasEnTratamiento(4, new ConsultaFormulario { Indicaciones = "Dar penicilina" }));
    }

    // ---------- RN-16 ----------

    [Fact]
    public void RN16_adelantar_una_dosis_exige_justificacion_y_queda_registrada()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var primero = f.TurnoEnEspera();
        f.Consultas.Iniciar(primero.Id, vet);
        var c1 = f.Consultas.ObtenerPorTurno(primero.Id)!;
        Assert.True(f.Vacunas.Registrar(c1.Id, new AplicacionFormulario { ProductoId = 6 }, vet).Exito);
        Assert.True(f.Consultas.Firmar(c1.Id, c1.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet).Exito);

        var inicio = DateTime.Today.AddHours(8);
        f.Agenda.OcuparComoOtroCliente(1, 1, inicio);
        var segundo = f.Agenda.TurnosDelDia(DateOnly.FromDateTime(inicio), 1).Last(t => t.Inicio == inicio);
        f.Agenda.RegistrarLlegada(segundo.Id);
        f.Consultas.Iniciar(segundo.Id, vet);
        var c2 = f.Consultas.ObtenerPorTurno(segundo.Id)!;

        var sinJustificar = f.Vacunas.Registrar(c2.Id, new AplicacionFormulario { ProductoId = 6 }, vet);
        Assert.False(sinJustificar.Exito);
        Assert.Contains("RN-16", sinJustificar.Mensaje);

        var justificada = f.Vacunas.Registrar(c2.Id, new AplicacionFormulario { ProductoId = 6, Justificacion = "Reinfestación confirmada en coprológico" }, vet);
        Assert.True(justificada.Exito, justificada.Mensaje);
        Assert.Equal("Reinfestación confirmada en coprológico", f.Vacunas.DeConsulta(c2.Id).Single().JustificacionAdelanto);
    }

    [Fact]
    public void RN16_la_primera_dosis_no_pide_justificacion()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var turno = f.TurnoEnEspera();
        f.Consultas.Iniciar(turno.Id, vet);
        var consulta = f.Consultas.ObtenerPorTurno(turno.Id)!;

        Assert.True(f.Vacunas.Registrar(consulta.Id, new AplicacionFormulario { ProductoId = 7 }, vet).Exito);
        Assert.Equal("", f.Vacunas.DeConsulta(consulta.Id).Single().JustificacionAdelanto);
    }
}
