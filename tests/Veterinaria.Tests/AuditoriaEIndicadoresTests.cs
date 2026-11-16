using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// A-027 parte 2: RF-ADM-07 (indicadores) y RF-ADM-08 (bitácora), RN-40.
public class AuditoriaEIndicadoresTests
{
    private const string Admin = "admin@vitalia.uy";
    private const string Recepcion = "recepcion@vitalia.uy";
    private const string Ferreira = "ferreira@vitalia.uy";
    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);

    // Consulta firmada y cobrada en efectivo, con una lectura de historia sin turno.
    private static (Fabrica f, Consulta consulta) ConsultaCobrada()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var turno = f.TurnoEnEspera();
        f.Consultas.Iniciar(turno.Id, vet);
        var consulta = f.Consultas.ObtenerPorTurno(turno.Id)!;
        f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet);
        var cargo = f.Cobros.CargoDeConsulta(consulta.Id)!;
        f.Cobros.Cobrar(new CobroFormulario { CargoId = cargo.Id, Monto = 1200, Medio = CobroService.Efectivo }, f.PersonalDe(Recepcion));
        f.Consultas.RegistrarLectura(vet, 2, "Control de vacunas");
        return (f, consulta);
    }

    [Fact]
    public void Solo_el_Administrador_consulta_la_bitacora()
    {
        var (f, _) = ConsultaCobrada();

        Assert.Empty(f.Bitacora.Buscar(f.PersonalDe(Recepcion)));
        Assert.Empty(f.Bitacora.Buscar(null));
        Assert.NotEmpty(f.Bitacora.Buscar(f.PersonalDe(Admin)));
    }

    [Fact]
    public void La_bitacora_junta_firmas_lecturas_cobros_stock_y_configuracion()
    {
        var (f, consulta) = ConsultaCobrada();
        var admin = f.PersonalDe(Admin);
        f.Administracion.AgregarFeriado(Hoy.AddDays(50), admin);

        var todo = f.Bitacora.Buscar(admin);

        Assert.Contains(todo, r => r.Entidad == BitacoraService.Historia && r.Accion == "Consulta firmada" && r.Detalle.Contains($"n.º {consulta.Id}"));
        Assert.Contains(todo, r => r.Accion == "Lectura de historia" && r.Detalle.Contains("Control de vacunas"));
        Assert.Contains(todo, r => r.Entidad == BitacoraService.Cobros && r.Accion == "Cobro");
        Assert.Contains(todo, r => r.Entidad == BitacoraService.Stock);
        Assert.Contains(todo, r => r.Entidad == BitacoraService.Configuracion && r.Accion == "Feriado agregado");
        Assert.Contains(todo, r => r.Entidad == BitacoraService.Internaciones);
        Assert.Contains(todo, r => r.Entidad == BitacoraService.Asistencias);
    }

    [Fact]
    public void La_bitacora_no_copia_texto_clinico()
    {
        var (f, _) = ConsultaCobrada();

        var todo = f.Bitacora.Buscar(f.PersonalDe(Admin));

        Assert.DoesNotContain(todo, r => r.Detalle.Contains("Gastritis") || r.Detalle.Contains("Vómitos"));
    }

    [Fact]
    public void La_bitacora_filtra_por_usuario_entidad_accion_y_fechas()
    {
        var (f, _) = ConsultaCobrada();
        var admin = f.PersonalDe(Admin);

        Assert.All(f.Bitacora.Buscar(admin, usuario: "ferreira"), r => Assert.Contains("Ferreira", r.Usuario));
        Assert.All(f.Bitacora.Buscar(admin, entidad: BitacoraService.Cobros), r => Assert.Equal(BitacoraService.Cobros, r.Entidad));
        Assert.All(f.Bitacora.Buscar(admin, accion: "firmada"), r => Assert.Contains("firmada", r.Accion, StringComparison.OrdinalIgnoreCase));
        Assert.Empty(f.Bitacora.Buscar(admin, desde: Hoy.AddDays(1)));
        Assert.All(f.Bitacora.Buscar(admin, desde: Hoy, hasta: Hoy), r => Assert.Equal(Hoy, DateOnly.FromDateTime(r.Fecha)));
    }

    [Fact]
    public void Los_ingresos_por_servicio_no_cuentan_comprobantes_anulados()
    {
        var (f, _) = ConsultaCobrada();

        var ingresos = f.Indicadores.IngresosPorServicio(Hoy, Hoy);
        var consulta = Assert.Single(ingresos);
        Assert.Equal("Consulta clínica general", consulta.Servicio);
        Assert.Equal(1200m, consulta.Total);

        f.Cobros.Anular(f.Cobros.Pagos().Single().Id, new AnulacionFormulario { Motivo = "Error" }, f.PersonalDe(Admin));
        Assert.Empty(f.Indicadores.IngresosPorServicio(Hoy, Hoy));
    }

    [Fact]
    public void Las_ausencias_se_calculan_sobre_turnos_que_ya_debian_ocurrir()
    {
        var f = new Fabrica();
        var turno = f.TurnoEnEspera();
        turno.Estado = "Ausente";

        var resumen = f.Indicadores.Ausencias(Hoy, Hoy);

        Assert.True(resumen.Ausentes >= 1);
        Assert.Equal(resumen.Atendidos + resumen.Ausentes == 0 ? 0 : (int)Math.Round(100.0 * resumen.Ausentes / (resumen.Atendidos + resumen.Ausentes)), resumen.PorcentajeAusencias);
    }

    [Fact]
    public void La_rotacion_cuenta_los_consumos_de_las_consultas()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var turno = f.TurnoEnEspera();
        f.Consultas.Iniciar(turno.Id, vet);
        var consulta = f.Consultas.ObtenerPorTurno(turno.Id)!;
        f.Consultas.RegistrarInsumo(consulta.Id, new ConsumoFormulario { ArticuloId = 8, Cantidad = 3 }, vet);
        f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet);

        var jeringas = f.Indicadores.Rotacion(30).Single(r => r.Articulo == f.Inventario.ObtenerArticulo(8)!.Nombre);

        Assert.Equal(3, jeringas.Consumo);
        Assert.Equal(f.Inventario.Stock(8), jeringas.Stock);
        Assert.NotNull(jeringas.DiasCobertura);
    }

    [Fact]
    public void La_ocupacion_no_pasa_del_100_y_es_0_si_el_profesional_no_atiende()
    {
        var f = new Fabrica();
        var domingo = Hoy;
        while (domingo.DayOfWeek != DayOfWeek.Sunday)
        {
            domingo = domingo.AddDays(1);
        }

        Assert.All(f.Indicadores.Ocupacion(domingo), o => Assert.Equal(0, o.Porcentaje));
        Assert.All(f.Indicadores.Ocupacion(Hoy), o => Assert.InRange(o.Porcentaje, 0, 100));
    }
}
