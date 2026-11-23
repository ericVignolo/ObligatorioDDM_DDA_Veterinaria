using Microsoft.Extensions.Options;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — tareas automáticas: qué corre en cada revisión y borrado de cuentas sin verificar (D-REG-02).
public class ApiTareasTests
{
    [Fact]
    public void Frecuentes_siempre_y_diarias_una_vez_por_dia_desde_la_hora_configurada()
    {
        var plan = new PlanTareas(6);
        var dia = new DateTime(2026, 11, 23);

        Assert.Equal(PlanTareas.Frecuentes, plan.QueToca(dia.AddHours(5).AddMinutes(59))); // antes de las 6
        var a_las_6 = plan.QueToca(dia.AddHours(6));
        Assert.Equal(PlanTareas.Frecuentes.Concat(PlanTareas.Diarias), a_las_6);
        Assert.Equal(PlanTareas.Frecuentes, plan.QueToca(dia.AddHours(6).AddMinutes(1))); // ya corrieron hoy
        Assert.Equal(PlanTareas.Frecuentes, plan.QueToca(dia.AddHours(23)));
        Assert.Contains(PlanTareas.AvisosRefuerzo, plan.QueToca(dia.AddDays(1).AddHours(7))); // al día siguiente
    }

    [Fact]
    public void Si_la_API_arranca_tarde_las_diarias_corren_en_la_primera_revision()
    {
        var plan = new PlanTareas(6);
        Assert.Contains(PlanTareas.LotesVencidos, plan.QueToca(new DateTime(2026, 11, 23, 15, 30, 0)));
    }

    [Fact]
    public void Opciones_por_defecto_activas_cada_minuto_y_a_las_6()
    {
        var o = new OpcionesTareas();
        Assert.Equal((true, 60, 6), (o.Activas, o.SegundosEntreRevisiones, o.HoraTareasDiarias));
    }
}
