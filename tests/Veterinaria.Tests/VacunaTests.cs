using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// A-021: RF-VET-04, RF-CLI-07, RN-15 (lote vencido) y RN-19 (refuerzo calculado).
// Ids del catálogo de demostración: 1 polivalente canina, 2 antirrábica, 3 triple felina,
// 6 antiparasitario interno (90 días) y 7 antiparasitario externo (30 días).
public class VacunaTests
{
    private const string Ferreira = "ferreira@vitalia.uy";
    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);

    // Toby (perro, animal 1) con la consulta de hoy abierta.
    private static (Fabrica f, Personal vet, Consulta consulta) ConsultaAbierta()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var turno = f.TurnoEnEspera();
        f.Consultas.Iniciar(turno.Id, vet);
        return (f, vet, f.Consultas.ObtenerPorTurno(turno.Id)!);
    }

    private static AplicacionFormulario Con(int productoId)
    {
        return new AplicacionFormulario { ProductoId = productoId };
    }

    private static void Firmar(Fabrica f, Consulta consulta, Personal vet)
    {
        var resultado = f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet);
        Assert.True(resultado.Exito, resultado.Mensaje);
    }

    [Fact]
    public void Registrar_calcula_el_refuerzo_con_el_intervalo_del_producto()
    {
        var (f, vet, consulta) = ConsultaAbierta();

        var resultado = f.Vacunas.Registrar(consulta.Id, Con(6), vet);

        Assert.True(resultado.Exito, resultado.Mensaje);
        var aplicacion = Assert.Single(f.Vacunas.DeConsulta(consulta.Id));
        Assert.Equal(Hoy, aplicacion.FechaAplicacion);
        Assert.Equal(Hoy.AddDays(90), aplicacion.FechaRefuerzo);
        Assert.Equal("Antiparasitario interno", aplicacion.Producto);
        Assert.False(string.IsNullOrEmpty(aplicacion.Laboratorio));
        Assert.Equal(1, aplicacion.AnimalId);
    }

    [Fact]
    public void Se_usa_el_lote_que_vence_antes_y_nunca_uno_vencido()
    {
        var (f, vet, consulta) = ConsultaAbierta();

        f.Vacunas.Registrar(consulta.Id, Con(1), vet);

        // L1-VENC venció (RN-15) y L1-A vence antes que L1-B (FEFO, RN-27).
        Assert.Equal("L1-A", f.Vacunas.DeConsulta(consulta.Id).Single().Lote);
    }

    [Fact]
    public void Sin_stock_vigente_no_se_aplica()
    {
        var (f, vet, consulta) = ConsultaAbierta();
        var admin = f.PersonalDe("admin@vitalia.uy");
        foreach (var lote in f.Inventario.LotesDe(1).Where(l => !l.Inhabilitado))
        {
            f.Inventario.DarDeBaja(new AjusteFormulario { LoteId = lote.Id, Cantidad = lote.Cantidad, Motivo = "Rotura" }, admin);
        }

        var resultado = f.Vacunas.Registrar(consulta.Id, Con(1), vet);

        Assert.False(resultado.Exito);
        Assert.Contains("RN-15", resultado.Mensaje);
        Assert.Empty(f.Vacunas.DeConsulta(consulta.Id));
    }

    [Fact]
    public void El_ultimo_dia_de_vigencia_del_lote_todavia_sirve()
    {
        Assert.False(InventarioService.Vencido(new LoteProducto { Vencimiento = Hoy }));
        Assert.True(InventarioService.Vencido(new LoteProducto { Vencimiento = Hoy.AddDays(-1) }));
    }

    [Fact]
    public void Un_producto_de_otra_especie_no_aplica()
    {
        var (f, vet, consulta) = ConsultaAbierta();

        var resultado = f.Vacunas.Registrar(consulta.Id, Con(3), vet);

        Assert.False(resultado.Exito);
        Assert.Contains("No aplicable", resultado.Mensaje);
        Assert.Empty(f.Vacunas.DeConsulta(consulta.Id));
    }

    [Fact]
    public void Sin_producto_no_se_registra()
    {
        var (f, vet, consulta) = ConsultaAbierta();

        Assert.False(f.Vacunas.Registrar(consulta.Id, new AplicacionFormulario { ProductoId = 0 }, vet).Exito);
        Assert.Empty(f.Vacunas.DeConsulta(consulta.Id));
    }

    [Fact]
    public void Firmar_descuenta_la_dosis_del_lote_y_quitar_antes_la_libera()
    {
        var (f, vet, consulta) = ConsultaAbierta();
        var antes = f.Inventario.Stock(6);

        f.Vacunas.Registrar(consulta.Id, Con(6), vet);
        Assert.Equal(antes, f.Inventario.Stock(6));
        Assert.Equal(antes - 1, f.Inventario.StockDisponible(6));

        Firmar(f, consulta, vet);

        Assert.Equal(antes - 1, f.Inventario.Stock(6));
        Assert.Contains(f.Inventario.Movimientos(6), m => m.Tipo == InventarioService.Consumo && m.Origen == $"Consulta {consulta.Id}");
    }

    [Fact]
    public void El_mismo_producto_no_se_registra_dos_veces_en_la_consulta()
    {
        var (f, vet, consulta) = ConsultaAbierta();
        f.Vacunas.Registrar(consulta.Id, Con(6), vet);

        var segundo = f.Vacunas.Registrar(consulta.Id, Con(6), vet);

        Assert.False(segundo.Exito);
        Assert.Single(f.Vacunas.DeConsulta(consulta.Id));
    }

    [Fact]
    public void Otro_veterinario_no_registra_en_una_consulta_ajena()
    {
        var (f, _, consulta) = ConsultaAbierta();

        var resultado = f.Vacunas.Registrar(consulta.Id, Con(6), f.PersonalDe("silveira@vitalia.uy"));

        Assert.False(resultado.Exito);
        Assert.Empty(f.Vacunas.DeConsulta(consulta.Id));
    }

    [Fact]
    public void Un_borrador_no_cuenta_para_el_carnet_hasta_firmar()
    {
        var (f, vet, consulta) = ConsultaAbierta();
        f.Vacunas.Registrar(consulta.Id, Con(7), vet);

        Assert.DoesNotContain(f.Vacunas.CarnetDe(1), i => i.Aplicacion.ProductoId == 7);

        Firmar(f, consulta, vet);

        var item = Assert.Single(f.Vacunas.CarnetDe(1), i => i.Aplicacion.ProductoId == 7);
        Assert.Equal(VacunaService.Vigente, item.Estado);
    }

    [Fact]
    public void Una_consulta_firmada_ya_no_acepta_ni_quita_aplicaciones()
    {
        var (f, vet, consulta) = ConsultaAbierta();
        f.Vacunas.Registrar(consulta.Id, Con(6), vet);
        var aplicacion = f.Vacunas.DeConsulta(consulta.Id).Single();
        Firmar(f, consulta, vet);

        Assert.False(f.Vacunas.Registrar(consulta.Id, Con(7), vet).Exito);
        Assert.False(f.Vacunas.Quitar(aplicacion.Id, vet).Exito);
        Assert.Single(f.Vacunas.DeConsulta(consulta.Id));
    }

    [Fact]
    public void En_el_borrador_se_puede_quitar_una_aplicacion()
    {
        var (f, vet, consulta) = ConsultaAbierta();
        f.Vacunas.Registrar(consulta.Id, Con(6), vet);
        var aplicacion = f.Vacunas.DeConsulta(consulta.Id).Single();

        var resultado = f.Vacunas.Quitar(aplicacion.Id, vet);

        Assert.True(resultado.Exito);
        Assert.Empty(f.Vacunas.DeConsulta(consulta.Id));
    }

    [Fact]
    public void Con_vacunas_registradas_no_se_cancela_sin_atencion()
    {
        var (f, vet, consulta) = ConsultaAbierta();
        f.Vacunas.Registrar(consulta.Id, Con(7), vet);

        var cancelar = f.Consultas.CancelarSinAtencion(consulta.Id, "El paciente se fue", vet);

        Assert.False(cancelar.Exito);
        Assert.DoesNotContain(f.Vacunas.CarnetDe(1), i => i.Aplicacion.ProductoId == 7);
    }

    [Fact]
    public void Un_turno_cancelado_sin_atencion_ya_no_acepta_vacunas()
    {
        var (f, vet, consulta) = ConsultaAbierta();
        f.Consultas.CancelarSinAtencion(consulta.Id, "El paciente se fue", vet);

        Assert.False(f.Vacunas.Registrar(consulta.Id, Con(6), vet).Exito);
    }

    [Fact]
    public void El_carnet_de_demostracion_muestra_vigente_refuerzo_proximo_y_vencida()
    {
        var f = new Fabrica();

        var carnet = f.Vacunas.CarnetDe(1);

        Assert.Equal(3, carnet.Count);
        Assert.Contains(carnet, i => i.Estado == VacunaService.Vigente);
        Assert.Contains(carnet, i => i.Estado == VacunaService.RefuerzoProximo);
        Assert.Contains(carnet, i => i.Estado == VacunaService.Vencida);
        // Lo más urgente primero: la vencida.
        Assert.Equal(VacunaService.Vencida, carnet[0].Estado);
    }

    [Theory]
    [InlineData(-1, VacunaService.Vencida)]
    [InlineData(0, VacunaService.RefuerzoProximo)]
    [InlineData(5, VacunaService.RefuerzoProximo)]
    [InlineData(6, VacunaService.Vigente)]
    public void El_estado_depende_de_los_dias_que_faltan_para_el_refuerzo(int dias, string esperado)
    {
        var f = new Fabrica();
        var aplicacion = new AplicacionSanitaria { FechaRefuerzo = Hoy.AddDays(dias) };

        Assert.Equal(esperado, f.Vacunas.EstadoDe(aplicacion));
    }

    [Fact]
    public void Los_dias_de_aviso_se_pueden_cambiar()
    {
        var f = new Fabrica();
        f.Vacunas.DiasAvisoRefuerzo = 3;
        var aplicacion = new AplicacionSanitaria { FechaRefuerzo = Hoy.AddDays(4) };

        Assert.Equal(VacunaService.Vigente, f.Vacunas.EstadoDe(aplicacion));
    }

    [Fact]
    public void Las_alertas_son_solo_refuerzos_proximos_o_vencidos_de_los_animales_del_responsable()
    {
        var f = new Fabrica();

        // Cuenta de demostración 100: Toby tiene un refuerzo próximo y uno vencido; Mishi está al día.
        var alertas = f.Vacunas.AlertasDe(100);

        Assert.Equal(2, alertas.Count);
        Assert.All(alertas, a => Assert.Equal("Toby", a.Animal.Nombre));
        Assert.DoesNotContain(alertas, a => a.Item.Estado == VacunaService.Vigente);

        // RN-18: un cliente nuevo no ve nada ajeno.
        var otro = f.ClienteVerificado();
        Assert.Empty(f.Vacunas.AlertasDe(otro.Id));
    }

    [Fact]
    public void Los_animales_de_ejemplo_traen_un_refuerzo_proximo_y_uno_vencido()
    {
        var f = new Fabrica();
        var cliente = f.ClienteVerificado();
        f.Animales.CargarEjemplos(cliente.Id);

        f.Vacunas.CargarCarnetEjemplo(cliente.Id);
        f.Vacunas.CargarCarnetEjemplo(cliente.Id);

        var alertas = f.Vacunas.AlertasDe(cliente.Id);
        Assert.Equal(2, alertas.Count);
        Assert.Contains(alertas, a => a.Animal.Nombre == "Lucero" && a.Item.Estado == VacunaService.RefuerzoProximo);
        Assert.Contains(alertas, a => a.Animal.Nombre == "Majada del potrero norte" && a.Item.Estado == VacunaService.Vencida);
    }
}
