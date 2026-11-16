using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// A-028: RF-CLI-07, RF-CLI-10, RF-CLI-18, RF-CLI-20, RF-REC-07, RN-43, RNF-CON-04, RNF-CON-10.
public class ComunicacionesTests
{
    private const string Ferreira = "ferreira@vitalia.uy";
    private const string Recepcion = "recepcion@vitalia.uy";
    private const string Admin = "admin@vitalia.uy";

    // Un cliente con un animal, cuyo turno en espera de Toby se le reasigna para firmar una consulta.
    private static (Fabrica f, Cuenta cuenta, Comunicacion? resumen) ConsultaFirmada(string canal = "")
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        var animal = f.AnimalDe(cuenta.Id);
        if (canal != "")
        {
            Assert.True(f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.ResumenClinico, canal).Exito);
        }

        var vet = f.PersonalDe(Ferreira);
        var turno = f.TurnoEnEspera();
        turno.ClienteId = cuenta.Id;
        turno.AnimalId = animal.Id;
        f.Consultas.Iniciar(turno.Id, vet);
        var consulta = f.Consultas.ObtenerPorTurno(turno.Id)!;
        var firma = f.Consultas.Firmar(consulta.Id, consulta.Version, Fabrica.ConsultaCompleta(), PersonalService.ContrasenaDemo, vet);
        Assert.True(firma.Exito, firma.Mensaje);
        return (f, cuenta, f.Comunicaciones.DeCuenta(cuenta.Id).FirstOrDefault(c => c.Tipo == ComunicacionesService.ResumenClinico));
    }

    [Fact]
    public void Firmar_la_consulta_envia_el_resumen_por_correo_al_responsable()
    {
        var (_, cuenta, resumen) = ConsultaFirmada();

        Assert.NotNull(resumen);
        Assert.Equal("Correo", resumen!.Canal);
        Assert.Equal(ComunicacionesService.Enviado, resumen.Estado);
        Assert.Equal(cuenta.Correo, resumen.Destino);
        Assert.Contains("Gastritis", resumen.Cuerpo);
        Assert.Contains("Firulais", resumen.Cuerpo);
    }

    [Fact]
    public void Con_la_preferencia_solo_app_el_resumen_queda_en_la_cuenta_sin_correo()
    {
        var (_, _, resumen) = ConsultaFirmada(ComunicacionesService.SoloApp);

        Assert.NotNull(resumen);
        Assert.Equal("En la app", resumen!.Canal);
        Assert.Equal(ComunicacionesService.Enviado, resumen.Estado);
        Assert.Equal("", resumen.Destino);
    }

    [Fact]
    public void Un_tipo_esencial_no_se_puede_dejar_de_recibir()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();

        var resultado = f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.ResumenClinico, ComunicacionesService.Ninguno);

        Assert.False(resultado.Exito);
        Assert.Equal(ComunicacionesService.Correo, f.Comunicaciones.CanalDe(cuenta.Id, ComunicacionesService.ResumenClinico));
    }

    [Fact]
    public void Los_tipos_opcionales_no_vienen_premarcados_salvo_el_aviso_de_refuerzo()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();

        Assert.Equal(ComunicacionesService.Ninguno, f.Comunicaciones.CanalDe(cuenta.Id, ComunicacionesService.AlertaSanitaria));
        Assert.Equal(ComunicacionesService.Correo, f.Comunicaciones.CanalDe(cuenta.Id, ComunicacionesService.AvisoRefuerzo));
    }

    [Fact]
    public void Cambiar_la_preferencia_con_un_canal_o_tipo_inexistente_falla()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();

        Assert.False(f.Comunicaciones.CambiarPreferencia(cuenta.Id, "Inventado", ComunicacionesService.Correo).Exito);
        Assert.False(f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, "Paloma").Exito);
        Assert.False(f.Comunicaciones.CambiarPreferencia(9999, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.Correo).Exito);
    }

    [Fact]
    public void La_alerta_sanitaria_llega_solo_a_quien_la_activo_y_tiene_esa_especie()
    {
        var f = new Fabrica();
        var conPerro = f.ClienteVerificado("perro@prueba.uy", "1.111.111-1");
        f.AnimalDe(conPerro.Id, "Perro", "Rocky");
        var conGato = f.ClienteVerificado("gato@prueba.uy", "2.222.222-2");
        f.AnimalDe(conGato.Id, "Gato", "Mishi");
        var sinActivar = f.ClienteVerificado("sin@prueba.uy", "3.333.333-3");
        f.AnimalDe(sinActivar.Id, "Perro", "Luna");
        f.Comunicaciones.CambiarPreferencia(conPerro.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Correo);
        f.Comunicaciones.CambiarPreferencia(conGato.Id, ComunicacionesService.AlertaSanitaria, ComunicacionesService.Correo);

        var resultado = f.Comunicaciones.EnviarAlertaSanitaria(f.PersonalDe(Admin), "Brote de parvovirus", "Hay casos en la zona.", "Perro");

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.Single(f.Comunicaciones.DeCuenta(conPerro.Id), c => c.Tipo == ComunicacionesService.AlertaSanitaria);
        Assert.DoesNotContain(f.Comunicaciones.DeCuenta(conGato.Id), c => c.Tipo == ComunicacionesService.AlertaSanitaria);
        Assert.DoesNotContain(f.Comunicaciones.DeCuenta(sinActivar.Id), c => c.Tipo == ComunicacionesService.AlertaSanitaria);
    }

    [Fact]
    public void Solo_administrador_o_veterinario_emiten_alertas_y_solo_recepcion_o_administrador_avisos()
    {
        var f = new Fabrica();

        Assert.False(f.Comunicaciones.EnviarAlertaSanitaria(f.PersonalDe(Recepcion), "T", "M").Exito);
        Assert.True(f.Comunicaciones.EnviarAlertaSanitaria(f.PersonalDe(Ferreira), "T", "M").Exito);
        Assert.False(f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Ferreira), "T", "M").Exito);
        Assert.True(f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Recepcion), "T", "M").Exito);
        Assert.False(f.Comunicaciones.EnviarAvisoOperativo(null, "T", "M").Exito);
    }

    [Fact]
    public void El_aviso_operativo_valida_titulo_y_mensaje()
    {
        var f = new Fabrica();
        var recepcion = f.PersonalDe(Recepcion);

        Assert.False(f.Comunicaciones.EnviarAvisoOperativo(recepcion, "", "Mensaje").Exito);
        Assert.False(f.Comunicaciones.EnviarAvisoOperativo(recepcion, "Título", "  ").Exito);
        Assert.False(f.Comunicaciones.EnviarAvisoOperativo(recepcion, new string('x', 121), "Mensaje").Exito);
        Assert.False(f.Comunicaciones.EnviarAvisoOperativo(recepcion, "Título", new string('x', 1001)).Exito);
    }

    [Fact]
    public void El_aviso_operativo_llega_a_todos_los_clientes_verificados_aunque_prefieran_solo_app()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoOperativo, ComunicacionesService.SoloApp);

        f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Recepcion), "Cerrado el lunes", "Feriado.");

        var aviso = Assert.Single(f.Comunicaciones.DeCuenta(cuenta.Id));
        Assert.Equal("En la app", aviso.Canal);
    }

    [Fact]
    public void Un_envio_que_falla_se_reintenta_tres_veces_y_queda_fallido()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.Comunicaciones.SimularFalla = true;

        f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Recepcion), "Aviso", "Texto");
        var mensaje = Assert.Single(f.Comunicaciones.DeCuenta(cuenta.Id));
        Assert.Equal(ComunicacionesService.Pendiente, mensaje.Estado);
        Assert.Equal(1, mensaje.Intentos);

        f.Comunicaciones.ProcesarCola();
        Assert.Equal(ComunicacionesService.Pendiente, mensaje.Estado);
        f.Comunicaciones.ProcesarCola();
        Assert.Equal(ComunicacionesService.Fallido, mensaje.Estado);
        Assert.Equal(ComunicacionesService.MaxIntentos, mensaje.Intentos);
        Assert.NotEqual("", mensaje.UltimoError);

        // Ya Fallido, la cola no lo toca más.
        Assert.Equal(0, f.Comunicaciones.ProcesarCola());
    }

    [Fact]
    public void La_cola_envia_los_pendientes_cuando_el_servidor_vuelve()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.Comunicaciones.SimularFalla = true;
        f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Recepcion), "Aviso", "Texto");

        f.Comunicaciones.SimularFalla = false;
        f.Comunicaciones.ProcesarCola();

        var mensaje = Assert.Single(f.Comunicaciones.DeCuenta(cuenta.Id));
        Assert.Equal(ComunicacionesService.Enviado, mensaje.Estado);
        Assert.NotNull(mensaje.FechaEnvio);
    }

    [Fact]
    public void Recepcion_reintenta_un_envio_fallido_y_solo_ese()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.Comunicaciones.SimularFalla = true;
        f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Recepcion), "Aviso", "Texto");
        f.Comunicaciones.ProcesarCola();
        f.Comunicaciones.ProcesarCola();
        var mensaje = f.Comunicaciones.DeCuenta(cuenta.Id).Single();
        Assert.Equal(ComunicacionesService.Fallido, mensaje.Estado);

        Assert.False(f.Comunicaciones.Reintentar(mensaje.Id, f.PersonalDe(Ferreira)).Exito);

        f.Comunicaciones.SimularFalla = false;
        var resultado = f.Comunicaciones.Reintentar(mensaje.Id, f.PersonalDe(Recepcion));

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.Equal(ComunicacionesService.Enviado, mensaje.Estado);
        Assert.False(f.Comunicaciones.Reintentar(mensaje.Id, f.PersonalDe(Recepcion)).Exito);
    }

    [Fact]
    public void Reenviar_crea_un_mensaje_nuevo_por_correo_y_deja_el_original_igual()
    {
        var (f, cuenta, resumen) = ConsultaFirmada(ComunicacionesService.SoloApp);
        var recepcion = f.PersonalDe(Recepcion);

        var resultado = f.Comunicaciones.Reenviar(resumen!.Id, recepcion);

        Assert.True(resultado.Exito, resultado.Mensaje);
        var mensajes = f.Comunicaciones.DeCuenta(cuenta.Id);
        var copia = mensajes.First();
        Assert.Equal(resumen.Id, copia.ReenvioDeId);
        Assert.Equal("Correo", copia.Canal);
        Assert.Equal(cuenta.Correo, copia.Destino);
        Assert.Equal(recepcion.Nombre, copia.Origen);
        Assert.Equal("En la app", resumen.Canal);
        Assert.Equal(2, mensajes.Count(c => c.Tipo == ComunicacionesService.ResumenClinico));
    }

    [Fact]
    public void Solo_recepcion_o_administrador_reenvian()
    {
        var (f, _, resumen) = ConsultaFirmada();

        Assert.False(f.Comunicaciones.Reenviar(resumen!.Id, f.PersonalDe(Ferreira)).Exito);
        Assert.False(f.Comunicaciones.Reenviar(resumen.Id, null).Exito);
        Assert.False(f.Comunicaciones.Reenviar(9999, f.PersonalDe(Recepcion)).Exito);
    }

    [Fact]
    public void Reenviar_y_reintentar_quedan_en_la_bitacora()
    {
        var (f, _, resumen) = ConsultaFirmada();
        var admin = f.PersonalDe(Admin);

        f.Comunicaciones.Reenviar(resumen!.Id, f.PersonalDe(Recepcion));

        var registros = f.Bitacora.Buscar(admin, entidad: BitacoraService.ComunicacionesEntidad);
        Assert.Contains(registros, r => r.Accion == "Reenvío");
    }

    [Fact]
    public void El_comprobante_se_envia_una_vez_y_despues_se_reenvia_desde_la_bandeja()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        var recepcion = f.PersonalDe(Recepcion);
        var cargo = new Cargo { Id = 1, ClienteId = cuenta.Id, Descripcion = "Consulta clínica general", Monto = 1200 };
        var pago = new Pago { Id = 7, CargoId = 1, Numero = "C-000007", Monto = 1200, Medio = "Efectivo", Fecha = DateTime.Now };

        var primero = f.Comunicaciones.EnviarComprobante(pago, cargo, recepcion);
        var segundo = f.Comunicaciones.EnviarComprobante(pago, cargo, recepcion);

        Assert.True(primero.Exito, primero.Mensaje);
        Assert.False(segundo.Exito);
        var mensaje = Assert.Single(f.Comunicaciones.DeCuenta(cuenta.Id));
        Assert.Contains("C-000007", mensaje.Cuerpo);
        Assert.False(f.Comunicaciones.EnviarComprobante(pago, cargo, f.PersonalDe(Ferreira)).Exito);
    }

    [Fact]
    public void Los_avisos_de_refuerzo_se_generan_una_sola_vez_por_refuerzo()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.Animales.CargarEjemplos(cuenta.Id);
        f.Vacunas.CargarCarnetEjemplo(cuenta.Id);
        var esperados = f.Vacunas.AlertasDe(cuenta.Id).Count;
        Assert.True(esperados > 0);

        var primera = f.AvisosRefuerzo.Generar();
        var segunda = f.AvisosRefuerzo.Generar();

        // Generar() cuenta todas las cuentas (las de demostración también tienen refuerzos): acá importa la de la prueba.
        Assert.True(primera >= esperados);
        Assert.Equal(0, segunda);
        Assert.Equal(esperados, f.Comunicaciones.DeCuenta(cuenta.Id).Count(c => c.Tipo == ComunicacionesService.AvisoRefuerzo));
    }

    [Fact]
    public void Quien_no_quiere_avisos_de_refuerzo_no_los_recibe()
    {
        var f = new Fabrica();
        var cuenta = f.ClienteVerificado();
        f.Animales.CargarEjemplos(cuenta.Id);
        f.Vacunas.CargarCarnetEjemplo(cuenta.Id);
        f.Comunicaciones.CambiarPreferencia(cuenta.Id, ComunicacionesService.AvisoRefuerzo, ComunicacionesService.Ninguno);

        f.AvisosRefuerzo.Generar();

        Assert.Empty(f.Comunicaciones.DeCuenta(cuenta.Id));
    }

    [Fact]
    public void Una_cuenta_sin_verificar_no_recibe_mensajes()
    {
        var f = new Fabrica();
        f.Cuentas.Registrar(Fabrica.RegistroValido("nueva@prueba.uy", "4.444.444-4"));
        var cuenta = f.Cuentas.ObtenerPorCorreo("nueva@prueba.uy")!;

        var mensaje = f.Comunicaciones.Encolar(cuenta.Id, ComunicacionesService.AvisoOperativo, "Asunto", "Cuerpo");

        Assert.Null(mensaje);
    }

    [Fact]
    public void El_cliente_solo_ve_y_marca_leidos_sus_mensajes()
    {
        var f = new Fabrica();
        var ana = f.ClienteVerificado("ana@prueba.uy", "5.555.555-5");
        var beto = f.ClienteVerificado("beto@prueba.uy", "6.666.666-6");
        f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Recepcion), "Aviso", "Texto");
        var deAna = f.Comunicaciones.DeCuenta(ana.Id).Single();

        Assert.DoesNotContain(f.Comunicaciones.DeCuenta(beto.Id), c => c.Id == deAna.Id);
        Assert.False(f.Comunicaciones.MarcarLeida(deAna.Id, beto.Id));
        Assert.Equal(1, f.Comunicaciones.NoLeidas(ana.Id));
        Assert.True(f.Comunicaciones.MarcarLeida(deAna.Id, ana.Id));
        Assert.Equal(0, f.Comunicaciones.NoLeidas(ana.Id));
    }

    [Fact]
    public void La_bandeja_filtra_por_estado_tipo_y_texto()
    {
        var f = new Fabrica();
        f.ClienteVerificado("filtro@prueba.uy", "7.777.777-7");
        f.Comunicaciones.EnviarAvisoOperativo(f.PersonalDe(Recepcion), "Horario de verano", "Texto");

        Assert.NotEmpty(f.Comunicaciones.Buscar(ComunicacionesService.Enviado, ComunicacionesService.AvisoOperativo, "verano"));
        Assert.Empty(f.Comunicaciones.Buscar(ComunicacionesService.Fallido));
        Assert.Empty(f.Comunicaciones.Buscar("", ComunicacionesService.ResumenClinico));
        Assert.Empty(f.Comunicaciones.Buscar("", "", "no existe este texto"));
    }
}
