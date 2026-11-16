using System.Text.Json;
using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// A-028: RF-CLI-19, RNF-LEG-01, RNF-LEG-02, RNF-LEG-06, RN-43.
public class DerechosTests
{
    private const string Recepcion = "recepcion@vitalia.uy";
    private const string Admin = "admin@vitalia.uy";

    [Fact]
    public void Exportar_entrega_un_json_con_los_datos_del_titular_y_sus_animales()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.AnimalDe(cuenta.Id, "Perro", "Firulais");

        var json = f.Derechos.Exportar(cuenta);

        using var documento = JsonDocument.Parse(json);
        var raiz = documento.RootElement;
        Assert.Equal(cuenta.Correo, raiz.GetProperty("titular").GetProperty("correo").GetString());
        Assert.Equal("Firulais", raiz.GetProperty("animales")[0].GetProperty("nombre").GetString());
        Assert.DoesNotContain("HashContrasena", json);
        Assert.DoesNotContain(cuenta.HashContrasena, json);
    }

    [Fact]
    public void Exportar_no_incluye_datos_de_otro_cliente()
    {
        var f = new Fabrica();
        var ana = f.ClienteVerificado("ana@prueba.uy", "1.111.111-1");
        var beto = f.ClienteVerificado("beto@prueba.uy", "2.222.222-2");
        f.AnimalDe(beto.Id, "Gato", "Michi");

        var json = f.Derechos.Exportar(ana);

        Assert.DoesNotContain("Michi", json);
        Assert.DoesNotContain(beto.Correo, json);
    }

    [Fact]
    public void Pedir_la_exportacion_queda_registrado_como_completado()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();

        var resultado = f.Derechos.SolicitarExportacion(cuenta);

        Assert.True(resultado.Exito);
        var solicitud = Assert.Single(f.Derechos.DeCuenta(cuenta.Id));
        Assert.Equal(DerechosService.Exportacion, solicitud.Tipo);
        Assert.Equal(DerechosService.Completada, solicitud.Estado);
        Assert.False(f.Derechos.SolicitarExportacion(null).Exito);
    }

    [Fact]
    public void Pedir_la_eliminacion_exige_confirmar_y_no_se_duplica()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();

        Assert.False(f.Derechos.SolicitarEliminacion(cuenta, false).Exito);
        Assert.True(f.Derechos.SolicitarEliminacion(cuenta, true).Exito);
        Assert.False(f.Derechos.SolicitarEliminacion(cuenta, true).Exito);
        Assert.Single(f.Derechos.DeCuenta(cuenta.Id));
    }

    [Fact]
    public void No_se_pide_la_eliminacion_con_un_cargo_con_saldo_pendiente()
    {
        var (f, cuenta) = ClienteConCargoPendiente();

        var resultado = f.Derechos.SolicitarEliminacion(cuenta, true);

        Assert.False(resultado.Exito);
        Assert.Contains("saldo", resultado.Mensaje);
        Assert.NotEmpty(f.Derechos.ObstaculosEliminacion(cuenta.Id));
    }

    [Fact]
    public void No_se_pide_la_eliminacion_con_un_turno_proximo()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        var animal = f.AnimalDe(cuenta.Id);
        var dia = f.DiaQueAtiende(1);
        var horario = f.Agenda.HorariosLibres(1, 1, animal.Id, dia).First();
        Assert.True(f.Agenda.Reservar(cuenta.Id, animal.Id, 1, 1, horario, out _).Exito);

        var resultado = f.Derechos.SolicitarEliminacion(cuenta, true);

        Assert.False(resultado.Exito);
        Assert.Contains("turnos", resultado.Mensaje);
    }

    [Fact]
    public void El_titular_cancela_su_pedido_mientras_nadie_lo_resolvio()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var solicitud = f.Derechos.DeCuenta(cuenta.Id).Single();

        Assert.False(f.Derechos.Cancelar(solicitud.Id, cuenta.Id + 1).Exito);
        Assert.True(f.Derechos.Cancelar(solicitud.Id, cuenta.Id).Exito);
        Assert.Equal(DerechosService.Cancelada, solicitud.Estado);
        Assert.False(f.Derechos.Cancelar(solicitud.Id, cuenta.Id).Exito);
        Assert.True(f.Derechos.SolicitarEliminacion(cuenta, true).Exito);
    }

    [Fact]
    public void Aprobar_anonimiza_la_cuenta_y_conserva_el_animal_sin_identificar_al_titular()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        var animal = f.AnimalDe(cuenta.Id, "Perro", "Firulais");
        var correoAnterior = cuenta.Correo;
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var solicitud = f.Derechos.DeCuenta(cuenta.Id).Single();

        var resultado = f.Derechos.Resolver(solicitud.Id, true, "", f.PersonalDe(Admin));

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.Equal(DerechosService.Completada, solicitud.Estado);
        var anonima = f.Cuentas.ObtenerPorId(cuenta.Id)!;
        Assert.True(anonima.Eliminada);
        Assert.Equal("", anonima.Telefono);
        Assert.Equal("", anonima.NumeroDocumento);
        Assert.Equal("", anonima.Calle);
        Assert.NotEqual(correoAnterior, anonima.Correo);
        Assert.Null(f.Cuentas.ObtenerPorCorreo(correoAnterior));
        // La historia clínica sigue: el animal existe y su responsable es la cuenta anonimizada.
        Assert.Equal(cuenta.Id, f.Animales.ObtenerParaPersonal(animal.Id)!.ResponsableId);
    }

    [Fact]
    public void Una_cuenta_eliminada_no_puede_ingresar_ni_recibe_mensajes()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.Derechos.SolicitarEliminacion(cuenta, true);
        f.Derechos.Resolver(f.Derechos.DeCuenta(cuenta.Id).Single().Id, true, "", f.PersonalDe(Admin));

        var ingreso = f.Sesion.Ingresar(new InicioSesion { Correo = "cliente@prueba.uy", Contrasena = "clave-segura-1" });

        Assert.False(ingreso.Exito);
        Assert.Null(f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoOperativo, "Asunto", "Cuerpo"));
    }

    [Fact]
    public void Aprobar_borra_los_mensajes_y_las_preferencias_del_titular()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.SoloApp);
        f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Recepcion), "Aviso", "Texto");
        Assert.NotEmpty(f.Comunicaciones.DeCuenta(cuenta.Id));
        f.Derechos.SolicitarEliminacion(cuenta, true);

        f.Derechos.Resolver(f.Derechos.DeCuenta(cuenta.Id).Single().Id, true, "", f.PersonalDe(Admin));

        Assert.Empty(f.Comunicaciones.DeCuenta(cuenta.Id));
        Assert.Equal(ComunicacionesService.Correo, f.Comunicaciones.CanalDe(cuenta.Id, ComunicacionesService.AvisoRefuerzo));
    }

    [Fact]
    public void Rechazar_exige_motivo_y_deja_la_cuenta_intacta()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var solicitud = f.Derechos.DeCuenta(cuenta.Id).Single();
        var admin = f.PersonalDe(Admin);

        Assert.False(f.Derechos.Resolver(solicitud.Id, false, " ", admin).Exito);
        var resultado = f.Derechos.Resolver(solicitud.Id, false, "Hay un trámite abierto.", admin);

        Assert.True(resultado.Exito);
        Assert.Equal(DerechosService.Rechazada, solicitud.Estado);
        Assert.Equal("Hay un trámite abierto.", solicitud.Detalle);
        Assert.False(f.Cuentas.ObtenerPorId(cuenta.Id)!.Eliminada);
    }

    [Fact]
    public void Solo_el_administrador_resuelve()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var solicitud = f.Derechos.DeCuenta(cuenta.Id).Single();

        Assert.False(f.Derechos.Resolver(solicitud.Id, true, "", f.PersonalDe(Recepcion)).Exito);
        Assert.False(f.Derechos.Resolver(solicitud.Id, true, "", null).Exito);
        Assert.Equal(DerechosService.Recibida, solicitud.Estado);
    }

    [Fact]
    public void No_se_aprueba_si_aparecio_una_deuda_despues_del_pedido()
    {
        var (f, cuenta) = ClienteConCargoPendiente(pedirAntes: true);
        var solicitud = f.Derechos.DeCuenta(cuenta.Id).Single();

        var resultado = f.Derechos.Resolver(solicitud.Id, true, "", f.PersonalDe(Admin));

        Assert.False(resultado.Exito);
        Assert.Equal(DerechosService.Recibida, solicitud.Estado);
        Assert.False(f.Cuentas.ObtenerPorId(cuenta.Id)!.Eliminada);
    }

    [Fact]
    public void Resolver_un_pedido_ya_resuelto_falla_y_la_resolucion_queda_en_la_bitacora()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.Derechos.SolicitarEliminacion(cuenta, true);
        var solicitud = f.Derechos.DeCuenta(cuenta.Id).Single();
        var admin = f.PersonalDe(Admin);
        f.Derechos.Resolver(solicitud.Id, true, "", admin);

        Assert.False(f.Derechos.Resolver(solicitud.Id, true, "", admin).Exito);
        var registros = f.Bitacora.Buscar(admin, entidad: BitacoraService.DatosPersonales);
        Assert.Single(registros);
        Assert.DoesNotContain("@", registros[0].Detalle);
    }

    [Fact]
    public void La_politica_de_conservacion_describe_cada_categoria_sin_inventar_plazos_legales()
    {
        var f = new Fabrica();

        var politicas = f.Derechos.Politicas();

        Assert.True(politicas.Count >= 5);
        Assert.All(politicas, p =>
        {
            Assert.NotEqual("", p.Categoria);
            Assert.NotEqual("", p.Finalidad);
            Assert.NotEqual("", p.Conservacion);
            Assert.NotEqual("", p.AlEliminar);
        });
        Assert.Contains(politicas, p => p.Categoria.StartsWith("Animales e historia clínica") && p.Conservacion.Contains("a confirmar"));
    }

    // Un cliente con una consulta firmada y sin cobrar: tiene un cargo con saldo.
    private static (Fabrica f, Cuenta cuenta) ClienteConCargoPendiente(bool pedirAntes = false)
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        var animal = f.AnimalDe(cuenta.Id);
        if (pedirAntes)
        {
            Assert.True(f.Derechos.SolicitarEliminacion(cuenta, true).Exito);
        }

        var vet = f.PersonalDe("ferreira@vitalia.uy");
        var turno = f.TurnoEnEspera();
        turno.ClienteId = cuenta.Id;
        turno.AnimalId = animal.Id;
        f.Consultas.Iniciar(turno.Id, vet);
        var consulta = f.Consultas.ObtenerPorTurno(turno.Id)!;
        Assert.True(f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet).Exito);
        return (f, cuenta);
    }
}
