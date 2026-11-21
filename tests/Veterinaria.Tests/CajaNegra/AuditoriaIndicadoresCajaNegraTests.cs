using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;
using Xunit;

namespace Veterinaria.Tests.CajaNegra;

// Pruebas de caja negra de la bitácora de auditoría (AU-01 a AU-04) y de los indicadores (IN-01 a IN-04),
// escritas solo con ESPECIFICACION_ADMINISTRACION.md, los modelos y las firmas públicas.
// Hay datos de demostración: se comparan cantidades antes y después o se filtra por lo que crea cada prueba.
public class AuditoriaIndicadoresCajaNegraTests
{
    private const string Admin = "admin@vitalia.uy";
    private const string Recepcion = "recepcion@vitalia.uy";
    private const string Ferreira = "ferreira@vitalia.uy";

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);

    // ---------- Ayudantes ----------

    // Turno de hoy, consulta iniciada, (opcionalmente) 3 jeringas usadas y consulta firmada.
    private static Consulta ConsultaFirmada(Fabrica f, Personal vet, bool conInsumo = false)
    {
        var turno = f.TurnoEnEspera();
        f.Consultas.Iniciar(turno.Id, vet);
        var consulta = f.Consultas.ObtenerPorTurno(turno.Id)!;

        if (conInsumo)
        {
            f.Consultas.RegistrarInsumo(consulta.Id, new ConsumoFormulario { ArticuloId = 8, Cantidad = 3 }, vet);
            consulta = f.Consultas.ObtenerPorTurno(turno.Id)!;
        }

        f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet);
        var firmada = f.Consultas.ObtenerPorTurno(turno.Id)!;
        Assert.Equal("Firmada", firmada.Estado);
        return firmada;
    }

    // Cobra el cargo completo de la consulta y devuelve el comprobante.
    private static Pago CobrarConsulta(Fabrica f, Consulta consulta, Personal recepcion)
    {
        var cargo = f.Cobros.CargoDeConsulta(consulta.Id)!;
        var antes = f.Cobros.Pagos().Count;
        f.Cobros.Cobrar(new CobroFormulario { CargoId = cargo.Id, Monto = (int)cargo.Monto, Medio = CobroService.Efectivo }, recepcion);
        Assert.Equal(antes + 1, f.Cobros.Pagos().Count);
        return f.Cobros.Pagos().Last(p => p.CargoId == cargo.Id);
    }

    private static decimal TotalDe(List<IngresoServicio> filas, string servicio)
    {
        return filas.Where(i => i.Servicio == servicio).Sum(i => i.Total);
    }

    private static int CobrosDe(List<IngresoServicio> filas, string servicio)
    {
        return filas.Where(i => i.Servicio == servicio).Sum(i => i.Cobros);
    }

    private static int ConsumoDe(List<RotacionArticulo> filas, string articulo)
    {
        return filas.Where(r => r.Articulo == articulo).Sum(r => r.Consumo);
    }

    private static bool Atiende(Fabrica f, Profesional profesional, DateOnly dia)
    {
        return profesional.Jornada.Any(j => j.Dia == dia.DayOfWeek)
               && !profesional.Ausencias.Any(a => dia >= a.Desde && dia <= a.Hasta)
               && !f.Agenda.EsFeriado(dia);
    }

    private static int Minutos(TimeOnly hora)
    {
        return (int)hora.ToTimeSpan().TotalMinutes;
    }

    // Día futuro de la Dra. Ferreira y una hora de su jornada libre durante 60 minutos (sin turnos activos ni descansos).
    private static DateTime HuecoDeFerreira(Fabrica f)
    {
        var profesional = f.Profesionales.ObtenerPorId(1)!;
        var dia = f.DiaQueAtiende(1);

        for (int intento = 0; intento < 60; intento++, dia = dia.AddDays(1))
        {
            if (!Atiende(f, profesional, dia))
            {
                continue;
            }

            var ocupados = f.Agenda.TurnosDelDia(dia, 1)
                .Where(t => t.Estado != "Cancelado" && t.Estado != "Ausente")
                .ToList();

            foreach (var franja in profesional.Jornada.Where(j => j.Dia == dia.DayOfWeek))
            {
                for (int m = Minutos(franja.Inicio); m + 60 <= Minutos(franja.Fin); m += 15)
                {
                    var inicio = dia.ToDateTime(TimeOnly.MinValue).AddMinutes(m);
                    var fin = inicio.AddMinutes(60);
                    var choca = ocupados.Any(t => t.Inicio < fin && t.Fin > inicio)
                                || profesional.Descansos.Any(d => d.Dia == dia.DayOfWeek
                                                                  && Minutos(d.Inicio) < m + 60
                                                                  && Minutos(d.Fin) > m);
                    if (!choca)
                    {
                        return inicio;
                    }
                }
            }
        }

        throw new InvalidOperationException("No se encontró un hueco libre de la Dra. Ferreira.");
    }

    // Día lejano (más allá de la ventana de reservas) en que la Dra. Ferreira atiende.
    private static DateOnly DiaLejanoQueAtiende(Fabrica f)
    {
        var profesional = f.Profesionales.ObtenerPorId(1)!;
        var dia = Hoy.AddDays(100);
        while (!Atiende(f, profesional, dia))
        {
            dia = dia.AddDays(1);
        }
        return dia;
    }

    // ---------- AU-01: solo Administrador activo ----------

    [Fact]
    public void Bitacora_administrador_activo_ve_registros_de_demostracion()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);

        Assert.NotEmpty(f.Bitacora.Buscar(admin));
    }

    [Fact]
    public void Bitacora_sin_sesion_devuelve_lista_vacia()
    {
        var f = new Fabrica();

        Assert.Empty(f.Bitacora.Buscar(null));
    }

    [Theory]
    [InlineData(Recepcion)]
    [InlineData(Ferreira)]
    public void Bitacora_otro_rol_devuelve_lista_vacia(string correo)
    {
        var f = new Fabrica();
        var persona = f.PersonalDe(correo);

        Assert.Empty(f.Bitacora.Buscar(persona));
        Assert.Empty(f.Bitacora.Buscar(persona, entidad: BitacoraService.Configuracion));
    }

    [Fact]
    public void Bitacora_administrador_bloqueado_devuelve_lista_vacia()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        admin.Activo = false;

        Assert.Empty(f.Bitacora.Buscar(admin));
    }

    [Fact]
    public void Consultar_la_bitacora_no_agrega_registros()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var recepcion = f.PersonalDe(Recepcion);

        var antes = f.Bitacora.Buscar(admin).Count;
        f.Bitacora.Buscar(recepcion);
        f.Bitacora.Buscar(null);
        f.Bitacora.Buscar(admin, usuario: "x", entidad: BitacoraService.Cobros, accion: "y", desde: Hoy, hasta: Hoy);

        Assert.Equal(antes, f.Bitacora.Buscar(admin).Count);
    }

    // ---------- AU-02 / AD-02: qué se junta ----------

    [Fact]
    public void Todas_las_entradas_usan_una_entidad_de_la_lista()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);

        Assert.All(f.Bitacora.Buscar(admin), r => Assert.Contains(r.Entidad, BitacoraService.Entidades));
    }

    [Fact]
    public void Agregar_feriado_deja_un_registro_de_configuracion_del_administrador()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var dia = Hoy.AddDays(120);
        var antes = f.Bitacora.Buscar(admin, entidad: BitacoraService.Configuracion).Count;

        f.Administracion.AgregarFeriado(dia, admin);

        Assert.True(f.Agenda.EsFeriado(dia));
        var despues = f.Bitacora.Buscar(admin, entidad: BitacoraService.Configuracion);
        Assert.Equal(antes + 1, despues.Count);

        var nuevo = despues.OrderByDescending(r => r.Fecha).First();
        Assert.Equal("Administrador", nuevo.Rol);
        Assert.True(nuevo.Usuario == admin.Nombre || nuevo.Usuario == admin.Correo);
        Assert.Equal(Hoy, DateOnly.FromDateTime(nuevo.Fecha));
        Assert.False(string.IsNullOrWhiteSpace(nuevo.Accion));
    }

    [Fact]
    public void Feriado_rechazado_no_deja_registro()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var recepcion = f.PersonalDe(Recepcion);
        var dia = Hoy.AddDays(130);
        var antes = f.Bitacora.Buscar(admin).Count;

        f.Administracion.AgregarFeriado(Hoy.AddDays(-1), admin);   // fecha pasada
        f.Administracion.AgregarFeriado(null, admin);              // sin fecha
        f.Administracion.AgregarFeriado(dia, recepcion);           // rol sin permiso

        Assert.False(f.Agenda.EsFeriado(dia));
        Assert.Equal(antes, f.Bitacora.Buscar(admin).Count);
    }

    [Fact]
    public void Feriado_repetido_deja_un_solo_registro()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var dia = Hoy.AddDays(140);
        var antes = f.Bitacora.Buscar(admin, entidad: BitacoraService.Configuracion).Count;

        f.Administracion.AgregarFeriado(dia, admin);
        f.Administracion.AgregarFeriado(dia, admin);

        Assert.Equal(antes + 1, f.Bitacora.Buscar(admin, entidad: BitacoraService.Configuracion).Count);
    }

    [Fact]
    public void Firmar_una_consulta_deja_registro_de_historia_con_el_numero_de_consulta()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var vet = f.PersonalDe(Ferreira);
        var antes = f.Bitacora.Buscar(admin, entidad: BitacoraService.Historia).Count;

        var consulta = ConsultaFirmada(f, vet);

        var despues = f.Bitacora.Buscar(admin, entidad: BitacoraService.Historia);
        Assert.True(despues.Count > antes);
        Assert.Contains(despues, r => r.Detalle.Contains(consulta.Id.ToString()));
    }

    [Fact]
    public void Lectura_de_historia_deja_registro_con_el_motivo_de_acceso()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var vet = f.PersonalDe(Ferreira);
        var turno = f.TurnoEnEspera();
        var motivo = "Control previo cirugia prueba-cn";
        var antes = f.Bitacora.Buscar(admin, entidad: BitacoraService.Historia).Count;

        f.Consultas.RegistrarLectura(vet, turno.AnimalId, motivo);

        var despues = f.Bitacora.Buscar(admin, entidad: BitacoraService.Historia);
        Assert.Equal(antes + 1, despues.Count);
        Assert.Contains(despues, r => r.Detalle.Contains(motivo));
    }

    [Fact]
    public void Insumo_de_consulta_deja_registro_de_inventario()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var vet = f.PersonalDe(Ferreira);
        var antes = f.Bitacora.Buscar(admin, entidad: BitacoraService.Stock).Count;

        ConsultaFirmada(f, vet, conInsumo: true);

        Assert.True(f.Bitacora.Buscar(admin, entidad: BitacoraService.Stock).Count > antes);
    }

    [Fact]
    public void Cobro_y_anulacion_dejan_registros_de_cobros_con_el_numero_de_comprobante()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var vet = f.PersonalDe(Ferreira);
        var recepcion = f.PersonalDe(Recepcion);
        var consulta = ConsultaFirmada(f, vet);
        var antes = f.Bitacora.Buscar(admin, entidad: BitacoraService.Cobros).Count;

        var pago = CobrarConsulta(f, consulta, recepcion);
        var trasCobro = f.Bitacora.Buscar(admin, entidad: BitacoraService.Cobros);
        Assert.Equal(antes + 1, trasCobro.Count);
        Assert.Contains(trasCobro, r => r.Detalle.Contains(pago.Numero));

        f.Cobros.Anular(pago.Id, new AnulacionFormulario { Motivo = "Error de monto" }, admin);
        Assert.Equal(antes + 2, f.Bitacora.Buscar(admin, entidad: BitacoraService.Cobros).Count);
    }

    [Fact]
    public void Cobro_rechazado_no_deja_registro()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var vet = f.PersonalDe(Ferreira);
        var recepcion = f.PersonalDe(Recepcion);
        var consulta = ConsultaFirmada(f, vet);
        var cargo = f.Cobros.CargoDeConsulta(consulta.Id)!;
        var pagosAntes = f.Cobros.Pagos().Count;
        var antes = f.Bitacora.Buscar(admin, entidad: BitacoraService.Cobros).Count;

        f.Cobros.Cobrar(new CobroFormulario { CargoId = cargo.Id, Monto = 0, Medio = CobroService.Efectivo }, recepcion);

        Assert.Equal(pagosAntes, f.Cobros.Pagos().Count);
        Assert.Equal(antes, f.Bitacora.Buscar(admin, entidad: BitacoraService.Cobros).Count);
    }

    // ---------- AU-03: sin texto clínico ----------

    [Fact]
    public void Ningun_registro_nuevo_copia_motivo_ni_diagnostico()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var vet = f.PersonalDe(Ferreira);
        var recepcion = f.PersonalDe(Recepcion);
        var clinico = Fabrica.ConsultaCompleta();

        int ConTextoClinico() => f.Bitacora.Buscar(admin).Count(r =>
            r.Detalle.Contains(clinico.Motivo, StringComparison.OrdinalIgnoreCase)
            || r.Detalle.Contains(clinico.Diagnostico, StringComparison.OrdinalIgnoreCase)
            || r.Accion.Contains(clinico.Motivo, StringComparison.OrdinalIgnoreCase)
            || r.Accion.Contains(clinico.Diagnostico, StringComparison.OrdinalIgnoreCase));

        var antes = ConTextoClinico();
        var totalAntes = f.Bitacora.Buscar(admin).Count;

        var consulta = ConsultaFirmada(f, vet, conInsumo: true);
        CobrarConsulta(f, consulta, recepcion);

        Assert.True(f.Bitacora.Buscar(admin).Count > totalAntes);
        Assert.Equal(antes, ConTextoClinico());
    }

    // ---------- AU-04: filtros y orden ----------

    [Fact]
    public void Bitacora_ordenada_de_lo_mas_reciente_a_lo_mas_antiguo()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        f.Administracion.AgregarFeriado(Hoy.AddDays(150), admin);

        var lista = f.Bitacora.Buscar(admin);

        for (int i = 1; i < lista.Count; i++)
        {
            Assert.True(lista[i - 1].Fecha >= lista[i].Fecha);
        }
    }

    [Fact]
    public void Filtro_de_usuario_contiene_sin_mayusculas()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        f.Administracion.AgregarFeriado(Hoy.AddDays(160), admin);
        var nuevo = f.Bitacora.Buscar(admin, entidad: BitacoraService.Configuracion).OrderByDescending(r => r.Fecha).First();
        var parte = nuevo.Usuario.Length > 3 ? nuevo.Usuario.Substring(1, 3) : nuevo.Usuario;

        var lista = f.Bitacora.Buscar(admin, usuario: parte.ToUpperInvariant());

        Assert.NotEmpty(lista);
        Assert.All(lista, r => Assert.Contains(parte, r.Usuario, StringComparison.OrdinalIgnoreCase));
        Assert.Empty(f.Bitacora.Buscar(admin, usuario: "zzz-usuario-inexistente-zzz"));
    }

    [Fact]
    public void Filtro_de_entidad_es_exacto()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        f.Administracion.AgregarFeriado(Hoy.AddDays(170), admin);

        var lista = f.Bitacora.Buscar(admin, entidad: BitacoraService.Configuracion);

        Assert.NotEmpty(lista);
        Assert.All(lista, r => Assert.Equal(BitacoraService.Configuracion, r.Entidad));
        Assert.Empty(f.Bitacora.Buscar(admin, entidad: "Config"));
    }

    [Fact]
    public void Filtro_de_accion_contiene_sin_mayusculas()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        f.Administracion.AgregarFeriado(Hoy.AddDays(180), admin);
        var nuevo = f.Bitacora.Buscar(admin, entidad: BitacoraService.Configuracion).OrderByDescending(r => r.Fecha).First();
        var parte = nuevo.Accion.Length > 3 ? nuevo.Accion.Substring(0, 3) : nuevo.Accion;

        var lista = f.Bitacora.Buscar(admin, accion: parte.ToLowerInvariant());

        Assert.NotEmpty(lista);
        Assert.All(lista, r => Assert.Contains(parte, r.Accion, StringComparison.OrdinalIgnoreCase));
        Assert.Empty(f.Bitacora.Buscar(admin, accion: "zzz-accion-inexistente-zzz"));
    }

    [Fact]
    public void Filtro_de_fechas_es_inclusivo()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        f.Administracion.AgregarFeriado(Hoy.AddDays(190), admin);

        var hoy = f.Bitacora.Buscar(admin, entidad: BitacoraService.Configuracion, desde: Hoy, hasta: Hoy);
        Assert.NotEmpty(hoy);
        Assert.All(hoy, r => Assert.Equal(Hoy, DateOnly.FromDateTime(r.Fecha)));

        Assert.All(f.Bitacora.Buscar(admin, desde: Hoy.AddDays(1)), r => Assert.True(DateOnly.FromDateTime(r.Fecha) >= Hoy.AddDays(1)));
        Assert.DoesNotContain(f.Bitacora.Buscar(admin, hasta: Hoy.AddDays(-1)), r => DateOnly.FromDateTime(r.Fecha) >= Hoy);
    }

    [Fact]
    public void Desde_posterior_a_hasta_no_devuelve_nada()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);

        Assert.Empty(f.Bitacora.Buscar(admin, desde: Hoy, hasta: Hoy.AddDays(-1)));
    }

    [Fact]
    public void Filtros_combinados_cumplen_todas_las_condiciones()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        f.Administracion.AgregarFeriado(Hoy.AddDays(200), admin);
        var nuevo = f.Bitacora.Buscar(admin, entidad: BitacoraService.Configuracion).OrderByDescending(r => r.Fecha).First();

        var lista = f.Bitacora.Buscar(admin, nuevo.Usuario, BitacoraService.Configuracion, nuevo.Accion, Hoy, Hoy);

        Assert.NotEmpty(lista);
        Assert.True(lista.Count <= f.Bitacora.Buscar(admin).Count);
        Assert.All(lista, r =>
        {
            Assert.Equal(BitacoraService.Configuracion, r.Entidad);
            Assert.Contains(nuevo.Usuario, r.Usuario, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(nuevo.Accion, r.Accion, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(Hoy, DateOnly.FromDateTime(r.Fecha));
        });
        Assert.Empty(f.Bitacora.Buscar(admin, nuevo.Usuario, BitacoraService.Cobros, nuevo.Accion, Hoy, Hoy)
            .Where(r => r.Entidad != BitacoraService.Cobros));
    }

    // ---------- IN-01: ocupación ----------

    [Fact]
    public void Ocupacion_porcentajes_entre_0_y_100()
    {
        var f = new Fabrica();
        var dia = f.DiaQueAtiende(1);

        var filas = f.Indicadores.Ocupacion(dia);

        Assert.NotEmpty(filas);
        Assert.All(filas, o =>
        {
            Assert.InRange(o.Porcentaje, 0, 100);
            Assert.True(o.MinutosOcupados >= 0);
            Assert.True(o.MinutosDisponibles >= 0);
        });
    }

    [Fact]
    public void Ocupacion_minutos_disponibles_son_la_jornada_sin_descansos()
    {
        var f = new Fabrica();
        var profesional = f.Profesionales.ObtenerPorId(1)!;
        var dia = f.DiaQueAtiende(1);
        var jornada = profesional.Jornada.Where(j => j.Dia == dia.DayOfWeek).Sum(j => Minutos(j.Fin) - Minutos(j.Inicio));
        var descansos = profesional.Descansos.Where(d => d.Dia == dia.DayOfWeek).Sum(d => Minutos(d.Fin) - Minutos(d.Inicio));

        var fila = f.Indicadores.Ocupacion(dia).Single(o => o.Profesional == profesional.Nombre);

        Assert.Equal(jornada - descansos, fila.MinutosDisponibles);
    }

    [Fact]
    public void Ocupacion_suma_turno_activo_y_no_cuenta_cancelado()
    {
        var f = new Fabrica();
        var profesional = f.Profesionales.ObtenerPorId(1)!;
        var inicio = HuecoDeFerreira(f);
        var dia = DateOnly.FromDateTime(inicio);
        var antes = f.Indicadores.Ocupacion(dia).Single(o => o.Profesional == profesional.Nombre).MinutosOcupados;

        f.Agenda.OcuparComoOtroCliente(1, 1, inicio);
        var turno = f.Agenda.TurnosDelDia(dia, 1).FirstOrDefault(t => t.Inicio == inicio);
        Assert.NotNull(turno);
        var duracion = (int)(turno!.Fin - turno.Inicio).TotalMinutes;

        var conTurno = f.Indicadores.Ocupacion(dia).Single(o => o.Profesional == profesional.Nombre);
        Assert.Equal(antes + duracion, conTurno.MinutosOcupados);
        Assert.True(conTurno.Porcentaje > 0);

        turno.Estado = "Cancelado";
        Assert.Equal(antes, f.Indicadores.Ocupacion(dia).Single(o => o.Profesional == profesional.Nombre).MinutosOcupados);
    }

    [Fact]
    public void Ocupacion_es_cero_en_dia_que_no_atiende()
    {
        var f = new Fabrica();
        var profesional = f.Profesionales.ObtenerPorId(1)!;
        var dia = Hoy.AddDays(1);
        while (profesional.Jornada.Any(j => j.Dia == dia.DayOfWeek))
        {
            dia = dia.AddDays(1);
            if (dia > Hoy.AddDays(8))
            {
                return; // atiende todos los días: el caso no aplica
            }
        }

        var fila = f.Indicadores.Ocupacion(dia).Single(o => o.Profesional == profesional.Nombre);

        Assert.Equal(0, fila.Porcentaje);
    }

    [Fact]
    public void Ocupacion_es_cero_en_feriado()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var dia = DiaLejanoQueAtiende(f);

        f.Administracion.AgregarFeriado(dia, admin);
        Assert.True(f.Agenda.EsFeriado(dia));

        Assert.All(f.Indicadores.Ocupacion(dia), o => Assert.Equal(0, o.Porcentaje));
    }

    // ---------- IN-02: ingresos por servicio ----------

    [Fact]
    public void Ingresos_suman_el_cobro_y_restan_al_anularlo()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var vet = f.PersonalDe(Ferreira);
        var recepcion = f.PersonalDe(Recepcion);
        var consulta = ConsultaFirmada(f, vet);
        var cargo = f.Cobros.CargoDeConsulta(consulta.Id)!;
        var antes = f.Indicadores.IngresosPorServicio(Hoy, Hoy);

        var pago = CobrarConsulta(f, consulta, recepcion);

        var conCobro = f.Indicadores.IngresosPorServicio(Hoy, Hoy);
        Assert.Equal(TotalDe(antes, cargo.Descripcion) + pago.Monto, TotalDe(conCobro, cargo.Descripcion));
        Assert.Equal(CobrosDe(antes, cargo.Descripcion) + 1, CobrosDe(conCobro, cargo.Descripcion));

        f.Cobros.Anular(pago.Id, new AnulacionFormulario { Motivo = "Cobrado por error" }, admin);

        var anulado = f.Indicadores.IngresosPorServicio(Hoy, Hoy);
        Assert.Equal(TotalDe(antes, cargo.Descripcion), TotalDe(anulado, cargo.Descripcion));
        Assert.Equal(CobrosDe(antes, cargo.Descripcion), CobrosDe(anulado, cargo.Descripcion));
    }

    [Fact]
    public void Ingresos_ordenados_de_mayor_a_menor_y_un_renglon_por_servicio()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var recepcion = f.PersonalDe(Recepcion);
        CobrarConsulta(f, ConsultaFirmada(f, vet), recepcion);

        var filas = f.Indicadores.IngresosPorServicio(Hoy.AddDays(-365), Hoy);

        Assert.NotEmpty(filas);
        Assert.Equal(filas.Count, filas.Select(i => i.Servicio).Distinct().Count());
        for (int i = 1; i < filas.Count; i++)
        {
            Assert.True(filas[i - 1].Total >= filas[i].Total);
        }
    }

    [Fact]
    public void Ingresos_fuera_del_periodo_no_cuentan_el_cobro_de_hoy()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var recepcion = f.PersonalDe(Recepcion);
        var pasadoAntes = f.Indicadores.IngresosPorServicio(Hoy.AddDays(-400), Hoy.AddDays(-1)).Sum(i => i.Total);
        var futuroAntes = f.Indicadores.IngresosPorServicio(Hoy.AddDays(1), Hoy.AddDays(30)).Sum(i => i.Total);

        CobrarConsulta(f, ConsultaFirmada(f, vet), recepcion);

        Assert.Equal(pasadoAntes, f.Indicadores.IngresosPorServicio(Hoy.AddDays(-400), Hoy.AddDays(-1)).Sum(i => i.Total));
        Assert.Equal(futuroAntes, f.Indicadores.IngresosPorServicio(Hoy.AddDays(1), Hoy.AddDays(30)).Sum(i => i.Total));
    }

    // ---------- IN-03: ausencias ----------

    [Theory]
    [InlineData("Ausente")]
    [InlineData("Cancelado")]
    [InlineData("Finalizado")]
    public void Ausencias_cuentan_el_turno_segun_su_estado(string estado)
    {
        var f = new Fabrica();
        var antes = f.Indicadores.Ausencias(Hoy, Hoy);

        var turno = f.TurnoEnEspera();
        turno.Estado = estado;

        var despues = f.Indicadores.Ausencias(Hoy, Hoy);
        Assert.Equal(antes.Turnos + 1, despues.Turnos);
        Assert.Equal(antes.Ausentes + (estado == "Ausente" ? 1 : 0), despues.Ausentes);
        Assert.Equal(antes.Cancelados + (estado == "Cancelado" ? 1 : 0), despues.Cancelados);
        Assert.Equal(antes.Atendidos + (estado == "Finalizado" ? 1 : 0), despues.Atendidos);
    }

    [Fact]
    public void Ausencias_tasa_es_ausentes_sobre_finalizados_mas_ausentes()
    {
        var f = new Fabrica();
        var turno = f.TurnoEnEspera();
        turno.Estado = "Ausente";

        var r = f.Indicadores.Ausencias(Hoy.AddDays(-365), Hoy);

        Assert.True(r.Ausentes >= 1);
        Assert.True(r.Turnos >= r.Atendidos + r.Ausentes + r.Cancelados);
        var esperado = 100.0 * r.Ausentes / (r.Atendidos + r.Ausentes);
        Assert.InRange(r.PorcentajeAusencias, (int)Math.Floor(esperado), (int)Math.Ceiling(esperado));
    }

    [Fact]
    public void Ausencias_periodo_sin_turnos_da_todo_cero()
    {
        var f = new Fabrica();
        var dia = Hoy.AddDays(500);

        var r = f.Indicadores.Ausencias(dia, dia);

        Assert.Equal(0, r.Turnos);
        Assert.Equal(0, r.Atendidos);
        Assert.Equal(0, r.Ausentes);
        Assert.Equal(0, r.Cancelados);
        Assert.Equal(0, r.PorcentajeAusencias);
    }

    [Fact]
    public void Ausencias_solo_cancelados_dan_tasa_cero()
    {
        var f = new Fabrica();
        var turno = f.TurnoEnEspera();
        // Se deja solo el turno creado: los de hoy de la demostración se cancelan para aislar el caso.
        foreach (var t in f.Agenda.TurnosDelDia(Hoy, 1))
        {
            t.Estado = "Cancelado";
        }
        var r = f.Indicadores.Ausencias(Hoy, Hoy);

        if (r.Atendidos + r.Ausentes == 0)
        {
            Assert.Equal(0, r.PorcentajeAusencias);
        }
        Assert.Equal("Cancelado", turno.Estado);
    }

    // ---------- IN-04: rotación ----------

    [Fact]
    public void Rotacion_cuenta_el_consumo_de_la_consulta_y_el_stock_actual()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var jeringa = f.Inventario.ObtenerArticulo(8)!.Nombre;
        var antes = ConsumoDe(f.Indicadores.Rotacion(30), jeringa);

        ConsultaFirmada(f, vet, conInsumo: true);

        var fila = f.Indicadores.Rotacion(30).Single(r => r.Articulo == jeringa);
        Assert.Equal(antes + 3, fila.Consumo);
        Assert.Equal(Convert.ToInt32(f.Inventario.Stock(8)), fila.Stock);
    }

    [Fact]
    public void Rotacion_dias_de_cobertura_redondeados_hacia_abajo_o_sin_dato()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        ConsultaFirmada(f, vet, conInsumo: true);
        const int dias = 30;

        var filas = f.Indicadores.Rotacion(dias);

        Assert.NotEmpty(filas);
        Assert.All(filas, r =>
        {
            if (r.Consumo == 0)
            {
                Assert.Null(r.DiasCobertura);
            }
            else
            {
                Assert.NotNull(r.DiasCobertura);
                Assert.Equal((int)Math.Floor((decimal)r.Stock * dias / r.Consumo), r.DiasCobertura!.Value);
            }
        });
    }

    [Fact]
    public void Rotacion_con_momento_futuro_no_incluye_consumos_de_hoy()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe(Ferreira);
        var jeringa = f.Inventario.ObtenerArticulo(8)!.Nombre;
        ConsultaFirmada(f, vet, conInsumo: true);

        var lejos = f.Indicadores.Rotacion(30, DateTime.Now.AddDays(400));

        Assert.Equal(0, ConsumoDe(lejos, jeringa));
        Assert.All(lejos.Where(r => r.Consumo == 0), r => Assert.Null(r.DiasCobertura));
    }
}
