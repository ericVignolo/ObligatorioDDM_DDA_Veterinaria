using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// A-027 (parte 1): RF-ADM-01 a RF-ADM-04, RF-ADM-10, RF-ADM-13, RN-33.
public class AdministracionTests
{
    private const string Admin = "admin@vitalia.uy";
    private const string Recepcion = "recepcion@vitalia.uy";
    private const string Ferreira = "ferreira@vitalia.uy";
    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);

    [Fact]
    public void Solo_el_Administrador_cambia_la_configuracion_y_queda_en_la_bitacora()
    {
        var f = new Fabrica();
        var datos = new PrestacionFormulario { Nombre = "Ecografía", Precio = 1800, DuracionMinutos = 30, ReservableEnLinea = true };

        Assert.False(f.Administracion.CrearServicio(datos, f.PersonalDe(Recepcion)).Exito);
        Assert.Empty(f.Auditoria.Buscar());

        Assert.True(f.Administracion.CrearServicio(datos, f.PersonalDe(Admin)).Exito);
        Assert.Contains(f.Prestaciones.ObtenerPublicas(), p => p.Nombre == "Ecografía" && p.PrecioDesde == 1800);
        var registro = Assert.Single(f.Auditoria.Buscar());
        Assert.Equal("Servicio", registro.Entidad);
        Assert.Equal("Gonzalo Rivas", registro.Usuario);
    }

    [Fact]
    public void Un_servicio_reservable_necesita_duracion_y_el_nombre_es_unico()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);

        Assert.False(f.Administracion.CrearServicio(new PrestacionFormulario { Nombre = "Corte de uñas", Precio = 300, DuracionMinutos = 0, ReservableEnLinea = true }, admin).Exito);
        Assert.False(f.Administracion.CrearServicio(new PrestacionFormulario { Nombre = "vacunación", Precio = 300 }, admin).Exito);
    }

    [Fact]
    public void Despublicar_saca_el_servicio_del_catalogo()
    {
        var f = new Fabrica();
        var datos = new PrestacionFormulario { Nombre = "Baño y estética", Descripcion = "x", DuracionMinutos = 60, ReservableEnLinea = true, Publicado = false };

        Assert.True(f.Administracion.EditarServicio(6, datos, f.PersonalDe(Admin)).Exito);

        Assert.DoesNotContain(f.Prestaciones.ObtenerPublicas(), p => p.Id == 6);
        Assert.NotNull(f.Prestaciones.ObtenerFicha(6));
    }

    [Fact]
    public void Una_tarifa_futura_no_cambia_el_precio_de_hoy_y_no_se_programan_fechas_pasadas()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);

        Assert.False(f.Administracion.ProgramarTarifa(new TarifaFormulario { PrestacionId = 1, Precio = 1500, VigenteDesde = Hoy.AddDays(-1) }, admin).Exito);
        Assert.True(f.Administracion.ProgramarTarifa(new TarifaFormulario { PrestacionId = 1, Precio = 1500, VigenteDesde = Hoy.AddDays(10) }, admin).Exito);

        Assert.Equal(1200m, f.Prestaciones.PrecioVigente(1, Hoy));
        Assert.Equal(1500m, f.Prestaciones.PrecioVigente(1, Hoy.AddDays(10)));
        Assert.Equal(1200m, f.Prestaciones.ObtenerFicha(1)!.PrecioDesde);
    }

    [Fact]
    public void El_cargo_usa_el_precio_vigente_en_la_fecha_del_turno()
    {
        var f = new Fabrica();
        f.Administracion.ProgramarTarifa(new TarifaFormulario { PrestacionId = 1, Precio = 1400, VigenteDesde = Hoy }, f.PersonalDe(Admin));
        var vet = f.PersonalDe(Ferreira);
        var turno = f.TurnoEnEspera();
        f.Consultas.Iniciar(turno.Id, vet);
        var consulta = f.Consultas.ObtenerPorTurno(turno.Id)!;

        f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet);

        Assert.Equal(1400m, f.Cobros.CargoDeConsulta(consulta.Id)!.Monto);
    }

    [Fact]
    public void Una_cuenta_bloqueada_no_ingresa_y_se_puede_desbloquear()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var recepcion = f.PersonalDe(Recepcion);

        Assert.True(f.Administracion.CambiarBloqueo(recepcion.Id, true, admin).Exito);
        Assert.Null(f.Personal.ValidarCredenciales(Recepcion, PersonalService.ContrasenaDemo));

        Assert.True(f.Administracion.CambiarBloqueo(recepcion.Id, false, admin).Exito);
        Assert.NotNull(f.Personal.ValidarCredenciales(Recepcion, PersonalService.ContrasenaDemo));
    }

    [Fact]
    public void El_Administrador_no_se_bloquea_a_si_mismo_ni_deja_la_clinica_sin_Administrador()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);

        Assert.False(f.Administracion.CambiarBloqueo(admin.Id, true, admin).Exito);
        Assert.False(f.Administracion.CambiarRol(admin.Id, PersonalService.Recepcion, 0, admin).Exito);
        Assert.True(admin.Activo);
    }

    [Fact]
    public void No_se_bloquea_a_la_guardia_sin_cambiarla_antes()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var ferreira = f.PersonalDe(Ferreira);

        Assert.False(f.Administracion.CambiarBloqueo(ferreira.Id, true, admin).Exito);

        Assert.True(f.Administracion.CambiarGuardia(f.PersonalDe("silveira@vitalia.uy").Id, admin).Exito);
        Assert.True(f.Administracion.CambiarBloqueo(ferreira.Id, true, admin).Exito);
    }

    [Fact]
    public void Alta_de_veterinario_necesita_una_ficha_libre_y_correo_unico()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);

        Assert.False(f.Administracion.CrearPersonal(new PersonalFormulario { Nombre = "Otra", Correo = "ferreira@vitalia.uy", Rol = PersonalService.Recepcion }, admin).Exito);
        Assert.False(f.Administracion.CrearPersonal(new PersonalFormulario { Nombre = "Dr. Nuevo", Correo = "nuevo@vitalia.uy", Rol = PersonalService.Veterinario, ProfesionalId = 1 }, admin).Exito);
        Assert.True(f.Administracion.CrearPersonal(new PersonalFormulario { Nombre = "Sofía Méndez", Correo = "mendez@vitalia.uy", Rol = PersonalService.Veterinario, ProfesionalId = 5 }, admin).Exito);

        Assert.NotNull(f.Personal.ValidarCredenciales("mendez@vitalia.uy", PersonalService.ContrasenaDemo));
    }

    [Fact]
    public void No_se_carga_una_ausencia_sobre_turnos_activos()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id);
        var dia = f.DiaQueAtiende(1);
        f.Agenda.Reservar(cliente.Id, animal.Id, 1, 1, Fabrica.A(dia, 9), out _);

        var conTurno = f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 1, Desde = dia, Hasta = dia, Motivo = "Congreso" }, admin);
        var libre = f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 1, Desde = Hoy.AddDays(40), Hasta = Hoy.AddDays(42), Motivo = "Congreso" }, admin);

        Assert.False(conTurno.Exito);
        Assert.True(libre.Exito, libre.Mensaje);
        Assert.Contains(f.Profesionales.ObtenerFicha(1)!.Ausencias, a => a.Desde == Hoy.AddDays(40));
    }

    [Fact]
    public void Un_feriado_nuevo_bloquea_ese_dia_en_la_agenda()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);
        var dia = Hoy.AddDays(45);
        while (f.Agenda.EsFeriado(dia))
        {
            dia = dia.AddDays(1); // la semilla ya trae feriados (p. ej. 1 de enero)
        }

        Assert.True(f.Administracion.AgregarFeriado(dia, admin).Exito);
        Assert.True(f.Agenda.EsFeriado(dia));
        Assert.False(f.Administracion.AgregarFeriado(dia, admin).Exito);
        Assert.False(f.Administracion.AgregarFeriado(Hoy.AddDays(-1), admin).Exito);

        Assert.True(f.Administracion.QuitarFeriado(dia, admin).Exito);
        Assert.False(f.Agenda.EsFeriado(dia));
    }

    [Fact]
    public void Un_box_ocupado_no_sale_de_servicio_y_uno_fuera_de_servicio_no_recibe_internaciones()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);

        Assert.False(f.Administracion.CambiarServicioBox(4, false, admin).Exito);
        Assert.True(f.Administracion.CambiarServicioBox(1, false, admin).Exito);

        Assert.DoesNotContain(f.Internaciones.BoxesLibresPara(f.Animales.ObtenerParaPersonal(1)!), b => b.Id == 1);
        Assert.True(f.Administracion.AgregarBox("Box 5", InternacionService.PequenosAnimales, admin).Exito);
        Assert.False(f.Administracion.AgregarBox("box 5", InternacionService.PequenosAnimales, admin).Exito);
    }

    [Fact]
    public void Los_parametros_de_asistencia_cambian_el_costo_y_validan_rangos()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);

        Assert.False(f.Administracion.CambiarParametrosAsistencia(0, 10, 1500, 40, 300, admin).Exito);
        Assert.True(f.Administracion.CambiarParametrosAsistencia(40, 15, 2000, 50, 0, admin).Exito);

        Assert.Equal(40, f.Asistencias.Cobertura);
        Assert.Equal(2000m + 50m * 10, f.Asistencias.Costo(AsistenciaService.Visita, 10, 3));
    }

    [Fact]
    public void Los_dias_de_aviso_de_refuerzo_van_de_3_a_5()
    {
        var f = new Fabrica();
        var admin = f.PersonalDe(Admin);

        Assert.False(f.Administracion.CambiarDiasAvisos(30, 7, admin).Exito);
        Assert.True(f.Administracion.CambiarDiasAvisos(15, 3, admin).Exito);

        Assert.Equal(15, f.Inventario.DiasProximoAVencer);
        Assert.Equal(3, f.Vacunas.DiasAvisoRefuerzo);
    }

    [Fact]
    public void Publicar_una_version_del_consentimiento_queda_en_la_bitacora()
    {
        var f = new Fabrica();

        Assert.True(f.Administracion.PublicarConsentimiento(8, "1.1", "Texto nuevo.", f.PersonalDe(Admin)).Exito);

        Assert.Equal("1.1", f.Consentimientos.DocumentoVigente(8)!.Version);
        Assert.Single(f.Auditoria.Buscar(entidad: "Consentimiento"));
    }
}
