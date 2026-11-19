using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;
using Veterinaria.Api.Data;

namespace Veterinaria.Tests;

// API — modelo de la base (migraciones Inicial y ModulosClinicos). No se conecta a MySQL:
// arma el modelo de EF Core con el proveedor de Oracle y genera el script CREATE, como haría Update-Database.
// Si una relación, un índice o un dato de la semilla está mal, falla acá y no en la PC de quien migra.
public class ApiBaseDeDatosTests
{
    private static AppDbContext Contexto()
    {
        var opciones = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySQL("server=localhost;port=3306;database=vitalia_prueba;user=root;password=sin_uso")
            .Options;
        return new AppDbContext(opciones);
    }

    [Fact]
    public void El_modelo_tiene_las_51_tablas()
    {
        using var db = Contexto();

        var tablas = db.Model.GetEntityTypes().Select(e => e.GetTableName()).Distinct().ToList();

        Assert.Equal(51, tablas.Count);
        foreach (var tabla in new[] { "Consultas", "Enmiendas", "Lotes", "MovimientosStock", "AplicacionesSanitarias", "Cargos", "Pagos", "NotasCredito", "Internaciones", "SolicitudesAsistencia", "Comunicaciones", "Numeradores" })
        {
            Assert.Contains(tabla, tablas);
        }
    }

    [Fact]
    public void Se_genera_el_script_de_MySQL_con_restricciones()
    {
        using var db = Contexto();

        var script = db.Database.GenerateCreateScript();

        Assert.Contains("CREATE TABLE `Consultas`", script);
        Assert.Contains("CK_Lotes_CantidadNoNegativa", script); // RN-26
        Assert.Contains("CK_Pagos_MontoPositivo", script);
        Assert.Contains("IX_Cargos_ConsultaId", script);        // RN-32
        Assert.Contains("IX_Animales_Microchip", script);       // RN-41
    }

    [Fact]
    public void No_hay_borrado_en_cascada()
    {
        using var db = Contexto();

        var cascadas = db.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(f => f.DeleteBehavior == DeleteBehavior.Cascade)
            .Select(f => $"{f.DeclaringEntityType.ClrType.Name}.{string.Join(",", f.Properties.Select(p => p.Name))}")
            .ToList();

        Assert.Empty(cascadas);
    }

    [Fact]
    public void La_contrasena_del_personal_de_la_semilla_es_la_de_demostracion()
    {
        // Mismo algoritmo que Blazor (CuentaService): PBKDF2-SHA256, 100 000 iteraciones.
        Assert.True(Veterinaria.Blazor.Services.CuentaService.ContrasenaCoincide("Vitalia2026!", ConfiguracionModulos.HashContrasenaDemo));
        Assert.False(Veterinaria.Blazor.Services.CuentaService.ContrasenaCoincide("otra", ConfiguracionModulos.HashContrasenaDemo));
    }
}
