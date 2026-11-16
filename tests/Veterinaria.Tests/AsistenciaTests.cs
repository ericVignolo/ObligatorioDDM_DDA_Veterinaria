using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// A-026: RN-46 a RN-59 y pruebas de aceptación de ESPECIFICACION_ALERTAS_ASISTENCIA §17.
// Las fechas son fijas (próximo lunes) para no depender de la hora real. Guardia = Dra. Ferreira (Personal 2).
public class AsistenciaTests
{
    private const string Recepcion = "recepcion@vitalia.uy";
    private const string Ferreira = "ferreira@vitalia.uy";
    private const string Silveira = "silveira@vitalia.uy";

    // Próximo lunes, a más de 2 días de hoy, a la hora indicada.
    private static DateTime Lunes(int hora)
    {
        var dia = DateTime.Today.AddDays(3);
        while (dia.DayOfWeek != DayOfWeek.Monday)
        {
            dia = dia.AddDays(1);
        }

        return dia.AddHours(hora);
    }

    private static SolicitudTelefonicaFormulario Datos(string nivel, string especie = "Perro", string modalidad = AsistenciaService.Visita, int km = 5, string direccion = "Rivera 100")
    {
        return new SolicitudTelefonicaFormulario
        {
            ClienteId = 100, AnimalId = 0, Especie = especie, Cantidad = 1, Sintomas = "Decaído", Nivel = nivel,
            Modalidad = modalidad, Direccion = direccion, DistanciaKm = km, TelefonoContacto = "099 000 000"
        };
    }

    private static SolicitudAsistencia Crear(Fabrica f, SolicitudTelefonicaFormulario datos, DateTime momento)
    {
        var resultado = f.Asistencias.Crear(datos, f.PersonalDe(Recepcion), momento);
        Assert.True(resultado.Exito, resultado.Mensaje);
        return f.Asistencias.Cola().Concat(f.Asistencias.Cerradas()).Single(s => s.ClaveIdempotencia == datos.ClaveIdempotencia);
    }

    [Fact]
    public void La_cola_ordena_por_nivel_y_dentro_del_nivel_por_antiguedad()
    {
        var f = new Fabrica();
        var t = Lunes(10);
        var amarilla = Crear(f, Datos(AsistenciaService.Amarillo), t);
        var naranja1 = Crear(f, Datos(AsistenciaService.Naranja), t.AddMinutes(1));
        var naranja2 = Crear(f, Datos(AsistenciaService.Naranja), t.AddMinutes(2));
        var verde = Crear(f, Datos(AsistenciaService.Verde), t.AddMinutes(3));

        var mias = f.Asistencias.Cola().Where(s => new[] { amarilla.Id, naranja1.Id, naranja2.Id, verde.Id }.Contains(s.Id)).Select(s => s.Id).ToList();

        Assert.Equal(new[] { naranja1.Id, naranja2.Id, amarilla.Id, verde.Id }, mias);
    }

    [Fact]
    public void Un_rojo_se_ofrece_enseguida_a_la_guardia_y_no_pasa_a_la_cola_comun()
    {
        var f = new Fabrica();

        var rojo = Crear(f, Datos(AsistenciaService.Rojo), Lunes(22));

        Assert.Equal(AsistenciaService.Ofrecida, rojo.Estado);
        Assert.Contains(f.PersonalDe(Ferreira).Id, rojo.OfrecidaA);
        Assert.False(AsistenciaService.ParaProximoDia(rojo, Lunes(22)));
    }

    [Fact]
    public void Fuera_de_horario_lo_no_rojo_queda_para_el_proximo_dia()
    {
        var f = new Fabrica();

        var amarilla = Crear(f, Datos(AsistenciaService.Amarillo), Lunes(22));

        Assert.True(AsistenciaService.ParaProximoDia(amarilla, Lunes(22)));
        Assert.False(AsistenciaService.ParaProximoDia(amarilla, Lunes(10)));
    }

    [Fact]
    public void Un_rojo_sin_veterinario_elegible_pasa_a_Recepcion()
    {
        var f = new Fabrica();

        // Caballo de noche: Acosta no está en jornada y la guardia no atiende caballos.
        var rojo = Crear(f, Datos(AsistenciaService.Rojo, "Caballo"), Lunes(22));

        Assert.Equal(AsistenciaService.Coordinacion, rojo.Estado);
        Assert.Empty(rojo.OfrecidaA);
        Assert.Null(rojo.EtaMinutos);
    }

    [Fact]
    public void Un_rojo_sin_aceptar_en_10_minutos_pasa_a_Recepcion()
    {
        var f = new Fabrica();
        var rojo = Crear(f, Datos(AsistenciaService.Rojo), Lunes(10));

        f.Asistencias.RevisarPlazos(Lunes(10).AddMinutes(9));
        Assert.Equal(AsistenciaService.Ofrecida, rojo.Estado);

        f.Asistencias.RevisarPlazos(Lunes(10).AddMinutes(10));
        Assert.Equal(AsistenciaService.Coordinacion, rojo.Estado);
    }

    [Fact]
    public void A_las_24_horas_se_pide_reevaluacion_sin_cerrar_ni_bajar_el_nivel()
    {
        var f = new Fabrica();
        var amarilla = Crear(f, Datos(AsistenciaService.Amarillo), Lunes(10));

        f.Asistencias.RevisarPlazos(Lunes(10).AddHours(24));

        Assert.True(amarilla.ReevaluacionPedida);
        Assert.Equal(AsistenciaService.Amarillo, amarilla.Nivel);
        Assert.Equal(AsistenciaService.Pendiente, amarilla.Estado);
    }

    [Fact]
    public void Un_empeoramiento_de_amarillo_a_rojo_activa_la_guardia()
    {
        var f = new Fabrica();
        var amarilla = Crear(f, Datos(AsistenciaService.Amarillo), Lunes(10));

        var resultado = f.Asistencias.InformarEmpeoramiento(amarilla.Id, new CambioNivelFormulario { Nivel = AsistenciaService.Rojo, Motivo = "Convulsiona" }, f.PersonalDe(Recepcion), Lunes(10).AddMinutes(30));

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.Equal(AsistenciaService.Rojo, amarilla.Nivel);
        Assert.Equal(AsistenciaService.Ofrecida, amarilla.Estado);
        var cambio = f.Asistencias.Eventos(amarilla.Id).First(e => e.NivelNuevo != "");
        Assert.Equal(AsistenciaService.Amarillo, cambio.NivelAnterior);
        Assert.Equal(AsistenciaService.Rojo, cambio.NivelNuevo);
    }

    [Fact]
    public void Un_empeoramiento_no_baja_el_nivel()
    {
        var f = new Fabrica();
        var naranja = Crear(f, Datos(AsistenciaService.Naranja), Lunes(10));

        var resultado = f.Asistencias.InformarEmpeoramiento(naranja.Id, new CambioNivelFormulario { Nivel = AsistenciaService.Verde, Motivo = "Mejoró" }, f.PersonalDe(Recepcion), Lunes(10));

        // El aviso queda registrado (sección 10), pero el nivel no baja (RN-52).
        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.Equal(AsistenciaService.Naranja, naranja.Nivel);
        Assert.Contains(f.Asistencias.Eventos(naranja.Id), e => e.Tipo == "Empeoramiento informado");
    }

    [Fact]
    public void Solo_un_veterinario_baja_el_nivel_y_con_motivo()
    {
        var f = new Fabrica();
        var naranja = Crear(f, Datos(AsistenciaService.Naranja), Lunes(10));
        var datos = new CambioNivelFormulario { Nivel = AsistenciaService.Verde, Motivo = "Revisado por teléfono: estable" };

        Assert.False(f.Asistencias.CambiarNivel(naranja.Id, datos, f.PersonalDe(Recepcion), Lunes(10)).Exito);
        Assert.False(f.Asistencias.CambiarNivel(naranja.Id, new CambioNivelFormulario { Nivel = AsistenciaService.Verde, Motivo = "" }, f.PersonalDe(Ferreira), Lunes(10)).Exito);
        Assert.True(f.Asistencias.CambiarNivel(naranja.Id, datos, f.PersonalDe(Ferreira), Lunes(10)).Exito);

        Assert.Equal(AsistenciaService.Verde, naranja.Nivel);
    }

    [Fact]
    public void La_estimacion_aparece_recien_al_aceptar()
    {
        var f = new Fabrica();
        var amarilla = Crear(f, Datos(AsistenciaService.Amarillo, km: 10), Lunes(10));
        var vet = f.PersonalDe(Ferreira);

        Assert.True(f.Asistencias.Asignar(amarilla.Id, vet.Id, f.PersonalDe(Recepcion), Lunes(10)).Exito);
        Assert.Null(amarilla.EtaMinutos);

        Assert.True(f.Asistencias.Aceptar(amarilla.Id, vet, Lunes(10).AddMinutes(1)).Exito);
        Assert.Equal(35, amarilla.EtaMinutos);
    }

    [Fact]
    public void No_se_asigna_a_un_veterinario_que_no_atiende_la_especie()
    {
        var f = new Fabrica();
        var amarilla = Crear(f, Datos(AsistenciaService.Amarillo, "Perro"), Lunes(10));

        var resultado = f.Asistencias.Asignar(amarilla.Id, f.PersonalDe(Silveira).Id, f.PersonalDe(Recepcion), Lunes(10));

        Assert.False(resultado.Exito);
        Assert.Equal(AsistenciaService.Pendiente, amarilla.Estado);
    }

    [Fact]
    public void Una_visita_sin_direccion_no_se_registra_pero_la_orientacion_si()
    {
        var f = new Fabrica();
        var recepcion = f.PersonalDe(Recepcion);

        Assert.False(f.Asistencias.Crear(Datos(AsistenciaService.Verde, direccion: ""), recepcion, Lunes(10)).Exito);
        Assert.True(f.Asistencias.Crear(Datos(AsistenciaService.Verde, modalidad: AsistenciaService.Orientacion, direccion: ""), recepcion, Lunes(10)).Exito);
    }

    [Fact]
    public void Fuera_de_cobertura_pasa_a_Recepcion_sin_descartarse()
    {
        var f = new Fabrica();

        var lejos = Crear(f, Datos(AsistenciaService.Amarillo, km: 31), Lunes(10));

        Assert.Equal(AsistenciaService.Coordinacion, lejos.Estado);
        Assert.Contains(lejos, f.Asistencias.Cola());
    }

    [Fact]
    public void Un_reintento_no_duplica_la_solicitud_ni_la_aceptacion()
    {
        var f = new Fabrica();
        var datos = Datos(AsistenciaService.Rojo);
        var s = Crear(f, datos, Lunes(10));
        var cantidad = f.Asistencias.Cola().Count;

        Assert.True(f.Asistencias.Crear(datos, f.PersonalDe(Recepcion), Lunes(10)).Exito);
        Assert.Equal(cantidad, f.Asistencias.Cola().Count);

        var vet = f.PersonalDe(Ferreira);
        f.Asistencias.Aceptar(s.Id, vet, Lunes(10).AddMinutes(1));
        var eventos = f.Asistencias.Eventos(s.Id).Count;
        Assert.True(f.Asistencias.Aceptar(s.Id, vet, Lunes(10).AddMinutes(2)).Exito);
        Assert.Equal(eventos, f.Asistencias.Eventos(s.Id).Count);
    }

    [Fact]
    public void La_visita_avanza_comparte_ubicacion_solo_en_camino_y_al_finalizar_genera_el_cargo()
    {
        var f = new Fabrica();
        var s = Crear(f, Datos(AsistenciaService.Rojo, km: 10), Lunes(10));
        var vet = f.PersonalDe(Ferreira);
        f.Asistencias.Aceptar(s.Id, vet, Lunes(10));

        f.Asistencias.Avanzar(s.Id, vet, Lunes(10));
        Assert.Equal(AsistenciaService.EnCamino, s.Estado);
        Assert.True(AsistenciaService.CompartiendoUbicacion(s));

        f.Asistencias.Avanzar(s.Id, vet, Lunes(10));
        f.Asistencias.Avanzar(s.Id, vet, Lunes(10));
        Assert.Equal(AsistenciaService.EnAtencion, s.Estado);
        Assert.False(AsistenciaService.CompartiendoUbicacion(s));

        Assert.False(f.Asistencias.Finalizar(s.Id, "", vet, Lunes(10)).Exito);
        Assert.True(f.Asistencias.Finalizar(s.Id, "Hidratado y estable", vet, Lunes(10)).Exito);

        var cargo = Assert.Single(f.Cobros.Cargos(), c => c.AsistenciaId == s.Id);
        Assert.Equal(1500m + 40m * 10, cargo.Monto);
    }

    [Fact]
    public void Un_rojo_solo_se_cierra_despues_de_pasar_a_Recepcion()
    {
        var f = new Fabrica();
        var recepcion = f.PersonalDe(Recepcion);
        var rojo = Crear(f, Datos(AsistenciaService.Rojo), Lunes(10));

        Assert.False(f.Asistencias.Cerrar(rojo.Id, AsistenciaService.NoConcretada, "No atiende", recepcion, Lunes(10)).Exito);

        f.Asistencias.RevisarPlazos(Lunes(10).AddMinutes(15));
        Assert.True(f.Asistencias.Cerrar(rojo.Id, AsistenciaService.NoConcretada, "Llamamos 3 veces sin respuesta", recepcion, Lunes(10).AddMinutes(20)).Exito);
        Assert.Equal(AsistenciaService.NoConcretada, rojo.Estado);
    }
}
