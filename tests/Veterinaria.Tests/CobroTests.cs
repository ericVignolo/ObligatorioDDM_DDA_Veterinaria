using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// A-023: RF-REC-05, RN-32, RN-33, RN-34. El cobro es presencial: no hay pago en línea (DT-ALC-02).
// La consulta clínica general (prestación 1) cuesta $ 1.200 en los datos de demostración.
public class CobroTests
{
    private const string Ferreira = "ferreira@vitalia.uy";
    private const string Recepcion = "recepcion@vitalia.uy";
    private const string Admin = "admin@vitalia.uy";

    // Una consulta firmada, con su cargo ya generado.
    private static (Fabrica f, Cargo cargo, Consulta consulta) CargoGenerado()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var turno = f.TurnoEnEspera();
        f.Consultas.Iniciar(turno.Id, vet);
        var consulta = f.Consultas.ObtenerPorTurno(turno.Id)!;
        var firma = f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet);
        Assert.True(firma.Exito, firma.Mensaje);
        return (f, f.Cobros.CargoDeConsulta(consulta.Id)!, consulta);
    }

    private static CobroFormulario Cobro(Cargo cargo, int monto, string medio = CobroService.Efectivo, string referencia = "")
    {
        return new CobroFormulario { CargoId = cargo.Id, Monto = monto, Medio = medio, Referencia = referencia };
    }

    [Fact]
    public void Firmar_la_consulta_genera_el_cargo_con_el_precio_de_la_prestacion()
    {
        var (f, cargo, consulta) = CargoGenerado();

        Assert.Equal(1200m, cargo.Monto);
        Assert.Equal("Consulta clínica general", cargo.Descripcion);
        Assert.Equal(consulta.Id, cargo.ConsultaId);
        Assert.Equal("Pendiente", f.Cobros.Estado(cargo));
        Assert.Equal(1200m, f.Cobros.Saldo(cargo));
    }

    [Fact]
    public void Una_consulta_sin_firmar_no_genera_cargo()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var turno = f.TurnoEnEspera();
        f.Consultas.Iniciar(turno.Id, vet);
        var borrador = f.Consultas.ObtenerPorTurno(turno.Id)!;

        var cargo = f.Cobros.GenerarCargo(borrador);

        Assert.Null(cargo);
        Assert.Empty(f.Cobros.Cargos());
    }

    [Fact]
    public void Cancelar_sin_atencion_no_genera_cargo()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var turno = f.TurnoEnEspera();
        f.Consultas.Iniciar(turno.Id, vet);
        var consulta = f.Consultas.ObtenerPorTurno(turno.Id)!;

        f.Consultas.CancelarSinAtencion(consulta.Id, "El paciente se retiró", vet);

        Assert.Empty(f.Cobros.Cargos());
    }

    [Fact]
    public void Generar_el_cargo_dos_veces_no_lo_duplica()
    {
        var (f, cargo, consulta) = CargoGenerado();

        var otra = f.Cobros.GenerarCargo(consulta);

        Assert.Same(cargo, otra);
        Assert.Single(f.Cobros.Cargos());
    }

    [Fact]
    public void Recepcion_cobra_todo_el_cargo_y_queda_pagado_con_su_comprobante()
    {
        var (f, cargo, _) = CargoGenerado();

        var resultado = f.Cobros.Cobrar(Cobro(cargo, 1200, CobroService.Debito), f.PersonalDe(Recepcion));

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.Equal("Pagado", f.Cobros.Estado(cargo));
        Assert.Equal(0m, f.Cobros.Saldo(cargo));
        var pago = Assert.Single(f.Cobros.Pagos());
        Assert.Equal("C-000001", pago.Numero);
        Assert.Equal(CobroService.Debito, pago.Medio);
        Assert.Equal("Laura Benítez", pago.PersonalNombre);
    }

    [Fact]
    public void Un_cobro_parcial_deja_saldo_y_se_completa_despues()
    {
        var (f, cargo, _) = CargoGenerado();
        var recepcion = f.PersonalDe(Recepcion);

        f.Cobros.Cobrar(Cobro(cargo, 500), recepcion);

        Assert.Equal("Pago parcial", f.Cobros.Estado(cargo));
        Assert.Equal(700m, f.Cobros.Saldo(cargo));

        var resto = f.Cobros.Cobrar(Cobro(cargo, 700), recepcion);

        Assert.True(resto.Exito);
        Assert.Equal("Pagado", f.Cobros.Estado(cargo));
        Assert.Equal(new[] { "C-000002", "C-000001" }, f.Cobros.Pagos().Select(p => p.Numero));
    }

    [Fact]
    public void No_se_cobra_mas_que_el_saldo()
    {
        var (f, cargo, _) = CargoGenerado();

        var resultado = f.Cobros.Cobrar(Cobro(cargo, 1201), f.PersonalDe(Recepcion));

        Assert.False(resultado.Exito);
        Assert.Empty(f.Cobros.Pagos());
        Assert.Equal(1200m, f.Cobros.Saldo(cargo));
    }

    [Fact]
    public void Un_cargo_pagado_no_se_cobra_de_nuevo()
    {
        var (f, cargo, _) = CargoGenerado();
        var recepcion = f.PersonalDe(Recepcion);
        f.Cobros.Cobrar(Cobro(cargo, 1200), recepcion);

        var segundo = f.Cobros.Cobrar(Cobro(cargo, 100), recepcion);

        Assert.False(segundo.Exito);
        Assert.Single(f.Cobros.Pagos());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-50)]
    public void El_monto_tiene_que_ser_positivo(int monto)
    {
        var (f, cargo, _) = CargoGenerado();

        var resultado = f.Cobros.Cobrar(Cobro(cargo, monto), f.PersonalDe(Recepcion));

        Assert.False(resultado.Exito);
        Assert.Empty(f.Cobros.Pagos());
    }

    [Fact]
    public void Solo_Recepcion_cobra()
    {
        var (f, cargo, _) = CargoGenerado();

        Assert.False(f.Cobros.Cobrar(Cobro(cargo, 1200), f.PersonalDe(Ferreira)).Exito);
        Assert.False(f.Cobros.Cobrar(Cobro(cargo, 1200), f.PersonalDe(Admin)).Exito);
        Assert.False(f.Cobros.Cobrar(Cobro(cargo, 1200), null).Exito);
        Assert.Empty(f.Cobros.Pagos());
    }

    [Fact]
    public void El_medio_de_pago_tiene_que_ser_uno_de_la_lista()
    {
        var (f, cargo, _) = CargoGenerado();
        var recepcion = f.PersonalDe(Recepcion);

        Assert.False(f.Cobros.Cobrar(Cobro(cargo, 1200, "Bitcoin"), recepcion).Exito);
        Assert.False(f.Cobros.Cobrar(Cobro(cargo, 1200, ""), recepcion).Exito);
        Assert.Empty(f.Cobros.Pagos());
    }

    [Fact]
    public void La_transferencia_exige_el_numero_de_operacion()
    {
        var (f, cargo, _) = CargoGenerado();
        var recepcion = f.PersonalDe(Recepcion);

        Assert.False(f.Cobros.Cobrar(Cobro(cargo, 1200, CobroService.Transferencia), recepcion).Exito);
        Assert.True(f.Cobros.Cobrar(Cobro(cargo, 1200, CobroService.Transferencia, "OP-98765"), recepcion).Exito);
        Assert.Equal("OP-98765", f.Cobros.Pagos().Single().Referencia);
    }

    [Fact]
    public void Solo_el_Administrador_anula_un_comprobante_y_con_motivo()
    {
        var (f, cargo, _) = CargoGenerado();
        f.Cobros.Cobrar(Cobro(cargo, 1200), f.PersonalDe(Recepcion));
        var pago = f.Cobros.Pagos().Single();

        Assert.False(f.Cobros.Anular(pago.Id, new AnulacionFormulario { Motivo = "Error" }, f.PersonalDe(Recepcion)).Exito);
        Assert.False(f.Cobros.Anular(pago.Id, new AnulacionFormulario { Motivo = "Error" }, f.PersonalDe(Ferreira)).Exito);
        Assert.False(f.Cobros.Anular(pago.Id, new AnulacionFormulario { Motivo = "  " }, f.PersonalDe(Admin)).Exito);
        Assert.Empty(f.Cobros.Notas());
        Assert.False(f.Cobros.EstaAnulado(pago.Id));
    }

    [Fact]
    public void Anular_emite_una_nota_de_credito_sin_modificar_el_comprobante()
    {
        var (f, cargo, _) = CargoGenerado();
        f.Cobros.Cobrar(Cobro(cargo, 1200, CobroService.Credito, "V-1"), f.PersonalDe(Recepcion));
        var pago = f.Cobros.Pagos().Single();

        var resultado = f.Cobros.Anular(pago.Id, new AnulacionFormulario { Motivo = "Se cobró de más" }, f.PersonalDe(Admin));

        Assert.True(resultado.Exito, resultado.Mensaje);
        var nota = Assert.Single(f.Cobros.Notas());
        Assert.Equal("NC-000001", nota.Numero);
        Assert.Equal(pago.Id, nota.PagoId);
        Assert.Equal(1200m, nota.Monto);
        Assert.Equal("Se cobró de más", nota.Motivo);
        Assert.Equal("Gonzalo Rivas", nota.PersonalNombre);

        // RN-34: el comprobante sigue igual; solo cambia que tiene una nota de crédito.
        Assert.True(f.Cobros.EstaAnulado(pago.Id));
        Assert.Equal("C-000001", pago.Numero);
        Assert.Equal(1200m, pago.Monto);
        Assert.Equal(CobroService.Credito, pago.Medio);
        Assert.Single(f.Cobros.Pagos());
    }

    [Fact]
    public void Un_comprobante_anulado_devuelve_el_saldo_y_se_puede_volver_a_cobrar()
    {
        var (f, cargo, _) = CargoGenerado();
        var recepcion = f.PersonalDe(Recepcion);
        f.Cobros.Cobrar(Cobro(cargo, 1200), recepcion);
        f.Cobros.Anular(f.Cobros.Pagos().Single().Id, new AnulacionFormulario { Motivo = "Error de monto" }, f.PersonalDe(Admin));

        Assert.Equal(1200m, f.Cobros.Saldo(cargo));
        Assert.Equal("Pendiente", f.Cobros.Estado(cargo));

        var otra = f.Cobros.Cobrar(Cobro(cargo, 1200, CobroService.Efectivo), recepcion);

        Assert.True(otra.Exito);
        Assert.Equal("Pagado", f.Cobros.Estado(cargo));
        Assert.Equal("C-000002", f.Cobros.Pagos().First().Numero);
    }

    [Fact]
    public void Un_comprobante_no_se_anula_dos_veces()
    {
        var (f, cargo, _) = CargoGenerado();
        f.Cobros.Cobrar(Cobro(cargo, 1200), f.PersonalDe(Recepcion));
        var pago = f.Cobros.Pagos().Single();
        var admin = f.PersonalDe(Admin);
        f.Cobros.Anular(pago.Id, new AnulacionFormulario { Motivo = "Error" }, admin);

        var segunda = f.Cobros.Anular(pago.Id, new AnulacionFormulario { Motivo = "Otra vez" }, admin);

        Assert.False(segunda.Exito);
        Assert.Single(f.Cobros.Notas());
    }

    [Fact]
    public void Los_totales_no_cuentan_los_comprobantes_anulados()
    {
        var (f, cargo, _) = CargoGenerado();
        var recepcion = f.PersonalDe(Recepcion);
        Assert.Equal(1200m, f.Cobros.TotalPendiente());

        f.Cobros.Cobrar(Cobro(cargo, 500), recepcion);
        Assert.Equal(700m, f.Cobros.TotalPendiente());
        Assert.Equal(500m, f.Cobros.CobradoHoy());

        f.Cobros.Anular(f.Cobros.Pagos().Single().Id, new AnulacionFormulario { Motivo = "Error" }, f.PersonalDe(Admin));
        Assert.Equal(1200m, f.Cobros.TotalPendiente());
        Assert.Equal(0m, f.Cobros.CobradoHoy());
    }

    [Fact]
    public void Cada_cliente_ve_solo_sus_cargos()
    {
        var (f, cargo, _) = CargoGenerado();

        Assert.Contains(f.Cobros.CargosDe(cargo.ClienteId), c => c.Id == cargo.Id);
        Assert.Empty(f.Cobros.CargosDe(cargo.ClienteId + 99));
    }
}
