using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// A-022: RF-ADM-05, RF-ADM-06, RF-VET-06 y RN-26 a RN-31.
// Artículos de demostración: 8 jeringa (120), 10 gasa (lote GAS-2609 de 8 que vence en 25 días y GAS-2611 de 30),
// 7 antiparasitario externo (6 de stock, mínimo 5), 2 antirrábica (bajo mínimo desde el arranque).
public class InventarioTests
{
    private const string Admin = "admin@vitalia.uy";
    private const string Ferreira = "ferreira@vitalia.uy";
    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);

    private static (Fabrica f, Personal vet, Consulta consulta) ConsultaAbierta()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var turno = f.TurnoEnEspera();
        f.Consultas.Iniciar(turno.Id, vet);
        return (f, vet, f.Consultas.ObtenerPorTurno(turno.Id)!);
    }

    private static ResultadoOperacion Firmar(Fabrica f, Consulta consulta, Personal vet)
    {
        return f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet);
    }

    private static LoteProducto Lote(Fabrica f, string numero)
    {
        return f.Inventario.Articulos().SelectMany(a => f.Inventario.LotesDe(a.Id)).First(l => l.Numero == numero);
    }

    [Fact]
    public void Los_lotes_vencidos_se_inhabilitan_y_se_dan_de_baja_solos()
    {
        var f = new Fabrica();

        var vencido = Lote(f, "L1-VENC");

        Assert.True(vencido.Inhabilitado);
        Assert.Equal(0, vencido.Cantidad);
        Assert.Contains(f.Inventario.Movimientos(1), m => m.LoteId == vencido.Id && m.Tipo == InventarioService.Baja && m.Cantidad == -2 && m.PersonaNombre == "Sistema");
    }

    [Fact]
    public void Consumir_usa_primero_el_lote_que_vence_antes_y_sigue_con_el_proximo()
    {
        var (f, vet, consulta) = ConsultaAbierta();

        f.Consultas.RegistrarInsumo(consulta.Id, new ConsumoFormulario { ArticuloId = 10, Cantidad = 10 }, vet);
        Assert.True(Firmar(f, consulta, vet).Exito);

        Assert.Equal(0, Lote(f, "GAS-2609").Cantidad);
        Assert.Equal(28, Lote(f, "GAS-2611").Cantidad);
    }

    [Fact]
    public void Sin_stock_suficiente_no_se_registra_ni_cambia_nada()
    {
        var (f, vet, consulta) = ConsultaAbierta();
        var antes = f.Inventario.Stock(11);

        var resultado = f.Consultas.RegistrarInsumo(consulta.Id, new ConsumoFormulario { ArticuloId = 11, Cantidad = antes + 1 }, vet);

        Assert.False(resultado.Exito);
        Assert.Contains("RN-26", resultado.Mensaje);
        Assert.Empty(f.Consultas.InsumosDe(consulta.Id));
        Assert.Equal(antes, f.Inventario.StockDisponible(11));
    }

    [Fact]
    public void El_insumo_se_aparta_al_registrar_y_se_descuenta_recien_al_firmar()
    {
        var (f, vet, consulta) = ConsultaAbierta();

        f.Consultas.RegistrarInsumo(consulta.Id, new ConsumoFormulario { ArticuloId = 8, Cantidad = 3 }, vet);
        Assert.Equal(120, f.Inventario.Stock(8));
        Assert.Equal(117, f.Inventario.StockDisponible(8));

        Assert.True(Firmar(f, consulta, vet).Exito);
        Assert.Equal(117, f.Inventario.Stock(8));
        Assert.Contains(f.Inventario.Movimientos(8), m => m.Tipo == InventarioService.Consumo && m.Cantidad == -3 && m.Origen == $"Consulta {consulta.Id}");
    }

    [Fact]
    public void Quitar_un_insumo_lo_libera()
    {
        var (f, vet, consulta) = ConsultaAbierta();
        f.Consultas.RegistrarInsumo(consulta.Id, new ConsumoFormulario { ArticuloId = 8, Cantidad = 3 }, vet);

        f.Consultas.QuitarInsumo(consulta.Id, 8, vet);

        Assert.Empty(f.Consultas.InsumosDe(consulta.Id));
        Assert.Equal(120, f.Inventario.StockDisponible(8));
    }

    [Fact]
    public void Una_vacuna_no_se_registra_como_insumo()
    {
        var (f, vet, consulta) = ConsultaAbierta();

        var resultado = f.Consultas.RegistrarInsumo(consulta.Id, new ConsumoFormulario { ArticuloId = 1, Cantidad = 1 }, vet);

        Assert.False(resultado.Exito);
    }

    [Fact]
    public void Con_insumos_registrados_no_se_cancela_sin_atencion()
    {
        var (f, vet, consulta) = ConsultaAbierta();
        f.Consultas.RegistrarInsumo(consulta.Id, new ConsumoFormulario { ArticuloId = 9, Cantidad = 1 }, vet);

        Assert.False(f.Consultas.CancelarSinAtencion(consulta.Id, "Se fue", vet).Exito);

        f.Consultas.QuitarInsumo(consulta.Id, 9, vet);
        Assert.True(f.Consultas.CancelarSinAtencion(consulta.Id, "Se fue", vet).Exito);
    }

    [Fact]
    public void Lo_apartado_no_lo_puede_usar_otra_consulta()
    {
        var (f, vet, consulta) = ConsultaAbierta();
        var disponible = f.Inventario.StockDisponible(11);
        f.Consultas.RegistrarInsumo(consulta.Id, new ConsumoFormulario { ArticuloId = 11, Cantidad = disponible }, vet);

        Assert.Equal(0, f.Inventario.StockDisponible(11));
        Assert.Null(f.Inventario.Reservar(999, 11, 1, 0));
    }

    [Fact]
    public void Cruzar_el_minimo_genera_una_alerta_en_el_mismo_paso()
    {
        var (f, vet, consulta) = ConsultaAbierta();
        var avisosAntes = f.Inventario.Alertas().Count(a => a.ArticuloId == 7);

        // Antiparasitario externo: 6 de stock, mínimo 5. Una dosis lo deja en 5 (sin aviso)...
        f.Vacunas.Registrar(consulta.Id, new AplicacionFormulario { ProductoId = 7 }, vet);
        Assert.True(Firmar(f, consulta, vet).Exito);
        Assert.Equal(avisosAntes, f.Inventario.Alertas().Count(a => a.ArticuloId == 7));

        // ...y un ajuste de -1 lo deja en 4: ahí sí.
        var admin = f.PersonalDe(Admin);
        var lote = f.Inventario.LotesDe(7).First(InventarioService.Usable);
        f.Inventario.Ajustar(new AjusteFormulario { LoteId = lote.Id, Cantidad = -1, Motivo = "Conteo físico" }, admin);

        var alerta = f.Inventario.Alertas().First(a => a.ArticuloId == 7);
        Assert.Equal(4, alerta.StockQueQuedo);
        Assert.Equal(avisosAntes + 1, f.Inventario.Alertas().Count(a => a.ArticuloId == 7));
    }

    [Fact]
    public void El_tablero_muestra_bajo_minimo_y_proximos_a_vencer()
    {
        var f = new Fabrica();

        Assert.Contains(f.Inventario.BajoMinimo(), a => a.Id == 2);
        Assert.Contains(f.Inventario.ProximosAVencer(), l => l.Numero == "GAS-2609");
        Assert.DoesNotContain(f.Inventario.ProximosAVencer(), l => l.Numero == "GAS-2611");
        Assert.DoesNotContain(f.Inventario.ProximosAVencer(), l => l.Inhabilitado);
    }

    [Fact]
    public void El_administrador_ingresa_un_lote_por_compra()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);

        var resultado = f.Inventario.IngresarCompra(new IngresoLoteFormulario { ArticuloId = 12, Numero = "ven-2701", Vencimiento = Hoy.AddYears(1), Cantidad = 20 }, admin);

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.Equal(35, f.Inventario.Stock(12));
        Assert.Contains(f.Inventario.Movimientos(12), m => m.Tipo == InventarioService.Compra && m.Cantidad == 20 && m.PersonaNombre == admin.Nombre);
    }

    [Fact]
    public void No_se_ingresa_un_lote_vencido_ni_uno_repetido()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);

        Assert.False(f.Inventario.IngresarCompra(new IngresoLoteFormulario { ArticuloId = 12, Numero = "X1", Vencimiento = Hoy, Cantidad = 5 }, admin).Exito);
        Assert.False(f.Inventario.IngresarCompra(new IngresoLoteFormulario { ArticuloId = 12, Numero = "VEN-2605", Vencimiento = Hoy.AddYears(1), Cantidad = 5 }, admin).Exito);
        Assert.Equal(15, f.Inventario.Stock(12));
    }

    [Fact]
    public void Solo_el_administrador_registra_compras_ajustes_y_bajas()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var lote = Lote(f, "VEN-2605");

        Assert.False(f.Inventario.IngresarCompra(new IngresoLoteFormulario { ArticuloId = 12, Numero = "N1", Vencimiento = Hoy.AddYears(1), Cantidad = 5 }, vet).Exito);
        Assert.False(f.Inventario.Ajustar(new AjusteFormulario { LoteId = lote.Id, Cantidad = 1, Motivo = "x" }, vet).Exito);
        Assert.False(f.Inventario.DarDeBaja(new AjusteFormulario { LoteId = lote.Id, Cantidad = 1, Motivo = "x" }, vet).Exito);
        Assert.Equal(15, lote.Cantidad);
    }

    [Fact]
    public void Un_ajuste_sin_motivo_no_se_registra()
    {
        var f = new Fabrica();
        var lote = Lote(f, "VEN-2605");

        var resultado = f.Inventario.Ajustar(new AjusteFormulario { LoteId = lote.Id, Cantidad = 2, Motivo = "  " }, f.PersonalDe(Admin));

        Assert.False(resultado.Exito);
        Assert.Equal(15, lote.Cantidad);
    }

    [Fact]
    public void Un_ajuste_o_una_baja_no_dejan_el_stock_negativo_ni_tocan_lo_apartado()
    {
        var (f, vet, consulta) = ConsultaAbierta();
        var admin = f.PersonalDe(Admin);
        var lote = Lote(f, "VEN-2605");
        f.Consultas.RegistrarInsumo(consulta.Id, new ConsumoFormulario { ArticuloId = 12, Cantidad = 5 }, vet);

        Assert.False(f.Inventario.Ajustar(new AjusteFormulario { LoteId = lote.Id, Cantidad = -16, Motivo = "Conteo" }, admin).Exito);
        Assert.False(f.Inventario.DarDeBaja(new AjusteFormulario { LoteId = lote.Id, Cantidad = 11, Motivo = "Rotura" }, admin).Exito);
        Assert.True(f.Inventario.DarDeBaja(new AjusteFormulario { LoteId = lote.Id, Cantidad = 10, Motivo = "Rotura" }, admin).Exito);
        Assert.Equal(5, lote.Cantidad);
        Assert.True(Firmar(f, consulta, vet).Exito);
        Assert.Equal(0, lote.Cantidad);
    }

    [Fact]
    public void Un_lote_vencido_no_se_ajusta()
    {
        var f = new Fabrica();
        var vencido = Lote(f, "L3-VENC");

        var resultado = f.Inventario.Ajustar(new AjusteFormulario { LoteId = vencido.Id, Cantidad = 3, Motivo = "Conteo" }, f.PersonalDe(Admin));

        Assert.False(resultado.Exito);
        Assert.Equal(0, vencido.Cantidad);
    }

    [Fact]
    public void Todo_movimiento_tiene_tipo_y_origen()
    {
        var f = new Fabrica();

        Assert.All(f.Inventario.Movimientos(), m =>
        {
            Assert.Contains(m.Tipo, new[] { InventarioService.Compra, InventarioService.Consumo, InventarioService.Ajuste, InventarioService.Baja });
            Assert.False(string.IsNullOrWhiteSpace(m.Origen));
        });
    }
}
