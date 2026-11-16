using System;
using System.Linq;
using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;
using Xunit;

namespace Veterinaria.Tests.CajaNegra;

// Pruebas de caja negra de Configuración y administración, parte 1 (A-027).
// Escritas solo a partir de docs/analisis/ESPECIFICACION_ADMINISTRACION.md, los modelos y las firmas públicas.
public class AdministracionCajaNegraTests
{
    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);

    private static Personal Admin(Fabrica f) => f.PersonalDe("admin@vitalia.uy");

    private static PrestacionFormulario Servicio(string nombre = "Baño medicado", int precio = 1500, int duracion = 30, bool reservable = false)
    {
        return new PrestacionFormulario
        {
            Nombre = nombre,
            Descripcion = "Servicio de prueba",
            Precio = precio,
            DuracionMinutos = duracion,
            ReservableEnLinea = reservable,
            Publicado = true
        };
    }

    // rol null = Recepción (no se usa la constante como valor por defecto por si no es const).
    private static PersonalFormulario NuevaCuenta(string correo = "nuevo@vitalia.uy", string? rol = null, int profesionalId = 0)
    {
        return new PersonalFormulario
        {
            Nombre = "Persona Nueva",
            Correo = correo,
            Rol = rol ?? PersonalService.Recepcion,
            ProfesionalId = profesionalId
        };
    }

    private static decimal Precio(Fabrica f, int prestacionId, DateOnly fecha)
    {
        return Convert.ToDecimal(f.Prestaciones.PrecioVigente(prestacionId, fecha));
    }

    // Primer día, desde hoy + desdeDias, que no es feriado.
    private static DateOnly DiaLejanoSinFeriado(Fabrica f, int desdeDias = 40)
    {
        var dia = Hoy.AddDays(desdeDias);
        while (f.Agenda.EsFeriado(dia))
        {
            dia = dia.AddDays(1);
        }
        return dia;
    }

    // Reserva un turno Confirmado con la Dra. Ferreira (profesional 1) en el primer día en que atiende.
    private static Turno TurnoReservadoFerreira(Fabrica f)
    {
        var cliente = f.ClienteVerificado();
        var animal = f.AnimalDe(cliente.Id);
        var dia = f.DiaQueAtiende(1);

        for (var hora = 7; hora <= 20; hora++)
        {
            foreach (var minuto in new[] { 0, 30 })
            {
                f.Agenda.Reservar(cliente.Id, animal.Id, 1, 1, Fabrica.A(dia, hora, minuto), out Turno? turno);
                if (turno != null)
                {
                    return turno;
                }
            }
        }

        throw new InvalidOperationException("No se pudo reservar un turno para la prueba.");
    }

    // ---------- AD-01: solo un Administrador activo ----------

    [Fact]
    public void AD01_Recepcion_no_puede_ejecutar_ninguna_accion_ni_cambia_nada()
    {
        var f = new Fabrica();
        var recepcion = f.PersonalDe("recepcion@vitalia.uy");
        var cantidadServicios = f.Prestaciones.ObtenerTodas().Count();
        var cantidadPersonal = f.Personal.ObtenerTodos().Count();
        var registros = f.Auditoria.Buscar().Count;

        Assert.False(f.Administracion.CrearServicio(Servicio(), recepcion).Exito);
        Assert.False(f.Administracion.CrearPersonal(NuevaCuenta(), recepcion).Exito);
        Assert.False(f.Administracion.CambiarBloqueo(3, true, recepcion).Exito);
        Assert.False(f.Administracion.AgregarFeriado(DiaLejanoSinFeriado(f), recepcion).Exito);
        Assert.False(f.Administracion.CambiarDiasAvisos(30, 4, recepcion).Exito);

        Assert.Equal(cantidadServicios, f.Prestaciones.ObtenerTodas().Count());
        Assert.Equal(cantidadPersonal, f.Personal.ObtenerTodos().Count());
        Assert.True(f.Personal.ObtenerPorId(3)!.Activo);
        Assert.Equal(registros, f.Auditoria.Buscar().Count);
    }

    [Fact]
    public void AD01_Veterinario_no_puede_programar_tarifa_ni_cambiar_guardia()
    {
        var f = new Fabrica();
        var vet = f.PersonalDe("ferreira@vitalia.uy");
        var guardiaAntes = f.Asistencias.GuardiaId;
        var tarifasAntes = f.Prestaciones.TarifasDe(1).Count();

        var tarifa = f.Administracion.ProgramarTarifa(new TarifaFormulario { PrestacionId = 1, Precio = 5000, VigenteDesde = Hoy }, vet);
        var guardia = f.Administracion.CambiarGuardia(4, vet);

        Assert.False(tarifa.Exito);
        Assert.False(guardia.Exito);
        Assert.Equal(1200m, Precio(f, 1, Hoy));
        Assert.Equal(tarifasAntes, f.Prestaciones.TarifasDe(1).Count());
        Assert.Equal(guardiaAntes, f.Asistencias.GuardiaId);
    }

    [Fact]
    public void AD01_Sin_usuario_null_se_rechaza()
    {
        var f = new Fabrica();
        var cantidad = f.Internaciones.Boxes().Count();

        Assert.False(f.Administracion.AgregarBox("Box nuevo", "Pequeños animales", null).Exito);
        Assert.False(f.Administracion.CrearServicio(Servicio(), null).Exito);
        Assert.Equal(cantidad, f.Internaciones.Boxes().Count());
    }

    [Fact]
    public void AD01_Administrador_bloqueado_no_puede_actuar()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        Assert.True(f.Administracion.CrearPersonal(NuevaCuenta("admin2@vitalia.uy", PersonalService.Administrador), admin).Exito);
        var admin2 = f.Personal.ObtenerTodos().First(p => p.Correo == "admin2@vitalia.uy");
        Assert.True(f.Administracion.CambiarBloqueo(admin2.Id, true, admin).Exito);

        var bloqueado = f.Personal.ObtenerPorId(admin2.Id)!;
        var resultado = f.Administracion.CrearServicio(Servicio("Servicio del bloqueado"), bloqueado);

        Assert.False(resultado.Exito);
        Assert.DoesNotContain(f.Prestaciones.ObtenerTodas(), p => p.Nombre == "Servicio del bloqueado");
    }

    // ---------- AD-02: bitácora ----------

    [Fact]
    public void AD02_Accion_exitosa_deja_un_registro_y_la_rechazada_no()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var antes = f.Auditoria.Buscar().Count;

        Assert.True(f.Administracion.CrearServicio(Servicio("Servicio auditado"), admin).Exito);
        var despuesExito = f.Auditoria.Buscar().Count;
        Assert.Equal(antes + 1, despuesExito);

        var ultimo = f.Auditoria.Buscar().OrderBy(r => r.Id).Last();
        Assert.False(string.IsNullOrWhiteSpace(ultimo.Usuario));
        Assert.False(string.IsNullOrWhiteSpace(ultimo.Rol));
        Assert.False(string.IsNullOrWhiteSpace(ultimo.Entidad));
        Assert.False(string.IsNullOrWhiteSpace(ultimo.Accion));

        Assert.False(f.Administracion.CrearServicio(Servicio("Servicio auditado"), admin).Exito);
        Assert.Equal(despuesExito, f.Auditoria.Buscar().Count);
    }

    // ---------- SV-01 / SV-02: alta de servicio ----------

    [Fact]
    public void SV01_Alta_valida_aparece_en_el_catalogo_con_su_precio()
    {
        var f = new Fabrica();

        var resultado = f.Administracion.CrearServicio(Servicio("Baño medicado", 1500, 30, true), Admin(f));

        Assert.True(resultado.Exito);
        var nuevo = f.Prestaciones.ObtenerTodas().Single(p => p.Nombre == "Baño medicado");
        Assert.True(nuevo.Publicado);
        Assert.True(nuevo.ReservableEnLinea);
        Assert.Equal(30, nuevo.DuracionMinutos);
        Assert.Equal(1500m, Precio(f, nuevo.Id, Hoy));
        Assert.Contains(f.Prestaciones.ObtenerPublicas(), p => p.Id == nuevo.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void SV01_Nombre_vacio_o_solo_espacios_se_rechaza(string nombre)
    {
        var f = new Fabrica();
        var cantidad = f.Prestaciones.ObtenerTodas().Count();

        Assert.False(f.Administracion.CrearServicio(Servicio(nombre), Admin(f)).Exito);
        Assert.Equal(cantidad, f.Prestaciones.ObtenerTodas().Count());
    }

    [Fact]
    public void SV01_Nombre_de_80_se_acepta_y_de_81_se_rechaza()
    {
        var f = new Fabrica();
        var admin = Admin(f);

        Assert.True(f.Administracion.CrearServicio(Servicio(new string('a', 80)), admin).Exito);
        Assert.False(f.Administracion.CrearServicio(Servicio(new string('b', 81)), admin).Exito);
        Assert.DoesNotContain(f.Prestaciones.ObtenerTodas(), p => p.Nombre == new string('b', 81));
    }

    [Fact]
    public void SV01_Nombre_repetido_sin_importar_mayusculas_se_rechaza()
    {
        var f = new Fabrica();
        var existente = f.Prestaciones.ObtenerTodas().First(p => p.Id == 1).Nombre;
        var cantidad = f.Prestaciones.ObtenerTodas().Count();

        Assert.False(f.Administracion.CrearServicio(Servicio(existente.ToUpperInvariant()), Admin(f)).Exito);
        Assert.False(f.Administracion.CrearServicio(Servicio(existente), Admin(f)).Exito);
        Assert.Equal(cantidad, f.Prestaciones.ObtenerTodas().Count());
    }

    [Fact]
    public void SV01_Descripcion_de_301_se_rechaza_y_de_300_se_acepta()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var largo = Servicio("Con descripción larga");
        largo.Descripcion = new string('d', 301);
        var justo = Servicio("Con descripción justa");
        justo.Descripcion = new string('d', 300);

        Assert.False(f.Administracion.CrearServicio(largo, admin).Exito);
        Assert.True(f.Administracion.CrearServicio(justo, admin).Exito);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(1000000, true)]
    [InlineData(1000001, false)]
    public void SV01_Bordes_del_precio(int precio, bool esperado)
    {
        var f = new Fabrica();

        var resultado = f.Administracion.CrearServicio(Servicio("Servicio de precio " + precio, precio), Admin(f));

        Assert.Equal(esperado, resultado.Exito);
        Assert.Equal(esperado, f.Prestaciones.ObtenerTodas().Any(p => p.Nombre == "Servicio de precio " + precio));
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(480, true)]
    [InlineData(481, false)]
    public void SV01_Bordes_de_la_duracion_sin_reserva_en_linea(int duracion, bool esperado)
    {
        var f = new Fabrica();

        var resultado = f.Administracion.CrearServicio(Servicio("Servicio de duración " + duracion, 1000, duracion, false), Admin(f));

        Assert.Equal(esperado, resultado.Exito);
    }

    [Theory]
    [InlineData(14, false)]
    [InlineData(15, true)]
    [InlineData(0, false)]
    public void SV02_Reserva_en_linea_exige_al_menos_15_minutos(int duracion, bool esperado)
    {
        var f = new Fabrica();
        var nombre = "Reservable de " + duracion;

        var resultado = f.Administracion.CrearServicio(Servicio(nombre, 1000, duracion, true), Admin(f));

        Assert.Equal(esperado, resultado.Exito);
        Assert.Equal(esperado, f.Prestaciones.ObtenerTodas().Any(p => p.Nombre == nombre));
    }

    // ---------- SV-03: edición ----------

    [Fact]
    public void SV03_Editar_cambia_los_datos_pero_no_el_precio()
    {
        var f = new Fabrica();
        var datos = Servicio("Consulta general renombrada", 99999, 40, true);
        datos.Descripcion = "Descripción nueva";

        var resultado = f.Administracion.EditarServicio(1, datos, Admin(f));

        Assert.True(resultado.Exito);
        var editado = f.Prestaciones.ObtenerTodas().First(p => p.Id == 1);
        Assert.Equal("Consulta general renombrada", editado.Nombre);
        Assert.Equal("Descripción nueva", editado.Descripcion);
        Assert.Equal(40, editado.DuracionMinutos);
        Assert.Equal(1200m, Precio(f, 1, Hoy));
    }

    [Fact]
    public void SV03_Despublicar_lo_saca_del_catalogo()
    {
        var f = new Fabrica();
        var actual = f.Prestaciones.ObtenerTodas().First(p => p.Id == 1);
        var datos = Servicio(actual.Nombre, 1200, actual.DuracionMinutos, actual.ReservableEnLinea);
        datos.Publicado = false;

        Assert.True(f.Administracion.EditarServicio(1, datos, Admin(f)).Exito);

        Assert.False(f.Prestaciones.ObtenerTodas().First(p => p.Id == 1).Publicado);
        Assert.DoesNotContain(f.Prestaciones.ObtenerPublicas(), p => p.Id == 1);
    }

    [Fact]
    public void SV03_Despublicar_no_cancela_los_turnos_ya_reservados()
    {
        var f = new Fabrica();
        var turno = TurnoReservadoFerreira(f);
        var actual = f.Prestaciones.ObtenerTodas().First(p => p.Id == 1);
        var datos = Servicio(actual.Nombre, 1200, actual.DuracionMinutos, actual.ReservableEnLinea);
        datos.Publicado = false;

        Assert.True(f.Administracion.EditarServicio(1, datos, Admin(f)).Exito);

        Assert.Equal("Confirmado", turno.Estado);
        Assert.Equal(1, turno.PrestacionId);
    }

    [Fact]
    public void SV03_Editar_con_el_nombre_de_otro_servicio_se_rechaza_y_no_cambia_nada()
    {
        var f = new Fabrica();
        var otro = f.Prestaciones.ObtenerTodas().First(p => p.Id == 8).Nombre;
        var original = f.Prestaciones.ObtenerTodas().First(p => p.Id == 1).Nombre;

        var resultado = f.Administracion.EditarServicio(1, Servicio(otro.ToLowerInvariant(), 1200, 30, true), Admin(f));

        Assert.False(resultado.Exito);
        Assert.Equal(original, f.Prestaciones.ObtenerTodas().First(p => p.Id == 1).Nombre);
    }

    [Fact]
    public void SV03_Editar_un_servicio_inexistente_se_rechaza()
    {
        var f = new Fabrica();

        Assert.False(f.Administracion.EditarServicio(9999, Servicio("Fantasma"), Admin(f)).Exito);
        Assert.DoesNotContain(f.Prestaciones.ObtenerTodas(), p => p.Nombre == "Fantasma");
    }

    // ---------- SV-04 / SV-05: tarifas ----------

    [Fact]
    public void SV04_Tarifa_de_hoy_cambia_el_precio_vigente_hoy()
    {
        var f = new Fabrica();

        var resultado = f.Administracion.ProgramarTarifa(new TarifaFormulario { PrestacionId = 1, Precio = 1500, VigenteDesde = Hoy }, Admin(f));

        Assert.True(resultado.Exito);
        Assert.Equal(1500m, Precio(f, 1, Hoy));
    }

    [Fact]
    public void SV05_Tarifa_futura_no_cambia_el_precio_de_hoy_y_rige_desde_su_fecha()
    {
        var f = new Fabrica();
        var desde = Hoy.AddDays(10);

        Assert.True(f.Administracion.ProgramarTarifa(new TarifaFormulario { PrestacionId = 1, Precio = 1800, VigenteDesde = desde }, Admin(f)).Exito);

        Assert.Equal(1200m, Precio(f, 1, Hoy));
        Assert.Equal(1200m, Precio(f, 1, desde.AddDays(-1)));
        Assert.Equal(1800m, Precio(f, 1, desde));
        Assert.Equal(1800m, Precio(f, 1, desde.AddDays(30)));
    }

    [Fact]
    public void SV04_Tarifa_con_fecha_pasada_se_rechaza_y_no_cambia_nada()
    {
        var f = new Fabrica();
        var antes = f.Prestaciones.TarifasDe(1).Count();

        var resultado = f.Administracion.ProgramarTarifa(new TarifaFormulario { PrestacionId = 1, Precio = 1500, VigenteDesde = Hoy.AddDays(-1) }, Admin(f));

        Assert.False(resultado.Exito);
        Assert.Equal(antes, f.Prestaciones.TarifasDe(1).Count());
        Assert.Equal(1200m, Precio(f, 1, Hoy));
    }

    [Fact]
    public void SV04_Tarifa_para_la_misma_fecha_reemplaza_a_la_anterior()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var desde = Hoy.AddDays(5);

        Assert.True(f.Administracion.ProgramarTarifa(new TarifaFormulario { PrestacionId = 1, Precio = 1500, VigenteDesde = desde }, admin).Exito);
        Assert.True(f.Administracion.ProgramarTarifa(new TarifaFormulario { PrestacionId = 1, Precio = 1700, VigenteDesde = desde }, admin).Exito);

        Assert.Equal(1, f.Prestaciones.TarifasDe(1).Count(t => t.VigenteDesde == desde));
        Assert.Equal(1700m, Precio(f, 1, desde));
    }

    [Fact]
    public void SV04_Tarifa_sin_fecha_con_precio_fuera_de_rango_o_servicio_inexistente_se_rechaza()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var antes = f.Prestaciones.TarifasDe(1).Count();

        Assert.False(f.Administracion.ProgramarTarifa(new TarifaFormulario { PrestacionId = 1, Precio = 1500, VigenteDesde = null }, admin).Exito);
        Assert.False(f.Administracion.ProgramarTarifa(new TarifaFormulario { PrestacionId = 1, Precio = 0, VigenteDesde = Hoy }, admin).Exito);
        Assert.False(f.Administracion.ProgramarTarifa(new TarifaFormulario { PrestacionId = 1, Precio = 1000001, VigenteDesde = Hoy }, admin).Exito);
        Assert.False(f.Administracion.ProgramarTarifa(new TarifaFormulario { PrestacionId = 9999, Precio = 1500, VigenteDesde = Hoy }, admin).Exito);

        Assert.Equal(antes, f.Prestaciones.TarifasDe(1).Count());
        Assert.Equal(1200m, Precio(f, 1, Hoy));
    }

    // ---------- US-01 / US-02: alta de cuentas ----------

    [Fact]
    public void US01_Alta_de_recepcion_puede_ingresar_con_la_contrasena_de_demostracion()
    {
        var f = new Fabrica();

        var resultado = f.Administracion.CrearPersonal(NuevaCuenta("nueva.recepcion@vitalia.uy"), Admin(f));

        Assert.True(resultado.Exito);
        var cuenta = f.Personal.ObtenerTodos().Single(p => p.Correo == "nueva.recepcion@vitalia.uy");
        Assert.Equal(PersonalService.Recepcion, cuenta.Rol);
        Assert.True(cuenta.Activo);
        Assert.NotNull(f.Personal.ValidarCredenciales("nueva.recepcion@vitalia.uy", PersonalService.ContrasenaDemo));
    }

    [Fact]
    public void US01_Correo_repetido_o_invalido_se_rechaza_y_no_crea_cuentas()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var cantidad = f.Personal.ObtenerTodos().Count();

        Assert.False(f.Administracion.CrearPersonal(NuevaCuenta("recepcion@vitalia.uy"), admin).Exito);
        Assert.False(f.Administracion.CrearPersonal(NuevaCuenta("no-es-un-correo"), admin).Exito);
        Assert.False(f.Administracion.CrearPersonal(NuevaCuenta(""), admin).Exito);

        Assert.Equal(cantidad, f.Personal.ObtenerTodos().Count());
    }

    [Fact]
    public void US01_Nombre_vacio_o_de_81_se_rechaza_y_de_80_se_acepta()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var vacio = NuevaCuenta("vacio@vitalia.uy");
        vacio.Nombre = "   ";
        var largo = NuevaCuenta("largo@vitalia.uy");
        largo.Nombre = new string('n', 81);
        var justo = NuevaCuenta("justo@vitalia.uy");
        justo.Nombre = new string('n', 80);

        Assert.False(f.Administracion.CrearPersonal(vacio, admin).Exito);
        Assert.False(f.Administracion.CrearPersonal(largo, admin).Exito);
        Assert.True(f.Administracion.CrearPersonal(justo, admin).Exito);
        Assert.DoesNotContain(f.Personal.ObtenerTodos(), p => p.Correo == "largo@vitalia.uy" || p.Correo == "vacio@vitalia.uy");
    }

    [Theory]
    [InlineData("Cliente")]
    [InlineData("")]
    [InlineData("Superusuario")]
    public void US01_Rol_desconocido_se_rechaza(string rol)
    {
        var f = new Fabrica();

        Assert.False(f.Administracion.CrearPersonal(NuevaCuenta("rol.raro@vitalia.uy", rol), Admin(f)).Exito);
        Assert.DoesNotContain(f.Personal.ObtenerTodos(), p => p.Correo == "rol.raro@vitalia.uy");
    }

    [Fact]
    public void US02_Veterinario_con_ficha_libre_se_crea_asociado_a_ella()
    {
        var f = new Fabrica();

        var resultado = f.Administracion.CrearPersonal(NuevaCuenta("mendez@vitalia.uy", PersonalService.Veterinario, 5), Admin(f));

        Assert.True(resultado.Exito);
        var cuenta = f.Personal.ObtenerTodos().Single(p => p.Correo == "mendez@vitalia.uy");
        Assert.Equal(PersonalService.Veterinario, cuenta.Rol);
        Assert.Equal(5, cuenta.ProfesionalId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(9999)]
    public void US02_Veterinario_sin_ficha_con_ficha_ocupada_o_inexistente_se_rechaza(int profesionalId)
    {
        var f = new Fabrica();
        var cantidad = f.Personal.ObtenerTodos().Count();

        Assert.False(f.Administracion.CrearPersonal(NuevaCuenta("vet.nuevo@vitalia.uy", PersonalService.Veterinario, profesionalId), Admin(f)).Exito);
        Assert.Equal(cantidad, f.Personal.ObtenerTodos().Count());
    }

    [Fact]
    public void US02_Cambiar_rol_a_veterinario_exige_ficha_libre()
    {
        var f = new Fabrica();
        var admin = Admin(f);

        Assert.False(f.Administracion.CambiarRol(1, PersonalService.Veterinario, 1, admin).Exito);
        Assert.Equal(PersonalService.Recepcion, f.Personal.ObtenerPorId(1)!.Rol);

        Assert.True(f.Administracion.CambiarRol(1, PersonalService.Veterinario, 5, admin).Exito);
        Assert.Equal(PersonalService.Veterinario, f.Personal.ObtenerPorId(1)!.Rol);
        Assert.Equal(5, f.Personal.ObtenerPorId(1)!.ProfesionalId);
    }

    // ---------- US-03: bloqueo ----------

    [Fact]
    public void US03_Cuenta_bloqueada_no_ingresa_y_al_desbloquear_vuelve_a_ingresar()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var cantidad = f.Personal.ObtenerTodos().Count();

        Assert.True(f.Administracion.CambiarBloqueo(1, true, admin).Exito);
        Assert.False(f.Personal.ObtenerPorId(1)!.Activo);
        Assert.Null(f.Personal.ValidarCredenciales("recepcion@vitalia.uy", PersonalService.ContrasenaDemo));
        Assert.False(f.Sesion.Ingresar(new InicioSesion { Correo = "recepcion@vitalia.uy", Contrasena = PersonalService.ContrasenaDemo }).Exito);
        Assert.Equal(cantidad, f.Personal.ObtenerTodos().Count());

        Assert.True(f.Administracion.CambiarBloqueo(1, false, admin).Exito);
        Assert.True(f.Personal.ObtenerPorId(1)!.Activo);
        Assert.NotNull(f.Personal.ValidarCredenciales("recepcion@vitalia.uy", PersonalService.ContrasenaDemo));
    }

    [Fact]
    public void US03_Bloquear_una_cuenta_inexistente_se_rechaza()
    {
        var f = new Fabrica();

        Assert.False(f.Administracion.CambiarBloqueo(9999, true, Admin(f)).Exito);
    }

    // ---------- US-04: el Administrador no se toca a sí mismo ----------

    [Fact]
    public void US04_Administrador_no_se_bloquea_ni_se_cambia_el_rol_a_si_mismo()
    {
        var f = new Fabrica();
        var admin = Admin(f);

        Assert.False(f.Administracion.CambiarBloqueo(admin.Id, true, admin).Exito);
        Assert.False(f.Administracion.CambiarRol(admin.Id, PersonalService.Recepcion, 0, admin).Exito);

        var actual = f.Personal.ObtenerPorId(admin.Id)!;
        Assert.True(actual.Activo);
        Assert.Equal(PersonalService.Administrador, actual.Rol);
        Assert.NotNull(f.Personal.ValidarCredenciales("admin@vitalia.uy", PersonalService.ContrasenaDemo));
    }

    [Fact]
    public void US04_Con_un_segundo_administrador_se_puede_bloquear_al_primero_y_queda_uno_activo()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        Assert.True(f.Administracion.CrearPersonal(NuevaCuenta("admin2@vitalia.uy", PersonalService.Administrador), admin).Exito);
        var admin2 = f.PersonalDe("admin2@vitalia.uy");

        Assert.True(f.Administracion.CambiarBloqueo(admin.Id, true, admin2).Exito);

        Assert.Contains(f.Personal.ObtenerTodos(), p => p.Rol == PersonalService.Administrador && p.Activo);
        // admin2 es ahora el único activo: no puede bloquearse a sí mismo.
        Assert.False(f.Administracion.CambiarBloqueo(admin2.Id, true, admin2).Exito);
        Assert.True(f.Personal.ObtenerPorId(admin2.Id)!.Activo);
    }

    // ---------- US-05 / PA-01: guardia ----------

    [Fact]
    public void US05_No_se_bloquea_al_veterinario_de_guardia()
    {
        var f = new Fabrica();

        var resultado = f.Administracion.CambiarBloqueo(2, true, Admin(f));

        Assert.False(resultado.Exito);
        Assert.True(f.Personal.ObtenerPorId(2)!.Activo);
    }

    [Fact]
    public void US05_PA01_Tras_cambiar_la_guardia_se_puede_bloquear_al_anterior()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var guardiaAntes = f.Asistencias.GuardiaId;

        Assert.True(f.Administracion.CambiarGuardia(4, admin).Exito);
        Assert.NotEqual(guardiaAntes, f.Asistencias.GuardiaId);

        Assert.True(f.Administracion.CambiarBloqueo(2, true, admin).Exito);
        Assert.False(f.Personal.ObtenerPorId(2)!.Activo);
    }

    [Fact]
    public void PA01_Guardia_solo_a_un_veterinario_activo()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var guardiaAntes = f.Asistencias.GuardiaId;
        Assert.True(f.Administracion.CambiarBloqueo(4, true, admin).Exito);

        Assert.False(f.Administracion.CambiarGuardia(1, admin).Exito);    // Recepción
        Assert.False(f.Administracion.CambiarGuardia(5, admin).Exito);    // Administrador
        Assert.False(f.Administracion.CambiarGuardia(4, admin).Exito);    // Veterinario bloqueado
        Assert.False(f.Administracion.CambiarGuardia(9999, admin).Exito); // Inexistente

        Assert.Equal(guardiaAntes, f.Asistencias.GuardiaId);
    }

    // ---------- HO-01 / HO-02 / HO-03: ausencias y feriados ----------

    [Fact]
    public void HO01_Ausencia_valida_se_agrega_y_al_quitarla_desaparece()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var desde = Hoy.AddDays(40);
        var hasta = Hoy.AddDays(42);

        var resultado = f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 3, Desde = desde, Hasta = hasta, Motivo = "Congreso" }, admin);

        Assert.True(resultado.Exito);
        Assert.Contains(f.Profesionales.ObtenerPorId(3)!.Ausencias, a => a.Desde == desde && a.Hasta == hasta);

        Assert.True(f.Administracion.QuitarAusencia(3, desde, admin).Exito);
        Assert.DoesNotContain(f.Profesionales.ObtenerPorId(3)!.Ausencias, a => a.Desde == desde);
    }

    [Fact]
    public void HO01_Ausencia_de_un_solo_dia_hasta_igual_a_desde_se_acepta()
    {
        var f = new Fabrica();
        var dia = Hoy.AddDays(45);

        Assert.True(f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 3, Desde = dia, Hasta = dia, Motivo = "Trámite" }, Admin(f)).Exito);
        Assert.Contains(f.Profesionales.ObtenerPorId(3)!.Ausencias, a => a.Desde == dia && a.Hasta == dia);
    }

    [Fact]
    public void HO01_Ausencias_invalidas_se_rechazan_y_no_cambian_nada()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var antes3 = f.Profesionales.ObtenerPorId(3)!.Ausencias.Count;
        var antes2 = f.Profesionales.ObtenerPorId(2)!.Ausencias.Count;

        // Desde en el pasado.
        Assert.False(f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 3, Desde = Hoy.AddDays(-1), Hasta = Hoy.AddDays(40), Motivo = "Licencia" }, admin).Exito);
        // Hasta antes que desde.
        Assert.False(f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 3, Desde = Hoy.AddDays(42), Hasta = Hoy.AddDays(40), Motivo = "Licencia" }, admin).Exito);
        // Motivo vacío, solo espacios y de 101.
        Assert.False(f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 3, Desde = Hoy.AddDays(40), Hasta = Hoy.AddDays(41), Motivo = "" }, admin).Exito);
        Assert.False(f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 3, Desde = Hoy.AddDays(40), Hasta = Hoy.AddDays(41), Motivo = "   " }, admin).Exito);
        Assert.False(f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 3, Desde = Hoy.AddDays(40), Hasta = Hoy.AddDays(41), Motivo = new string('m', 101) }, admin).Exito);
        // Fecha sin completar y profesional inexistente.
        Assert.False(f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 3, Desde = null, Hasta = Hoy.AddDays(41), Motivo = "Licencia" }, admin).Exito);
        Assert.False(f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 9999, Desde = Hoy.AddDays(40), Hasta = Hoy.AddDays(41), Motivo = "Licencia" }, admin).Exito);
        // Superpuesta con la ausencia de Silveira (hoy+18 a hoy+22), tocando solo el último día.
        Assert.False(f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 2, Desde = Hoy.AddDays(22), Hasta = Hoy.AddDays(25), Motivo = "Licencia" }, admin).Exito);

        Assert.Equal(antes3, f.Profesionales.ObtenerPorId(3)!.Ausencias.Count);
        Assert.Equal(antes2, f.Profesionales.ObtenerPorId(2)!.Ausencias.Count);
    }

    [Fact]
    public void HO01_Motivo_de_100_caracteres_se_acepta()
    {
        var f = new Fabrica();

        Assert.True(f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 3, Desde = Hoy.AddDays(50), Hasta = Hoy.AddDays(51), Motivo = new string('m', 100) }, Admin(f)).Exito);
    }

    [Fact]
    public void HO02_No_se_carga_ausencia_ni_feriado_sobre_un_turno_confirmado()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var turno = TurnoReservadoFerreira(f);
        var dia = DateOnly.FromDateTime(turno.Inicio);
        var ausenciasAntes = f.Profesionales.ObtenerPorId(1)!.Ausencias.Count;

        var ausencia = f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 1, Desde = dia, Hasta = dia, Motivo = "Licencia" }, admin);
        var feriado = f.Administracion.AgregarFeriado(dia, admin);

        Assert.False(ausencia.Exito);
        Assert.False(feriado.Exito);
        Assert.Equal(ausenciasAntes, f.Profesionales.ObtenerPorId(1)!.Ausencias.Count);
        Assert.False(f.Agenda.EsFeriado(dia));
        Assert.Equal("Confirmado", turno.Estado);
    }

    [Fact]
    public void HO02_No_se_carga_ausencia_sobre_un_turno_en_espera()
    {
        var f = new Fabrica();
        var turno = f.TurnoEnEspera();
        var dia = DateOnly.FromDateTime(turno.Inicio);

        var resultado = f.Administracion.AgregarAusencia(new AusenciaFormulario { ProfesionalId = 1, Desde = dia, Hasta = dia, Motivo = "Licencia" }, Admin(f));

        Assert.False(resultado.Exito);
        Assert.DoesNotContain(f.Profesionales.ObtenerPorId(1)!.Ausencias, a => a.Desde <= dia && a.Hasta >= dia);
    }

    [Fact]
    public void HO03_Feriado_nuevo_se_agrega_y_al_quitarlo_se_habilita_el_dia()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var dia = DiaLejanoSinFeriado(f);

        Assert.True(f.Administracion.AgregarFeriado(dia, admin).Exito);
        Assert.True(f.Agenda.EsFeriado(dia));

        Assert.True(f.Administracion.QuitarFeriado(dia, admin).Exito);
        Assert.False(f.Agenda.EsFeriado(dia));
    }

    [Fact]
    public void HO03_Feriado_repetido_pasado_o_sin_fecha_se_rechaza()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var dia = DiaLejanoSinFeriado(f);
        Assert.True(f.Administracion.AgregarFeriado(dia, admin).Exito);
        var cantidad = f.Agenda.Feriados().Count();

        Assert.False(f.Administracion.AgregarFeriado(dia, admin).Exito);
        Assert.False(f.Administracion.AgregarFeriado(Hoy.AddDays(-1), admin).Exito);
        Assert.False(f.Administracion.AgregarFeriado(null, admin).Exito);

        Assert.Equal(cantidad, f.Agenda.Feriados().Count());
        Assert.False(f.Agenda.EsFeriado(Hoy.AddDays(-1)));
    }

    // ---------- PA-02 / PA-03 / PA-04: parámetros ----------

    [Fact]
    public void PA02_Parametros_de_asistencia_validos_y_en_los_bordes_se_guardan()
    {
        var f = new Fabrica();
        var admin = Admin(f);

        Assert.True(f.Administracion.CambiarParametrosAsistencia(200, 60, 100000, 10000, 10000, admin).Exito);
        Assert.Equal(200m, Convert.ToDecimal(f.Asistencias.Cobertura));
        Assert.Equal(60m, Convert.ToDecimal(f.Asistencias.MinutosEscalarRojo));
        Assert.Equal(100000m, Convert.ToDecimal(f.Asistencias.Base));
        Assert.Equal(10000m, Convert.ToDecimal(f.Asistencias.PorKm));
        Assert.Equal(10000m, Convert.ToDecimal(f.Asistencias.PorAnimalExtra));

        Assert.True(f.Administracion.CambiarParametrosAsistencia(1, 1, 0, 0, 0, admin).Exito);
        Assert.Equal(1m, Convert.ToDecimal(f.Asistencias.Cobertura));
        Assert.Equal(0m, Convert.ToDecimal(f.Asistencias.Base));
    }

    [Theory]
    [InlineData(0, 10, 1000, 50, 200)]
    [InlineData(201, 10, 1000, 50, 200)]
    [InlineData(50, 0, 1000, 50, 200)]
    [InlineData(50, 61, 1000, 50, 200)]
    [InlineData(50, 10, -1, 50, 200)]
    [InlineData(50, 10, 100001, 50, 200)]
    [InlineData(50, 10, 1000, -1, 200)]
    [InlineData(50, 10, 1000, 10001, 200)]
    [InlineData(50, 10, 1000, 50, -1)]
    [InlineData(50, 10, 1000, 50, 10001)]
    public void PA02_Parametros_de_asistencia_fuera_de_rango_se_rechazan_sin_cambiar_nada(int cobertura, int rojo, int tarifaBase, int porKm, int porAnimal)
    {
        var f = new Fabrica();
        var coberturaAntes = Convert.ToDecimal(f.Asistencias.Cobertura);
        var rojoAntes = Convert.ToDecimal(f.Asistencias.MinutosEscalarRojo);
        var baseAntes = Convert.ToDecimal(f.Asistencias.Base);
        var porKmAntes = Convert.ToDecimal(f.Asistencias.PorKm);
        var porAnimalAntes = Convert.ToDecimal(f.Asistencias.PorAnimalExtra);

        Assert.False(f.Administracion.CambiarParametrosAsistencia(cobertura, rojo, tarifaBase, porKm, porAnimal, Admin(f)).Exito);

        Assert.Equal(coberturaAntes, Convert.ToDecimal(f.Asistencias.Cobertura));
        Assert.Equal(rojoAntes, Convert.ToDecimal(f.Asistencias.MinutosEscalarRojo));
        Assert.Equal(baseAntes, Convert.ToDecimal(f.Asistencias.Base));
        Assert.Equal(porKmAntes, Convert.ToDecimal(f.Asistencias.PorKm));
        Assert.Equal(porAnimalAntes, Convert.ToDecimal(f.Asistencias.PorAnimalExtra));
    }

    [Theory]
    [InlineData(1, 3, true)]
    [InlineData(180, 5, true)]
    [InlineData(30, 4, true)]
    [InlineData(0, 4, false)]
    [InlineData(181, 4, false)]
    [InlineData(30, 2, false)]
    [InlineData(30, 6, false)]
    public void PA03_Dias_de_avisos(int diasVencimiento, int diasRefuerzo, bool esperado)
    {
        var f = new Fabrica();
        var vencimientoAntes = Convert.ToDecimal(f.Inventario.DiasProximoAVencer);
        var refuerzoAntes = Convert.ToDecimal(f.Vacunas.DiasAvisoRefuerzo);

        var resultado = f.Administracion.CambiarDiasAvisos(diasVencimiento, diasRefuerzo, Admin(f));

        Assert.Equal(esperado, resultado.Exito);
        if (esperado)
        {
            Assert.Equal((decimal)diasVencimiento, Convert.ToDecimal(f.Inventario.DiasProximoAVencer));
            Assert.Equal((decimal)diasRefuerzo, Convert.ToDecimal(f.Vacunas.DiasAvisoRefuerzo));
        }
        else
        {
            Assert.Equal(vencimientoAntes, Convert.ToDecimal(f.Inventario.DiasProximoAVencer));
            Assert.Equal(refuerzoAntes, Convert.ToDecimal(f.Vacunas.DiasAvisoRefuerzo));
        }
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(100000, true)]
    [InlineData(-1, false)]
    [InlineData(100001, false)]
    public void PA04_Stock_minimo_por_articulo(int minimo, bool esperado)
    {
        var f = new Fabrica();
        var articuloId = f.Inventario.Articulos().First().Id;
        var antes = Convert.ToDecimal(f.Inventario.ObtenerArticulo(articuloId)!.StockMinimo);

        var resultado = f.Administracion.CambiarStockMinimo(articuloId, minimo, Admin(f));

        Assert.Equal(esperado, resultado.Exito);
        var despues = Convert.ToDecimal(f.Inventario.ObtenerArticulo(articuloId)!.StockMinimo);
        Assert.Equal(esperado ? (decimal)minimo : antes, despues);
    }

    [Fact]
    public void PA04_Stock_minimo_de_un_articulo_inexistente_se_rechaza()
    {
        var f = new Fabrica();

        Assert.False(f.Administracion.CambiarStockMinimo(999999, 5, Admin(f)).Exito);
    }

    // ---------- PA-05: boxes ----------

    [Fact]
    public void PA05_Box_nuevo_valido_se_agrega_activo()
    {
        var f = new Fabrica();
        var admin = Admin(f);

        Assert.True(f.Administracion.AgregarBox("Box 9", "Grandes animales", admin).Exito);
        Assert.True(f.Administracion.AgregarBox(new string('x', 40), "Pequeños animales", admin).Exito);

        var nuevo = f.Internaciones.Boxes().Single(b => b.Nombre == "Box 9");
        Assert.Equal("Grandes animales", nuevo.Tipo);
        Assert.True(nuevo.Activo);
    }

    [Fact]
    public void PA05_Box_con_nombre_repetido_vacio_largo_o_tipo_invalido_se_rechaza()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var existente = f.Internaciones.Boxes().First().Nombre;
        var cantidad = f.Internaciones.Boxes().Count();

        Assert.False(f.Administracion.AgregarBox(existente, "Pequeños animales", admin).Exito);
        Assert.False(f.Administracion.AgregarBox("", "Pequeños animales", admin).Exito);
        Assert.False(f.Administracion.AgregarBox("   ", "Pequeños animales", admin).Exito);
        Assert.False(f.Administracion.AgregarBox(new string('x', 41), "Pequeños animales", admin).Exito);
        Assert.False(f.Administracion.AgregarBox("Box reptiles", "Reptiles", admin).Exito);
        Assert.False(f.Administracion.AgregarBox("Box sin tipo", "", admin).Exito);

        Assert.Equal(cantidad, f.Internaciones.Boxes().Count());
    }

    [Fact]
    public void PA05_Box_ocupado_no_se_saca_de_servicio()
    {
        var f = new Fabrica();

        Assert.False(f.Administracion.CambiarServicioBox(4, false, Admin(f)).Exito);
        Assert.True(f.Internaciones.ObtenerBox(4)!.Activo);
    }

    [Fact]
    public void PA05_Box_fuera_de_servicio_no_se_ofrece_y_vuelve_al_reactivarlo()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var cliente = f.ClienteVerificado();
        var perro = f.AnimalDe(cliente.Id);
        Assert.Contains(f.Internaciones.BoxesLibresPara(perro), b => b.Id == 1);

        Assert.True(f.Administracion.CambiarServicioBox(1, false, admin).Exito);
        Assert.False(f.Internaciones.ObtenerBox(1)!.Activo);
        Assert.DoesNotContain(f.Internaciones.BoxesLibresPara(perro), b => b.Id == 1);

        Assert.True(f.Administracion.CambiarServicioBox(1, true, admin).Exito);
        Assert.Contains(f.Internaciones.BoxesLibresPara(perro), b => b.Id == 1);
    }

    // ---------- PA-06: consentimiento ----------

    [Fact]
    public void PA06_Version_nueva_de_consentimiento_queda_vigente()
    {
        var f = new Fabrica();

        var resultado = f.Administracion.PublicarConsentimiento(8, "2.0", "Texto nuevo del consentimiento para castración.", Admin(f));

        Assert.True(resultado.Exito);
        Assert.Equal("2.0", f.Consentimientos.DocumentoVigente(8)?.Version);
    }

    [Fact]
    public void PA06_Version_igual_a_la_vigente_o_texto_vacio_se_rechaza()
    {
        var f = new Fabrica();
        var admin = Admin(f);
        var vigente = f.Consentimientos.DocumentoVigente(8)?.Version;

        Assert.False(f.Administracion.PublicarConsentimiento(8, vigente ?? "1.0", "Texto distinto", admin).Exito);
        Assert.False(f.Administracion.PublicarConsentimiento(8, "2.0", "", admin).Exito);
        Assert.False(f.Administracion.PublicarConsentimiento(8, "2.0", "   ", admin).Exito);
        Assert.False(f.Administracion.PublicarConsentimiento(8, "", "Texto", admin).Exito);

        Assert.Equal(vigente, f.Consentimientos.DocumentoVigente(8)?.Version);
    }
}
