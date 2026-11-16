using System.Text.Json;
using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;
using Xunit;

namespace Veterinaria.Tests.CajaNegra;

// Pruebas de caja negra de A-028 (comunicaciones y derechos del titular).
// Se escribieron solo con ESPECIFICACION_COMUNICACIONES.md, los modelos y las firmas públicas.
public class ComunicacionesDerechosCajaNegraTests
{
    private const string Recepcion = "recepcion@vitalia.uy";
    private const string Veterinario = "ferreira@vitalia.uy";
    private const string Administrador = "admin@vitalia.uy";

    // Cédulas uruguayas con dígito verificador válido.
    private static readonly string[] Cedulas = { "1.234.567-2", "2.345.678-3", "3.456.789-4", "4.567.890-5" };

    // ---------- Ayudas ----------

    private static Cuenta Cliente(Fabrica f, int n = 0)
    {
        return f.ClienteVerificado("cliente" + n + "@prueba.uy", Cedulas[n]);
    }

    private static Personal Inactivo(Fabrica f, string correo)
    {
        var persona = f.PersonalDe(correo);
        persona.Activo = false;
        return persona;
    }

    private static int Cantidad(Fabrica f, Cuenta cuenta, string tipo)
    {
        return f.Comunicaciones.DeCuenta(cuenta.Id).Count(c => c.Tipo == tipo);
    }

    private static int RegistrosBitacora(Fabrica f, string entidad)
    {
        return f.Bitacora.Buscar(f.PersonalDe(Administrador), entidad: entidad).Count;
    }

    // Deja un mensaje por correo en estado Fallido.
    private static Comunicacion MensajeFallido(Fabrica f, Cuenta cuenta, string clave = "")
    {
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.Correo);
        f.Comunicaciones.SimularFalla = true;
        var mensaje = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "Refuerzo", "Cuerpo", clave);
        Assert.NotNull(mensaje);
        for (var i = 0; i < ComunicacionesService.MaxIntentos; i++)
        {
            f.Comunicaciones.ProcesarCola();
        }

        f.Comunicaciones.SimularFalla = false;
        Assert.Equal(ComunicacionesService.Fallido, mensaje!.Estado);
        return mensaje;
    }

    // Convierte en obstáculo un turno de otro cliente reasignándolo al cliente dado (patrón de Fabrica).
    private static Turno TurnoProximoDe(Fabrica f, Cuenta cuenta, Animal animal)
    {
        var turno = f.TurnoEnEspera();
        turno.ClienteId = cuenta.Id;
        turno.AnimalId = animal.Id;
        turno.Estado = "Confirmado";
        turno.Inicio = DateTime.Now.AddDays(2);
        turno.Fin = turno.Inicio.AddMinutes(30);
        return turno;
    }

    private static SolicitudDerecho UltimaSolicitud(Fabrica f, Cuenta cuenta, string tipo)
    {
        return f.Derechos.DeCuenta(cuenta.Id).Last(s => s.Tipo == tipo);
    }

    private static Cuenta Actual(Fabrica f, int id)
    {
        return f.Cuentas.Todas().First(c => c.Id == id);
    }

    // ---------- Catálogo y preferencias (CM-13, D-COM-02, D-COM-03) ----------

    [Fact]
    public void Catalogo_tiene_los_cinco_tipos_y_esenciales_segun_decision()
    {
        var f = new Fabrica();
        var servicio = f.Comunicaciones;

        Assert.NotNull(servicio.ObtenerTipo(ComunicacionesService.ResumenClinico));
        Assert.NotNull(servicio.ObtenerTipo(ComunicacionesService.Comprobante));
        Assert.NotNull(servicio.ObtenerTipo(ComunicacionesService.AvisoOperativo));
        Assert.NotNull(servicio.ObtenerTipo(ComunicacionesService.AvisoRefuerzo));
        Assert.NotNull(servicio.ObtenerTipo(ComunicacionesService.AlertaSanitaria));

        Assert.True(servicio.ObtenerTipo(ComunicacionesService.ResumenClinico)!.Esencial);
        Assert.True(servicio.ObtenerTipo(ComunicacionesService.Comprobante)!.Esencial);
        Assert.True(servicio.ObtenerTipo(ComunicacionesService.AvisoOperativo)!.Esencial);
        Assert.False(servicio.ObtenerTipo(ComunicacionesService.AvisoRefuerzo)!.Esencial);
        Assert.False(servicio.ObtenerTipo(ComunicacionesService.AlertaSanitaria)!.Esencial);
    }

    [Fact]
    public void ObtenerTipo_inexistente_devuelve_null()
    {
        var f = new Fabrica();

        Assert.Null(f.Comunicaciones.ObtenerTipo("Inexistente"));
        Assert.Null(f.Comunicaciones.ObtenerTipo(""));
    }

    [Fact]
    public void Canales_incluye_correo_solo_app_y_no_recibir()
    {
        var f = new Fabrica();

        Assert.Contains(ComunicacionesService.Correo, f.Comunicaciones.Canales);
        Assert.Contains(ComunicacionesService.SoloApp, f.Comunicaciones.Canales);
        Assert.Contains(ComunicacionesService.Ninguno, f.Comunicaciones.Canales);
    }

    [Fact]
    public void MaxIntentos_es_tres()
    {
        Assert.Equal(3, ComunicacionesService.MaxIntentos);
    }

    [Fact]
    public void Preferencias_por_defecto_refuerzo_por_correo_y_alerta_sin_recibir()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        Assert.Equal(ComunicacionesService.Correo, f.Comunicaciones.CanalDe(cuenta.Id, ComunicacionesService.AvisoRefuerzo));
        Assert.Equal(ComunicacionesService.Ninguno, f.Comunicaciones.CanalDe(cuenta.Id, ComunicacionesService.AlertaSanitaria));
    }

    [Fact]
    public void Preferencias_por_defecto_de_los_opcionales_nunca_estan_premarcadas_RN43()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        Assert.Equal(ComunicacionesService.Ninguno, f.Comunicaciones.CanalDe(cuenta.Id, ComunicacionesService.AlertaSanitaria));
    }

    [Fact]
    public void PreferenciasDe_lista_un_canal_por_cada_tipo_del_catalogo()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        var preferencias = f.Comunicaciones.PreferenciasDe(cuenta.Id);

        Assert.Equal(f.Comunicaciones.Tipos.Count, preferencias.Count);
        foreach (var tipo in f.Comunicaciones.Tipos)
        {
            Assert.Contains(preferencias, p => p.Tipo.Nombre == tipo.Nombre);
        }
    }

    [Fact]
    public void CambiarPreferencia_en_un_opcional_a_cada_canal_valido_se_guarda()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        foreach (var canal in new[] { ComunicacionesService.SoloApp, ComunicacionesService.Ninguno, ComunicacionesService.Correo })
        {
            var r = f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, canal);

            Assert.True(r.Exito);
            Assert.Equal(canal, f.Comunicaciones.CanalDe(cuenta.Id, ComunicacionesService.AvisoRefuerzo));
        }
    }

    [Theory]
    [InlineData(ComunicacionesService.Correo)]
    [InlineData(ComunicacionesService.SoloApp)]
    public void CambiarPreferencia_en_un_esencial_acepta_correo_y_solo_app(string canal)
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        var r = f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.ResumenClinico, canal);

        Assert.True(r.Exito);
        Assert.Equal(canal, f.Comunicaciones.CanalDe(cuenta.Id, ComunicacionesService.ResumenClinico));
    }

    [Theory]
    [InlineData(ComunicacionesService.ResumenClinico)]
    [InlineData(ComunicacionesService.Comprobante)]
    [InlineData(ComunicacionesService.AvisoOperativo)]
    public void CambiarPreferencia_en_un_esencial_rechaza_no_recibir_y_no_cambia_nada(string tipo)
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, tipo, ComunicacionesService.SoloApp);

        var r = f.Comunicaciones.CambiarPreferencia(cuenta.Id, tipo, ComunicacionesService.Ninguno);

        Assert.False(r.Exito);
        Assert.Equal(ComunicacionesService.SoloApp, f.Comunicaciones.CanalDe(cuenta.Id, tipo));
    }

    [Theory]
    [InlineData("SMS")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Whatsapp")]
    public void CambiarPreferencia_con_canal_invalido_falla_y_no_cambia_nada(string canal)
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var antes = f.Comunicaciones.CanalDe(cuenta.Id, ComunicacionesService.AvisoRefuerzo);

        var r = f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, canal);

        Assert.False(r.Exito);
        Assert.Equal(antes, f.Comunicaciones.CanalDe(cuenta.Id, ComunicacionesService.AvisoRefuerzo));
    }

    [Theory]
    [InlineData("Inexistente")]
    [InlineData("")]
    public void CambiarPreferencia_con_tipo_inexistente_falla(string tipo)
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        var r = f.Comunicaciones.CambiarPreferencia(cuenta.Id, tipo, ComunicacionesService.Correo);

        Assert.False(r.Exito);
    }

    [Fact]
    public void CambiarPreferencia_con_cuenta_inexistente_falla()
    {
        var f = new Fabrica();

        var r = f.Comunicaciones.CambiarPreferencia(999999, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.Correo);

        Assert.False(r.Exito);
    }

    [Fact]
    public void CambiarPreferencia_con_cuenta_eliminada_falla()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        Assert.True(f.Cuentas.Anonimizar(cuenta.Id));

        var r = f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.SoloApp);

        Assert.False(r.Exito);
    }

    [Fact]
    public void Preferencia_de_un_cliente_no_afecta_a_otro()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);

        f.Comunicaciones.CambiarPreferencia(a.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Correo);

        Assert.Equal(ComunicacionesService.Correo, f.Comunicaciones.CanalDe(a.Id, ComunicacionesService.AlertaSanitaria));
        Assert.Equal(ComunicacionesService.Ninguno, f.Comunicaciones.CanalDe(b.Id, ComunicacionesService.AlertaSanitaria));
    }

    // ---------- Encolar (CM-01, CM-02, CM-07) ----------

    [Fact]
    public void Encolar_por_correo_guarda_destino_y_queda_enviado()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.Correo);

        var m = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "Asunto", "Cuerpo");

        Assert.NotNull(m);
        Assert.Equal(cuenta.Id, m!.CuentaId);
        Assert.Equal(ComunicacionesService.AvisoRefuerzo, m.Tipo);
        Assert.Equal("Correo", m.Canal);
        Assert.Equal(cuenta.Correo, m.Destino);
        Assert.Equal(ComunicacionesService.Enviado, m.Estado);
        Assert.NotNull(m.FechaEnvio);
        Assert.False(m.Leida);
        Assert.Equal("Sistema", m.Origen);
    }

    [Fact]
    public void Encolar_por_solo_app_queda_enviado_sin_destino()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.SoloApp);

        var m = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "Asunto", "Cuerpo");

        Assert.NotNull(m);
        Assert.Equal("En la app", m!.Canal);
        Assert.Equal(ComunicacionesService.Enviado, m.Estado);
        Assert.Equal("", m.Destino);
    }

    [Fact]
    public void Encolar_solo_app_con_falla_simulada_igual_queda_enviado()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.SoloApp);
        f.Comunicaciones.SimularFalla = true;

        var m = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "Asunto", "Cuerpo");

        Assert.NotNull(m);
        Assert.Equal(ComunicacionesService.Enviado, m!.Estado);
    }

    [Fact]
    public void Encolar_conserva_el_destino_aunque_el_cliente_cambie_de_correo_despues()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.Correo);
        var correoOriginal = cuenta.Correo;
        var m = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "Asunto", "Cuerpo");
        Assert.NotNull(m);

        cuenta.Correo = "nuevo@prueba.uy";

        Assert.Equal(correoOriginal, f.Comunicaciones.ObtenerPorId(m!.Id)!.Destino);
    }

    [Fact]
    public void Encolar_con_cuenta_inexistente_devuelve_null()
    {
        var f = new Fabrica();

        Assert.Null(f.Comunicaciones.Encolar(999999, ComunicacionesService.AvisoRefuerzo, "A", "C"));
        Assert.Empty(f.Comunicaciones.Todas());
    }

    [Fact]
    public void Encolar_con_cuenta_sin_verificar_devuelve_null()
    {
        var f = new Fabrica();
        f.Cuentas.Registrar(Fabrica.RegistroValido("sinverificar@prueba.uy", Cedulas[2]));
        var cuenta = f.Cuentas.ObtenerPorCorreo("sinverificar@prueba.uy")!;

        var m = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C");

        Assert.Null(m);
        Assert.Empty(f.Comunicaciones.Todas());
    }

    [Fact]
    public void Encolar_con_cuenta_eliminada_devuelve_null()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        Assert.True(f.Cuentas.Anonimizar(cuenta.Id));

        Assert.Null(f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C"));
    }

    [Theory]
    [InlineData("Inexistente")]
    [InlineData("")]
    public void Encolar_con_tipo_inexistente_devuelve_null(string tipo)
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        Assert.Null(f.Comunicaciones.Encolar(cuenta.Id, tipo, "A", "C"));
        Assert.Empty(f.Comunicaciones.Todas());
    }

    [Fact]
    public void Encolar_si_el_cliente_eligio_no_recibir_ese_tipo_devuelve_null()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.Ninguno);

        var m = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C");

        Assert.Null(m);
        Assert.Empty(f.Comunicaciones.DeCuenta(cuenta.Id));
    }

    [Fact]
    public void Encolar_alerta_sanitaria_con_el_canal_por_defecto_no_genera_nada()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        var m = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AlertaSanitaria, "A", "C");

        Assert.Null(m);
    }

    [Fact]
    public void Encolar_con_la_misma_clave_no_duplica_el_mensaje()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        var primero = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C", "clave-1");
        var segundo = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C", "clave-1");

        Assert.NotNull(primero);
        Assert.Null(segundo);
        Assert.Single(f.Comunicaciones.DeCuenta(cuenta.Id));
    }

    [Fact]
    public void Encolar_con_clave_distinta_genera_otro_mensaje()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        var primero = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C", "clave-1");
        var segundo = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C", "clave-2");

        Assert.NotNull(primero);
        Assert.NotNull(segundo);
        Assert.Equal(2, f.Comunicaciones.DeCuenta(cuenta.Id).Count);
    }

    [Fact]
    public void Encolar_con_la_misma_clave_pero_otro_tipo_genera_ambos()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Correo);

        var primero = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C", "k");
        var segundo = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AlertaSanitaria, "A", "C", "k");

        Assert.NotNull(primero);
        Assert.NotNull(segundo);
    }

    [Fact]
    public void Encolar_con_la_misma_clave_en_otra_cuenta_genera_ambos()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);

        var primero = f.Comunicaciones.Encolar(a.Id, ComunicacionesService.AvisoRefuerzo, "A", "C", "k");
        var segundo = f.Comunicaciones.Encolar(b.Id, ComunicacionesService.AvisoRefuerzo, "A", "C", "k");

        Assert.NotNull(primero);
        Assert.NotNull(segundo);
    }

    [Fact]
    public void Encolar_sin_clave_dos_veces_genera_dos_mensajes()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C");
        f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C");

        Assert.Equal(2, f.Comunicaciones.DeCuenta(cuenta.Id).Count);
    }

    [Fact]
    public void Encolar_con_origen_indicado_lo_guarda()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        var m = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C", "", "Ana Pérez");

        Assert.NotNull(m);
        Assert.Equal("Ana Pérez", m!.Origen);
    }

    // ---------- Reintentos y cola (CM-03, CM-04, CM-05) ----------

    [Fact]
    public void Falla_simulada_deja_el_mensaje_pendiente_con_un_intento()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.SimularFalla = true;

        var m = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C");

        Assert.NotNull(m);
        Assert.Equal(ComunicacionesService.Pendiente, m!.Estado);
        Assert.Equal(1, m.Intentos);
        Assert.NotEqual("", m.UltimoError);
        Assert.Null(m.FechaEnvio);
    }

    [Fact]
    public void Segundo_intento_fallido_sigue_pendiente_y_el_tercero_pasa_a_fallido()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.SimularFalla = true;
        var m = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C")!;

        f.Comunicaciones.ProcesarCola();
        Assert.Equal(2, m.Intentos);
        Assert.Equal(ComunicacionesService.Pendiente, m.Estado);

        f.Comunicaciones.ProcesarCola();
        Assert.Equal(3, m.Intentos);
        Assert.Equal(ComunicacionesService.Fallido, m.Estado);
    }

    [Fact]
    public void ProcesarCola_devuelve_cuantos_intento_y_envia_los_pendientes_si_ya_no_hay_falla()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        f.Comunicaciones.SimularFalla = true;
        var ma = f.Comunicaciones.Encolar(a.Id, ComunicacionesService.AvisoRefuerzo, "A", "C")!;
        var mb = f.Comunicaciones.Encolar(b.Id, ComunicacionesService.AvisoRefuerzo, "A", "C")!;
        f.Comunicaciones.SimularFalla = false;

        var intentados = f.Comunicaciones.ProcesarCola();

        Assert.Equal(2, intentados);
        Assert.Equal(ComunicacionesService.Enviado, ma.Estado);
        Assert.Equal(ComunicacionesService.Enviado, mb.Estado);
        Assert.NotNull(ma.FechaEnvio);
    }

    [Fact]
    public void ProcesarCola_sin_pendientes_devuelve_cero()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C");

        Assert.Equal(0, f.Comunicaciones.ProcesarCola());
    }

    [Fact]
    public void ProcesarCola_no_toca_los_fallidos_ni_los_enviados()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        var fallido = MensajeFallido(f, a);
        var enviado = f.Comunicaciones.Encolar(b.Id, ComunicacionesService.AvisoRefuerzo, "A", "C")!;

        var intentados = f.Comunicaciones.ProcesarCola();

        Assert.Equal(0, intentados);
        Assert.Equal(ComunicacionesService.Fallido, fallido.Estado);
        Assert.Equal(ComunicacionesService.MaxIntentos, fallido.Intentos);
        Assert.Equal(ComunicacionesService.Enviado, enviado.Estado);
    }

    [Fact]
    public void ProcesarCola_no_intenta_los_mensajes_solo_app()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.SoloApp);
        f.Comunicaciones.SimularFalla = true;
        f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C");

        Assert.Equal(0, f.Comunicaciones.ProcesarCola());
    }

    [Theory]
    [InlineData(Recepcion)]
    [InlineData(Administrador)]
    public void Reintentar_un_fallido_lo_envia_si_ya_no_hay_falla(string correo)
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var m = MensajeFallido(f, cuenta);

        var r = f.Comunicaciones.Reintentar(m.Id, f.PersonalDe(correo));

        Assert.True(r.Exito);
        Assert.Equal(ComunicacionesService.Enviado, m.Estado);
    }

    [Fact]
    public void Reintentar_con_falla_vigente_reinicia_los_intentos_y_queda_pendiente()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var m = MensajeFallido(f, cuenta);
        f.Comunicaciones.SimularFalla = true;

        f.Comunicaciones.Reintentar(m.Id, f.PersonalDe(Recepcion));

        Assert.Equal(1, m.Intentos);
        Assert.Equal(ComunicacionesService.Pendiente, m.Estado);
    }

    [Fact]
    public void Reintentar_un_mensaje_enviado_falla_y_no_cambia_nada()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var m = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C")!;

        var r = f.Comunicaciones.Reintentar(m.Id, f.PersonalDe(Recepcion));

        Assert.False(r.Exito);
        Assert.Equal(ComunicacionesService.Enviado, m.Estado);
    }

    [Fact]
    public void Reintentar_un_mensaje_pendiente_falla_y_no_cambia_nada()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.SimularFalla = true;
        var m = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C")!;

        var r = f.Comunicaciones.Reintentar(m.Id, f.PersonalDe(Recepcion));

        Assert.False(r.Exito);
        Assert.Equal(1, m.Intentos);
        Assert.Equal(ComunicacionesService.Pendiente, m.Estado);
    }

    [Fact]
    public void Reintentar_un_id_inexistente_falla()
    {
        var f = new Fabrica();

        Assert.False(f.Comunicaciones.Reintentar(999999, f.PersonalDe(Recepcion)).Exito);
    }

    [Fact]
    public void Reintentar_como_veterinario_falla_y_no_cambia_nada()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var m = MensajeFallido(f, cuenta);

        var r = f.Comunicaciones.Reintentar(m.Id, f.PersonalDe(Veterinario));

        Assert.False(r.Exito);
        Assert.Equal(ComunicacionesService.Fallido, m.Estado);
    }

    [Fact]
    public void Reintentar_sin_persona_falla()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var m = MensajeFallido(f, cuenta);

        Assert.False(f.Comunicaciones.Reintentar(m.Id, null).Exito);
        Assert.Equal(ComunicacionesService.Fallido, m.Estado);
    }

    [Fact]
    public void Reintentar_con_recepcion_inactiva_falla()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var m = MensajeFallido(f, cuenta);

        var r = f.Comunicaciones.Reintentar(m.Id, Inactivo(f, Recepcion));

        Assert.False(r.Exito);
        Assert.Equal(ComunicacionesService.Fallido, m.Estado);
    }

    // ---------- Reenviar (CM-06) ----------

    [Theory]
    [InlineData(Recepcion)]
    [InlineData(Administrador)]
    public void Reenviar_crea_un_mensaje_nuevo_por_correo_y_no_cambia_el_original(string correo)
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var persona = f.PersonalDe(correo);
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.SoloApp);
        var original = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "Asunto original", "Cuerpo original")!;
        var cantidadAntes = f.Comunicaciones.Todas().Count;

        var r = f.Comunicaciones.Reenviar(original.Id, persona);

        Assert.True(r.Exito);
        Assert.Equal(cantidadAntes + 1, f.Comunicaciones.Todas().Count);
        var nuevo = f.Comunicaciones.Todas().Single(c => c.ReenvioDeId == original.Id);
        Assert.NotEqual(original.Id, nuevo.Id);
        Assert.Equal("Correo", nuevo.Canal);
        Assert.Equal(cuenta.Correo, nuevo.Destino);
        Assert.Equal(original.Asunto, nuevo.Asunto);
        Assert.Equal(original.Cuerpo, nuevo.Cuerpo);
        Assert.Equal(persona.Nombre, nuevo.Origen);
        Assert.Equal(cuenta.Id, nuevo.CuentaId);

        Assert.Equal("En la app", original.Canal);
        Assert.Equal("", original.Destino);
        Assert.Equal(ComunicacionesService.Enviado, original.Estado);
        Assert.Equal(0, original.ReenvioDeId);
    }

    [Fact]
    public void Reenviar_como_veterinario_falla_y_no_crea_nada()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var original = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C")!;
        var antes = f.Comunicaciones.Todas().Count;

        var r = f.Comunicaciones.Reenviar(original.Id, f.PersonalDe(Veterinario));

        Assert.False(r.Exito);
        Assert.Equal(antes, f.Comunicaciones.Todas().Count);
    }

    [Fact]
    public void Reenviar_sin_persona_o_con_recepcion_inactiva_falla()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var original = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C")!;
        var antes = f.Comunicaciones.Todas().Count;

        Assert.False(f.Comunicaciones.Reenviar(original.Id, null).Exito);
        Assert.False(f.Comunicaciones.Reenviar(original.Id, Inactivo(f, Recepcion)).Exito);
        Assert.Equal(antes, f.Comunicaciones.Todas().Count);
    }

    [Fact]
    public void Reenviar_un_id_inexistente_falla()
    {
        var f = new Fabrica();

        Assert.False(f.Comunicaciones.Reenviar(999999, f.PersonalDe(Recepcion)).Exito);
    }

    [Fact]
    public void Reenviar_a_una_cuenta_eliminada_falla_y_no_crea_nada()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var original = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C")!;
        Assert.True(f.Cuentas.Anonimizar(cuenta.Id));
        var antes = f.Comunicaciones.Todas().Count;

        var r = f.Comunicaciones.Reenviar(original.Id, f.PersonalDe(Recepcion));

        Assert.False(r.Exito);
        Assert.Equal(antes, f.Comunicaciones.Todas().Count);
    }

    [Fact]
    public void Reenviar_no_cuenta_como_duplicado_de_clave_RNF_CON_10()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var original = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C", "k-reenvio")!;
        Assert.True(f.Comunicaciones.Reenviar(original.Id, f.PersonalDe(Recepcion)).Exito);
        var antes = f.Comunicaciones.DeCuenta(cuenta.Id).Count;

        var otro = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C", "k-reenvio");

        Assert.Null(otro);
        Assert.Equal(antes, f.Comunicaciones.DeCuenta(cuenta.Id).Count);
    }

    [Fact]
    public void Reenviar_un_fallido_crea_un_mensaje_nuevo_y_el_original_sigue_fallido()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var original = MensajeFallido(f, cuenta);

        var r = f.Comunicaciones.Reenviar(original.Id, f.PersonalDe(Recepcion));

        Assert.True(r.Exito);
        Assert.Equal(ComunicacionesService.Fallido, original.Estado);
        Assert.Contains(f.Comunicaciones.Todas(), c => c.ReenvioDeId == original.Id);
    }

    // ---------- Resumen clínico (CM-08) ----------

    [Fact]
    public void EncolarResumenClinico_genera_el_mensaje_con_clave_y_contenido_clinico()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var consulta = new Consulta
        {
            Id = 7,
            Motivo = "Vomitos persistentes",
            Diagnostico = "Gastritis aguda",
            Tratamiento = "Dieta blanda",
            Indicaciones = "Control en una semana",
            PesoKg = 18.2m
        };

        var m = f.Comunicaciones.EncolarResumenClinico(cuenta.Id, consulta);

        Assert.NotNull(m);
        Assert.Equal(ComunicacionesService.ResumenClinico, m!.Tipo);
        Assert.Equal("consulta-7", m.Clave);
        Assert.Equal(cuenta.Id, m.CuentaId);
        Assert.Contains("Vomitos persistentes", m.Cuerpo);
        Assert.Contains("Gastritis aguda", m.Cuerpo);
        Assert.Contains("Dieta blanda", m.Cuerpo);
        Assert.Contains("Control en una semana", m.Cuerpo);
        Assert.Contains("18", m.Cuerpo);
    }

    [Fact]
    public void EncolarResumenClinico_dos_veces_para_la_misma_consulta_no_duplica()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var consulta = new Consulta { Id = 8, Motivo = "M", Diagnostico = "D" };

        var primero = f.Comunicaciones.EncolarResumenClinico(cuenta.Id, consulta);
        var segundo = f.Comunicaciones.EncolarResumenClinico(cuenta.Id, consulta);

        Assert.NotNull(primero);
        Assert.Null(segundo);
        Assert.Equal(1, Cantidad(f, cuenta, ComunicacionesService.ResumenClinico));
    }

    [Fact]
    public void EncolarResumenClinico_respeta_el_canal_solo_app()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.ResumenClinico, ComunicacionesService.SoloApp);

        var m = f.Comunicaciones.EncolarResumenClinico(cuenta.Id, new Consulta { Id = 9, Motivo = "M", Diagnostico = "D" });

        Assert.NotNull(m);
        Assert.Equal("En la app", m!.Canal);
        Assert.Equal("", m.Destino);
    }

    [Fact]
    public void EncolarResumenClinico_para_cuenta_inexistente_o_eliminada_devuelve_null()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        Assert.True(f.Cuentas.Anonimizar(cuenta.Id));
        var consulta = new Consulta { Id = 10, Motivo = "M", Diagnostico = "D" };

        Assert.Null(f.Comunicaciones.EncolarResumenClinico(999999, consulta));
        Assert.Null(f.Comunicaciones.EncolarResumenClinico(cuenta.Id, consulta));
    }

    [Fact]
    public void EncolarResumenClinico_solo_llega_al_cliente_indicado()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);

        f.Comunicaciones.EncolarResumenClinico(a.Id, new Consulta { Id = 11, Motivo = "M", Diagnostico = "D" });

        Assert.Equal(1, Cantidad(f, a, ComunicacionesService.ResumenClinico));
        Assert.Equal(0, Cantidad(f, b, ComunicacionesService.ResumenClinico));
    }

    // ---------- Comprobante (CM-09) ----------

    private static (Pago, Cargo) PagoDe(Cuenta cuenta, int pagoId = 5)
    {
        var cargo = new Cargo { Id = 3, ClienteId = cuenta.Id, Descripcion = "Consulta", Monto = 800m };
        var pago = new Pago { Id = pagoId, CargoId = 3, Numero = "C-000001", Monto = 800m, Medio = "Efectivo" };
        return (pago, cargo);
    }

    [Theory]
    [InlineData(Recepcion)]
    [InlineData(Administrador)]
    public void EnviarComprobante_genera_el_mensaje_con_clave_del_pago(string correo)
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var (pago, cargo) = PagoDe(cuenta);

        var r = f.Comunicaciones.EnviarComprobante(pago, cargo, f.PersonalDe(correo));

        Assert.True(r.Exito);
        var m = f.Comunicaciones.DeCuenta(cuenta.Id).Single(c => c.Tipo == ComunicacionesService.Comprobante);
        Assert.Equal("pago-5", m.Clave);
    }

    [Fact]
    public void EnviarComprobante_una_segunda_vez_falla_y_no_duplica()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var (pago, cargo) = PagoDe(cuenta);
        Assert.True(f.Comunicaciones.EnviarComprobante(pago, cargo, f.PersonalDe(Recepcion)).Exito);

        var r = f.Comunicaciones.EnviarComprobante(pago, cargo, f.PersonalDe(Recepcion));

        Assert.False(r.Exito);
        Assert.Equal(1, Cantidad(f, cuenta, ComunicacionesService.Comprobante));
    }

    [Fact]
    public void EnviarComprobante_de_otro_pago_si_se_genera()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var (pago1, cargo) = PagoDe(cuenta, 5);
        var (pago2, _) = PagoDe(cuenta, 6);

        Assert.True(f.Comunicaciones.EnviarComprobante(pago1, cargo, f.PersonalDe(Recepcion)).Exito);
        Assert.True(f.Comunicaciones.EnviarComprobante(pago2, cargo, f.PersonalDe(Recepcion)).Exito);
        Assert.Equal(2, Cantidad(f, cuenta, ComunicacionesService.Comprobante));
    }

    [Fact]
    public void EnviarComprobante_como_veterinario_o_sin_persona_falla_y_no_genera()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var (pago, cargo) = PagoDe(cuenta);

        Assert.False(f.Comunicaciones.EnviarComprobante(pago, cargo, f.PersonalDe(Veterinario)).Exito);
        Assert.False(f.Comunicaciones.EnviarComprobante(pago, cargo, null).Exito);
        Assert.False(f.Comunicaciones.EnviarComprobante(pago, cargo, Inactivo(f, Recepcion)).Exito);
        Assert.Equal(0, Cantidad(f, cuenta, ComunicacionesService.Comprobante));
    }

    [Fact]
    public void EnviarComprobante_llega_solo_al_cliente_del_cargo()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        var (pago, cargo) = PagoDe(a);

        f.Comunicaciones.EnviarComprobante(pago, cargo, f.PersonalDe(Recepcion));

        Assert.Equal(1, Cantidad(f, a, ComunicacionesService.Comprobante));
        Assert.Equal(0, Cantidad(f, b, ComunicacionesService.Comprobante));
    }

    // ---------- Aviso de la clínica (CM-10) ----------

    [Theory]
    [InlineData(Recepcion)]
    [InlineData(Administrador)]
    public void AvisoOperativo_lo_pueden_emitir_recepcion_y_administrador(string correo)
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        var r = f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(correo), "Cierre por feriado", "La clínica cierra el lunes.");

        Assert.True(r.Exito);
        Assert.Equal(1, Cantidad(f, cuenta, ComunicacionesService.AvisoOperativo));
    }

    [Fact]
    public void AvisoOperativo_como_veterinario_sin_persona_o_inactivo_falla_y_no_genera()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        Assert.False(f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Veterinario), "T", "M").Exito);
        Assert.False(f.Comunicaciones.EnviarAvisoOperativo(null, "T", "M").Exito);
        Assert.False(f.Comunicaciones.EnviarAvisoOperativo(Inactivo(f, Administrador), "T", "M").Exito);
        Assert.Equal(0, Cantidad(f, cuenta, ComunicacionesService.AvisoOperativo));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(120, true)]
    [InlineData(121, false)]
    [InlineData(0, false)]
    public void AvisoOperativo_borde_de_largo_del_titulo(int largo, bool valido)
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        var r = f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Recepcion), new string('t', largo), "Mensaje");

        Assert.Equal(valido, r.Exito);
        Assert.Equal(valido ? 1 : 0, Cantidad(f, cuenta, ComunicacionesService.AvisoOperativo));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    [InlineData(0, false)]
    public void AvisoOperativo_borde_de_largo_del_mensaje(int largo, bool valido)
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        var r = f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Recepcion), "Titulo", new string('m', largo));

        Assert.Equal(valido, r.Exito);
        Assert.Equal(valido ? 1 : 0, Cantidad(f, cuenta, ComunicacionesService.AvisoOperativo));
    }

    [Fact]
    public void AvisoOperativo_llega_a_todos_los_verificados_por_su_canal_y_no_a_los_demas()
    {
        var f = new Fabrica();
        var porCorreo = Cliente(f, 0);
        var porApp = Cliente(f, 1);
        var eliminado = Cliente(f, 2);
        f.Cuentas.Registrar(Fabrica.RegistroValido("sinverificar@prueba.uy", Cedulas[3]));
        var sinVerificar = f.Cuentas.ObtenerPorCorreo("sinverificar@prueba.uy")!;
        f.Comunicaciones.CambiarPreferencia(porCorreo.Id, ComunicacionesService.AvisoOperativo, ComunicacionesService.Correo);
        f.Comunicaciones.CambiarPreferencia(porApp.Id, ComunicacionesService.AvisoOperativo, ComunicacionesService.SoloApp);
        Assert.True(f.Cuentas.Anonimizar(eliminado.Id));

        var r = f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Recepcion), "Titulo", "Mensaje");

        Assert.True(r.Exito);
        var m1 = f.Comunicaciones.DeCuenta(porCorreo.Id).Single(c => c.Tipo == ComunicacionesService.AvisoOperativo);
        var m2 = f.Comunicaciones.DeCuenta(porApp.Id).Single(c => c.Tipo == ComunicacionesService.AvisoOperativo);
        Assert.Equal("Correo", m1.Canal);
        Assert.Equal("En la app", m2.Canal);
        Assert.Empty(f.Comunicaciones.DeCuenta(sinVerificar.Id));
        Assert.Equal(0, Cantidad(f, eliminado, ComunicacionesService.AvisoOperativo));
    }

    // ---------- Alerta sanitaria (CM-11) ----------

    [Theory]
    [InlineData(Administrador)]
    [InlineData(Veterinario)]
    public void AlertaSanitaria_la_pueden_emitir_administrador_y_veterinario_y_llega_a_quien_la_activo(string correo)
    {
        var f = new Fabrica();
        var activa = Cliente(f, 0);
        var inactiva = Cliente(f, 1);
        f.Comunicaciones.CambiarPreferencia(activa.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Correo);

        var r = f.Comunicaciones.EnviarAlertaSanitaria(f.PersonalDe(correo), "Brote", "Hay un brote de parvovirus.");

        Assert.True(r.Exito);
        Assert.Equal(1, Cantidad(f, activa, ComunicacionesService.AlertaSanitaria));
        Assert.Equal(0, Cantidad(f, inactiva, ComunicacionesService.AlertaSanitaria));
    }

    [Fact]
    public void AlertaSanitaria_como_recepcion_sin_persona_o_inactivo_falla_y_no_genera()
    {
        var f = new Fabrica();
        var activa = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(activa.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Correo);

        Assert.False(f.Comunicaciones.EnviarAlertaSanitaria(f.PersonalDe(Recepcion), "T", "M").Exito);
        Assert.False(f.Comunicaciones.EnviarAlertaSanitaria(null, "T", "M").Exito);
        Assert.False(f.Comunicaciones.EnviarAlertaSanitaria(Inactivo(f, Veterinario), "T", "M").Exito);
        Assert.Equal(0, Cantidad(f, activa, ComunicacionesService.AlertaSanitaria));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(120, true)]
    [InlineData(121, false)]
    [InlineData(0, false)]
    public void AlertaSanitaria_borde_de_largo_del_titulo(int largo, bool valido)
    {
        var f = new Fabrica();
        var activa = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(activa.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Correo);

        var r = f.Comunicaciones.EnviarAlertaSanitaria(f.PersonalDe(Administrador), new string('t', largo), "Mensaje");

        Assert.Equal(valido, r.Exito);
        Assert.Equal(valido ? 1 : 0, Cantidad(f, activa, ComunicacionesService.AlertaSanitaria));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(1000, true)]
    [InlineData(1001, false)]
    [InlineData(0, false)]
    public void AlertaSanitaria_borde_de_largo_del_mensaje(int largo, bool valido)
    {
        var f = new Fabrica();
        var activa = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(activa.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Correo);

        var r = f.Comunicaciones.EnviarAlertaSanitaria(f.PersonalDe(Administrador), "Titulo", new string('m', largo));

        Assert.Equal(valido, r.Exito);
        Assert.Equal(valido ? 1 : 0, Cantidad(f, activa, ComunicacionesService.AlertaSanitaria));
    }

    [Fact]
    public void AlertaSanitaria_filtrada_por_especie_llega_solo_a_quienes_tienen_esa_especie()
    {
        var f = new Fabrica();
        var duenoPerro = Cliente(f, 0);
        var duenoGato = Cliente(f, 1);
        f.AnimalDe(duenoPerro.Id, "Perro", "Rex");
        f.AnimalDe(duenoGato.Id, "Gato", "Michi");
        f.Comunicaciones.CambiarPreferencia(duenoPerro.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Correo);
        f.Comunicaciones.CambiarPreferencia(duenoGato.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Correo);

        var r = f.Comunicaciones.EnviarAlertaSanitaria(f.PersonalDe(Administrador), "Brote", "Parvovirus", "Perro");

        Assert.True(r.Exito);
        Assert.Equal(1, Cantidad(f, duenoPerro, ComunicacionesService.AlertaSanitaria));
        Assert.Equal(0, Cantidad(f, duenoGato, ComunicacionesService.AlertaSanitaria));
    }

    [Fact]
    public void AlertaSanitaria_sin_especie_llega_a_todos_los_que_la_activaron()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        f.AnimalDe(a.Id, "Perro", "Rex");
        f.AnimalDe(b.Id, "Gato", "Michi");
        f.Comunicaciones.CambiarPreferencia(a.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Correo);
        f.Comunicaciones.CambiarPreferencia(b.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.SoloApp);

        f.Comunicaciones.EnviarAlertaSanitaria(f.PersonalDe(Veterinario), "Brote", "Aviso general");

        Assert.Equal(1, Cantidad(f, a, ComunicacionesService.AlertaSanitaria));
        Assert.Equal(1, Cantidad(f, b, ComunicacionesService.AlertaSanitaria));
    }

    [Fact]
    public void AlertaSanitaria_no_llega_a_quien_eligio_no_recibir()
    {
        var f = new Fabrica();
        var a = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(a.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Correo);
        f.Comunicaciones.CambiarPreferencia(a.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Ninguno);

        f.Comunicaciones.EnviarAlertaSanitaria(f.PersonalDe(Administrador), "Brote", "Aviso");

        Assert.Equal(0, Cantidad(f, a, ComunicacionesService.AlertaSanitaria));
    }

    // ---------- Avisos de refuerzo (CM-12) ----------

    [Fact]
    public void AvisosRefuerzo_Generar_dos_veces_no_duplica()
    {
        var f = new Fabrica();
        Cliente(f);
        f.AvisosRefuerzo.Generar();
        var totalTrasPrimera = f.Comunicaciones.Todas().Count;

        var segunda = f.AvisosRefuerzo.Generar();

        Assert.Equal(0, segunda);
        Assert.Equal(totalTrasPrimera, f.Comunicaciones.Todas().Count);
    }

    [Fact]
    public void AvisosRefuerzo_Generar_devuelve_lo_que_agrego_a_la_bandeja()
    {
        var f = new Fabrica();
        Cliente(f);
        var antes = f.Comunicaciones.Todas().Count;

        var generados = f.AvisosRefuerzo.Generar();

        Assert.True(generados >= 0);
        Assert.Equal(antes + generados, f.Comunicaciones.Todas().Count);
        Assert.All(f.Comunicaciones.Todas().Skip(antes), c => Assert.Equal(ComunicacionesService.AvisoRefuerzo, c.Tipo));
    }

    [Fact]
    public void AvisosRefuerzo_Generar_no_crea_avisos_para_cuentas_sin_verificar_ni_eliminadas()
    {
        var f = new Fabrica();
        f.Cuentas.Registrar(Fabrica.RegistroValido("sinverificar@prueba.uy", Cedulas[1]));
        var sinVerificar = f.Cuentas.ObtenerPorCorreo("sinverificar@prueba.uy")!;
        f.AnimalDe(sinVerificar.Id, "Perro", "Rex");
        var eliminada = Cliente(f, 0);
        f.AnimalDe(eliminada.Id, "Perro", "Toto");
        Assert.True(f.Cuentas.Anonimizar(eliminada.Id));

        f.AvisosRefuerzo.Generar();

        Assert.Empty(f.Comunicaciones.DeCuenta(sinVerificar.Id));
        Assert.Empty(f.Comunicaciones.DeCuenta(eliminada.Id));
    }

    // ---------- Bandeja del cliente (CM-14) ----------

    [Fact]
    public void DeCuenta_devuelve_solo_los_mensajes_del_cliente_con_los_mas_nuevos_primero()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        f.Comunicaciones.Encolar(a.Id, ComunicacionesService.AvisoRefuerzo, "1", "C", "k1");
        f.Comunicaciones.Encolar(a.Id, ComunicacionesService.AvisoRefuerzo, "2", "C", "k2");
        f.Comunicaciones.Encolar(a.Id, ComunicacionesService.AvisoRefuerzo, "3", "C", "k3");
        f.Comunicaciones.Encolar(b.Id, ComunicacionesService.AvisoRefuerzo, "B", "C", "k1");

        var bandejaA = f.Comunicaciones.DeCuenta(a.Id);

        Assert.Equal(3, bandejaA.Count);
        Assert.All(bandejaA, m => Assert.Equal(a.Id, m.CuentaId));
        for (var i = 1; i < bandejaA.Count; i++)
        {
            Assert.True(bandejaA[i - 1].FechaCreacion >= bandejaA[i].FechaCreacion);
        }

        Assert.Single(f.Comunicaciones.DeCuenta(b.Id));
    }

    [Fact]
    public void DeCuenta_de_una_cuenta_sin_mensajes_esta_vacia()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        Assert.Empty(f.Comunicaciones.DeCuenta(cuenta.Id));
        Assert.Equal(0, f.Comunicaciones.NoLeidas(cuenta.Id));
    }

    [Fact]
    public void MarcarLeida_marca_el_propio_mensaje_y_baja_las_no_leidas()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.SoloApp);
        var m1 = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "1", "C", "k1")!;
        f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "2", "C", "k2");
        Assert.Equal(2, f.Comunicaciones.NoLeidas(cuenta.Id));

        var ok = f.Comunicaciones.MarcarLeida(m1.Id, cuenta.Id);

        Assert.True(ok);
        Assert.True(m1.Leida);
        Assert.Equal(1, f.Comunicaciones.NoLeidas(cuenta.Id));
    }

    [Fact]
    public void MarcarLeida_de_un_mensaje_ajeno_falla_y_no_cambia_nada_RN18()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        f.Comunicaciones.CambiarPreferencia(a.Id, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.SoloApp);
        var delA = f.Comunicaciones.Encolar(a.Id, ComunicacionesService.AvisoRefuerzo, "1", "C")!;

        var ok = f.Comunicaciones.MarcarLeida(delA.Id, b.Id);

        Assert.False(ok);
        Assert.False(delA.Leida);
        Assert.Equal(1, f.Comunicaciones.NoLeidas(a.Id));
    }

    [Fact]
    public void MarcarLeida_de_un_id_inexistente_falla()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        Assert.False(f.Comunicaciones.MarcarLeida(999999, cuenta.Id));
    }

    [Fact]
    public void ObtenerPorId_devuelve_el_mensaje_o_null()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var m = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C")!;

        Assert.Equal(m.Id, f.Comunicaciones.ObtenerPorId(m.Id)!.Id);
        Assert.Null(f.Comunicaciones.ObtenerPorId(999999));
    }

    [Fact]
    public void EliminarDe_borra_solo_los_mensajes_de_esa_cuenta_y_devuelve_cuantos()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        f.Comunicaciones.Encolar(a.Id, ComunicacionesService.AvisoRefuerzo, "1", "C", "k1");
        f.Comunicaciones.Encolar(a.Id, ComunicacionesService.AvisoRefuerzo, "2", "C", "k2");
        f.Comunicaciones.Encolar(b.Id, ComunicacionesService.AvisoRefuerzo, "3", "C", "k3");

        var borrados = f.Comunicaciones.EliminarDe(a.Id);

        Assert.Equal(2, borrados);
        Assert.Empty(f.Comunicaciones.DeCuenta(a.Id));
        Assert.Single(f.Comunicaciones.DeCuenta(b.Id));
        Assert.Equal(0, f.Comunicaciones.EliminarDe(a.Id));
    }

    // ---------- Bandeja del personal (CM-15) ----------

    [Fact]
    public void Buscar_sin_filtros_devuelve_todas()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "1", "C", "k1");
        f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "2", "C", "k2");

        Assert.Equal(f.Comunicaciones.Todas().Count, f.Comunicaciones.Buscar().Count);
    }

    [Fact]
    public void Buscar_por_estado_devuelve_solo_ese_estado()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        var fallido = MensajeFallido(f, a);
        var enviado = f.Comunicaciones.Encolar(b.Id, ComunicacionesService.AvisoRefuerzo, "ok", "C")!;

        var fallidos = f.Comunicaciones.Buscar(ComunicacionesService.Fallido);
        var enviados = f.Comunicaciones.Buscar(ComunicacionesService.Enviado);

        Assert.Contains(fallidos, m => m.Id == fallido.Id);
        Assert.DoesNotContain(fallidos, m => m.Id == enviado.Id);
        Assert.All(fallidos, m => Assert.Equal(ComunicacionesService.Fallido, m.Estado));
        Assert.Contains(enviados, m => m.Id == enviado.Id);
        Assert.DoesNotContain(enviados, m => m.Id == fallido.Id);
    }

    [Fact]
    public void Buscar_por_tipo_devuelve_solo_ese_tipo()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var (pago, cargo) = PagoDe(cuenta);
        f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "R", "C");
        f.Comunicaciones.EnviarComprobante(pago, cargo, f.PersonalDe(Recepcion));

        var comprobantes = f.Comunicaciones.Buscar("", ComunicacionesService.Comprobante);

        Assert.Single(comprobantes);
        Assert.All(comprobantes, m => Assert.Equal(ComunicacionesService.Comprobante, m.Tipo));
    }

    [Fact]
    public void Buscar_por_texto_encuentra_por_asunto_destino_y_nombre_del_cliente()
    {
        var f = new Fabrica();
        var registro = Fabrica.RegistroValido("zoraida.buscada@prueba.uy", Cedulas[1]);
        registro.Nombre = "Zoraidaxq";
        f.Cuentas.Registrar(registro);
        var zoraida = f.Cuentas.ObtenerPorCorreo("zoraida.buscada@prueba.uy")!;
        f.Cuentas.Verificar("zoraida.buscada@prueba.uy", zoraida.CodigoDemo);
        var otra = Cliente(f, 0);
        var delaZoraida = f.Comunicaciones.Encolar(zoraida.Id, ComunicacionesService.AvisoRefuerzo, "Asuntounicozz", "C")!;
        var deLaOtra = f.Comunicaciones.Encolar(otra.Id, ComunicacionesService.AvisoRefuerzo, "Otro asunto", "C")!;

        var porAsunto = f.Comunicaciones.Buscar("", "", "Asuntounicozz");
        var porDestino = f.Comunicaciones.Buscar("", "", "zoraida.buscada@prueba.uy");
        var porNombre = f.Comunicaciones.Buscar("", "", "Zoraidaxq");

        Assert.Contains(porAsunto, m => m.Id == delaZoraida.Id);
        Assert.DoesNotContain(porAsunto, m => m.Id == deLaOtra.Id);
        Assert.Contains(porDestino, m => m.Id == delaZoraida.Id);
        Assert.DoesNotContain(porDestino, m => m.Id == deLaOtra.Id);
        Assert.Contains(porNombre, m => m.Id == delaZoraida.Id);
        Assert.DoesNotContain(porNombre, m => m.Id == deLaOtra.Id);
    }

    [Fact]
    public void Buscar_texto_sin_coincidencias_devuelve_vacio()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C");

        Assert.Empty(f.Comunicaciones.Buscar("", "", "texto-que-no-existe-xyz"));
    }

    [Fact]
    public void Buscar_combina_estado_y_tipo()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var fallido = MensajeFallido(f, a);
        var b = Cliente(f, 1);
        f.Comunicaciones.Encolar(b.Id, ComunicacionesService.AvisoRefuerzo, "ok", "C");

        var r = f.Comunicaciones.Buscar(ComunicacionesService.Fallido, ComunicacionesService.AvisoRefuerzo);

        Assert.Single(r);
        Assert.Equal(fallido.Id, r[0].Id);
        Assert.Empty(f.Comunicaciones.Buscar(ComunicacionesService.Fallido, ComunicacionesService.Comprobante));
    }

    // ---------- Auditoría de comunicaciones (CM-16) ----------

    [Fact]
    public void Aviso_operativo_deja_registro_en_la_bitacora_sin_copiar_el_cuerpo()
    {
        var f = new Fabrica();
        Cliente(f);
        var antes = RegistrosBitacora(f, BitacoraService.ComunicacionesEntidad);

        f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Recepcion), "Titulo", "CUERPOSECRETOXYZ");

        var registros = f.Bitacora.Buscar(f.PersonalDe(Administrador), entidad: BitacoraService.ComunicacionesEntidad);
        Assert.True(registros.Count > antes);
        Assert.DoesNotContain(registros, r => r.Detalle.Contains("CUERPOSECRETOXYZ"));
    }

    [Fact]
    public void Alerta_sanitaria_deja_registro_en_la_bitacora_sin_copiar_el_cuerpo()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Correo);
        var antes = RegistrosBitacora(f, BitacoraService.ComunicacionesEntidad);

        f.Comunicaciones.EnviarAlertaSanitaria(f.PersonalDe(Administrador), "Titulo", "CUERPOALERTAXYZ");

        var registros = f.Bitacora.Buscar(f.PersonalDe(Administrador), entidad: BitacoraService.ComunicacionesEntidad);
        Assert.True(registros.Count > antes);
        Assert.DoesNotContain(registros, r => r.Detalle.Contains("CUERPOALERTAXYZ"));
    }

    [Fact]
    public void Reenviar_y_reintentar_dejan_registro_en_la_bitacora()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var fallido = MensajeFallido(f, cuenta);
        var antes = RegistrosBitacora(f, BitacoraService.ComunicacionesEntidad);

        f.Comunicaciones.Reintentar(fallido.Id, f.PersonalDe(Recepcion));
        var trasReintentar = RegistrosBitacora(f, BitacoraService.ComunicacionesEntidad);
        f.Comunicaciones.Reenviar(fallido.Id, f.PersonalDe(Recepcion));
        var trasReenviar = RegistrosBitacora(f, BitacoraService.ComunicacionesEntidad);

        Assert.True(trasReintentar > antes);
        Assert.True(trasReenviar > trasReintentar);
    }

    [Fact]
    public void Enviar_comprobante_deja_registro_en_la_bitacora()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var (pago, cargo) = PagoDe(cuenta);
        var antes = RegistrosBitacora(f, BitacoraService.ComunicacionesEntidad);

        f.Comunicaciones.EnviarComprobante(pago, cargo, f.PersonalDe(Recepcion));

        Assert.True(RegistrosBitacora(f, BitacoraService.ComunicacionesEntidad) > antes);
    }

    [Fact]
    public void Operaciones_de_comunicaciones_rechazadas_no_dejan_registro()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var fallido = MensajeFallido(f, cuenta);
        var antes = RegistrosBitacora(f, BitacoraService.ComunicacionesEntidad);

        f.Comunicaciones.Reintentar(fallido.Id, f.PersonalDe(Veterinario));
        f.Comunicaciones.Reenviar(fallido.Id, f.PersonalDe(Veterinario));
        f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Veterinario), "T", "M");
        f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Recepcion), "", "M");
        f.Comunicaciones.EnviarAlertaSanitaria(f.PersonalDe(Recepcion), "T", "M");

        Assert.Equal(antes, RegistrosBitacora(f, BitacoraService.ComunicacionesEntidad));
    }

    // ---------- Derechos: política (DR-09) ----------

    [Fact]
    public void Politicas_documenta_cada_categoria_con_todos_sus_campos()
    {
        var f = new Fabrica();

        var politicas = f.Derechos.Politicas();

        Assert.NotEmpty(politicas);
        Assert.All(politicas, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.Categoria));
            Assert.False(string.IsNullOrWhiteSpace(p.Finalidad));
            Assert.False(string.IsNullOrWhiteSpace(p.Acceso));
            Assert.False(string.IsNullOrWhiteSpace(p.Conservacion));
            Assert.False(string.IsNullOrWhiteSpace(p.AlEliminar));
        });
    }

    [Fact]
    public void Politicas_historia_clinica_y_cobros_dicen_a_confirmar_D_DER_03()
    {
        var f = new Fabrica();
        var politicas = f.Derechos.Politicas();

        var clinica = politicas.Where(p => p.Categoria.Contains("clínic", StringComparison.OrdinalIgnoreCase)
                                           || p.Categoria.Contains("clinic", StringComparison.OrdinalIgnoreCase)).ToList();
        var cobros = politicas.Where(p => p.Categoria.Contains("cobr", StringComparison.OrdinalIgnoreCase)).ToList();

        Assert.NotEmpty(clinica);
        Assert.NotEmpty(cobros);
        Assert.All(clinica, p => Assert.Contains("a confirmar", p.Conservacion, StringComparison.OrdinalIgnoreCase));
        Assert.All(cobros, p => Assert.Contains("a confirmar", p.Conservacion, StringComparison.OrdinalIgnoreCase));
    }

    // ---------- Derechos: exportar (DR-01, DR-02) ----------

    [Fact]
    public void Exportar_devuelve_un_json_valido_con_los_datos_del_cliente()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.AnimalDe(cuenta.Id, "Perro", "Firulais");

        var json = f.Derechos.Exportar(cuenta);

        using var documento = JsonDocument.Parse(json);
        Assert.Contains(cuenta.Correo, json);
        Assert.Contains("Firulais", json);
    }

    [Fact]
    public void Exportar_nunca_incluye_el_derivado_de_la_contrasena()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        Assert.NotEqual("", cuenta.HashContrasena);

        var json = f.Derechos.Exportar(cuenta);

        Assert.DoesNotContain(cuenta.HashContrasena, json);
    }

    [Fact]
    public void Exportar_no_incluye_datos_de_otro_cliente()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        f.AnimalDe(a.Id, "Perro", "Firulais");
        f.AnimalDe(b.Id, "Gato", "Michiajeno");
        f.Comunicaciones.Encolar(b.Id, ComunicacionesService.AvisoRefuerzo, "AsuntoDeB", "CuerpoDeB");

        var json = f.Derechos.Exportar(a);

        Assert.DoesNotContain(b.Correo, json);
        Assert.DoesNotContain("Michiajeno", json);
        Assert.DoesNotContain("AsuntoDeB", json);
        Assert.DoesNotContain(b.NumeroDocumento, json);
    }

    [Fact]
    public void Exportar_incluye_las_comunicaciones_del_propio_cliente()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "AsuntoPropioQw", "Cuerpo");

        var json = f.Derechos.Exportar(cuenta);

        Assert.Contains("AsuntoPropioQw", json);
    }

    [Fact]
    public void SolicitarExportacion_deja_un_pedido_completado_de_tipo_exportacion()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        var r = f.Derechos.SolicitarExportacion(cuenta);

        Assert.True(r.Exito);
        var pedido = Assert.Single(f.Derechos.DeCuenta(cuenta.Id));
        Assert.Equal(DerechosService.Exportacion, pedido.Tipo);
        Assert.Equal(DerechosService.Completada, pedido.Estado);
        Assert.Equal(cuenta.Id, pedido.CuentaId);
    }

    [Fact]
    public void SolicitarExportacion_sin_cuenta_falla_y_no_deja_pedido()
    {
        var f = new Fabrica();
        var antes = f.Derechos.Todas().Count;

        var r = f.Derechos.SolicitarExportacion(null);

        Assert.False(r.Exito);
        Assert.Equal(antes, f.Derechos.Todas().Count);
    }

    [Fact]
    public void SolicitarExportacion_con_cuenta_eliminada_falla()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        Assert.True(f.Cuentas.Anonimizar(cuenta.Id));
        var eliminada = Actual(f, cuenta.Id);
        var antes = f.Derechos.Todas().Count;

        var r = f.Derechos.SolicitarExportacion(eliminada);

        Assert.False(r.Exito);
        Assert.Equal(antes, f.Derechos.Todas().Count);
    }

    // ---------- Derechos: eliminar, solicitud (DR-03, DR-04) ----------

    [Fact]
    public void SolicitarEliminacion_sin_confirmar_falla_y_no_deja_pedido()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        var r = f.Derechos.SolicitarEliminacion(cuenta, false);

        Assert.False(r.Exito);
        Assert.Empty(f.Derechos.DeCuenta(cuenta.Id));
    }

    [Fact]
    public void SolicitarEliminacion_confirmada_deja_un_pedido_recibido()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        var r = f.Derechos.SolicitarEliminacion(cuenta, true);

        Assert.True(r.Exito);
        var pedido = Assert.Single(f.Derechos.DeCuenta(cuenta.Id));
        Assert.Equal(DerechosService.Eliminacion, pedido.Tipo);
        Assert.Equal(DerechosService.Recibida, pedido.Estado);
        Assert.False(Actual(f, cuenta.Id).Eliminada);
    }

    [Fact]
    public void SolicitarEliminacion_sin_cuenta_falla()
    {
        var f = new Fabrica();

        Assert.False(f.Derechos.SolicitarEliminacion(null, true).Exito);
    }

    [Fact]
    public void SolicitarEliminacion_con_un_pedido_recibido_no_admite_otro()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        Assert.True(f.Derechos.SolicitarEliminacion(cuenta, true).Exito);

        var r = f.Derechos.SolicitarEliminacion(cuenta, true);

        Assert.False(r.Exito);
        Assert.Single(f.Derechos.DeCuenta(cuenta.Id).Where(s => s.Tipo == DerechosService.Eliminacion));
    }

    [Fact]
    public void SolicitarEliminacion_con_un_pedido_de_otro_cliente_no_bloquea()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        Assert.True(f.Derechos.SolicitarEliminacion(a, true).Exito);

        Assert.True(f.Derechos.SolicitarEliminacion(b, true).Exito);
    }

    [Fact]
    public void ObstaculosEliminacion_de_un_cliente_sin_pendientes_esta_vacia()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.AnimalDe(cuenta.Id);

        Assert.Empty(f.Derechos.ObstaculosEliminacion(cuenta.Id));
    }

    [Fact]
    public void Un_turno_proximo_es_obstaculo_y_impide_pedir_la_eliminacion()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var animal = f.AnimalDe(cuenta.Id);
        TurnoProximoDe(f, cuenta, animal);

        var obstaculos = f.Derechos.ObstaculosEliminacion(cuenta.Id);
        var r = f.Derechos.SolicitarEliminacion(cuenta, true);

        Assert.NotEmpty(obstaculos);
        Assert.False(r.Exito);
        Assert.Empty(f.Derechos.DeCuenta(cuenta.Id));
    }

    [Fact]
    public void Un_turno_cancelado_no_es_obstaculo()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var animal = f.AnimalDe(cuenta.Id);
        var turno = TurnoProximoDe(f, cuenta, animal);
        turno.Estado = "Cancelado";

        Assert.Empty(f.Derechos.ObstaculosEliminacion(cuenta.Id));
        Assert.True(f.Derechos.SolicitarEliminacion(cuenta, true).Exito);
    }

    [Fact]
    public void Un_turno_proximo_de_otro_cliente_no_es_obstaculo()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        var animalB = f.AnimalDe(b.Id);
        TurnoProximoDe(f, b, animalB);

        Assert.Empty(f.Derechos.ObstaculosEliminacion(a.Id));
        Assert.True(f.Derechos.SolicitarEliminacion(a, true).Exito);
    }

    // ---------- Derechos: cancelar (DR-05) ----------

    [Fact]
    public void Cancelar_el_propio_pedido_recibido_lo_deja_cancelado_y_permite_pedir_de_nuevo()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var pedido = UltimaSolicitud(f, cuenta, DerechosService.Eliminacion);

        var r = f.Derechos.Cancelar(pedido.Id, cuenta.Id);

        Assert.True(r.Exito);
        Assert.Equal(DerechosService.Cancelada, pedido.Estado);
        Assert.True(f.Derechos.SolicitarEliminacion(cuenta, true).Exito);
    }

    [Fact]
    public void Cancelar_el_pedido_de_otro_cliente_falla_y_no_cambia_nada()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        f.Derechos.SolicitarEliminacion(a, true);
        var pedido = UltimaSolicitud(f, a, DerechosService.Eliminacion);

        var r = f.Derechos.Cancelar(pedido.Id, b.Id);

        Assert.False(r.Exito);
        Assert.Equal(DerechosService.Recibida, pedido.Estado);
    }

    [Fact]
    public void Cancelar_un_pedido_ya_cancelado_o_completado_falla()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Derechos.SolicitarExportacion(cuenta);
        var exportacion = UltimaSolicitud(f, cuenta, DerechosService.Exportacion);
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var eliminacion = UltimaSolicitud(f, cuenta, DerechosService.Eliminacion);
        Assert.True(f.Derechos.Cancelar(eliminacion.Id, cuenta.Id).Exito);

        Assert.False(f.Derechos.Cancelar(eliminacion.Id, cuenta.Id).Exito);
        Assert.False(f.Derechos.Cancelar(exportacion.Id, cuenta.Id).Exito);
        Assert.Equal(DerechosService.Completada, exportacion.Estado);
        Assert.Equal(DerechosService.Cancelada, eliminacion.Estado);
    }

    [Fact]
    public void Cancelar_un_id_inexistente_falla()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        Assert.False(f.Derechos.Cancelar(999999, cuenta.Id).Exito);
    }

    [Fact]
    public void Un_pedido_resuelto_no_se_puede_cancelar()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var pedido = UltimaSolicitud(f, cuenta, DerechosService.Eliminacion);
        Assert.True(f.Derechos.Resolver(pedido.Id, false, "Motivo de rechazo", f.PersonalDe(Administrador)).Exito);

        Assert.False(f.Derechos.Cancelar(pedido.Id, cuenta.Id).Exito);
        Assert.Equal(DerechosService.Rechazada, pedido.Estado);
    }

    // ---------- Derechos: resolver (DR-06, DR-07, DR-08) ----------

    [Fact]
    public void Rechazar_con_motivo_deja_el_pedido_rechazado_y_la_cuenta_intacta()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var correo = cuenta.Correo;
        var nombre = cuenta.Nombre;
        var hash = cuenta.HashContrasena;
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var pedido = UltimaSolicitud(f, cuenta, DerechosService.Eliminacion);

        var r = f.Derechos.Resolver(pedido.Id, false, "Falta verificar identidad", f.PersonalDe(Administrador));

        Assert.True(r.Exito);
        Assert.Equal(DerechosService.Rechazada, pedido.Estado);
        Assert.NotNull(pedido.FechaResolucion);
        Assert.NotEqual("", pedido.ResueltaPor);
        Assert.Contains("Falta verificar identidad", pedido.Detalle);
        var actual = Actual(f, cuenta.Id);
        Assert.False(actual.Eliminada);
        Assert.Equal(correo, actual.Correo);
        Assert.Equal(nombre, actual.Nombre);
        Assert.Equal(hash, actual.HashContrasena);
    }

    [Fact]
    public void Rechazar_sin_motivo_falla_y_el_pedido_sigue_recibido()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var pedido = UltimaSolicitud(f, cuenta, DerechosService.Eliminacion);

        var r = f.Derechos.Resolver(pedido.Id, false, "", f.PersonalDe(Administrador));

        Assert.False(r.Exito);
        Assert.Equal(DerechosService.Recibida, pedido.Estado);
        Assert.Null(pedido.FechaResolucion);
    }

    [Fact]
    public void Aprobar_anonimiza_la_cuenta_borra_mensajes_y_preferencias_y_completa_el_pedido()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var id = cuenta.Id;
        var correo = cuenta.Correo;
        var documento = cuenta.NumeroDocumento;
        var telefono = cuenta.Telefono;
        var hash = cuenta.HashContrasena;
        f.Comunicaciones.CambiarPreferencia(id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Correo);
        f.Comunicaciones.Encolar(id, ComunicacionesService.AvisoRefuerzo, "A", "C");
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var pedido = UltimaSolicitud(f, cuenta, DerechosService.Eliminacion);

        var r = f.Derechos.Resolver(pedido.Id, true, "", f.PersonalDe(Administrador));

        Assert.True(r.Exito);
        Assert.Equal(DerechosService.Completada, pedido.Estado);
        Assert.NotNull(pedido.FechaResolucion);
        var actual = Actual(f, id);
        Assert.True(actual.Eliminada);
        Assert.NotNull(actual.FechaEliminacion);
        Assert.NotEqual(correo, actual.Correo);
        Assert.NotEqual("Juana", actual.Nombre);
        Assert.NotEqual("Pérez", actual.Apellido);
        Assert.NotEqual(documento, actual.NumeroDocumento);
        Assert.NotEqual(telefono, actual.Telefono);
        Assert.NotEqual("Rivera", actual.Calle);
        Assert.NotEqual(hash, actual.HashContrasena);
        Assert.Empty(f.Comunicaciones.DeCuenta(id));
        Assert.Equal(ComunicacionesService.Ninguno, f.Comunicaciones.CanalDe(id, ComunicacionesService.AlertaSanitaria));
    }

    [Fact]
    public void Aprobar_conserva_los_animales_con_su_responsable_DR_07()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var animal = f.AnimalDe(cuenta.Id, "Perro", "Firulais");
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var pedido = UltimaSolicitud(f, cuenta, DerechosService.Eliminacion);

        Assert.True(f.Derechos.Resolver(pedido.Id, true, "", f.PersonalDe(Administrador)).Exito);

        var animales = f.Animales.ObtenerDe(cuenta.Id);
        Assert.Contains(animales, a => a.Id == animal.Id && a.ResponsableId == cuenta.Id);
    }

    [Fact]
    public void Aprobar_no_toca_las_cuentas_ni_mensajes_de_otros_clientes()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        var correoB = b.Correo;
        var nombreB = b.Nombre;
        f.Comunicaciones.Encolar(b.Id, ComunicacionesService.AvisoRefuerzo, "DeB", "C");
        f.Derechos.SolicitarEliminacion(a, true);
        var pedido = UltimaSolicitud(f, a, DerechosService.Eliminacion);

        Assert.True(f.Derechos.Resolver(pedido.Id, true, "", f.PersonalDe(Administrador)).Exito);

        var actualB = Actual(f, b.Id);
        Assert.False(actualB.Eliminada);
        Assert.Equal(correoB, actualB.Correo);
        Assert.Equal(nombreB, actualB.Nombre);
        Assert.Single(f.Comunicaciones.DeCuenta(b.Id));
    }

    [Fact]
    public void Resolver_como_recepcion_o_veterinario_falla_y_no_cambia_nada()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var pedido = UltimaSolicitud(f, cuenta, DerechosService.Eliminacion);

        Assert.False(f.Derechos.Resolver(pedido.Id, true, "", f.PersonalDe(Recepcion)).Exito);
        Assert.False(f.Derechos.Resolver(pedido.Id, true, "", f.PersonalDe(Veterinario)).Exito);
        Assert.False(f.Derechos.Resolver(pedido.Id, false, "Motivo", f.PersonalDe(Recepcion)).Exito);

        Assert.Equal(DerechosService.Recibida, pedido.Estado);
        Assert.False(Actual(f, cuenta.Id).Eliminada);
    }

    [Fact]
    public void Resolver_sin_persona_o_con_administrador_inactivo_falla()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var pedido = UltimaSolicitud(f, cuenta, DerechosService.Eliminacion);

        Assert.False(f.Derechos.Resolver(pedido.Id, true, "", null).Exito);
        Assert.False(f.Derechos.Resolver(pedido.Id, true, "", Inactivo(f, Administrador)).Exito);

        Assert.Equal(DerechosService.Recibida, pedido.Estado);
        Assert.False(Actual(f, cuenta.Id).Eliminada);
    }

    [Fact]
    public void Resolver_un_id_inexistente_falla()
    {
        var f = new Fabrica();

        Assert.False(f.Derechos.Resolver(999999, true, "", f.PersonalDe(Administrador)).Exito);
    }

    [Fact]
    public void Resolver_un_pedido_ya_resuelto_o_cancelado_falla()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var cancelado = UltimaSolicitud(f, cuenta, DerechosService.Eliminacion);
        Assert.True(f.Derechos.Cancelar(cancelado.Id, cuenta.Id).Exito);

        Assert.False(f.Derechos.Resolver(cancelado.Id, true, "", f.PersonalDe(Administrador)).Exito);
        Assert.Equal(DerechosService.Cancelada, cancelado.Estado);
        Assert.False(Actual(f, cuenta.Id).Eliminada);
    }

    [Fact]
    public void Aprobar_dos_veces_el_mismo_pedido_falla_la_segunda()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var pedido = UltimaSolicitud(f, cuenta, DerechosService.Eliminacion);
        Assert.True(f.Derechos.Resolver(pedido.Id, true, "", f.PersonalDe(Administrador)).Exito);

        Assert.False(f.Derechos.Resolver(pedido.Id, true, "", f.PersonalDe(Administrador)).Exito);
    }

    [Fact]
    public void Aprobar_vuelve_a_controlar_los_obstaculos_y_si_aparecio_uno_falla()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        var animal = f.AnimalDe(cuenta.Id);
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var pedido = UltimaSolicitud(f, cuenta, DerechosService.Eliminacion);
        TurnoProximoDe(f, cuenta, animal);

        var r = f.Derechos.Resolver(pedido.Id, true, "", f.PersonalDe(Administrador));

        Assert.False(r.Exito);
        Assert.Equal(DerechosService.Recibida, pedido.Estado);
        Assert.False(Actual(f, cuenta.Id).Eliminada);
    }

    [Fact]
    public void Tras_eliminar_la_cuenta_no_se_pueden_pedir_exportacion_ni_comunicaciones()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var pedido = UltimaSolicitud(f, cuenta, DerechosService.Eliminacion);
        Assert.True(f.Derechos.Resolver(pedido.Id, true, "", f.PersonalDe(Administrador)).Exito);

        Assert.False(f.Derechos.SolicitarExportacion(Actual(f, cuenta.Id)).Exito);
        Assert.Null(f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "A", "C"));
    }

    [Fact]
    public void Aprobar_y_rechazar_dejan_registro_en_datos_personales_sin_datos_personales()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        var correoA = a.Correo;
        var documentoA = a.NumeroDocumento;
        var correoB = b.Correo;
        f.Derechos.SolicitarEliminacion(a, true);
        f.Derechos.SolicitarEliminacion(b, true);
        var pedidoA = UltimaSolicitud(f, a, DerechosService.Eliminacion);
        var pedidoB = UltimaSolicitud(f, b, DerechosService.Eliminacion);
        var antes = RegistrosBitacora(f, BitacoraService.DatosPersonales);

        Assert.True(f.Derechos.Resolver(pedidoA.Id, true, "", f.PersonalDe(Administrador)).Exito);
        var trasAprobar = RegistrosBitacora(f, BitacoraService.DatosPersonales);
        Assert.True(f.Derechos.Resolver(pedidoB.Id, false, "Motivo", f.PersonalDe(Administrador)).Exito);
        var trasRechazar = RegistrosBitacora(f, BitacoraService.DatosPersonales);

        Assert.Equal(antes + 1, trasAprobar);
        Assert.Equal(trasAprobar + 1, trasRechazar);
        var registros = f.Bitacora.Buscar(f.PersonalDe(Administrador), entidad: BitacoraService.DatosPersonales);
        Assert.DoesNotContain(registros, r => r.Detalle.Contains(correoA) || r.Detalle.Contains(documentoA) || r.Detalle.Contains(correoB));
        Assert.DoesNotContain(registros, r => r.Detalle.Contains("Juana"));
    }

    [Fact]
    public void Resolver_rechazado_por_permiso_motivo_u_obstaculo_no_deja_registro_en_datos_personales()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var pedido = UltimaSolicitud(f, cuenta, DerechosService.Eliminacion);
        var antes = RegistrosBitacora(f, BitacoraService.DatosPersonales);

        f.Derechos.Resolver(pedido.Id, true, "", f.PersonalDe(Recepcion));
        f.Derechos.Resolver(pedido.Id, false, "", f.PersonalDe(Administrador));
        f.Derechos.Resolver(999999, true, "", f.PersonalDe(Administrador));

        Assert.Equal(antes, RegistrosBitacora(f, BitacoraService.DatosPersonales));
    }

    // ---------- Derechos: listas y aislamiento ----------

    [Fact]
    public void DeCuenta_de_derechos_muestra_solo_los_pedidos_del_titular_y_Todas_los_de_todos()
    {
        var f = new Fabrica();
        var a = Cliente(f, 0);
        var b = Cliente(f, 1);
        f.Derechos.SolicitarExportacion(a);
        f.Derechos.SolicitarEliminacion(b, true);

        var deA = f.Derechos.DeCuenta(a.Id);
        var deB = f.Derechos.DeCuenta(b.Id);

        Assert.Single(deA);
        Assert.Single(deB);
        Assert.All(deA, s => Assert.Equal(a.Id, s.CuentaId));
        Assert.All(deB, s => Assert.Equal(b.Id, s.CuentaId));
        Assert.Contains(f.Derechos.Todas(), s => s.CuentaId == a.Id);
        Assert.Contains(f.Derechos.Todas(), s => s.CuentaId == b.Id);
    }

    [Fact]
    public void Bitacora_Buscar_solo_responde_al_administrador()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);
        f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Recepcion), "T", "M");

        Assert.NotEmpty(f.Bitacora.Buscar(f.PersonalDe(Administrador), entidad: BitacoraService.ComunicacionesEntidad));
        Assert.Empty(f.Bitacora.Buscar(f.PersonalDe(Recepcion), entidad: BitacoraService.ComunicacionesEntidad));
        Assert.Empty(f.Bitacora.Buscar(f.PersonalDe(Veterinario), entidad: BitacoraService.ComunicacionesEntidad));
        Assert.Empty(f.Bitacora.Buscar(null, entidad: BitacoraService.ComunicacionesEntidad));
    }

    // ---------- CuentaService.Anonimizar ----------

    [Fact]
    public void Anonimizar_una_cuenta_inexistente_devuelve_false()
    {
        var f = new Fabrica();

        Assert.False(f.Cuentas.Anonimizar(999999));
    }

    [Fact]
    public void Todas_incluye_las_cuentas_registradas()
    {
        var f = new Fabrica();
        var cuenta = Cliente(f);

        Assert.Contains(f.Cuentas.Todas(), c => c.Id == cuenta.Id);
    }
}
