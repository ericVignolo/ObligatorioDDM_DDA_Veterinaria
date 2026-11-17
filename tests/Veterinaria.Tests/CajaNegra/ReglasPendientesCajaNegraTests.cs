using System.Globalization;
using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;
using Xunit;

namespace Veterinaria.Tests.CajaNegra;

// Pruebas de caja negra de las reglas pendientes de la sección A (RN-08, RN-09, RN-16, RN-17, RN-35, RN-36).
// Escritas solo con docs/analisis/ESPECIFICACION_REGLAS_PENDIENTES.md, los modelos y las firmas públicas (RP-01 a RP-20).
public class ReglasPendientesCajaNegraTests
{
    private const int Ferreira = 1;
    private const int ConsultaGeneral = 1;
    private const int Castracion = 8;
    private const int Toby = 1;
    private const int Canela = 4;

    // ---------- Ayudas ----------

    private static Personal Recepcion(Fabrica f) => f.PersonalDe("recepcion@vitalia.uy");
    private static Personal Veterinaria(Fabrica f) => f.PersonalDe("ferreira@vitalia.uy");
    private static Personal Admin(Fabrica f) => f.PersonalDe("admin@vitalia.uy");

    private static (Cuenta cliente, Animal animal) ClienteConAnimal(Fabrica f, string correo = "cliente@prueba.uy", string cedula = "1.234.567-2", string nombreAnimal = "Firulais")
    {
        var cliente = f.ClienteVerificado(correo, cedula);
        var animal = f.AnimalDe(cliente.Id, "Perro", nombreAnimal);
        return (cliente, animal);
    }

    private static DateTime Libre(Fabrica f, int prestacionId, int animalId, DateOnly dia, int indice = 0)
    {
        var libres = f.Agenda.HorariosLibres(Ferreira, prestacionId, animalId, dia);
        Assert.True(libres.Count > indice, "No hay suficientes horarios libres para preparar la prueba.");
        return libres[indice];
    }

    // Crea un turno «Ausente» del cliente con el inicio indicado (en el pasado).
    private static Turno Ausencia(Fabrica f, int clienteId, int animalId, DateTime inicioPasado)
    {
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animalId, dia);
        f.Agenda.OcuparComoOtroCliente(Ferreira, ConsultaGeneral, hora);
        var turno = f.Agenda.TurnosDelDia(dia, Ferreira).Last(t => t.Inicio == hora);
        turno.ClienteId = clienteId;
        turno.AnimalId = animalId;
        turno.Estado = "Ausente";
        turno.Inicio = inicioPasado;
        turno.Fin = inicioPasado.AddMinutes(30);
        return turno;
    }

    private static void Ausencias(Fabrica f, int clienteId, int animalId, int cantidad)
    {
        for (int i = 0; i < cantidad; i++)
        {
            Ausencia(f, clienteId, animalId, DateTime.Today.AddMonths(-1).AddDays(-i).AddHours(10));
        }
    }

    private static int TurnosDe(Fabrica f, DateOnly dia) => f.Agenda.TurnosDelDia(dia).Count;

    private static Turno Recargar(Fabrica f, Turno turno)
    {
        return f.Agenda.TurnosDelDia(DateOnly.FromDateTime(turno.Inicio)).Single(t => t.Id == turno.Id);
    }

    private static int Sobreturnos(Fabrica f)
    {
        return f.Auditoria.Buscar().Count(r => r.Entidad == "Agenda" && r.Accion == "Sobreturno");
    }

    private static bool Igual(string a, string b)
    {
        return string.Compare(a.Trim(), b.Trim(), CultureInfo.InvariantCulture,
            CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0;
    }

    private static bool Contiene(IEnumerable<string> lista, string valor) => lista.Any(x => Igual(x, valor));

    // Consulta en borrador de hoy para Toby con la Dra. Ferreira.
    private static Consulta ConsultaEnCurso(Fabrica f, Turno? turno = null)
    {
        turno ??= f.TurnoEnEspera();
        var iniciar = f.Consultas.Iniciar(turno.Id, Veterinaria(f));
        Assert.True(iniciar.Exito);
        return f.Consultas.ObtenerPorTurno(turno.Id)!;
    }

    private static ResultadoOperacion Firmar(Fabrica f, Consulta consulta, ConsultaFormulario datos)
    {
        return f.Consultas.Firmar(consulta.Id, consulta.Version, datos, PersonalService.ContrasenaDemo, Veterinaria(f));
    }

    // Cargo común (no seña) de una consulta firmada hoy para el cliente.
    private static Cargo CargoComun(Fabrica f, int clienteId, int animalId)
    {
        var turno = f.TurnoEnEspera();
        turno.ClienteId = clienteId;
        turno.AnimalId = animalId;
        var consulta = ConsultaEnCurso(f, turno);
        Assert.True(Firmar(f, consulta, Fabrica.ConsultaCompleta()).Exito);
        var cargo = f.Cobros.CargosDe(clienteId).Single(c => !c.EsSena);
        Assert.True(cargo.Monto > 0);
        return cargo;
    }

    // Castración reservada por el cliente (queda pendiente de seña).
    private static Turno CirugiaReservada(Fabrica f, Cuenta cliente, Animal animal)
    {
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, Castracion, animal.Id, dia);
        var r = f.Agenda.Reservar(cliente.Id, animal.Id, Castracion, Ferreira, hora, out var turno);
        Assert.True(r.Exito);
        Assert.NotNull(turno);
        return turno!;
    }

    private static ResultadoOperacion Pagar(Fabrica f, Cargo cargo, int monto)
    {
        return f.Cobros.Cobrar(new CobroFormulario { CargoId = cargo.Id, Monto = monto, Medio = CobroService.Efectivo, Referencia = "" }, Recepcion(f));
    }

    // ================= RN-08: ausencias sin aviso (RP-01 a RP-03) =================

    [Fact]
    public void RP01_cliente_sin_ausencias_cuenta_cero()
    {
        var f = new Fabrica();
        var (cliente, _) = ClienteConAnimal(f);
        Assert.Equal(0, f.Agenda.AusenciasRecientes(cliente.Id));
    }

    [Fact]
    public void RP01_ausencia_de_hace_11_meses_y_medio_cuenta()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        Ausencia(f, cliente.Id, animal.Id, DateTime.Today.AddMonths(-12).AddDays(2).AddHours(10));
        Assert.Equal(1, f.Agenda.AusenciasRecientes(cliente.Id));
    }

    [Fact]
    public void RP01_ausencia_de_mas_de_12_meses_no_cuenta()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        Ausencia(f, cliente.Id, animal.Id, DateTime.Today.AddMonths(-12).AddDays(-2).AddHours(10));
        Assert.Equal(0, f.Agenda.AusenciasRecientes(cliente.Id));
    }

    [Fact]
    public void RP01_turno_cancelado_o_finalizado_no_cuenta_como_ausencia()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        Ausencia(f, cliente.Id, animal.Id, DateTime.Today.AddMonths(-1).AddHours(10)).Estado = "Cancelado";
        Ausencia(f, cliente.Id, animal.Id, DateTime.Today.AddMonths(-2).AddHours(10)).Estado = "Finalizado";
        Assert.Equal(0, f.Agenda.AusenciasRecientes(cliente.Id));
    }

    [Fact]
    public void RP01_ausencias_de_otro_cliente_no_cuentan()
    {
        var f = new Fabrica();
        var (cliente, _) = ClienteConAnimal(f);
        var (otro, animalOtro) = ClienteConAnimal(f, "otro@prueba.uy", "2.345.678-7", "Rex");
        Ausencias(f, otro.Id, animalOtro.Id, 3);
        Assert.Equal(0, f.Agenda.AusenciasRecientes(cliente.Id));
        Assert.Equal(3, f.Agenda.AusenciasRecientes(otro.Id));
    }

    [Fact]
    public void RP02_con_una_ausencia_no_hay_aviso_y_reserva()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        Ausencias(f, cliente.Id, animal.Id, 1);
        Assert.Equal("", f.Agenda.MensajeAusencias(cliente.Id));
        Assert.False(f.Agenda.BloqueadoPorAusencias(cliente.Id));

        var dia = f.DiaQueAtiende(Ferreira);
        var r = f.Agenda.Reservar(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, Libre(f, ConsultaGeneral, animal.Id, dia), out var turno);
        Assert.True(r.Exito);
        Assert.NotNull(turno);
    }

    [Fact]
    public void RP02_con_dos_ausencias_hay_aviso_pero_reserva()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        Ausencias(f, cliente.Id, animal.Id, AgendaService.AvisoAusencias);
        Assert.NotEqual("", f.Agenda.MensajeAusencias(cliente.Id));
        Assert.False(f.Agenda.BloqueadoPorAusencias(cliente.Id));

        var dia = f.DiaQueAtiende(Ferreira);
        var r = f.Agenda.Reservar(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, Libre(f, ConsultaGeneral, animal.Id, dia), out var turno);
        Assert.True(r.Exito);
        Assert.NotNull(turno);
    }

    [Fact]
    public void RP02_con_tres_ausencias_el_cliente_queda_bloqueado_y_no_se_crea_turno()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        Assert.Equal(3, f.Agenda.LimiteAusencias);
        Ausencias(f, cliente.Id, animal.Id, 3);
        Assert.True(f.Agenda.BloqueadoPorAusencias(cliente.Id));
        Assert.NotEqual("", f.Agenda.MensajeAusencias(cliente.Id));

        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);
        var antes = TurnosDe(f, dia);
        var r = f.Agenda.Reservar(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, out _);

        Assert.False(r.Exito);
        Assert.Equal(antes, TurnosDe(f, dia));
        Assert.Contains(hora, f.Agenda.HorariosLibres(Ferreira, ConsultaGeneral, animal.Id, dia));
    }

    [Fact]
    public void RP02_limite_de_ausencias_mas_alto_no_bloquea_con_tres()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        f.Agenda.LimiteAusencias = 4;
        Ausencias(f, cliente.Id, animal.Id, 3);
        Assert.False(f.Agenda.BloqueadoPorAusencias(cliente.Id));

        var dia = f.DiaQueAtiende(Ferreira);
        var r = f.Agenda.Reservar(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, Libre(f, ConsultaGeneral, animal.Id, dia), out _);
        Assert.True(r.Exito);
    }

    [Fact]
    public void RP02_ausencias_viejas_no_bloquean()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        Ausencias(f, cliente.Id, animal.Id, 2);
        Ausencia(f, cliente.Id, animal.Id, DateTime.Today.AddMonths(-13).AddHours(10));
        Assert.Equal(2, f.Agenda.AusenciasRecientes(cliente.Id));
        Assert.False(f.Agenda.BloqueadoPorAusencias(cliente.Id));
    }

    [Fact]
    public void RP03_recepcion_da_turno_por_mostrador_a_cliente_bloqueado()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        Ausencias(f, cliente.Id, animal.Id, 3);
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, false, "", Recepcion(f), out var turno);

        Assert.True(r.Exito);
        Assert.NotNull(turno);
        Assert.Equal(cliente.Id, turno!.ClienteId);
        Assert.Equal(hora, turno.Inicio);
    }

    [Fact]
    public void RP03_administrador_da_turno_por_mostrador_a_cliente_bloqueado()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        Ausencias(f, cliente.Id, animal.Id, 3);
        var dia = f.DiaQueAtiende(Ferreira);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, Libre(f, ConsultaGeneral, animal.Id, dia), false, "", Admin(f), out var turno);

        Assert.True(r.Exito);
        Assert.NotNull(turno);
    }

    [Fact]
    public void RP03_las_ausencias_no_generan_recargos()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        Ausencias(f, cliente.Id, animal.Id, 3);
        Assert.Empty(f.Cobros.CargosDe(cliente.Id));
        Assert.Equal(0m, f.Cobros.DeudaVencida(cliente.Id));
    }

    // ================= RN-09: mostrador y sobreturno (RP-04 a RP-07) =================

    [Fact]
    public void RP04_veterinario_no_puede_dar_turno_por_mostrador()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);
        var antes = TurnosDe(f, dia);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, false, "", Veterinaria(f), out _);

        Assert.False(r.Exito);
        Assert.Equal(antes, TurnosDe(f, dia));
    }

    [Fact]
    public void RP04_sin_persona_no_se_da_turno_por_mostrador()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);
        var antes = TurnosDe(f, dia);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, false, "", null, out _);

        Assert.False(r.Exito);
        Assert.Equal(antes, TurnosDe(f, dia));
    }

    [Fact]
    public void RP04_veterinario_no_puede_dar_sobreturno()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);
        f.Agenda.OcuparComoOtroCliente(Ferreira, ConsultaGeneral, hora);
        var antes = TurnosDe(f, dia);
        var auditoriaAntes = Sobreturnos(f);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, true, "Dolor agudo", Veterinaria(f), out _);

        Assert.False(r.Exito);
        Assert.Equal(antes, TurnosDe(f, dia));
        Assert.Equal(auditoriaAntes, Sobreturnos(f));
    }

    [Fact]
    public void RP04_animal_de_otro_cliente_se_rechaza()
    {
        var f = new Fabrica();
        var (cliente, _) = ClienteConAnimal(f);
        var (_, animalAjeno) = ClienteConAnimal(f, "otro@prueba.uy", "2.345.678-7", "Rex");
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animalAjeno.Id, dia);
        var antes = TurnosDe(f, dia);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animalAjeno.Id, ConsultaGeneral, Ferreira, hora, false, "", Recepcion(f), out _);

        Assert.False(r.Exito);
        Assert.Equal(antes, TurnosDe(f, dia));
    }

    [Fact]
    public void RP05_sin_sobreturno_un_horario_ocupado_se_rechaza()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);
        f.Agenda.OcuparComoOtroCliente(Ferreira, ConsultaGeneral, hora);
        var antes = TurnosDe(f, dia);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, false, "Dolor agudo", Recepcion(f), out _);

        Assert.False(r.Exito);
        Assert.Equal(antes, TurnosDe(f, dia));
    }

    [Fact]
    public void RP05_sin_sobreturno_un_horario_libre_se_reserva_confirmado()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, false, "", Recepcion(f), out var turno);

        Assert.True(r.Exito);
        Assert.NotNull(turno);
        Assert.Equal("Confirmado", turno!.Estado);
        Assert.False(turno.EsSobreturno);
        Assert.DoesNotContain(hora, f.Agenda.HorariosLibres(Ferreira, ConsultaGeneral, animal.Id, dia));
    }

    [Fact]
    public void RP06_sobreturno_con_motivo_de_4_caracteres_se_rechaza()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);
        f.Agenda.OcuparComoOtroCliente(Ferreira, ConsultaGeneral, hora);
        var antes = TurnosDe(f, dia);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, true, "Dolo", Recepcion(f), out _);

        Assert.False(r.Exito);
        Assert.Equal(antes, TurnosDe(f, dia));
    }

    [Fact]
    public void RP06_sobreturno_con_motivo_vacio_o_solo_espacios_se_rechaza()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);
        f.Agenda.OcuparComoOtroCliente(Ferreira, ConsultaGeneral, hora);
        var antes = TurnosDe(f, dia);

        var vacio = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, true, "", Recepcion(f), out _);
        var espacios = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, true, "        ", Recepcion(f), out _);

        Assert.False(vacio.Exito);
        Assert.False(espacios.Exito);
        Assert.Equal(antes, TurnosDe(f, dia));
    }

    [Fact]
    public void RP06_sobreturno_con_motivo_de_5_caracteres_se_superpone_con_otro_cliente()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);
        f.Agenda.OcuparComoOtroCliente(Ferreira, ConsultaGeneral, hora);
        var antes = TurnosDe(f, dia);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, true, "Dolor", Recepcion(f), out var turno);

        Assert.True(r.Exito);
        Assert.NotNull(turno);
        Assert.Equal(antes + 1, TurnosDe(f, dia));
        Assert.Equal(hora, turno!.Inicio);
    }

    [Fact]
    public void RP06_sobreturno_en_horario_ya_pasado_se_rechaza()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var profesional = f.Profesionales.ObtenerPorId(Ferreira)!;
        var dia = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        while (!profesional.Jornada.Any(j => j.Dia == dia.DayOfWeek))
        {
            dia = dia.AddDays(-1);
        }
        var franja = profesional.Jornada.First(j => j.Dia == dia.DayOfWeek);
        var inicio = dia.ToDateTime(franja.Inicio);
        var antes = TurnosDe(f, dia);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, inicio, true, "Urgencia del día", Recepcion(f), out _);

        Assert.False(r.Exito);
        Assert.Equal(antes, TurnosDe(f, dia));
    }

    [Fact]
    public void RP06_sobreturno_fuera_de_la_jornada_se_rechaza()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var franjas = f.Profesionales.ObtenerPorId(Ferreira)!.Jornada.Where(j => j.Dia == dia.DayOfWeek).ToList();
        var hora = Enumerable.Range(0, 24)
            .Select(h => new TimeOnly(h, 0))
            .First(t => !franjas.Any(j => t >= j.Inicio && t < j.Fin));
        var inicio = dia.ToDateTime(hora);
        var antes = TurnosDe(f, dia);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, inicio, true, "Urgencia del día", Recepcion(f), out _);

        Assert.False(r.Exito);
        Assert.Equal(antes, TurnosDe(f, dia));
    }

    [Fact]
    public void RP06_sobreturno_en_dia_de_ausencia_del_profesional_se_rechaza()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var profesional = f.Profesionales.ObtenerPorId(Ferreira)!;
        var franja = profesional.Jornada.First(j => j.Dia == dia.DayOfWeek);
        profesional.Ausencias.Add(new Ausencia { Desde = dia, Hasta = dia, Motivo = "Capacitación" });
        var antes = TurnosDe(f, dia);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, dia.ToDateTime(franja.Inicio), true, "Urgencia del día", Recepcion(f), out _);

        Assert.False(r.Exito);
        Assert.Equal(antes, TurnosDe(f, dia));
    }

    [Fact]
    public void RP06_sobreturno_superpuesto_con_turno_activo_del_mismo_animal_se_rechaza()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);
        Assert.True(f.Agenda.Reservar(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, out _).Exito);
        var antes = TurnosDe(f, dia);

        var mismo = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, true, "Urgencia del día", Recepcion(f), out _);
        var solapado = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora.AddMinutes(15), true, "Urgencia del día", Recepcion(f), out _);

        Assert.False(mismo.Exito);
        Assert.False(solapado.Exito);
        Assert.Equal(antes, TurnosDe(f, dia));
    }

    [Fact]
    public void RP06_sobreturno_de_otro_animal_del_mismo_cliente_se_permite()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var segundo = f.AnimalDe(cliente.Id, "Perro", "Manchita");
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);
        Assert.True(f.Agenda.Reservar(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, out _).Exito);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, segundo.Id, ConsultaGeneral, Ferreira, hora, true, "Vienen juntos", Recepcion(f), out var turno);

        Assert.True(r.Exito);
        Assert.NotNull(turno);
    }

    [Fact]
    public void RP06_turno_cancelado_del_animal_no_impide_el_sobreturno()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);
        Assert.True(f.Agenda.Reservar(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, out var previo).Exito);
        Assert.True(f.Agenda.Cancelar(previo!.Id, cliente.Id, "").Exito);
        f.Agenda.OcuparComoOtroCliente(Ferreira, ConsultaGeneral, hora);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, true, "Urgencia del día", Recepcion(f), out _);

        Assert.True(r.Exito);
    }

    [Fact]
    public void RP07_sobreturno_queda_marcado_con_motivo_y_creador()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);
        f.Agenda.OcuparComoOtroCliente(Ferreira, ConsultaGeneral, hora);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, true, "Dolor abdominal agudo", Recepcion(f), out var turno);

        Assert.True(r.Exito);
        Assert.True(turno!.EsSobreturno);
        Assert.Equal("Dolor abdominal agudo", turno.MotivoSobreturno);
        Assert.False(string.IsNullOrWhiteSpace(turno.CreadoPor));
    }

    [Fact]
    public void RP07_sobreturno_se_audita_sin_el_motivo()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var hora = Libre(f, ConsultaGeneral, animal.Id, dia);
        f.Agenda.OcuparComoOtroCliente(Ferreira, ConsultaGeneral, hora);
        var antes = Sobreturnos(f);
        const string motivo = "Motivo reservado XQZ123";

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, hora, true, motivo, Admin(f), out _);

        Assert.True(r.Exito);
        Assert.Equal(antes + 1, Sobreturnos(f));
        var registro = f.Auditoria.Buscar().Last(a => a.Entidad == "Agenda" && a.Accion == "Sobreturno");
        Assert.DoesNotContain("XQZ123", registro.Detalle);
        Assert.Equal(BitacoraService.Agenda, registro.Entidad);
    }

    [Fact]
    public void RP07_mostrador_sin_sobreturno_guarda_creador_y_no_audita_sobreturno()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        var antes = Sobreturnos(f);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, Libre(f, ConsultaGeneral, animal.Id, dia), false, "", Recepcion(f), out var turno);

        Assert.True(r.Exito);
        Assert.False(turno!.EsSobreturno);
        Assert.False(string.IsNullOrWhiteSpace(turno.CreadoPor));
        Assert.Equal(antes, Sobreturnos(f));
    }

    [Fact]
    public void RP07_reserva_del_cliente_no_tiene_creador_ni_sobreturno()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);

        var r = f.Agenda.Reservar(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, Libre(f, ConsultaGeneral, animal.Id, dia), out var turno);

        Assert.True(r.Exito);
        Assert.Equal("", turno!.CreadoPor);
        Assert.False(turno.EsSobreturno);
        Assert.Equal("", turno.MotivoSobreturno);
    }

    // ================= RN-16: intervalo mínimo entre dosis (RP-08 a RP-10) =================

    [Fact]
    public void RP08_los_productos_tienen_los_intervalos_minimos_de_demostracion()
    {
        var f = new Fabrica();
        var intervalos = f.Vacunas.Productos.Select(p => p.IntervaloMinimoDias).ToList();
        Assert.All(intervalos, i => Assert.True(i > 0));
        Assert.Contains(330, intervalos);
        Assert.Contains(150, intervalos);
        Assert.Contains(75, intervalos);
        Assert.Contains(25, intervalos);
        Assert.All(f.Vacunas.Productos.Where(p => p.Tipo == "Antiparasitario"),
            p => Assert.True(p.IntervaloMinimoDias == 75 || p.IntervaloMinimoDias == 25));
    }

    [Fact]
    public void RP09_adelanto_sin_justificacion_se_rechaza_y_no_registra()
    {
        var f = new Fabrica();
        var consulta = ConsultaEnCurso(f);

        var r = f.Vacunas.Registrar(consulta.Id, new AplicacionFormulario { ProductoId = 1, Justificacion = "" }, Veterinaria(f));

        Assert.False(r.Exito);
        Assert.Empty(f.Vacunas.DeConsulta(consulta.Id));
    }

    [Fact]
    public void RP09_adelanto_con_justificacion_de_9_caracteres_se_rechaza()
    {
        var f = new Fabrica();
        var consulta = ConsultaEnCurso(f);

        var r = f.Vacunas.Registrar(consulta.Id, new AplicacionFormulario { ProductoId = 1, Justificacion = "123456789" }, Veterinaria(f));

        Assert.False(r.Exito);
        Assert.Empty(f.Vacunas.DeConsulta(consulta.Id));
    }

    [Fact]
    public void RP09_adelanto_con_justificacion_de_solo_espacios_se_rechaza()
    {
        var f = new Fabrica();
        var consulta = ConsultaEnCurso(f);

        var r = f.Vacunas.Registrar(consulta.Id, new AplicacionFormulario { ProductoId = 1, Justificacion = "            " }, Veterinaria(f));

        Assert.False(r.Exito);
        Assert.Empty(f.Vacunas.DeConsulta(consulta.Id));
    }

    [Fact]
    public void RP10_adelanto_con_justificacion_de_10_caracteres_se_registra_y_la_guarda()
    {
        var f = new Fabrica();
        var consulta = ConsultaEnCurso(f);

        var r = f.Vacunas.Registrar(consulta.Id, new AplicacionFormulario { ProductoId = 1, Justificacion = "Viaje 1234" }, Veterinaria(f));

        Assert.True(r.Exito);
        var aplicacion = Assert.Single(f.Vacunas.DeConsulta(consulta.Id));
        Assert.Equal(1, aplicacion.ProductoId);
        Assert.Equal("Viaje 1234", aplicacion.JustificacionAdelanto);
    }

    [Fact]
    public void RP10_sin_adelanto_la_justificacion_escrita_no_se_guarda()
    {
        var f = new Fabrica();
        var consulta = ConsultaEnCurso(f);

        // Producto 2: aplicado hace 400 días, más que cualquier intervalo mínimo.
        var r = f.Vacunas.Registrar(consulta.Id, new AplicacionFormulario { ProductoId = 2, Justificacion = "Texto que no corresponde" }, Veterinaria(f));

        Assert.True(r.Exito);
        var aplicacion = Assert.Single(f.Vacunas.DeConsulta(consulta.Id));
        Assert.Equal("", aplicacion.JustificacionAdelanto);
    }

    [Fact]
    public void RP10_sin_aplicacion_anterior_no_exige_justificacion()
    {
        var f = new Fabrica();
        var consulta = ConsultaEnCurso(f);

        var r = f.Vacunas.Registrar(consulta.Id, new AplicacionFormulario { ProductoId = 7, Justificacion = "" }, Veterinaria(f));

        Assert.True(r.Exito);
        var aplicacion = Assert.Single(f.Vacunas.DeConsulta(consulta.Id));
        Assert.Equal("", aplicacion.JustificacionAdelanto);
    }

    // ================= RN-17: alergias (RP-11 a RP-13) =================

    [Fact]
    public void RP11_alergias_de_consultas_firmadas_se_incluyen()
    {
        var f = new Fabrica();
        Assert.True(Contiene(f.Consultas.AlergiasDe(Canela), "Penicilina"));
    }

    [Fact]
    public void RP11_animal_sin_consultas_no_tiene_alergias()
    {
        var f = new Fabrica();
        var (_, animal) = ClienteConAnimal(f);
        Assert.Empty(f.Consultas.AlergiasDe(animal.Id));
        Assert.Empty(f.Consultas.AlergiasDe(animal.Id, ""));
    }

    [Fact]
    public void RP11_separa_por_coma_punto_y_coma_barra_salto_de_linea_e_y()
    {
        var f = new Fabrica();
        var (_, animal) = ClienteConAnimal(f);

        var alergias = f.Consultas.AlergiasDe(animal.Id, "Pollo, Soja; Maní / Lactosa\nGluten y Cerdo");

        Assert.True(Contiene(alergias, "Pollo"));
        Assert.True(Contiene(alergias, "Soja"));
        Assert.True(Contiene(alergias, "Maní"));
        Assert.True(Contiene(alergias, "Lactosa"));
        Assert.True(Contiene(alergias, "Gluten"));
        Assert.True(Contiene(alergias, "Cerdo"));
        Assert.Equal(6, alergias.Count);
    }

    [Fact]
    public void RP11_descarta_las_de_menos_de_3_letras_y_conserva_las_de_3()
    {
        var f = new Fabrica();
        var (_, animal) = ClienteConAnimal(f);

        var alergias = f.Consultas.AlergiasDe(animal.Id, "Ab, Ajo");

        Assert.Single(alergias);
        Assert.True(Contiene(alergias, "Ajo"));
    }

    [Theory]
    [InlineData("Ninguna")]
    [InlineData("ninguna")]
    [InlineData("No")]
    [InlineData("Sin alergias")]
    [InlineData("")]
    [InlineData("   ")]
    public void RP11_descarta_respuestas_que_niegan_alergias(string texto)
    {
        var f = new Fabrica();
        var (_, animal) = ClienteConAnimal(f);
        Assert.Empty(f.Consultas.AlergiasDe(animal.Id, texto));
    }

    [Fact]
    public void RP11_alergias_de_un_borrador_guardado_no_cuentan_para_otras_consultas()
    {
        var f = new Fabrica();
        var turno = f.TurnoEnEspera();
        var consulta = ConsultaEnCurso(f, turno);
        var datos = Fabrica.ConsultaCompleta();
        datos.Alergias = "Gluten";
        Assert.True(f.Consultas.Guardar(consulta.Id, consulta.Version, datos, Veterinaria(f)).Exito);

        Assert.False(Contiene(f.Consultas.AlergiasDe(Toby), "Gluten"));
    }

    [Fact]
    public void RP12_alergia_en_el_tratamiento_es_choque_sin_importar_mayusculas()
    {
        var f = new Fabrica();
        var datos = Fabrica.ConsultaCompleta();
        datos.Tratamiento = "Se indica PENICILINA G cada 12 horas";
        Assert.True(Contiene(f.Consultas.AlergiasEnTratamiento(Canela, datos), "Penicilina"));
    }

    [Fact]
    public void RP12_alergia_en_las_indicaciones_es_choque()
    {
        var f = new Fabrica();
        var datos = Fabrica.ConsultaCompleta();
        datos.Indicaciones = "Continuar con penicilina en casa.";
        Assert.True(Contiene(f.Consultas.AlergiasEnTratamiento(Canela, datos), "Penicilina"));
    }

    [Fact]
    public void RP12_alergia_con_tilde_distinta_es_choque()
    {
        var f = new Fabrica();
        var datos = Fabrica.ConsultaCompleta();
        datos.Tratamiento = "Penicilína benzatínica";
        Assert.True(Contiene(f.Consultas.AlergiasEnTratamiento(Canela, datos), "Penicilina"));
    }

    [Fact]
    public void RP12_alergia_en_curso_sin_tilde_choca_con_tratamiento_con_tilde()
    {
        var f = new Fabrica();
        var (_, animal) = ClienteConAnimal(f);
        var datos = Fabrica.ConsultaCompleta();
        datos.Alergias = "Mani";
        datos.Tratamiento = "Dieta sin maní";
        Assert.True(Contiene(f.Consultas.AlergiasEnTratamiento(animal.Id, datos), "Mani"));
    }

    [Fact]
    public void RP12_parte_de_otra_palabra_no_es_choque()
    {
        var f = new Fabrica();
        var datos = Fabrica.ConsultaCompleta();
        datos.Tratamiento = "Amoxicilina con inhibidor de penicilinasa";
        Assert.False(Contiene(f.Consultas.AlergiasEnTratamiento(Canela, datos), "Penicilina"));
    }

    [Fact]
    public void RP12_sin_tratamiento_ni_indicaciones_no_hay_choque()
    {
        var f = new Fabrica();
        var datos = Fabrica.ConsultaCompleta();
        Assert.Empty(f.Consultas.AlergiasEnTratamiento(Canela, datos));
    }

    [Fact]
    public void RP13_firmar_con_choque_sin_confirmar_falla_y_sigue_en_borrador()
    {
        var f = new Fabrica();
        var turno = f.TurnoEnEspera();
        var consulta = ConsultaEnCurso(f, turno);
        var datos = Fabrica.ConsultaCompleta();
        datos.Alergias = "Penicilina";
        datos.Tratamiento = "Penicilina benzatínica";
        datos.ConfirmaAlergia = false;

        var r = Firmar(f, consulta, datos);

        Assert.False(r.Exito);
        Assert.Equal("Borrador", f.Consultas.ObtenerPorTurno(turno.Id)!.Estado);
    }

    [Fact]
    public void RP13_firmar_con_choque_confirmado_firma_y_guarda_la_alergia()
    {
        var f = new Fabrica();
        var turno = f.TurnoEnEspera();
        var consulta = ConsultaEnCurso(f, turno);
        var datos = Fabrica.ConsultaCompleta();
        datos.Alergias = "Penicilina";
        datos.Tratamiento = "Penicilina benzatínica";
        datos.ConfirmaAlergia = true;

        var r = Firmar(f, consulta, datos);

        Assert.True(r.Exito);
        var firmada = f.Consultas.ObtenerPorTurno(turno.Id)!;
        Assert.Equal("Firmada", firmada.Estado);
        Assert.Contains("penicilina", firmada.AlergiaConfirmada.ToLowerInvariant());
    }

    [Fact]
    public void RP13_firmar_sin_choque_deja_vacia_la_alergia_confirmada()
    {
        var f = new Fabrica();
        var turno = f.TurnoEnEspera();
        var consulta = ConsultaEnCurso(f, turno);
        var datos = Fabrica.ConsultaCompleta();
        datos.Alergias = "Penicilina";
        datos.Tratamiento = "Amoxicilina";
        datos.ConfirmaAlergia = true;

        var r = Firmar(f, consulta, datos);

        Assert.True(r.Exito);
        Assert.Equal("", f.Consultas.ObtenerPorTurno(turno.Id)!.AlergiaConfirmada);
    }

    [Fact]
    public void RP13_guardar_borrador_con_choque_no_se_bloquea()
    {
        var f = new Fabrica();
        var turno = f.TurnoEnEspera();
        var consulta = ConsultaEnCurso(f, turno);
        var datos = Fabrica.ConsultaCompleta();
        datos.Alergias = "Penicilina";
        datos.Tratamiento = "Penicilina benzatínica";

        var r = f.Consultas.Guardar(consulta.Id, consulta.Version, datos, Veterinaria(f));

        Assert.True(r.Exito);
        Assert.Equal("Borrador", f.Consultas.ObtenerPorTurno(turno.Id)!.Estado);
    }

    // ================= RN-35: seña de cirugía (RP-14 a RP-17) =================

    [Fact]
    public void RP14_cirugia_reservada_por_el_cliente_queda_pendiente_de_sena_y_ocupa_el_horario()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);

        var turno = CirugiaReservada(f, cliente, animal);

        Assert.Equal(AgendaService.PendienteSena, turno.Estado);
        Assert.DoesNotContain(turno.Inicio, f.Agenda.HorariosLibres(Ferreira, Castracion, animal.Id, DateOnly.FromDateTime(turno.Inicio)));
    }

    [Fact]
    public void RP14_cirugia_por_mostrador_queda_pendiente_de_sena()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);

        var r = f.Agenda.ReservarPorMostrador(cliente.Id, animal.Id, Castracion, Ferreira, Libre(f, Castracion, animal.Id, dia), false, "", Recepcion(f), out var turno);

        Assert.True(r.Exito);
        Assert.Equal(AgendaService.PendienteSena, turno!.Estado);
    }

    [Fact]
    public void RP14_consulta_comun_queda_confirmada_y_no_pendiente_de_sena()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);

        var r = f.Agenda.Reservar(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, Libre(f, ConsultaGeneral, animal.Id, dia), out var turno);

        Assert.True(r.Exito);
        Assert.Equal("Confirmado", turno!.Estado);
    }

    [Fact]
    public void RP14_turno_pendiente_de_sena_se_puede_cancelar()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var turno = CirugiaReservada(f, cliente, animal);

        var r = f.Agenda.Cancelar(turno.Id, cliente.Id, "");

        Assert.True(r.Exito);
        Assert.Equal("Cancelado", Recargar(f, turno).Estado);
    }

    [Fact]
    public void RP14_turno_pendiente_de_sena_no_admite_registrar_la_llegada()
    {
        var f = new Fabrica();
        var inicio = DateTime.Today.AddHours(7);
        f.Agenda.OcuparComoOtroCliente(Ferreira, Castracion, inicio);
        var turno = f.Agenda.TurnosDelDia(DateOnly.FromDateTime(inicio), Ferreira).Last(t => t.Inicio == inicio);
        turno.Estado = AgendaService.PendienteSena;

        var r = f.Agenda.RegistrarLlegada(turno.Id);

        Assert.False(r.Exito);
        Assert.Equal(AgendaService.PendienteSena, Recargar(f, turno).Estado);
        Assert.Null(Recargar(f, turno).HoraLlegada);
    }

    [Fact]
    public void RP15_genera_sena_del_30_por_ciento_redondeada()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var turno = CirugiaReservada(f, cliente, animal);
        var precio = f.Prestaciones.PrecioVigente(Castracion, DateOnly.FromDateTime(turno.Inicio));

        var sena = f.Cobros.GenerarSena(turno);

        Assert.NotNull(sena);
        Assert.True(sena!.EsSena);
        Assert.Equal(turno.Id, sena.TurnoId);
        Assert.Equal(cliente.Id, sena.ClienteId);
        Assert.Equal(decimal.Truncate(sena.Monto), sena.Monto);
        Assert.True(Math.Abs(sena.Monto - precio * 30m / 100m) <= 0.5m);
        Assert.Equal(sena.Id, f.Cobros.SenaDe(turno.Id)!.Id);
    }

    [Fact]
    public void RP15_porcentaje_de_sena_configurado_se_respeta_y_redondea()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var turno = CirugiaReservada(f, cliente, animal);
        var precio = f.Prestaciones.PrecioVigente(Castracion, DateOnly.FromDateTime(turno.Inicio));
        f.Cobros.PorcentajeSena = 33;

        var sena = f.Cobros.GenerarSena(turno);

        Assert.NotNull(sena);
        Assert.Equal(decimal.Truncate(sena!.Monto), sena.Monto);
        Assert.True(Math.Abs(sena.Monto - precio * 33m / 100m) <= 0.5m);
    }

    [Fact]
    public void RP15_sena_del_100_por_ciento_es_el_precio_completo()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var turno = CirugiaReservada(f, cliente, animal);
        var precio = f.Prestaciones.PrecioVigente(Castracion, DateOnly.FromDateTime(turno.Inicio));
        f.Cobros.PorcentajeSena = 100;

        var sena = f.Cobros.GenerarSena(turno);

        Assert.NotNull(sena);
        Assert.True(Math.Abs(sena!.Monto - precio) <= 0.5m);
    }

    [Fact]
    public void RP15_la_sena_se_genera_una_sola_vez_por_turno()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var turno = CirugiaReservada(f, cliente, animal);

        var primera = f.Cobros.GenerarSena(turno);
        f.Cobros.GenerarSena(turno);

        Assert.NotNull(primera);
        Assert.Single(f.Cobros.CargosDe(cliente.Id), c => c.EsSena && c.TurnoId == turno.Id);
        Assert.Equal(primera!.Id, f.Cobros.SenaDe(turno.Id)!.Id);
    }

    [Fact]
    public void RP15_turno_confirmado_sin_sena_no_genera_sena()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var dia = f.DiaQueAtiende(Ferreira);
        f.Agenda.Reservar(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, Libre(f, ConsultaGeneral, animal.Id, dia), out var turno);

        var sena = f.Cobros.GenerarSena(turno);

        Assert.Null(sena);
        Assert.Null(f.Cobros.SenaDe(turno!.Id));
        Assert.DoesNotContain(f.Cobros.CargosDe(cliente.Id), c => c.EsSena);
    }

    [Fact]
    public void RP15_turno_nulo_no_genera_sena()
    {
        var f = new Fabrica();
        Assert.Null(f.Cobros.GenerarSena(null));
    }

    [Fact]
    public void RP15_turno_pendiente_cancelado_no_genera_sena()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var turno = CirugiaReservada(f, cliente, animal);
        Assert.True(f.Agenda.Cancelar(turno.Id, cliente.Id, "").Exito);

        Assert.Null(f.Cobros.GenerarSena(Recargar(f, turno)));
        Assert.Null(f.Cobros.SenaDe(turno.Id));
    }

    [Fact]
    public void RP16_pago_total_de_la_sena_confirma_el_turno()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var turno = CirugiaReservada(f, cliente, animal);
        var sena = f.Cobros.GenerarSena(turno)!;

        var r = Pagar(f, sena, (int)sena.Monto);

        Assert.True(r.Exito);
        Assert.Equal(0m, f.Cobros.Saldo(sena));
        Assert.Equal("Confirmado", Recargar(f, turno).Estado);
    }

    [Fact]
    public void RP16_pago_parcial_no_confirma_y_el_resto_si()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var turno = CirugiaReservada(f, cliente, animal);
        var sena = f.Cobros.GenerarSena(turno)!;
        Assert.True(sena.Monto > 1);

        Assert.True(Pagar(f, sena, (int)sena.Monto - 1).Exito);
        Assert.Equal(1m, f.Cobros.Saldo(sena));
        Assert.Equal(AgendaService.PendienteSena, Recargar(f, turno).Estado);

        Assert.True(Pagar(f, sena, 1).Exito);
        Assert.Equal("Confirmado", Recargar(f, turno).Estado);
    }

    [Fact]
    public void RP16_anular_un_pago_de_la_sena_vuelve_a_pendiente_de_sena()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var turno = CirugiaReservada(f, cliente, animal);
        var sena = f.Cobros.GenerarSena(turno)!;
        Assert.True(Pagar(f, sena, (int)sena.Monto).Exito);
        var pago = f.Cobros.PagosDe(sena.Id).Last();

        var r = f.Cobros.Anular(pago.Id, new AnulacionFormulario { Motivo = "Cobro duplicado" }, Admin(f));

        Assert.True(r.Exito);
        Assert.Equal(sena.Monto, f.Cobros.Saldo(sena));
        Assert.Equal(AgendaService.PendienteSena, Recargar(f, turno).Estado);
    }

    [Fact]
    public void RP16_recepcion_no_puede_anular_y_el_turno_sigue_confirmado()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var turno = CirugiaReservada(f, cliente, animal);
        var sena = f.Cobros.GenerarSena(turno)!;
        Assert.True(Pagar(f, sena, (int)sena.Monto).Exito);
        var pago = f.Cobros.PagosDe(sena.Id).Last();

        var r = f.Cobros.Anular(pago.Id, new AnulacionFormulario { Motivo = "Cobro duplicado" }, Recepcion(f));

        Assert.False(r.Exito);
        Assert.Equal(0m, f.Cobros.Saldo(sena));
        Assert.Equal("Confirmado", Recargar(f, turno).Estado);
    }

    [Fact]
    public void RP16_veterinario_no_puede_cobrar_la_sena_y_no_cambia_nada()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var turno = CirugiaReservada(f, cliente, animal);
        var sena = f.Cobros.GenerarSena(turno)!;

        var r = f.Cobros.Cobrar(new CobroFormulario { CargoId = sena.Id, Monto = (int)sena.Monto, Medio = CobroService.Efectivo }, Veterinaria(f));

        Assert.False(r.Exito);
        Assert.Empty(f.Cobros.PagosDe(sena.Id));
        Assert.Equal(sena.Monto, f.Cobros.Saldo(sena));
        Assert.Equal(AgendaService.PendienteSena, Recargar(f, turno).Estado);
    }

    // RP-17 (descuento de la seña en el cargo final) no se cubre: ver ambigüedades.

    // ================= RN-36: deuda vencida (RP-18 y RP-19) =================

    [Fact]
    public void RP18_cargo_de_mas_de_30_dias_es_deuda_vencida()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var cargo = CargoComun(f, cliente.Id, animal.Id);
        cargo.Fecha = DateTime.Now.AddDays(-(CobroService.DiasParaVencer + 1));

        Assert.Equal(f.Cobros.Saldo(cargo), f.Cobros.DeudaVencida(cliente.Id));
        Assert.True(f.Cobros.DeudaVencida(cliente.Id) > 0);
    }

    [Fact]
    public void RP18_cargo_de_29_dias_no_es_deuda_vencida()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var cargo = CargoComun(f, cliente.Id, animal.Id);
        cargo.Fecha = DateTime.Now.AddDays(-(CobroService.DiasParaVencer - 1));

        Assert.Equal(0m, f.Cobros.DeudaVencida(cliente.Id));
    }

    [Fact]
    public void RP18_pago_parcial_reduce_la_deuda_vencida_al_saldo()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var cargo = CargoComun(f, cliente.Id, animal.Id);
        Assert.True(cargo.Monto > 1);
        cargo.Fecha = DateTime.Now.AddDays(-45);
        Assert.True(Pagar(f, cargo, (int)cargo.Monto - 1).Exito);

        Assert.Equal(1m, f.Cobros.DeudaVencida(cliente.Id));
    }

    [Fact]
    public void RP18_cargo_pagado_no_es_deuda()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var cargo = CargoComun(f, cliente.Id, animal.Id);
        cargo.Fecha = DateTime.Now.AddDays(-45);
        Assert.True(Pagar(f, cargo, (int)cargo.Monto).Exito);

        Assert.Equal(0m, f.Cobros.DeudaVencida(cliente.Id));
    }

    [Fact]
    public void RP18_la_sena_vieja_no_cuenta_como_deuda()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var turno = CirugiaReservada(f, cliente, animal);
        var sena = f.Cobros.GenerarSena(turno)!;
        sena.Fecha = DateTime.Now.AddDays(-60);

        Assert.Equal(0m, f.Cobros.DeudaVencida(cliente.Id));
    }

    [Fact]
    public void RP18_deuda_de_otro_cliente_no_cuenta()
    {
        var f = new Fabrica();
        var (cliente, _) = ClienteConAnimal(f);
        var (otro, animalOtro) = ClienteConAnimal(f, "otro@prueba.uy", "2.345.678-7", "Rex");
        var cargo = CargoComun(f, otro.Id, animalOtro.Id);
        cargo.Fecha = DateTime.Now.AddDays(-45);

        Assert.Equal(0m, f.Cobros.DeudaVencida(cliente.Id));
    }

    [Fact]
    public void RP19_sin_deuda_no_hay_aviso()
    {
        var f = new Fabrica();
        var (cliente, _) = ClienteConAnimal(f);
        Assert.Equal(3000, f.Cobros.LimiteDeuda);
        Assert.Equal("", f.Cobros.AvisoDeuda(cliente.Id));
    }

    [Fact]
    public void RP19_deuda_que_supera_el_limite_da_aviso()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var cargo = CargoComun(f, cliente.Id, animal.Id);
        cargo.Fecha = DateTime.Now.AddDays(-45);
        f.Cobros.LimiteDeuda = 0;

        Assert.NotEqual("", f.Cobros.AvisoDeuda(cliente.Id));
    }

    [Fact]
    public void RP19_deuda_igual_al_limite_no_da_aviso_y_uno_menos_si()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var cargo = CargoComun(f, cliente.Id, animal.Id);
        cargo.Fecha = DateTime.Now.AddDays(-45);
        var deuda = (int)f.Cobros.DeudaVencida(cliente.Id);

        f.Cobros.LimiteDeuda = deuda;
        Assert.Equal("", f.Cobros.AvisoDeuda(cliente.Id));

        f.Cobros.LimiteDeuda = deuda - 1;
        Assert.NotEqual("", f.Cobros.AvisoDeuda(cliente.Id));
    }

    [Fact]
    public void RP19_deuda_no_vencida_no_da_aviso_aunque_el_limite_sea_cero()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        CargoComun(f, cliente.Id, animal.Id);
        f.Cobros.LimiteDeuda = 0;

        Assert.Equal("", f.Cobros.AvisoDeuda(cliente.Id));
    }

    [Fact]
    public void RP19_la_deuda_no_bloquea_la_reserva()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        var cargo = CargoComun(f, cliente.Id, animal.Id);
        cargo.Fecha = DateTime.Now.AddDays(-45);
        f.Cobros.LimiteDeuda = 0;
        Assert.NotEqual("", f.Cobros.AvisoDeuda(cliente.Id));
        var dia = f.DiaQueAtiende(Ferreira);

        var r = f.Agenda.Reservar(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, Libre(f, ConsultaGeneral, animal.Id, dia), out var turno);

        Assert.True(r.Exito);
        Assert.NotNull(turno);
    }

    // ================= Parámetros (RP-20) =================

    [Fact]
    public void RP20_administrador_cambia_los_parametros()
    {
        var f = new Fabrica();

        var r = f.Administracion.CambiarParametrosAgenda(5, 40, 5000, Admin(f));

        Assert.True(r.Exito);
        Assert.Equal(5, f.Agenda.LimiteAusencias);
        Assert.Equal(40, f.Cobros.PorcentajeSena);
        Assert.Equal(5000, f.Cobros.LimiteDeuda);
    }

    [Theory]
    [InlineData(2, 10, 0)]
    [InlineData(10, 100, 1000000)]
    public void RP20_limites_exactos_se_aceptan(int ausencias, int sena, int deuda)
    {
        var f = new Fabrica();

        var r = f.Administracion.CambiarParametrosAgenda(ausencias, sena, deuda, Admin(f));

        Assert.True(r.Exito);
        Assert.Equal(ausencias, f.Agenda.LimiteAusencias);
        Assert.Equal(sena, f.Cobros.PorcentajeSena);
        Assert.Equal(deuda, f.Cobros.LimiteDeuda);
    }

    [Theory]
    [InlineData(1, 30, 3000)]
    [InlineData(11, 30, 3000)]
    [InlineData(0, 30, 3000)]
    [InlineData(5, 9, 3000)]
    [InlineData(5, 101, 3000)]
    [InlineData(5, 0, 3000)]
    [InlineData(5, 30, -1)]
    [InlineData(5, 30, 1000001)]
    [InlineData(1, 101, -1)]
    public void RP20_valores_fuera_de_rango_no_cambian_nada(int ausencias, int sena, int deuda)
    {
        var f = new Fabrica();

        var r = f.Administracion.CambiarParametrosAgenda(ausencias, sena, deuda, Admin(f));

        Assert.False(r.Exito);
        Assert.Equal(3, f.Agenda.LimiteAusencias);
        Assert.Equal(30, f.Cobros.PorcentajeSena);
        Assert.Equal(3000, f.Cobros.LimiteDeuda);
    }

    [Theory]
    [InlineData("recepcion@vitalia.uy")]
    [InlineData("ferreira@vitalia.uy")]
    public void RP20_solo_el_administrador_cambia_los_parametros(string correo)
    {
        var f = new Fabrica();

        var r = f.Administracion.CambiarParametrosAgenda(5, 40, 5000, f.PersonalDe(correo));

        Assert.False(r.Exito);
        Assert.Equal(3, f.Agenda.LimiteAusencias);
        Assert.Equal(30, f.Cobros.PorcentajeSena);
        Assert.Equal(3000, f.Cobros.LimiteDeuda);
    }

    [Fact]
    public void RP20_sin_persona_no_cambia_los_parametros()
    {
        var f = new Fabrica();

        var r = f.Administracion.CambiarParametrosAgenda(5, 40, 5000, null);

        Assert.False(r.Exito);
        Assert.Equal(3, f.Agenda.LimiteAusencias);
        Assert.Equal(30, f.Cobros.PorcentajeSena);
        Assert.Equal(3000, f.Cobros.LimiteDeuda);
    }

    [Fact]
    public void RP20_el_nuevo_limite_de_ausencias_se_aplica_a_la_reserva()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        Ausencias(f, cliente.Id, animal.Id, 2);
        Assert.True(f.Administracion.CambiarParametrosAgenda(2, 30, 3000, Admin(f)).Exito);

        Assert.True(f.Agenda.BloqueadoPorAusencias(cliente.Id));
        var dia = f.DiaQueAtiende(Ferreira);
        var r = f.Agenda.Reservar(cliente.Id, animal.Id, ConsultaGeneral, Ferreira, Libre(f, ConsultaGeneral, animal.Id, dia), out _);
        Assert.False(r.Exito);
    }

    [Fact]
    public void RP20_el_nuevo_porcentaje_se_aplica_a_la_sena()
    {
        var f = new Fabrica();
        var (cliente, animal) = ClienteConAnimal(f);
        Assert.True(f.Administracion.CambiarParametrosAgenda(3, 50, 3000, Admin(f)).Exito);
        var turno = CirugiaReservada(f, cliente, animal);
        var precio = f.Prestaciones.PrecioVigente(Castracion, DateOnly.FromDateTime(turno.Inicio));

        var sena = f.Cobros.GenerarSena(turno);

        Assert.NotNull(sena);
        Assert.True(Math.Abs(sena!.Monto - precio / 2m) <= 0.5m);
    }
}
