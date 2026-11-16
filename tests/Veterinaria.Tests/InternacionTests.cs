using Veterinaria.Blazor.Models;
using Veterinaria.Blazor.Services;

namespace Veterinaria.Tests;

// A-024: RF-VET-09 a RF-VET-11, RF-CLI-09, RN-18, RN-21 a RN-25.
// Datos de arranque: Canela (4, caballo de Pedro, cliente 101) internada en el Box equino (4); Toby (1, perro de Ana, 100);
// «Rodeo La Esperanza» (3) es un grupo.
public class InternacionTests
{
    private const string Recepcion = "recepcion@vitalia.uy";
    private const string Ferreira = "ferreira@vitalia.uy";
    private const string Admin = "admin@vitalia.uy";

    private static AdmisionFormulario AdmisionDe(int animalId)
    {
        return new AdmisionFormulario { AnimalId = animalId, Motivo = "Postoperatorio", TelefonoContacto = "099 111 222" };
    }

    // Toby admitido por Recepción.
    private static (Fabrica f, Internacion internacion) TobyAdmitido()
    {
        var f = new Fabrica();
        var resultado = f.Internaciones.Admitir(AdmisionDe(1), f.PersonalDe(Recepcion));
        Assert.True(resultado.Exito, resultado.Mensaje);
        return (f, f.Internaciones.AbiertaDe(1)!);
    }

    // Toby internado en el Box 1 por la Dra. Ferreira.
    private static (Fabrica f, Internacion internacion, Personal vet) TobyInternado()
    {
        var (f, internacion) = TobyAdmitido();
        var vet = f.PersonalDe(Ferreira);
        var resultado = f.Internaciones.IngresarClinico(internacion.Id, new IngresoClinicoFormulario { BoxId = 1, PlanTerapeutico = "Analgesia y control" }, vet);
        Assert.True(resultado.Exito, resultado.Mensaje);
        return (f, internacion, vet);
    }

    private static NovedadFormulario Entrada(string texto, bool visible, string tipo = InternacionService.Novedad)
    {
        return new NovedadFormulario { Texto = texto, Visible = visible, Tipo = tipo };
    }

    [Fact]
    public void Arranca_con_Canela_internada_en_el_box_equino()
    {
        var f = new Fabrica();

        var canela = f.Internaciones.AbiertaDe(4);

        Assert.NotNull(canela);
        Assert.Equal(InternacionService.Internado, canela!.Estado);
        Assert.Same(canela, f.Internaciones.OcupanteDe(4));
        Assert.Equal(2, f.Internaciones.NovedadesVisibles(canela.Id).Count);
        Assert.Equal(4, f.Internaciones.Novedades(canela.Id).Count);
    }

    [Fact]
    public void Recepcion_admite_y_queda_esperando_el_ingreso_clinico_sin_box()
    {
        var (f, internacion) = TobyAdmitido();

        Assert.Equal(InternacionService.Admitida, internacion.Estado);
        Assert.Equal(0, internacion.BoxId);
        Assert.Equal(100, internacion.ClienteId);
        Assert.Equal("Laura Benítez", internacion.AdmitidoPor);
    }

    [Fact]
    public void Solo_Recepcion_admite()
    {
        var f = new Fabrica();

        Assert.False(f.Internaciones.Admitir(AdmisionDe(1), f.PersonalDe(Ferreira)).Exito);
        Assert.False(f.Internaciones.Admitir(AdmisionDe(1), f.PersonalDe(Admin)).Exito);
        Assert.False(f.Internaciones.Admitir(AdmisionDe(1), null).Exito);
        Assert.Null(f.Internaciones.AbiertaDe(1));
    }

    [Fact]
    public void Un_animal_no_tiene_dos_internaciones_abiertas()
    {
        var (f, _) = TobyAdmitido();

        var segunda = f.Internaciones.Admitir(AdmisionDe(1), f.PersonalDe(Recepcion));

        Assert.False(segunda.Exito);
        Assert.Single(f.Internaciones.Abiertas(), i => i.AnimalId == 1);
    }

    [Fact]
    public void Un_grupo_de_animales_no_se_interna()
    {
        var f = new Fabrica();

        Assert.False(f.Internaciones.Admitir(AdmisionDe(3), f.PersonalDe(Recepcion)).Exito);
        Assert.Null(f.Internaciones.AbiertaDe(3));
    }

    [Fact]
    public void La_admision_exige_motivo_y_telefono()
    {
        var f = new Fabrica();
        var recepcion = f.PersonalDe(Recepcion);

        Assert.False(f.Internaciones.Admitir(new AdmisionFormulario { AnimalId = 1, Motivo = " ", TelefonoContacto = "099" }, recepcion).Exito);
        Assert.False(f.Internaciones.Admitir(new AdmisionFormulario { AnimalId = 1, Motivo = "Control", TelefonoContacto = "" }, recepcion).Exito);
        Assert.Null(f.Internaciones.AbiertaDe(1));
    }

    [Fact]
    public void Recepcion_anula_una_admision_sin_ingreso_y_con_motivo()
    {
        var (f, internacion) = TobyAdmitido();
        var recepcion = f.PersonalDe(Recepcion);

        Assert.False(f.Internaciones.AnularAdmision(internacion.Id, "", recepcion).Exito);
        Assert.True(f.Internaciones.AnularAdmision(internacion.Id, "El responsable se lo llevó", recepcion).Exito);

        Assert.Equal(InternacionService.Anulada, internacion.Estado);
        Assert.Null(f.Internaciones.AbiertaDe(1));
        Assert.Empty(f.Internaciones.DeAnimal(1, 100));
    }

    [Fact]
    public void No_se_anula_una_internacion_que_ya_ingreso()
    {
        var (f, internacion, _) = TobyInternado();

        Assert.False(f.Internaciones.AnularAdmision(internacion.Id, "Error", f.PersonalDe(Recepcion)).Exito);
        Assert.Equal(InternacionService.Internado, internacion.Estado);
    }

    [Fact]
    public void El_veterinario_asigna_box_y_plan()
    {
        var (f, internacion, _) = TobyInternado();

        Assert.Equal(InternacionService.Internado, internacion.Estado);
        Assert.Equal(1, internacion.BoxId);
        Assert.Equal("Analgesia y control", internacion.PlanTerapeutico);
        Assert.Same(internacion, f.Internaciones.OcupanteDe(1));
    }

    [Fact]
    public void Solo_un_veterinario_hace_el_ingreso_clinico()
    {
        var (f, internacion) = TobyAdmitido();
        var datos = new IngresoClinicoFormulario { BoxId = 1, PlanTerapeutico = "Plan" };

        Assert.False(f.Internaciones.IngresarClinico(internacion.Id, datos, f.PersonalDe(Recepcion)).Exito);
        Assert.False(f.Internaciones.IngresarClinico(internacion.Id, datos, f.PersonalDe(Admin)).Exito);
        Assert.Equal(InternacionService.Admitida, internacion.Estado);
    }

    [Fact]
    public void Un_box_ocupado_no_recibe_otro_paciente()
    {
        var (f, _, vet) = TobyInternado();
        f.Internaciones.Admitir(AdmisionDe(2), f.PersonalDe(Recepcion));
        var mishi = f.Internaciones.AbiertaDe(2)!;

        var resultado = f.Internaciones.IngresarClinico(mishi.Id, new IngresoClinicoFormulario { BoxId = 1, PlanTerapeutico = "Plan" }, vet);

        Assert.False(resultado.Exito);
        Assert.Equal(InternacionService.Admitida, mishi.Estado);
        Assert.DoesNotContain(f.Internaciones.BoxesLibresPara(f.Animales.ObtenerParaPersonal(2)!), b => b.Id == 1);
    }

    [Fact]
    public void Un_perro_no_va_al_box_equino()
    {
        var (f, internacion) = TobyAdmitido();
        // Se libera el box equino dando el alta a Canela.
        var vet = f.PersonalDe(Ferreira);
        f.Internaciones.DarAlta(f.Internaciones.AbiertaDe(4)!.Id, new AltaFormulario { EvolucionFinal = "Bien", IndicacionesDomicilio = "Reposo" }, vet);

        var resultado = f.Internaciones.IngresarClinico(internacion.Id, new IngresoClinicoFormulario { BoxId = 4, PlanTerapeutico = "Plan" }, vet);

        Assert.False(resultado.Exito);
        Assert.Equal(InternacionService.Admitida, internacion.Estado);
    }

    [Fact]
    public void El_veterinario_publica_entradas_visibles_e_internas()
    {
        var (f, internacion, vet) = TobyInternado();

        Assert.True(f.Internaciones.Publicar(internacion.Id, Entrada("Comió bien", visible: true), vet).Exito);
        Assert.True(f.Internaciones.Publicar(internacion.Id, Entrada("FC 90, sin fiebre", visible: false, InternacionService.Evolucion), vet).Exito);

        Assert.Equal(2, f.Internaciones.Novedades(internacion.Id).Count);
        var visible = Assert.Single(f.Internaciones.NovedadesVisibles(internacion.Id));
        Assert.Equal("Comió bien", visible.Texto);
    }

    [Fact]
    public void Solo_un_veterinario_publica_y_solo_en_una_internacion_en_curso()
    {
        var (f, internacion) = TobyAdmitido();

        Assert.False(f.Internaciones.Publicar(internacion.Id, Entrada("Antes del ingreso", true), f.PersonalDe(Ferreira)).Exito);

        var (f2, internado, _) = TobyInternado();
        Assert.False(f2.Internaciones.Publicar(internado.Id, Entrada("Hola", true), f2.PersonalDe(Recepcion)).Exito);
        Assert.False(f2.Internaciones.Publicar(internado.Id, Entrada("  ", true), f2.PersonalDe(Ferreira)).Exito);
        Assert.False(f2.Internaciones.Publicar(internado.Id, Entrada("Texto", true, "Chisme"), f2.PersonalDe(Ferreira)).Exito);
        Assert.Empty(f2.Internaciones.Novedades(internado.Id));
    }

    [Fact]
    public void Rectificar_agrega_una_entrada_nueva_y_conserva_la_original()
    {
        var (f, internacion, vet) = TobyInternado();
        f.Internaciones.Publicar(internacion.Id, Entrada("Temperatura 40", visible: true), vet);
        var original = f.Internaciones.Novedades(internacion.Id).Single();

        var resultado = f.Internaciones.Rectificar(internacion.Id, original.Id, Entrada("Temperatura 38,5; el 40 fue un error", visible: false), vet);

        Assert.True(resultado.Exito, resultado.Mensaje);
        var entradas = f.Internaciones.Novedades(internacion.Id);
        Assert.Equal(2, entradas.Count);
        Assert.Equal("Temperatura 40", original.Texto);
        var rectificacion = entradas.Single(n => n.Tipo == InternacionService.Rectificacion);
        Assert.Equal(original.Id, rectificacion.RectificaId);
        // Misma visibilidad que la original, aunque el formulario diga otra cosa.
        Assert.True(rectificacion.Visible);
    }

    [Fact]
    public void El_alta_exige_evolucion_final_e_indicaciones()
    {
        var (f, internacion, vet) = TobyInternado();

        Assert.False(f.Internaciones.DarAlta(internacion.Id, new AltaFormulario { EvolucionFinal = "", IndicacionesDomicilio = "Reposo" }, vet).Exito);
        Assert.False(f.Internaciones.DarAlta(internacion.Id, new AltaFormulario { EvolucionFinal = "Bien", IndicacionesDomicilio = " " }, vet).Exito);

        Assert.Equal(InternacionService.Internado, internacion.Estado);
        Assert.Same(internacion, f.Internaciones.OcupanteDe(1));
    }

    [Fact]
    public void El_alta_libera_el_box_y_cierra_la_internacion()
    {
        var (f, internacion, vet) = TobyInternado();

        var resultado = f.Internaciones.DarAlta(internacion.Id, new AltaFormulario { EvolucionFinal = "Recuperado", IndicacionesDomicilio = "Reposo 5 días" }, vet);

        Assert.True(resultado.Exito, resultado.Mensaje);
        Assert.Equal(InternacionService.Alta, internacion.Estado);
        Assert.Null(f.Internaciones.OcupanteDe(1));
        Assert.Null(f.Internaciones.AbiertaDe(1));
        Assert.Contains(internacion, f.Internaciones.Altas());
        Assert.False(f.Internaciones.Publicar(internacion.Id, Entrada("Después del alta", true), vet).Exito);
    }

    [Fact]
    public void Despues_del_alta_el_animal_se_puede_volver_a_internar()
    {
        var (f, internacion, vet) = TobyInternado();
        f.Internaciones.DarAlta(internacion.Id, new AltaFormulario { EvolucionFinal = "Bien", IndicacionesDomicilio = "Reposo" }, vet);

        Assert.True(f.Internaciones.Admitir(AdmisionDe(1), f.PersonalDe(Recepcion)).Exito);
    }

    [Fact]
    public void Solo_un_veterinario_da_el_alta()
    {
        var (f, internacion, _) = TobyInternado();
        var datos = new AltaFormulario { EvolucionFinal = "Bien", IndicacionesDomicilio = "Reposo" };

        Assert.False(f.Internaciones.DarAlta(internacion.Id, datos, f.PersonalDe(Recepcion)).Exito);
        Assert.False(f.Internaciones.DarAlta(internacion.Id, datos, f.PersonalDe(Admin)).Exito);
        Assert.Equal(InternacionService.Internado, internacion.Estado);
    }

    [Fact]
    public void El_cliente_ve_solo_las_internaciones_de_sus_animales()
    {
        var f = new Fabrica();

        Assert.Single(f.Internaciones.DeAnimal(4, 101));
        Assert.Empty(f.Internaciones.DeAnimal(4, 100));
    }
}
