using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;
using Xunit;
using static Veterinaria.Blazor.Services.AsistenciaService;

namespace Veterinaria.Tests.CajaNegra;

// Pruebas de caja negra del panel de asistencia (A-026). Escritas solo con la especificación
// (ESPECIFICACION_ALERTAS_ASISTENCIA.md, secciones 4, 6, 12, 13, 17 y 20; RN-46 a RN-59),
// los modelos y las firmas públicas. Todas las fechas son fijas (próximo lunes o martes) y
// se pasa siempre el parámetro «momento». Solo se miran las solicitudes que crea cada prueba.
public class AsistenciaCajaNegraTests
{
    // ---------- Fechas fijas ----------

    // Próximo lunes a más de 2 días de hoy.
    private static DateOnly ProximoLunes()
    {
        var dia = DateOnly.FromDateTime(DateTime.Today.AddDays(3));
        while (dia.DayOfWeek != DayOfWeek.Monday)
        {
            dia = dia.AddDays(1);
        }
        return dia;
    }

    private static DateTime Lunes(int hora, int minuto = 0) => Fabrica.A(ProximoLunes(), hora, minuto);

    private static DateTime Martes(int hora, int minuto = 0) => Fabrica.A(ProximoLunes().AddDays(1), hora, minuto);

    // ---------- Usuarios ----------

    private static Personal Recepcion(Fabrica f) => f.PersonalDe("recepcion@vitalia.uy");

    private static Personal Ferreira(Fabrica f) => f.PersonalDe("ferreira@vitalia.uy");

    private static Personal Acosta(Fabrica f) => f.PersonalDe("acosta@vitalia.uy");

    private static Personal Admin(Fabrica f) => f.PersonalDe("admin@vitalia.uy");

    // ---------- Ayudas ----------

    private static SolicitudTelefonicaFormulario Formulario(
        string nivel,
        string modalidad = Visita,
        string especie = "Perro",
        int distanciaKm = 10,
        string direccion = "Rivera 100, Nueva Palmira",
        int cantidad = 1,
        int clienteId = 100,
        int animalId = 0)
    {
        return new SolicitudTelefonicaFormulario
        {
            ClienteId = clienteId,
            AnimalId = animalId,
            Especie = especie,
            Cantidad = cantidad,
            Sintomas = "Decaído y sin comer desde ayer",
            Nivel = nivel,
            Modalidad = modalidad,
            Direccion = direccion,
            Referencias = "Portón verde",
            DistanciaKm = distanciaKm,
            TelefonoContacto = "099 123 456"
        };
    }

    // Todas las solicitudes guardadas con esa clave (para no depender de cómo se numeran).
    private static List<SolicitudAsistencia> BuscarPorClave(Fabrica f, string clave)
    {
        var lista = new List<SolicitudAsistencia>();
        for (int i = 1; i <= 2000; i++)
        {
            var s = f.Asistencias.ObtenerPorId(i);
            if (s != null && s.ClaveIdempotencia == clave)
            {
                lista.Add(s);
            }
        }
        return lista;
    }

    private static SolicitudAsistencia Crear(Fabrica f, SolicitudTelefonicaFormulario datos, DateTime momento)
    {
        var r = f.Asistencias.Crear(datos, Recepcion(f), momento);
        Assert.True(r.Exito, r.Mensaje);
        var creadas = BuscarPorClave(f, datos.ClaveIdempotencia);
        Assert.Single(creadas);
        return creadas[0];
    }

    private static SolicitudAsistencia Obtener(Fabrica f, int id) => f.Asistencias.ObtenerPorId(id)!;

    // Amarilla de perro (visita a 10 km) creada el lunes 10:00, ofrecida a Ferreira y aceptada por ella.
    private static SolicitudAsistencia CrearAceptada(Fabrica f, string modalidad = Visita)
    {
        var s = Crear(f, Formulario(Amarillo, modalidad), Lunes(10));
        Assert.True(f.Asistencias.Asignar(s.Id, 2, Recepcion(f), Lunes(10, 5)).Exito);
        Assert.True(f.Asistencias.Aceptar(s.Id, Ferreira(f), Lunes(10, 6)).Exito);
        return Obtener(f, s.Id);
    }

    // Lleva una visita aceptada hasta «En atención».
    private static void HastaEnAtencion(Fabrica f, int id)
    {
        Assert.True(f.Asistencias.Avanzar(id, Ferreira(f), Lunes(10, 10)).Exito);
        Assert.True(f.Asistencias.Avanzar(id, Ferreira(f), Lunes(10, 40)).Exito);
        Assert.True(f.Asistencias.Avanzar(id, Ferreira(f), Lunes(10, 45)).Exito);
        Assert.Equal(EnAtencion, Obtener(f, id).Estado);
    }

    private static CambioNivelFormulario Cambio(string nivel, string motivo = "Respira con dificultad")
    {
        return new CambioNivelFormulario { Nivel = nivel, Motivo = motivo };
    }

    // Foto del estado observable de una solicitud, para verificar que un rechazo no cambia nada.
    private sealed record Foto(string Estado, string Nivel, int VeterinarioId, string Ofrecidos, int? Eta, int Eventos, string MotivoCierre, string Resultado);

    private static Foto Tomar(Fabrica f, int id)
    {
        var s = Obtener(f, id);
        return new Foto(s.Estado, s.Nivel, s.VeterinarioId, string.Join(",", s.OfrecidaA), s.EtaMinutos,
            f.Asistencias.Eventos(id).Count, s.MotivoCierre, s.Resultado);
    }

    // =====================================================================
    // Creación (Recepción registra la llamada), RN-46, RN-56, RN-58, RN-59
    // =====================================================================

    [Fact]
    public void Crear_visita_amarilla_por_Recepcion_queda_pendiente_con_el_nivel_del_cliente_y_sin_estimacion()
    {
        var f = new Fabrica();

        var s = Crear(f, Formulario(Amarillo, animalId: 1), Lunes(10));

        Assert.Equal(Pendiente, s.Estado);
        Assert.Equal(Amarillo, s.Nivel);
        Assert.Equal(Amarillo, s.NivelCliente);
        Assert.Equal(Lunes(10), s.FechaCreacion);
        Assert.Equal(0, s.VeterinarioId);
        Assert.Null(s.EtaMinutos);
        Assert.True(EstaActiva(s));
        Assert.True(SinAceptar(s));
        Assert.False(CompartiendoUbicacion(s));
        Assert.Contains(f.Asistencias.Cola(), x => x.Id == s.Id);
        Assert.NotEmpty(f.Asistencias.Eventos(s.Id));
    }

    [Fact]
    public void Crear_dos_veces_con_la_misma_clave_no_duplica_la_solicitud()
    {
        var f = new Fabrica();
        var datos = Formulario(Naranja);

        f.Asistencias.Crear(datos, Recepcion(f), Lunes(10));
        f.Asistencias.Crear(datos, Recepcion(f), Lunes(10, 1));

        Assert.Single(BuscarPorClave(f, datos.ClaveIdempotencia));
    }

    [Fact]
    public void Crear_sin_sesion_o_por_un_veterinario_no_registra_nada()
    {
        var f = new Fabrica();
        var sinSesion = Formulario(Amarillo);
        var porVeterinario = Formulario(Amarillo);

        Assert.False(f.Asistencias.Crear(sinSesion, null, Lunes(10)).Exito);
        Assert.False(f.Asistencias.Crear(porVeterinario, Ferreira(f), Lunes(10)).Exito);

        Assert.Empty(BuscarPorClave(f, sinSesion.ClaveIdempotencia));
        Assert.Empty(BuscarPorClave(f, porVeterinario.ClaveIdempotencia));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_visita_sin_direccion_no_se_registra(string direccion)
    {
        var f = new Fabrica();
        var datos = Formulario(Amarillo, Visita, direccion: direccion);

        Assert.False(f.Asistencias.Crear(datos, Recepcion(f), Lunes(10)).Exito);
        Assert.Empty(BuscarPorClave(f, datos.ClaveIdempotencia));
    }

    [Fact]
    public void Crear_orientacion_sin_direccion_si_se_registra()
    {
        var f = new Fabrica();

        var s = Crear(f, Formulario(Verde, Orientacion, direccion: "", distanciaKm: 0), Lunes(10));

        Assert.Equal(Orientacion, s.Modalidad);
        Assert.Equal(Pendiente, s.Estado);
    }

    [Theory]
    [InlineData("sintomasVacios")]
    [InlineData("sintomasEspacios")]
    [InlineData("sintomas1001")]
    [InlineData("nivelVacio")]
    [InlineData("nivelInexistente")]
    [InlineData("modalidadVacia")]
    [InlineData("modalidadInexistente")]
    [InlineData("cantidadCero")]
    [InlineData("cantidad1001")]
    [InlineData("telefonoVacio")]
    [InlineData("telefonoEspacios")]
    [InlineData("telefono31")]
    [InlineData("direccion201")]
    [InlineData("referencias301")]
    [InlineData("distanciaNegativa")]
    [InlineData("distancia501")]
    [InlineData("clienteCero")]
    public void Crear_con_datos_invalidos_no_registra_nada(string caso)
    {
        var f = new Fabrica();
        var datos = Formulario(Amarillo);
        switch (caso)
        {
            case "sintomasVacios": datos.Sintomas = ""; break;
            case "sintomasEspacios": datos.Sintomas = "    "; break;
            case "sintomas1001": datos.Sintomas = new string('a', 1001); break;
            case "nivelVacio": datos.Nivel = ""; break;
            case "nivelInexistente": datos.Nivel = "Azul"; break;
            case "modalidadVacia": datos.Modalidad = ""; break;
            case "modalidadInexistente": datos.Modalidad = "Telepatía"; break;
            case "cantidadCero": datos.Cantidad = 0; break;
            case "cantidad1001": datos.Cantidad = 1001; break;
            case "telefonoVacio": datos.TelefonoContacto = ""; break;
            case "telefonoEspacios": datos.TelefonoContacto = "   "; break;
            case "telefono31": datos.TelefonoContacto = new string('9', 31); break;
            case "direccion201": datos.Direccion = new string('d', 201); break;
            case "referencias301": datos.Referencias = new string('r', 301); break;
            case "distanciaNegativa": datos.DistanciaKm = -1; break;
            case "distancia501": datos.DistanciaKm = 501; break;
            case "clienteCero": datos.ClienteId = 0; break;
        }

        var r = f.Asistencias.Crear(datos, Recepcion(f), Lunes(10));

        Assert.False(r.Exito);
        Assert.Empty(BuscarPorClave(f, datos.ClaveIdempotencia));
    }

    [Fact]
    public void Crear_con_los_largos_maximos_se_acepta()
    {
        var f = new Fabrica();
        var datos = Formulario(Verde);
        datos.Sintomas = new string('a', 1000);
        datos.Direccion = new string('d', 200);
        datos.Referencias = new string('r', 300);
        datos.TelefonoContacto = new string('9', 30);
        datos.Cantidad = 1000;
        datos.Especie = "Vaca";

        var s = Crear(f, datos, Lunes(10));

        Assert.Equal(1000, s.Sintomas.Length);
    }

    [Theory]
    [InlineData(30, Pendiente)]
    [InlineData(31, Coordinacion)]
    public void Crear_visita_respeta_la_cobertura_de_30_km(int km, string estadoEsperado)
    {
        var f = new Fabrica();

        var s = Crear(f, Formulario(Amarillo, Visita, distanciaKm: km), Lunes(10));

        Assert.Equal(estadoEsperado, s.Estado);
        Assert.Equal(Amarillo, s.Nivel);
        Assert.Equal(0, s.VeterinarioId);
        Assert.Null(s.EtaMinutos);
        Assert.True(EstaActiva(s));
    }

    [Fact]
    public void Crear_orientacion_lejana_no_se_escala_por_cobertura()
    {
        var f = new Fabrica();

        var s = Crear(f, Formulario(Amarillo, Orientacion, distanciaKm: 45), Lunes(10));

        Assert.Equal(Pendiente, s.Estado);
    }

    [Fact]
    public void Crear_informa_el_costo_segun_la_tarifa_de_demostracion()
    {
        var f = new Fabrica();

        Assert.Equal(1900m, CalcularCosto(Visita, 10, 1));
        Assert.Equal(2500m, CalcularCosto(Visita, 10, 3));
        Assert.Equal(0m, CalcularCosto(Orientacion, 10, 3));

        var visita = Crear(f, Formulario(Amarillo, Visita, distanciaKm: 10, cantidad: 3, especie: "Oveja"), Lunes(10));
        var orientacion = Crear(f, Formulario(Amarillo, Orientacion, distanciaKm: 10, cantidad: 3, especie: "Oveja"), Lunes(10));

        Assert.Equal(2500m, visita.Costo);
        Assert.Equal(0m, orientacion.Costo);
    }

    [Fact]
    public void Grupo_de_ganado_sin_animal_registrado_se_registra_y_filtra_profesionales_competentes()
    {
        var f = new Fabrica();

        var vacas = Crear(f, Formulario(Amarillo, especie: "Vaca", cantidad: 20, animalId: 0), Lunes(15));
        var aves = Crear(f, Formulario(Amarillo, especie: "Ave", cantidad: 50, animalId: 0), Lunes(10));

        var paraVacas = f.Asistencias.Elegibles(vacas, Lunes(15)).Select(p => p.Id).ToList();
        var paraAves = f.Asistencias.Elegibles(aves, Lunes(10)).Select(p => p.Id).ToList();

        Assert.Contains(3, paraVacas);
        Assert.DoesNotContain(2, paraVacas);
        Assert.DoesNotContain(4, paraVacas);
        Assert.Contains(2, paraAves);
        Assert.DoesNotContain(3, paraAves);
        Assert.DoesNotContain(4, paraAves);
        Assert.Equal(20, vacas.Cantidad);
    }

    // =====================================================================
    // Rojo: guardia, oferta simultánea, escalamiento (RN-49, RN-55, D-ASI-04)
    // =====================================================================

    [Fact]
    public void Rojo_en_jornada_se_ofrece_enseguida_a_los_elegibles_sin_estimacion()
    {
        var f = new Fabrica();

        var s = Crear(f, Formulario(Rojo), Lunes(10));

        Assert.Equal(Ofrecida, s.Estado);
        Assert.Contains(2, s.OfrecidaA);
        Assert.DoesNotContain(4, s.OfrecidaA);
        Assert.NotNull(s.FechaOferta);
        Assert.Null(s.EtaMinutos);
        Assert.Equal(0, s.VeterinarioId);
        Assert.True(SinAceptar(s));
    }

    [Fact]
    public void Rojo_fuera_de_horario_busca_guardia_y_no_se_difiere_al_dia_siguiente()
    {
        var f = new Fabrica();
        Assert.False(EnHorarioClinica(Lunes(22)));

        var s = Crear(f, Formulario(Rojo), Lunes(22));

        Assert.Equal(Ofrecida, s.Estado);
        Assert.Contains(GuardiaIdDe(f), s.OfrecidaA);
        Assert.False(ParaProximoDia(s, Lunes(23)));
    }

    private static int GuardiaIdDe(Fabrica f) => f.Asistencias.GuardiaId;

    [Fact]
    public void Rojo_sin_elegibles_pasa_a_Recepcion_sin_inventar_profesional_ni_estimacion()
    {
        var f = new Fabrica();

        // Caballo un lunes: Acosta atiende martes y jueves y la guardia no atiende caballos.
        var s = Crear(f, Formulario(Rojo, especie: "Caballo"), Lunes(10));

        Assert.Equal(Coordinacion, s.Estado);
        Assert.Equal(0, s.VeterinarioId);
        Assert.Equal("", s.VeterinarioNombre);
        Assert.Null(s.EtaMinutos);
        Assert.Empty(s.OfrecidaA);
        Assert.True(EstaActiva(s));
    }

    [Fact]
    public void Rojo_de_caballo_un_martes_se_ofrece_a_Acosta()
    {
        var f = new Fabrica();

        var s = Crear(f, Formulario(Rojo, especie: "Caballo"), Martes(10));

        Assert.Equal(Ofrecida, s.Estado);
        Assert.Contains(4, s.OfrecidaA);
        Assert.DoesNotContain(2, s.OfrecidaA);
        Assert.Contains(f.Asistencias.Elegibles(s, Martes(10)), p => p.Id == 4);
    }

    [Fact]
    public void Rojo_sin_aceptar_pasa_a_Recepcion_despues_de_10_minutos()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Rojo), Lunes(10));

        f.Asistencias.RevisarPlazos(Lunes(10, 9));
        Assert.Equal(Ofrecida, Obtener(f, s.Id).Estado);

        f.Asistencias.RevisarPlazos(Lunes(10, 11));
        var despues = Obtener(f, s.Id);
        Assert.Equal(Coordinacion, despues.Estado);
        Assert.Equal(Rojo, despues.Nivel);
        Assert.True(EstaActiva(despues));
    }

    [Fact]
    public void Rojo_rechazado_por_todos_los_elegibles_pasa_a_Recepcion()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Rojo), Lunes(10));
        Assert.Equal(Ofrecida, s.Estado);

        var r = f.Asistencias.Rechazar(s.Id, "Estoy en cirugía", Ferreira(f), Lunes(10, 2));

        Assert.True(r.Exito);
        Assert.Equal(Coordinacion, Obtener(f, s.Id).Estado);
    }

    [Fact]
    public void Rojo_con_deuda_o_falla_de_pago_se_crea_igual()
    {
        // No hay forma pública de cargar una deuda: se verifica que el rojo se crea y escala sin pedir pago.
        var f = new Fabrica();

        var s = Crear(f, Formulario(Rojo, clienteId: 101, especie: "Vaca", cantidad: 3), Lunes(22));

        Assert.True(EstaActiva(s));
        Assert.Equal(Rojo, s.Nivel);
    }

    // =====================================================================
    // Asignación y aceptación (D-ASI-05, RN-53, RNF-CON-10)
    // =====================================================================

    [Fact]
    public void Asignar_por_Recepcion_ofrece_la_solicitud_al_veterinario_elegido()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Naranja), Lunes(10));
        Assert.Contains(f.Asistencias.Elegibles(s, Lunes(10)), p => p.Id == 2);

        var r = f.Asistencias.Asignar(s.Id, 2, Recepcion(f), Lunes(10, 5));

        Assert.True(r.Exito);
        var despues = Obtener(f, s.Id);
        Assert.Equal(Ofrecida, despues.Estado);
        Assert.Contains(2, despues.OfrecidaA);
        Assert.Null(despues.EtaMinutos);
    }

    [Fact]
    public void Asignar_a_un_veterinario_no_competente_para_la_especie_no_cambia_nada()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Naranja), Lunes(10));
        Assert.DoesNotContain(f.Asistencias.Elegibles(s, Lunes(10)), p => p.Id == 4);
        var antes = Tomar(f, s.Id);

        Assert.False(f.Asistencias.Asignar(s.Id, 4, Recepcion(f), Lunes(10, 5)).Exito);
        Assert.False(f.Asistencias.Asignar(s.Id, 999, Recepcion(f), Lunes(10, 5)).Exito);

        Assert.Equal(antes, Tomar(f, s.Id));
    }

    [Fact]
    public void Asignar_sin_ser_Recepcion_no_cambia_nada()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Naranja), Lunes(10));
        var antes = Tomar(f, s.Id);

        Assert.False(f.Asistencias.Asignar(s.Id, 2, Ferreira(f), Lunes(10, 5)).Exito);
        Assert.False(f.Asistencias.Asignar(s.Id, 2, null, Lunes(10, 5)).Exito);
        Assert.False(f.Asistencias.Asignar(9999, 2, Recepcion(f), Lunes(10, 5)).Exito);

        Assert.Equal(antes, Tomar(f, s.Id));
    }

    [Fact]
    public void Aceptar_fija_el_veterinario_y_recien_ahi_muestra_la_estimacion()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Amarillo, distanciaKm: 10), Lunes(10));
        f.Asistencias.Asignar(s.Id, 2, Recepcion(f), Lunes(10, 5));
        Assert.Null(Obtener(f, s.Id).EtaMinutos);

        var r = f.Asistencias.Aceptar(s.Id, Ferreira(f), Lunes(10, 6));

        Assert.True(r.Exito);
        var despues = Obtener(f, s.Id);
        Assert.Equal(Aceptada, despues.Estado);
        Assert.Equal(2, despues.VeterinarioId);
        Assert.NotEqual("", despues.VeterinarioNombre);
        Assert.Equal(Lunes(10, 6), despues.FechaAceptacion);
        Assert.Equal(35, despues.EtaMinutos);
        Assert.False(SinAceptar(despues));
        Assert.False(CompartiendoUbicacion(despues));
        Assert.Contains(f.Asistencias.DeVeterinario(2), x => x.Id == s.Id);
    }

    [Fact]
    public void Aceptar_un_rojo_por_el_primero_que_lo_toma()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Rojo, distanciaKm: 5), Lunes(22));

        Assert.True(f.Asistencias.Aceptar(s.Id, Ferreira(f), Lunes(22, 3)).Exito);

        var despues = Obtener(f, s.Id);
        Assert.Equal(Aceptada, despues.Estado);
        Assert.Equal(2, despues.VeterinarioId);
        Assert.Equal(25, despues.EtaMinutos);

        // Ya no se escala a Recepción aunque pasen los 10 minutos.
        f.Asistencias.RevisarPlazos(Lunes(22, 30));
        Assert.Equal(Aceptada, Obtener(f, s.Id).Estado);
    }

    [Fact]
    public void Aceptar_quien_no_recibio_la_oferta_o_despues_de_otro_no_cambia_nada()
    {
        var f = new Fabrica();
        var ofrecida = Crear(f, Formulario(Amarillo), Lunes(10));
        f.Asistencias.Asignar(ofrecida.Id, 2, Recepcion(f), Lunes(10, 5));
        var antesOfrecida = Tomar(f, ofrecida.Id);

        Assert.False(f.Asistencias.Aceptar(ofrecida.Id, Acosta(f), Lunes(10, 6)).Exito);
        Assert.False(f.Asistencias.Aceptar(ofrecida.Id, Recepcion(f), Lunes(10, 6)).Exito);
        Assert.False(f.Asistencias.Aceptar(ofrecida.Id, Admin(f), Lunes(10, 6)).Exito);
        Assert.False(f.Asistencias.Aceptar(ofrecida.Id, null, Lunes(10, 6)).Exito);
        Assert.Equal(antesOfrecida, Tomar(f, ofrecida.Id));

        var pendiente = Crear(f, Formulario(Amarillo), Lunes(10));
        Assert.False(f.Asistencias.Aceptar(pendiente.Id, Ferreira(f), Lunes(10, 6)).Exito);
        Assert.Equal(Pendiente, Obtener(f, pendiente.Id).Estado);

        var aceptada = CrearAceptada(f);
        Assert.False(f.Asistencias.Aceptar(aceptada.Id, Acosta(f), Lunes(10, 7)).Exito);
        Assert.Equal(2, Obtener(f, aceptada.Id).VeterinarioId);
    }

    [Fact]
    public void Aceptar_repetido_no_duplica_la_aceptacion()
    {
        var f = new Fabrica();
        var s = CrearAceptada(f);
        var antes = Tomar(f, s.Id);
        var fecha = Obtener(f, s.Id).FechaAceptacion;

        f.Asistencias.Aceptar(s.Id, Ferreira(f), Lunes(10, 8));

        Assert.Equal(antes, Tomar(f, s.Id));
        Assert.Equal(fecha, Obtener(f, s.Id).FechaAceptacion);
        Assert.Single(f.Asistencias.DeVeterinario(2).Where(x => x.Id == s.Id));
    }

    [Fact]
    public void Rechazar_con_motivo_devuelve_la_solicitud_a_la_cola()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Naranja), Lunes(10));
        f.Asistencias.Asignar(s.Id, 2, Recepcion(f), Lunes(10, 5));

        var r = f.Asistencias.Rechazar(s.Id, "Sin vehículo hoy", Ferreira(f), Lunes(10, 6));

        Assert.True(r.Exito);
        var despues = Obtener(f, s.Id);
        Assert.Equal(Pendiente, despues.Estado);
        Assert.DoesNotContain(2, despues.OfrecidaA);
        Assert.Equal(0, despues.VeterinarioId);
        Assert.Equal(Naranja, despues.Nivel);
        Assert.Contains(f.Asistencias.Cola(), x => x.Id == s.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rechazar_sin_motivo_no_cambia_nada(string motivo)
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Naranja), Lunes(10));
        f.Asistencias.Asignar(s.Id, 2, Recepcion(f), Lunes(10, 5));
        var antes = Tomar(f, s.Id);

        Assert.False(f.Asistencias.Rechazar(s.Id, motivo, Ferreira(f), Lunes(10, 6)).Exito);
        Assert.False(f.Asistencias.Rechazar(s.Id, "No puedo", Acosta(f), Lunes(10, 6)).Exito);
        Assert.False(f.Asistencias.Rechazar(s.Id, "No puedo", null, Lunes(10, 6)).Exito);

        Assert.Equal(antes, Tomar(f, s.Id));
    }

    // =====================================================================
    // Avance de estados y ubicación del veterinario (D-ASI-06, RN-57)
    // =====================================================================

    [Fact]
    public void Avanzar_una_visita_recorre_los_estados_y_comparte_ubicacion_solo_en_camino()
    {
        var f = new Fabrica();
        var s = CrearAceptada(f, Visita);
        Assert.Equal(EnCamino, SiguienteEstado(Obtener(f, s.Id)));

        Assert.True(f.Asistencias.Avanzar(s.Id, Ferreira(f), Lunes(10, 10)).Exito);
        Assert.Equal(EnCamino, Obtener(f, s.Id).Estado);
        Assert.True(CompartiendoUbicacion(Obtener(f, s.Id)));
        Assert.Equal(EnElLugar, SiguienteEstado(Obtener(f, s.Id)));

        Assert.True(f.Asistencias.Avanzar(s.Id, Ferreira(f), Lunes(10, 40)).Exito);
        Assert.Equal(EnElLugar, Obtener(f, s.Id).Estado);
        Assert.False(CompartiendoUbicacion(Obtener(f, s.Id)));
        Assert.Equal(EnAtencion, SiguienteEstado(Obtener(f, s.Id)));

        Assert.True(f.Asistencias.Avanzar(s.Id, Ferreira(f), Lunes(10, 45)).Exito);
        Assert.Equal(EnAtencion, Obtener(f, s.Id).Estado);
        Assert.False(CompartiendoUbicacion(Obtener(f, s.Id)));
    }

    [Fact]
    public void Avanzar_una_orientacion_salta_de_aceptada_a_en_atencion()
    {
        var f = new Fabrica();
        var s = CrearAceptada(f, Orientacion);
        Assert.Equal(EnAtencion, SiguienteEstado(Obtener(f, s.Id)));

        Assert.True(f.Asistencias.Avanzar(s.Id, Ferreira(f), Lunes(10, 10)).Exito);

        var despues = Obtener(f, s.Id);
        Assert.Equal(EnAtencion, despues.Estado);
        Assert.False(CompartiendoUbicacion(despues));
    }

    [Fact]
    public void Avanzar_por_otro_veterinario_por_Recepcion_o_sin_aceptar_no_cambia_nada()
    {
        var f = new Fabrica();
        var aceptada = CrearAceptada(f);
        var antes = Tomar(f, aceptada.Id);

        Assert.False(f.Asistencias.Avanzar(aceptada.Id, Acosta(f), Lunes(10, 10)).Exito);
        Assert.False(f.Asistencias.Avanzar(aceptada.Id, Recepcion(f), Lunes(10, 10)).Exito);
        Assert.False(f.Asistencias.Avanzar(aceptada.Id, null, Lunes(10, 10)).Exito);
        Assert.Equal(antes, Tomar(f, aceptada.Id));

        var pendiente = Crear(f, Formulario(Amarillo), Lunes(10));
        Assert.False(f.Asistencias.Avanzar(pendiente.Id, Ferreira(f), Lunes(10, 10)).Exito);
        Assert.Equal(Pendiente, Obtener(f, pendiente.Id).Estado);
    }

    // =====================================================================
    // Finalización y cargo (D-ASI-03, RN-58)
    // =====================================================================

    [Fact]
    public void Finalizar_registra_el_resultado_cierra_y_genera_el_cargo_con_el_costo()
    {
        var f = new Fabrica();
        var s = CrearAceptada(f, Visita);
        HastaEnAtencion(f, s.Id);

        var r = f.Asistencias.Finalizar(s.Id, "Se hidrató y queda en observación", Ferreira(f), Lunes(11, 30));

        Assert.True(r.Exito);
        var despues = Obtener(f, s.Id);
        Assert.Equal(Finalizada, despues.Estado);
        Assert.Equal("Se hidrató y queda en observación", despues.Resultado);
        Assert.False(EstaActiva(despues));
        Assert.False(CompartiendoUbicacion(despues));
        Assert.Contains(f.Asistencias.Cerradas(), x => x.Id == s.Id);
        Assert.DoesNotContain(f.Asistencias.Cola(), x => x.Id == s.Id);

        var cargo = Assert.Single(f.Cobros.Cargos().Where(c => c.AsistenciaId == s.Id));
        Assert.Equal(1900m, cargo.Monto);
        Assert.Equal(despues.Costo, cargo.Monto);
        Assert.Equal(100, cargo.ClienteId);
    }

    [Fact]
    public void Finalizar_sin_resultado_antes_de_atender_o_por_otro_no_cambia_nada()
    {
        var f = new Fabrica();
        var s = CrearAceptada(f, Visita);

        // Todavía en «Aceptada»: no se puede finalizar sin haber llegado ni atendido.
        var enAceptada = Tomar(f, s.Id);
        Assert.False(f.Asistencias.Finalizar(s.Id, "Atendido", Ferreira(f), Lunes(10, 20)).Exito);
        Assert.Equal(enAceptada, Tomar(f, s.Id));

        HastaEnAtencion(f, s.Id);
        var antes = Tomar(f, s.Id);

        Assert.False(f.Asistencias.Finalizar(s.Id, "", Ferreira(f), Lunes(11)).Exito);
        Assert.False(f.Asistencias.Finalizar(s.Id, "   ", Ferreira(f), Lunes(11)).Exito);
        Assert.False(f.Asistencias.Finalizar(s.Id, "Atendido", Acosta(f), Lunes(11)).Exito);
        Assert.False(f.Asistencias.Finalizar(s.Id, "Atendido", Recepcion(f), Lunes(11)).Exito);
        Assert.False(f.Asistencias.Finalizar(s.Id, "Atendido", null, Lunes(11)).Exito);

        Assert.Equal(antes, Tomar(f, s.Id));
        Assert.Empty(f.Cobros.Cargos().Where(c => c.AsistenciaId == s.Id));
    }

    [Fact]
    public void Finalizar_dos_veces_no_duplica_el_cargo()
    {
        var f = new Fabrica();
        var s = CrearAceptada(f, Visita);
        HastaEnAtencion(f, s.Id);

        f.Asistencias.Finalizar(s.Id, "Atendido", Ferreira(f), Lunes(11));
        f.Asistencias.Finalizar(s.Id, "Atendido", Ferreira(f), Lunes(11, 1));

        Assert.Equal(Finalizada, Obtener(f, s.Id).Estado);
        Assert.Single(f.Cobros.Cargos().Where(c => c.AsistenciaId == s.Id));
    }

    [Fact]
    public void Finalizar_una_orientacion_no_genera_un_cargo_con_importe()
    {
        var f = new Fabrica();
        var s = CrearAceptada(f, Orientacion);
        Assert.True(f.Asistencias.Avanzar(s.Id, Ferreira(f), Lunes(10, 10)).Exito);

        Assert.True(f.Asistencias.Finalizar(s.Id, "Se coordinó traslado a la clínica", Ferreira(f), Lunes(10, 30)).Exito);

        Assert.Equal(Finalizada, Obtener(f, s.Id).Estado);
        Assert.All(f.Cobros.Cargos().Where(c => c.AsistenciaId == s.Id), c => Assert.Equal(0m, c.Monto));
    }

    // =====================================================================
    // Cambio de nivel del veterinario (RN-52, D-ASI-08)
    // =====================================================================

    [Fact]
    public void CambiarNivel_cualquier_veterinario_puede_bajar_con_motivo_y_queda_trazado()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Amarillo), Lunes(10));

        var r = f.Asistencias.CambiarNivel(s.Id, Cambio(Verde, "Llamé al cliente: come y camina"), Acosta(f), Lunes(10, 15));

        Assert.True(r.Exito);
        var despues = Obtener(f, s.Id);
        Assert.Equal(Verde, despues.Nivel);
        Assert.Equal(Amarillo, despues.NivelCliente);
        Assert.Contains(f.Asistencias.Eventos(s.Id), e =>
            e.NivelAnterior == Amarillo && e.NivelNuevo == Verde
            && e.Autor.Trim() != "" && e.Texto.Contains("Llamé al cliente: come y camina"));
    }

    [Fact]
    public void CambiarNivel_sin_motivo_con_nivel_invalido_o_sin_ser_veterinario_no_cambia_nada()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Amarillo), Lunes(10));
        var antes = Tomar(f, s.Id);

        Assert.False(f.Asistencias.CambiarNivel(s.Id, Cambio(Verde, ""), Ferreira(f), Lunes(10, 15)).Exito);
        Assert.False(f.Asistencias.CambiarNivel(s.Id, Cambio(Verde, "   "), Ferreira(f), Lunes(10, 15)).Exito);
        Assert.False(f.Asistencias.CambiarNivel(s.Id, Cambio(Verde, new string('m', 301)), Ferreira(f), Lunes(10, 15)).Exito);
        Assert.False(f.Asistencias.CambiarNivel(s.Id, Cambio("Azul"), Ferreira(f), Lunes(10, 15)).Exito);
        Assert.False(f.Asistencias.CambiarNivel(s.Id, Cambio(""), Ferreira(f), Lunes(10, 15)).Exito);
        Assert.False(f.Asistencias.CambiarNivel(s.Id, Cambio(Verde), Recepcion(f), Lunes(10, 15)).Exito);
        Assert.False(f.Asistencias.CambiarNivel(s.Id, Cambio(Verde), Admin(f), Lunes(10, 15)).Exito);
        Assert.False(f.Asistencias.CambiarNivel(s.Id, Cambio(Verde), null, Lunes(10, 15)).Exito);

        Assert.Equal(antes, Tomar(f, s.Id));
    }

    [Fact]
    public void CambiarNivel_bajar_un_rojo_ofrecido_lo_devuelve_a_la_cola()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Rojo), Lunes(10));
        Assert.Equal(Ofrecida, s.Estado);

        var r = f.Asistencias.CambiarNivel(s.Id, Cambio(Naranja, "Sangrado controlado por teléfono"), Ferreira(f), Lunes(10, 3));

        Assert.True(r.Exito);
        var despues = Obtener(f, s.Id);
        Assert.Equal(Naranja, despues.Nivel);
        Assert.Equal(Pendiente, despues.Estado);
        Assert.Contains(f.Asistencias.Cola(), x => x.Id == s.Id);
    }

    // =====================================================================
    // Empeoramiento (RN-51, D-ASI-07, D-ASI-08, prueba 3 y 10)
    // =====================================================================

    [Fact]
    public void Empeoramiento_de_amarilla_a_roja_queda_trazado_y_activa_la_guardia()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Amarillo), Lunes(18));
        var eventosAntes = f.Asistencias.Eventos(s.Id).Count;

        var r = f.Asistencias.InformarEmpeoramiento(s.Id, Cambio(Rojo, "Convulsiona"), Recepcion(f), Lunes(22));

        Assert.True(r.Exito);
        var despues = Obtener(f, s.Id);
        Assert.Equal(Rojo, despues.Nivel);
        Assert.Equal(Ofrecida, despues.Estado);
        Assert.Contains(GuardiaIdDe(f), despues.OfrecidaA);
        Assert.True(f.Asistencias.Eventos(s.Id).Count > eventosAntes);
        Assert.Contains(f.Asistencias.Eventos(s.Id), e => e.NivelAnterior == Amarillo && e.NivelNuevo == Rojo);
        Assert.False(ParaProximoDia(despues, Lunes(23)));
    }

    [Fact]
    public void Empeoramiento_que_indica_un_nivel_menor_deja_el_evento_pero_no_baja()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Naranja), Lunes(10));
        var eventosAntes = f.Asistencias.Eventos(s.Id).Count;

        f.Asistencias.InformarEmpeoramiento(s.Id, Cambio(Verde, "Dice que está igual"), Recepcion(f), Lunes(10, 30));

        var despues = Obtener(f, s.Id);
        Assert.Equal(Naranja, despues.Nivel);
        Assert.Equal(Pendiente, despues.Estado);
        Assert.True(f.Asistencias.Eventos(s.Id).Count > eventosAntes);
    }

    [Fact]
    public void Empeoramiento_repetido_no_duplica_ofertas_ni_cambia_el_estado()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Amarillo), Lunes(10));

        f.Asistencias.InformarEmpeoramiento(s.Id, Cambio(Rojo, "No respira bien"), Recepcion(f), Lunes(10, 20));
        var trasPrimero = Obtener(f, s.Id);
        var fechaOferta = trasPrimero.FechaOferta;
        f.Asistencias.InformarEmpeoramiento(s.Id, Cambio(Rojo, "No respira bien"), Recepcion(f), Lunes(10, 20));

        var despues = Obtener(f, s.Id);
        Assert.Equal(Rojo, despues.Nivel);
        Assert.Equal(Ofrecida, despues.Estado);
        Assert.Equal(despues.OfrecidaA.Count, despues.OfrecidaA.Distinct().Count());
        Assert.Equal(fechaOferta, despues.FechaOferta);
    }

    [Fact]
    public void Empeoramiento_a_rojo_de_una_aceptada_conserva_al_veterinario()
    {
        var f = new Fabrica();
        var s = CrearAceptada(f);

        Assert.True(f.Asistencias.InformarEmpeoramiento(s.Id, Cambio(Rojo, "Empeoró"), Recepcion(f), Lunes(10, 8)).Exito);

        var despues = Obtener(f, s.Id);
        Assert.Equal(Rojo, despues.Nivel);
        Assert.Equal(Aceptada, despues.Estado);
        Assert.Equal(2, despues.VeterinarioId);
    }

    [Fact]
    public void Empeoramiento_a_rojo_fuera_de_cobertura_sigue_en_Recepcion()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Amarillo, Visita, distanciaKm: 45), Lunes(10));
        Assert.Equal(Coordinacion, s.Estado);

        f.Asistencias.InformarEmpeoramiento(s.Id, Cambio(Rojo, "Empeoró"), Recepcion(f), Lunes(10, 10));

        var despues = Obtener(f, s.Id);
        Assert.Equal(Rojo, despues.Nivel);
        Assert.Equal(Coordinacion, despues.Estado);
        Assert.Equal(0, despues.VeterinarioId);
    }

    [Fact]
    public void Empeoramiento_sin_sesion_o_sobre_una_solicitud_cerrada_no_cambia_nada()
    {
        var f = new Fabrica();
        var activa = Crear(f, Formulario(Amarillo), Lunes(10));
        var antesActiva = Tomar(f, activa.Id);
        Assert.False(f.Asistencias.InformarEmpeoramiento(activa.Id, Cambio(Rojo), null, Lunes(10, 5)).Exito);
        Assert.Equal(antesActiva, Tomar(f, activa.Id));

        var cerrada = Crear(f, Formulario(Amarillo), Lunes(10));
        Assert.True(f.Asistencias.Cerrar(cerrada.Id, CanceladaClinica, "El cliente lo llevó a otra clínica", Recepcion(f), Lunes(10, 5)).Exito);
        var antesCerrada = Tomar(f, cerrada.Id);

        Assert.False(f.Asistencias.InformarEmpeoramiento(cerrada.Id, Cambio(Rojo), Recepcion(f), Lunes(10, 10)).Exito);
        Assert.Equal(antesCerrada, Tomar(f, cerrada.Id));
    }

    [Fact]
    public void Empeoramiento_del_cliente_solo_lo_puede_informar_el_titular()
    {
        var f = new Fabrica();
        var titular = f.ClienteVerificado("titular@prueba.uy", "1.234.567-2");
        var otro = f.ClienteVerificado("otro@prueba.uy", "2.345.678-3");
        var s = Crear(f, Formulario(Verde, Orientacion, clienteId: titular.Id, direccion: ""), Lunes(10));
        var antes = Tomar(f, s.Id);

        Assert.False(f.Asistencias.InformarEmpeoramientoCliente(s.Id, Cambio(Naranja), otro, Lunes(10, 5)).Exito);
        Assert.False(f.Asistencias.InformarEmpeoramientoCliente(s.Id, Cambio(Naranja), null, Lunes(10, 5)).Exito);
        Assert.Equal(antes, Tomar(f, s.Id));

        Assert.True(f.Asistencias.InformarEmpeoramientoCliente(s.Id, Cambio(Naranja, "Vomita sangre"), titular, Lunes(10, 6)).Exito);
        Assert.Equal(Naranja, Obtener(f, s.Id).Nivel);
    }

    // =====================================================================
    // Plazos, reevaluación y orden de la cola (RN-48, RN-50, pruebas 1, 2 y 4)
    // =====================================================================

    [Fact]
    public void A_las_24_horas_se_pide_reevaluacion_sin_cerrar_ni_bajar_el_nivel()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Amarillo), Lunes(10));

        f.Asistencias.RevisarPlazos(Lunes(10).AddHours(23));
        Assert.False(Obtener(f, s.Id).ReevaluacionPedida);

        f.Asistencias.RevisarPlazos(Lunes(10).AddHours(25));
        var despues = Obtener(f, s.Id);
        Assert.True(despues.ReevaluacionPedida);
        Assert.Equal(Amarillo, despues.Nivel);
        Assert.Equal(Pendiente, despues.Estado);
        Assert.True(EstaActiva(despues));

        // Sin respuesta del cliente, más tiempo tampoco la cierra ni la baja.
        f.Asistencias.RevisarPlazos(Lunes(10).AddHours(72));
        Assert.Equal(Amarillo, Obtener(f, s.Id).Nivel);
        Assert.True(EstaActiva(Obtener(f, s.Id)));
    }

    [Fact]
    public void Cola_ordena_naranjas_por_antiguedad_y_luego_amarillas_y_verdes()
    {
        var f = new Fabrica();
        var amarilla = Crear(f, Formulario(Amarillo), Lunes(9));
        var verde = Crear(f, Formulario(Verde), Lunes(9, 1));
        var naranjaVieja = Crear(f, Formulario(Naranja), Lunes(9, 2));
        var naranjaNueva = Crear(f, Formulario(Naranja), Lunes(9, 3));

        var ids = f.Asistencias.Cola().Select(x => x.Id).ToList();
        var iNV = ids.IndexOf(naranjaVieja.Id);
        var iNN = ids.IndexOf(naranjaNueva.Id);
        var iA = ids.IndexOf(amarilla.Id);
        var iV = ids.IndexOf(verde.Id);

        Assert.True(iNV >= 0 && iNN >= 0 && iA >= 0 && iV >= 0);
        Assert.True(iNV < iNN, "Dos naranjas mantienen el orden de creación.");
        Assert.True(iNN < iA, "La amarilla va detrás de todas las naranjas.");
        Assert.True(iA < iV, "La verde va detrás de la amarilla.");

        // Si la cola muestra también las rojas, van primero.
        var roja = Crear(f, Formulario(Rojo), Lunes(9, 4));
        var conRoja = f.Asistencias.Cola().Select(x => x.Id).ToList();
        var iR = conRoja.IndexOf(roja.Id);
        if (iR >= 0)
        {
            Assert.True(iR < conRoja.IndexOf(naranjaVieja.Id));
        }
    }

    [Fact]
    public void Al_cierre_naranjas_y_amarillas_pendientes_pasan_al_dia_siguiente_y_la_roja_no()
    {
        var f = new Fabrica();
        var naranja = Crear(f, Formulario(Naranja), Lunes(17));
        var amarilla = Crear(f, Formulario(Amarillo), Lunes(17));
        var roja = Crear(f, Formulario(Rojo), Lunes(17));

        Assert.False(ParaProximoDia(Obtener(f, amarilla.Id), Lunes(17, 30)));

        Assert.True(ParaProximoDia(Obtener(f, naranja.Id), Lunes(20)));
        Assert.True(ParaProximoDia(Obtener(f, amarilla.Id), Lunes(20)));
        Assert.False(ParaProximoDia(Obtener(f, roja.Id), Lunes(20)));

        // Conserva fecha original y nivel.
        Assert.Equal(Lunes(17), Obtener(f, amarilla.Id).FechaCreacion);
        Assert.Equal(Amarillo, Obtener(f, amarilla.Id).Nivel);
    }

    [Fact]
    public void Horario_de_la_clinica_y_descripcion_de_cada_nivel()
    {
        var lunes = ProximoLunes();
        Assert.True(EnHorarioClinica(Fabrica.A(lunes, 10)));
        Assert.True(EnHorarioClinica(Fabrica.A(lunes, 18, 59)));
        Assert.False(EnHorarioClinica(Fabrica.A(lunes, 8, 59)));
        Assert.False(EnHorarioClinica(Fabrica.A(lunes, 22)));
        Assert.True(EnHorarioClinica(Fabrica.A(lunes.AddDays(5), 12)));
        Assert.False(EnHorarioClinica(Fabrica.A(lunes.AddDays(5), 14)));
        Assert.False(EnHorarioClinica(Fabrica.A(lunes.AddDays(6), 10)));

        Assert.Equal(new[] { Rojo, Naranja, Amarillo, Verde }, Niveles);
        foreach (var nivel in Niveles)
        {
            Assert.False(string.IsNullOrWhiteSpace(Descripcion(nivel)));
        }
        Assert.True(Prioridad(Rojo) != Prioridad(Naranja) && Prioridad(Naranja) != Prioridad(Amarillo) && Prioridad(Amarillo) != Prioridad(Verde));
    }

    // =====================================================================
    // Cierre sin atención por Recepción (D-ASI-09)
    // =====================================================================

    [Fact]
    public void Cerrar_por_Recepcion_antes_de_que_salga_el_veterinario()
    {
        var f = new Fabrica();
        var pendiente = Crear(f, Formulario(Verde), Lunes(10));
        var aceptada = CrearAceptada(f);

        Assert.True(f.Asistencias.Cerrar(pendiente.Id, CanceladaClinica, "El cliente ya no lo necesita", Recepcion(f), Lunes(10, 20)).Exito);
        Assert.True(f.Asistencias.Cerrar(aceptada.Id, NoConcretada, "No atiende el teléfono", Recepcion(f), Lunes(10, 20)).Exito);

        var p = Obtener(f, pendiente.Id);
        Assert.Equal(CanceladaClinica, p.Estado);
        Assert.Equal("El cliente ya no lo necesita", p.MotivoCierre);
        Assert.False(EstaActiva(p));
        Assert.Contains(f.Asistencias.Cerradas(), x => x.Id == pendiente.Id);
        Assert.DoesNotContain(f.Asistencias.Cola(), x => x.Id == pendiente.Id);

        var a = Obtener(f, aceptada.Id);
        Assert.Equal(NoConcretada, a.Estado);
        Assert.False(CompartiendoUbicacion(a));
        Assert.Empty(f.Cobros.Cargos().Where(c => c.AsistenciaId == aceptada.Id));
    }

    [Fact]
    public void Cerrar_sin_motivo_con_estado_invalido_o_sin_ser_Recepcion_no_cambia_nada()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Verde), Lunes(10));
        var antes = Tomar(f, s.Id);

        Assert.False(f.Asistencias.Cerrar(s.Id, CanceladaClinica, "", Recepcion(f), Lunes(10, 5)).Exito);
        Assert.False(f.Asistencias.Cerrar(s.Id, CanceladaClinica, "   ", Recepcion(f), Lunes(10, 5)).Exito);
        Assert.False(f.Asistencias.Cerrar(s.Id, Finalizada, "Listo", Recepcion(f), Lunes(10, 5)).Exito);
        Assert.False(f.Asistencias.Cerrar(s.Id, Aceptada, "Listo", Recepcion(f), Lunes(10, 5)).Exito);
        Assert.False(f.Asistencias.Cerrar(s.Id, "Borrada", "Listo", Recepcion(f), Lunes(10, 5)).Exito);
        Assert.False(f.Asistencias.Cerrar(s.Id, CanceladaClinica, "Listo", Ferreira(f), Lunes(10, 5)).Exito);
        Assert.False(f.Asistencias.Cerrar(s.Id, CanceladaClinica, "Listo", null, Lunes(10, 5)).Exito);

        Assert.Equal(antes, Tomar(f, s.Id));
    }

    [Fact]
    public void Cerrar_despues_de_que_el_veterinario_salio_o_ya_finalizada_no_cambia_nada()
    {
        var f = new Fabrica();
        var s = CrearAceptada(f);
        Assert.True(f.Asistencias.Avanzar(s.Id, Ferreira(f), Lunes(10, 10)).Exito);
        var antes = Tomar(f, s.Id);

        Assert.False(f.Asistencias.Cerrar(s.Id, CanceladaClinica, "Se arrepintió", Recepcion(f), Lunes(10, 15)).Exito);
        Assert.Equal(antes, Tomar(f, s.Id));

        var cerrada = Crear(f, Formulario(Verde), Lunes(10));
        Assert.True(f.Asistencias.Cerrar(cerrada.Id, CanceladaClinica, "Duplicada por error", Recepcion(f), Lunes(10, 5)).Exito);
        var antesCerrada = Tomar(f, cerrada.Id);
        Assert.False(f.Asistencias.Cerrar(cerrada.Id, NoConcretada, "Otra vez", Recepcion(f), Lunes(10, 6)).Exito);
        Assert.Equal(antesCerrada, Tomar(f, cerrada.Id));
    }

    [Fact]
    public void Cerrar_un_rojo_solo_despues_de_que_paso_a_Recepcion()
    {
        var f = new Fabrica();
        var s = Crear(f, Formulario(Rojo), Lunes(10));
        var antes = Tomar(f, s.Id);

        Assert.False(f.Asistencias.Cerrar(s.Id, NoConcretada, "No contesta", Recepcion(f), Lunes(10, 2)).Exito);
        Assert.Equal(antes, Tomar(f, s.Id));

        f.Asistencias.RevisarPlazos(Lunes(10, 11));
        Assert.Equal(Coordinacion, Obtener(f, s.Id).Estado);

        Assert.True(f.Asistencias.Cerrar(s.Id, NoConcretada, "Tres llamadas sin respuesta", Recepcion(f), Lunes(10, 20)).Exito);
        Assert.Equal(NoConcretada, Obtener(f, s.Id).Estado);
        Assert.NotEmpty(f.Asistencias.Eventos(s.Id));
    }
}
