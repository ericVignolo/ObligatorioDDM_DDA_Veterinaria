using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests.CajaNegra;

// Pruebas de caja negra de Internación (A-024, CU-VET-02), escritas solo desde
// docs/analisis/ESPECIFICACION_INTERNACION.md, los modelos y las firmas públicas.
// Datos de arranque: 1 Toby (cliente 100), 2 Mishi (cliente 100), 3 grupo (cliente 101),
// 4 Canela (cliente 101, internada en el Box equino id 4). Boxes 1-3 pequeños, 4 equino.
public class InternacionCajaNegraTests
{
    private const int Toby = 1;
    private const int Mishi = 2;
    private const int Grupo = 3;
    private const int Canela = 4;
    private const int BoxEquino = 4;

    private static Personal Recepcion(Fabrica f) => f.PersonalDe("recepcion@vitalia.uy");
    private static Personal Ferreira(Fabrica f) => f.PersonalDe("ferreira@vitalia.uy");
    private static Personal Acosta(Fabrica f) => f.PersonalDe("acosta@vitalia.uy");
    private static Personal Admin(Fabrica f) => f.PersonalDe("admin@vitalia.uy");

    private static AdmisionFormulario AdmisionValida(int animalId)
    {
        return new AdmisionFormulario { AnimalId = animalId, Motivo = "Deshidratación", TelefonoContacto = "099 123 456", Observaciones = "" };
    }

    private static IngresoClinicoFormulario IngresoValido(int boxId)
    {
        return new IngresoClinicoFormulario { BoxId = boxId, PlanTerapeutico = "Suero y control cada 6 horas" };
    }

    private static NovedadFormulario NovedadValida(bool visible, string tipo = "Novedad")
    {
        return new NovedadFormulario { Texto = "Comió bien", Tipo = tipo, Visible = visible };
    }

    private static AltaFormulario AltaValida()
    {
        return new AltaFormulario { EvolucionFinal = "Evolucionó favorablemente", IndicacionesDomicilio = "Dieta blanda por 3 días" };
    }

    private static string Texto(int largo) => new string('a', largo);

    // Admite a Toby (Recepción) y hace el ingreso clínico en el box indicado (Ferreira).
    private static Internacion InternarToby(Fabrica f, int boxId = 1)
    {
        Assert.True(f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f)).Exito);
        var internacion = f.Internaciones.AbiertaDe(Toby)!;
        Assert.True(f.Internaciones.IngresarClinico(internacion.Id, IngresoValido(boxId), Ferreira(f)).Exito);
        return f.Internaciones.ObtenerPorId(internacion.Id)!;
    }

    private static Internacion InternacionDeCanela(Fabrica f) => f.Internaciones.AbiertaDe(Canela)!;

    // ---------- Datos de arranque y boxes ----------

    [Fact]
    public void Arranque_tiene_tres_boxes_pequenos_y_uno_equino()
    {
        var f = new Fabrica();
        var boxes = f.Internaciones.Boxes();

        Assert.Equal(4, boxes.Count);
        Assert.Equal(3, boxes.Count(b => b.Tipo == InternacionService.PequenosAnimales));
        Assert.Equal(InternacionService.GrandesAnimales, f.Internaciones.ObtenerBox(BoxEquino)!.Tipo);
    }

    [Fact]
    public void Arranque_Canela_internada_en_box_equino_con_cuatro_entradas_dos_visibles()
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);

        Assert.NotNull(canela);
        Assert.Equal(InternacionService.Internado, canela.Estado);
        Assert.Equal(BoxEquino, canela.BoxId);
        Assert.Equal(Canela, f.Internaciones.OcupanteDe(BoxEquino)!.AnimalId);
        Assert.Equal(4, f.Internaciones.Novedades(canela.Id).Count);
        Assert.Equal(2, f.Internaciones.NovedadesVisibles(canela.Id).Count);
    }

    [Fact]
    public void Tipo_de_box_segun_especie()
    {
        var f = new Fabrica();
        var toby = f.Animales.ObtenerParaPersonal(Toby)!;
        var canela = f.Animales.ObtenerParaPersonal(Canela)!;

        Assert.Equal(InternacionService.PequenosAnimales, InternacionService.TipoDeBoxPara(toby));
        Assert.Equal(InternacionService.GrandesAnimales, InternacionService.TipoDeBoxPara(canela));
    }

    [Fact]
    public void Boxes_libres_para_perro_son_los_pequenos_y_para_caballo_ninguno_si_el_equino_esta_ocupado()
    {
        var f = new Fabrica();
        var toby = f.Animales.ObtenerParaPersonal(Toby)!;
        var canela = f.Animales.ObtenerParaPersonal(Canela)!;
        var otroCaballo = f.AnimalDe(101, canela.Especie, "Tornado");

        var libresToby = f.Internaciones.BoxesLibresPara(toby);
        Assert.Equal(3, libresToby.Count);
        Assert.All(libresToby, b => Assert.Equal(InternacionService.PequenosAnimales, b.Tipo));
        Assert.Empty(f.Internaciones.BoxesLibresPara(otroCaballo));
    }

    // ---------- I-01 / I-03: admisión ----------

    [Fact]
    public void Recepcion_admite_y_la_internacion_queda_Admitida_sin_box()
    {
        var f = new Fabrica();

        var resultado = f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f));

        Assert.True(resultado.Exito);
        var internacion = f.Internaciones.AbiertaDe(Toby);
        Assert.NotNull(internacion);
        Assert.Equal(InternacionService.Admitida, internacion!.Estado);
        Assert.Equal(0, internacion.BoxId);
        Assert.Equal(100, internacion.ClienteId);
        Assert.Equal("Deshidratación", internacion.Motivo);
        Assert.True(InternacionService.EstaAbierta(internacion));
        Assert.Contains(f.Internaciones.Abiertas(), i => i.Id == internacion.Id);
    }

    [Fact]
    public void Veterinario_administrador_o_sin_sesion_no_admiten()
    {
        var f = new Fabrica();
        var abiertasAntes = f.Internaciones.Abiertas().Count;

        Assert.False(f.Internaciones.Admitir(AdmisionValida(Toby), Ferreira(f)).Exito);
        Assert.False(f.Internaciones.Admitir(AdmisionValida(Toby), Admin(f)).Exito);
        Assert.False(f.Internaciones.Admitir(AdmisionValida(Toby), null).Exito);

        Assert.Null(f.Internaciones.AbiertaDe(Toby));
        Assert.Equal(abiertasAntes, f.Internaciones.Abiertas().Count);
    }

    [Theory]
    [InlineData("", "099 123 456")]
    [InlineData("   ", "099 123 456")]
    [InlineData("Deshidratación", "")]
    [InlineData("Deshidratación", "   ")]
    public void Admision_sin_motivo_o_sin_telefono_se_rechaza(string motivo, string telefono)
    {
        var f = new Fabrica();
        var datos = new AdmisionFormulario { AnimalId = Toby, Motivo = motivo, TelefonoContacto = telefono };

        Assert.False(f.Internaciones.Admitir(datos, Recepcion(f)).Exito);
        Assert.Null(f.Internaciones.AbiertaDe(Toby));
    }

    [Fact]
    public void Admision_con_largos_maximos_se_acepta()
    {
        var f = new Fabrica();
        var datos = new AdmisionFormulario { AnimalId = Toby, Motivo = Texto(200), TelefonoContacto = new string('9', 30), Observaciones = Texto(500) };

        Assert.True(f.Internaciones.Admitir(datos, Recepcion(f)).Exito);
    }

    [Theory]
    [InlineData(201, 30, 500)]
    [InlineData(200, 31, 500)]
    [InlineData(200, 30, 501)]
    public void Admision_con_un_caracter_de_mas_se_rechaza(int largoMotivo, int largoTelefono, int largoObservaciones)
    {
        var f = new Fabrica();
        var datos = new AdmisionFormulario
        {
            AnimalId = Toby,
            Motivo = Texto(largoMotivo),
            TelefonoContacto = new string('9', largoTelefono),
            Observaciones = Texto(largoObservaciones)
        };

        Assert.False(f.Internaciones.Admitir(datos, Recepcion(f)).Exito);
        Assert.Null(f.Internaciones.AbiertaDe(Toby));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(999)]
    public void Admision_de_animal_inexistente_se_rechaza(int animalId)
    {
        var f = new Fabrica();
        var abiertasAntes = f.Internaciones.Abiertas().Count;

        Assert.False(f.Internaciones.Admitir(AdmisionValida(animalId), Recepcion(f)).Exito);
        Assert.Equal(abiertasAntes, f.Internaciones.Abiertas().Count);
    }

    // ---------- I-02: una abierta por animal; los grupos no se internan ----------

    [Fact]
    public void Animal_con_internacion_abierta_no_se_admite_de_nuevo()
    {
        var f = new Fabrica();
        Assert.True(f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f)).Exito);
        var abiertasAntes = f.Internaciones.Abiertas().Count;

        Assert.False(f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f)).Exito);
        Assert.False(f.Internaciones.Admitir(AdmisionValida(Canela), Recepcion(f)).Exito);
        Assert.Equal(abiertasAntes, f.Internaciones.Abiertas().Count);
    }

    [Fact]
    public void Grupo_de_animales_no_se_interna()
    {
        var f = new Fabrica();

        Assert.False(f.Internaciones.Admitir(AdmisionValida(Grupo), Recepcion(f)).Exito);
        Assert.Null(f.Internaciones.AbiertaDe(Grupo));
    }

    // ---------- I-04: anulación ----------

    [Fact]
    public void Recepcion_anula_una_admision_y_el_animal_se_puede_volver_a_admitir()
    {
        var f = new Fabrica();
        f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f));
        var id = f.Internaciones.AbiertaDe(Toby)!.Id;

        Assert.True(f.Internaciones.AnularAdmision(id, "Se lo llevó el dueño", Recepcion(f)).Exito);

        var anulada = f.Internaciones.ObtenerPorId(id)!;
        Assert.Equal(InternacionService.Anulada, anulada.Estado);
        Assert.False(InternacionService.EstaAbierta(anulada));
        Assert.Null(f.Internaciones.AbiertaDe(Toby));
        Assert.True(f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f)).Exito);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Anulacion_sin_motivo_se_rechaza(string motivo)
    {
        var f = new Fabrica();
        f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f));
        var id = f.Internaciones.AbiertaDe(Toby)!.Id;

        Assert.False(f.Internaciones.AnularAdmision(id, motivo, Recepcion(f)).Exito);
        Assert.Equal(InternacionService.Admitida, f.Internaciones.ObtenerPorId(id)!.Estado);
    }

    [Fact]
    public void Anulacion_con_motivo_de_200_se_acepta_y_de_201_se_rechaza()
    {
        var f = new Fabrica();
        f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f));
        var id = f.Internaciones.AbiertaDe(Toby)!.Id;

        Assert.False(f.Internaciones.AnularAdmision(id, Texto(201), Recepcion(f)).Exito);
        Assert.Equal(InternacionService.Admitida, f.Internaciones.ObtenerPorId(id)!.Estado);
        Assert.True(f.Internaciones.AnularAdmision(id, Texto(200), Recepcion(f)).Exito);
    }

    [Fact]
    public void Veterinario_o_administrador_no_anulan_una_admision()
    {
        var f = new Fabrica();
        f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f));
        var id = f.Internaciones.AbiertaDe(Toby)!.Id;

        Assert.False(f.Internaciones.AnularAdmision(id, "Error", Ferreira(f)).Exito);
        Assert.False(f.Internaciones.AnularAdmision(id, "Error", Admin(f)).Exito);
        Assert.False(f.Internaciones.AnularAdmision(id, "Error", null).Exito);
        Assert.Equal(InternacionService.Admitida, f.Internaciones.ObtenerPorId(id)!.Estado);
    }

    [Fact]
    public void Internacion_ya_Internado_no_se_anula_ni_libera_el_box()
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);

        Assert.False(f.Internaciones.AnularAdmision(canela.Id, "Error", Recepcion(f)).Exito);
        Assert.Equal(InternacionService.Internado, f.Internaciones.ObtenerPorId(canela.Id)!.Estado);
        Assert.Equal(Canela, f.Internaciones.OcupanteDe(BoxEquino)!.AnimalId);
    }

    [Fact]
    public void Anular_una_internacion_inexistente_falla()
    {
        var f = new Fabrica();

        Assert.False(f.Internaciones.AnularAdmision(9999, "Error", Recepcion(f)).Exito);
    }

    // ---------- I-05 / I-06: ingreso clínico ----------

    [Fact]
    public void Veterinario_hace_el_ingreso_clinico_y_queda_Internado_en_el_box()
    {
        var f = new Fabrica();
        var internacion = InternarToby(f, 2);

        Assert.Equal(InternacionService.Internado, internacion.Estado);
        Assert.Equal(2, internacion.BoxId);
        Assert.Equal("Suero y control cada 6 horas", internacion.PlanTerapeutico);
        Assert.NotNull(internacion.FechaIngreso);
        Assert.Equal(Toby, f.Internaciones.OcupanteDe(2)!.AnimalId);
        Assert.DoesNotContain(f.Internaciones.BoxesLibresPara(f.Animales.ObtenerParaPersonal(Mishi)!), b => b.Id == 2);
    }

    [Fact]
    public void Recepcion_o_administrador_no_hacen_el_ingreso_clinico()
    {
        var f = new Fabrica();
        f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f));
        var id = f.Internaciones.AbiertaDe(Toby)!.Id;

        Assert.False(f.Internaciones.IngresarClinico(id, IngresoValido(1), Recepcion(f)).Exito);
        Assert.False(f.Internaciones.IngresarClinico(id, IngresoValido(1), Admin(f)).Exito);
        Assert.False(f.Internaciones.IngresarClinico(id, IngresoValido(1), null).Exito);

        var sinCambios = f.Internaciones.ObtenerPorId(id)!;
        Assert.Equal(InternacionService.Admitida, sinCambios.Estado);
        Assert.Equal(0, sinCambios.BoxId);
        Assert.Null(f.Internaciones.OcupanteDe(1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2000)]
    public void Ingreso_clinico_con_plan_vacio_o_de_mas_de_2000_se_rechaza(int largoPlan)
    {
        var f = new Fabrica();
        f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f));
        var id = f.Internaciones.AbiertaDe(Toby)!.Id;
        var plan = largoPlan == 0 ? "   " : Texto(largoPlan + 1);

        Assert.False(f.Internaciones.IngresarClinico(id, new IngresoClinicoFormulario { BoxId = 1, PlanTerapeutico = plan }, Ferreira(f)).Exito);
        Assert.Equal(InternacionService.Admitida, f.Internaciones.ObtenerPorId(id)!.Estado);
        Assert.Null(f.Internaciones.OcupanteDe(1));
    }

    [Fact]
    public void Ingreso_clinico_con_plan_de_2000_se_acepta()
    {
        var f = new Fabrica();
        f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f));
        var id = f.Internaciones.AbiertaDe(Toby)!.Id;

        Assert.True(f.Internaciones.IngresarClinico(id, new IngresoClinicoFormulario { BoxId = 1, PlanTerapeutico = Texto(2000) }, Acosta(f)).Exito);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99)]
    public void Ingreso_clinico_sin_box_o_con_box_inexistente_se_rechaza(int boxId)
    {
        var f = new Fabrica();
        f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f));
        var id = f.Internaciones.AbiertaDe(Toby)!.Id;

        Assert.False(f.Internaciones.IngresarClinico(id, IngresoValido(boxId), Ferreira(f)).Exito);
        Assert.Equal(InternacionService.Admitida, f.Internaciones.ObtenerPorId(id)!.Estado);
    }

    [Fact]
    public void Box_ocupado_no_admite_un_segundo_internado()
    {
        var f = new Fabrica();
        InternarToby(f, 1);
        f.Internaciones.Admitir(AdmisionValida(Mishi), Recepcion(f));
        var idMishi = f.Internaciones.AbiertaDe(Mishi)!.Id;

        Assert.False(f.Internaciones.IngresarClinico(idMishi, IngresoValido(1), Ferreira(f)).Exito);

        Assert.Equal(InternacionService.Admitida, f.Internaciones.ObtenerPorId(idMishi)!.Estado);
        Assert.Equal(Toby, f.Internaciones.OcupanteDe(1)!.AnimalId);
    }

    [Fact]
    public void Box_de_otro_tipo_se_rechaza_aunque_este_libre()
    {
        var f = new Fabrica();
        // Se libera el Box equino dando el alta a Canela.
        Assert.True(f.Internaciones.DarAlta(InternacionDeCanela(f).Id, AltaValida(), Ferreira(f)).Exito);
        f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f));
        var idToby = f.Internaciones.AbiertaDe(Toby)!.Id;

        Assert.False(f.Internaciones.IngresarClinico(idToby, IngresoValido(BoxEquino), Ferreira(f)).Exito);
        Assert.Null(f.Internaciones.OcupanteDe(BoxEquino));

        // Y un caballo no entra en un box de pequeños animales.
        var especieCaballo = f.Animales.ObtenerParaPersonal(Canela)!.Especie;
        var tornado = f.AnimalDe(101, especieCaballo, "Tornado");
        Assert.True(f.Internaciones.Admitir(AdmisionValida(tornado.Id), Recepcion(f)).Exito);
        var idTornado = f.Internaciones.AbiertaDe(tornado.Id)!.Id;
        Assert.False(f.Internaciones.IngresarClinico(idTornado, IngresoValido(1), Ferreira(f)).Exito);
        Assert.True(f.Internaciones.IngresarClinico(idTornado, IngresoValido(BoxEquino), Ferreira(f)).Exito);
    }

    [Fact]
    public void Ingreso_clinico_sobre_una_internacion_que_no_esta_Admitida_se_rechaza()
    {
        var f = new Fabrica();
        var toby = InternarToby(f, 1);

        // Ya Internado: no se repite el ingreso ni se cambia de box.
        Assert.False(f.Internaciones.IngresarClinico(toby.Id, IngresoValido(2), Ferreira(f)).Exito);
        Assert.Equal(1, f.Internaciones.ObtenerPorId(toby.Id)!.BoxId);
        Assert.Null(f.Internaciones.OcupanteDe(2));

        // Anulada: tampoco.
        f.Internaciones.Admitir(AdmisionValida(Mishi), Recepcion(f));
        var idMishi = f.Internaciones.AbiertaDe(Mishi)!.Id;
        f.Internaciones.AnularAdmision(idMishi, "Se fue", Recepcion(f));
        Assert.False(f.Internaciones.IngresarClinico(idMishi, IngresoValido(3), Ferreira(f)).Exito);
        Assert.Equal(InternacionService.Anulada, f.Internaciones.ObtenerPorId(idMishi)!.Estado);
    }

    // ---------- I-07: publicar ----------

    [Theory]
    [InlineData("Novedad", true)]
    [InlineData("Evolución", false)]
    public void Veterinario_publica_novedad_o_evolucion_con_su_visibilidad(string tipo, bool visible)
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);
        var antes = f.Internaciones.Novedades(canela.Id).Count;
        var vet = Acosta(f);

        Assert.True(f.Internaciones.Publicar(canela.Id, NovedadValida(visible, tipo), vet).Exito);

        var novedades = f.Internaciones.Novedades(canela.Id);
        Assert.Equal(antes + 1, novedades.Count);
        var nueva = novedades.OrderBy(n => n.Id).Last();
        Assert.Equal(tipo, nueva.Tipo);
        Assert.Equal(visible, nueva.Visible);
        Assert.Equal(vet.Id, nueva.AutorId);
        Assert.Equal(canela.Id, nueva.InternacionId);
    }

    [Fact]
    public void Recepcion_administrador_o_sin_sesion_no_publican()
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);
        var antes = f.Internaciones.Novedades(canela.Id).Count;

        Assert.False(f.Internaciones.Publicar(canela.Id, NovedadValida(true), Recepcion(f)).Exito);
        Assert.False(f.Internaciones.Publicar(canela.Id, NovedadValida(true), Admin(f)).Exito);
        Assert.False(f.Internaciones.Publicar(canela.Id, NovedadValida(true), null).Exito);
        Assert.Equal(antes, f.Internaciones.Novedades(canela.Id).Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Publicar_sin_texto_se_rechaza(string texto)
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);
        var antes = f.Internaciones.Novedades(canela.Id).Count;

        Assert.False(f.Internaciones.Publicar(canela.Id, new NovedadFormulario { Texto = texto, Tipo = InternacionService.Novedad, Visible = true }, Ferreira(f)).Exito);
        Assert.Equal(antes, f.Internaciones.Novedades(canela.Id).Count);
    }

    [Fact]
    public void Publicar_texto_de_1000_se_acepta_y_de_1001_se_rechaza()
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);
        var antes = f.Internaciones.Novedades(canela.Id).Count;

        Assert.False(f.Internaciones.Publicar(canela.Id, new NovedadFormulario { Texto = Texto(1001), Tipo = InternacionService.Novedad }, Ferreira(f)).Exito);
        Assert.Equal(antes, f.Internaciones.Novedades(canela.Id).Count);
        Assert.True(f.Internaciones.Publicar(canela.Id, new NovedadFormulario { Texto = Texto(1000), Tipo = InternacionService.Novedad }, Ferreira(f)).Exito);
    }

    [Theory]
    [InlineData("Rectificación")]
    [InlineData("Otro")]
    [InlineData("")]
    public void Publicar_con_tipo_distinto_de_novedad_o_evolucion_se_rechaza(string tipo)
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);
        var antes = f.Internaciones.Novedades(canela.Id).Count;

        Assert.False(f.Internaciones.Publicar(canela.Id, NovedadValida(true, tipo), Ferreira(f)).Exito);
        Assert.Equal(antes, f.Internaciones.Novedades(canela.Id).Count);
    }

    [Fact]
    public void Publicar_en_una_internacion_que_no_esta_Internado_se_rechaza()
    {
        var f = new Fabrica();
        f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f));
        var idAdmitida = f.Internaciones.AbiertaDe(Toby)!.Id;

        Assert.False(f.Internaciones.Publicar(idAdmitida, NovedadValida(true), Ferreira(f)).Exito);
        Assert.Empty(f.Internaciones.Novedades(idAdmitida));

        var canela = InternacionDeCanela(f);
        f.Internaciones.DarAlta(canela.Id, AltaValida(), Ferreira(f));
        var antes = f.Internaciones.Novedades(canela.Id).Count;
        Assert.False(f.Internaciones.Publicar(canela.Id, NovedadValida(true), Ferreira(f)).Exito);
        Assert.Equal(antes, f.Internaciones.Novedades(canela.Id).Count);

        Assert.False(f.Internaciones.Publicar(9999, NovedadValida(true), Ferreira(f)).Exito);
    }

    // ---------- I-08: rectificación ----------

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Rectificar_agrega_una_entrada_nueva_con_la_visibilidad_de_la_original(bool visibleOriginal)
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);
        var original = f.Internaciones.Novedades(canela.Id).First(n => n.Visible == visibleOriginal);
        var textoOriginal = original.Texto;
        var antes = f.Internaciones.Novedades(canela.Id).Count;

        // Se pide la visibilidad contraria: debe prevalecer la de la original.
        var datos = new NovedadFormulario { Texto = "Corrección: era 38,5 °C", Tipo = InternacionService.Novedad, Visible = !visibleOriginal };
        Assert.True(f.Internaciones.Rectificar(canela.Id, original.Id, datos, Acosta(f)).Exito);

        var novedades = f.Internaciones.Novedades(canela.Id);
        Assert.Equal(antes + 1, novedades.Count);
        var rectificacion = novedades.OrderBy(n => n.Id).Last();
        Assert.Equal(InternacionService.Rectificacion, rectificacion.Tipo);
        Assert.Equal(original.Id, rectificacion.RectificaId);
        Assert.Equal(visibleOriginal, rectificacion.Visible);

        // La original no se edita ni se borra.
        var originalDespues = f.Internaciones.ObtenerNovedad(original.Id);
        Assert.NotNull(originalDespues);
        Assert.Equal(textoOriginal, originalDespues!.Texto);
        Assert.Equal(visibleOriginal, originalDespues.Visible);
    }

    [Fact]
    public void Recepcion_o_administrador_no_rectifican()
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);
        var original = f.Internaciones.NovedadesVisibles(canela.Id).First();
        var antes = f.Internaciones.Novedades(canela.Id).Count;

        Assert.False(f.Internaciones.Rectificar(canela.Id, original.Id, NovedadValida(true), Recepcion(f)).Exito);
        Assert.False(f.Internaciones.Rectificar(canela.Id, original.Id, NovedadValida(true), Admin(f)).Exito);
        Assert.False(f.Internaciones.Rectificar(canela.Id, original.Id, NovedadValida(true), null).Exito);
        Assert.Equal(antes, f.Internaciones.Novedades(canela.Id).Count);
    }

    [Fact]
    public void Rectificar_sin_texto_o_una_entrada_inexistente_o_de_otra_internacion_se_rechaza()
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);
        var original = f.Internaciones.Novedades(canela.Id).First();
        var toby = InternarToby(f, 1);
        var antesCanela = f.Internaciones.Novedades(canela.Id).Count;

        Assert.False(f.Internaciones.Rectificar(canela.Id, original.Id, new NovedadFormulario { Texto = "  " }, Ferreira(f)).Exito);
        Assert.False(f.Internaciones.Rectificar(canela.Id, 9999, NovedadValida(true), Ferreira(f)).Exito);
        Assert.False(f.Internaciones.Rectificar(toby.Id, original.Id, NovedadValida(true), Ferreira(f)).Exito);

        Assert.Equal(antesCanela, f.Internaciones.Novedades(canela.Id).Count);
        Assert.Empty(f.Internaciones.Novedades(toby.Id));
    }

    // ---------- I-09: visibilidad ----------

    [Fact]
    public void Nota_interna_no_aparece_en_las_novedades_visibles()
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);
        var visiblesAntes = f.Internaciones.NovedadesVisibles(canela.Id).Count;

        f.Internaciones.Publicar(canela.Id, new NovedadFormulario { Texto = "Nota interna", Tipo = InternacionService.Novedad, Visible = false }, Ferreira(f));
        f.Internaciones.Publicar(canela.Id, new NovedadFormulario { Texto = "Para el dueño", Tipo = InternacionService.Novedad, Visible = true }, Ferreira(f));

        var visibles = f.Internaciones.NovedadesVisibles(canela.Id);
        Assert.Equal(visiblesAntes + 1, visibles.Count);
        Assert.All(visibles, n => Assert.True(n.Visible));
        Assert.DoesNotContain(visibles, n => n.Texto == "Nota interna");
        Assert.Contains(f.Internaciones.Novedades(canela.Id), n => n.Texto == "Nota interna");
    }

    // ---------- I-10: alta ----------

    [Fact]
    public void Veterinario_da_el_alta_registra_los_datos_y_libera_el_box()
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);

        Assert.True(f.Internaciones.DarAlta(canela.Id, AltaValida(), Acosta(f)).Exito);

        var alta = f.Internaciones.ObtenerPorId(canela.Id)!;
        Assert.Equal(InternacionService.Alta, alta.Estado);
        Assert.False(InternacionService.EstaAbierta(alta));
        Assert.Equal("Evolucionó favorablemente", alta.EvolucionFinal);
        Assert.Equal("Dieta blanda por 3 días", alta.IndicacionesDomicilio);
        Assert.NotNull(alta.FechaAlta);
        Assert.Null(f.Internaciones.OcupanteDe(BoxEquino));
        Assert.Null(f.Internaciones.AbiertaDe(Canela));
        Assert.Contains(f.Internaciones.Altas(), i => i.Id == canela.Id);
        Assert.DoesNotContain(f.Internaciones.Abiertas(), i => i.Id == canela.Id);
        Assert.Contains(f.Internaciones.BoxesLibresPara(f.Animales.ObtenerParaPersonal(Canela)!), b => b.Id == BoxEquino);

        // Tras el alta se puede volver a internar.
        Assert.True(f.Internaciones.Admitir(AdmisionValida(Canela), Recepcion(f)).Exito);
    }

    [Fact]
    public void Recepcion_administrador_o_sin_sesion_no_dan_el_alta()
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);

        Assert.False(f.Internaciones.DarAlta(canela.Id, AltaValida(), Recepcion(f)).Exito);
        Assert.False(f.Internaciones.DarAlta(canela.Id, AltaValida(), Admin(f)).Exito);
        Assert.False(f.Internaciones.DarAlta(canela.Id, AltaValida(), null).Exito);

        Assert.Equal(InternacionService.Internado, f.Internaciones.ObtenerPorId(canela.Id)!.Estado);
        Assert.Equal(Canela, f.Internaciones.OcupanteDe(BoxEquino)!.AnimalId);
    }

    [Theory]
    [InlineData("", "Dieta blanda")]
    [InlineData("   ", "Dieta blanda")]
    [InlineData("Bien", "")]
    [InlineData("Bien", "   ")]
    public void Alta_sin_evolucion_final_o_sin_indicaciones_se_rechaza(string evolucion, string indicaciones)
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);

        Assert.False(f.Internaciones.DarAlta(canela.Id, new AltaFormulario { EvolucionFinal = evolucion, IndicacionesDomicilio = indicaciones }, Ferreira(f)).Exito);
        Assert.Equal(InternacionService.Internado, f.Internaciones.ObtenerPorId(canela.Id)!.Estado);
        Assert.Equal(Canela, f.Internaciones.OcupanteDe(BoxEquino)!.AnimalId);
    }

    [Theory]
    [InlineData(1001, 2000, false)]
    [InlineData(1000, 2001, false)]
    [InlineData(1000, 2000, true)]
    public void Alta_respeta_los_largos_maximos(int largoEvolucion, int largoIndicaciones, bool esperado)
    {
        var f = new Fabrica();
        var canela = InternacionDeCanela(f);
        var datos = new AltaFormulario { EvolucionFinal = Texto(largoEvolucion), IndicacionesDomicilio = Texto(largoIndicaciones) };

        Assert.Equal(esperado, f.Internaciones.DarAlta(canela.Id, datos, Ferreira(f)).Exito);
        var estadoEsperado = esperado ? InternacionService.Alta : InternacionService.Internado;
        Assert.Equal(estadoEsperado, f.Internaciones.ObtenerPorId(canela.Id)!.Estado);
    }

    [Fact]
    public void Alta_sobre_una_admision_sin_ingreso_o_ya_dada_de_alta_se_rechaza()
    {
        var f = new Fabrica();
        f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f));
        var idToby = f.Internaciones.AbiertaDe(Toby)!.Id;

        Assert.False(f.Internaciones.DarAlta(idToby, AltaValida(), Ferreira(f)).Exito);
        Assert.Equal(InternacionService.Admitida, f.Internaciones.ObtenerPorId(idToby)!.Estado);

        var canela = InternacionDeCanela(f);
        Assert.True(f.Internaciones.DarAlta(canela.Id, AltaValida(), Ferreira(f)).Exito);
        var otraAlta = new AltaFormulario { EvolucionFinal = "Otra", IndicacionesDomicilio = "Otras" };
        Assert.False(f.Internaciones.DarAlta(canela.Id, otraAlta, Acosta(f)).Exito);
        Assert.Equal("Evolucionó favorablemente", f.Internaciones.ObtenerPorId(canela.Id)!.EvolucionFinal);

        Assert.False(f.Internaciones.DarAlta(9999, AltaValida(), Ferreira(f)).Exito);
    }

    // ---------- I-11: el cliente solo ve lo suyo, sin anuladas ----------

    [Fact]
    public void Cliente_solo_ve_las_internaciones_de_sus_animales()
    {
        var f = new Fabrica();

        Assert.NotEmpty(f.Internaciones.DeAnimal(Canela, 101));
        Assert.All(f.Internaciones.DeAnimal(Canela, 101), i => Assert.Equal(Canela, i.AnimalId));
        Assert.Empty(f.Internaciones.DeAnimal(Canela, 100));
    }

    [Fact]
    public void Cliente_no_ve_las_admisiones_anuladas_pero_si_las_vigentes()
    {
        var f = new Fabrica();
        f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f));
        var anulada = f.Internaciones.AbiertaDe(Toby)!.Id;
        f.Internaciones.AnularAdmision(anulada, "Error de carga", Recepcion(f));
        f.Internaciones.Admitir(AdmisionValida(Toby), Recepcion(f));
        var vigente = f.Internaciones.AbiertaDe(Toby)!.Id;

        var delCliente = f.Internaciones.DeAnimal(Toby, 100);

        Assert.DoesNotContain(delCliente, i => i.Id == anulada);
        Assert.Contains(delCliente, i => i.Id == vigente);
        Assert.Empty(f.Internaciones.DeAnimal(Toby, 101));
    }
}
