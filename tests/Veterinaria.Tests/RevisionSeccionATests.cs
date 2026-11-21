using System.Text.RegularExpressions;

namespace Veterinaria.Tests;

// A-031: comprobaciones estáticas que dejan por escrito los hallazgos de la revisión de la sección A
// (docs/analisis/REVISION_SECCION_A.md). Leen los .razor; no reemplazan la prueba con F5.
public class RevisionSeccionATests
{
    private static string Raiz()
    {
        var carpeta = new DirectoryInfo(AppContext.BaseDirectory);
        while (carpeta is not null)
        {
            var candidata = Path.Combine(carpeta.FullName, "desktop", "Veterinaria.Blazor");
            if (Directory.Exists(Path.Combine(candidata, "Components")))
            {
                return candidata;
            }

            carpeta = carpeta.Parent;
        }

        throw new DirectoryNotFoundException("No se encontró desktop/Veterinaria.Blazor");
    }

    private static string Leer(string relativo) => File.ReadAllText(Path.Combine(Raiz(), relativo));

    private static List<string> PatronesDePagina() =>
        Directory.GetFiles(Path.Combine(Raiz(), "Components"), "*.razor", SearchOption.AllDirectories)
            .SelectMany(File.ReadAllLines)
            .Select(l => Regex.Match(l, @"^@page ""/([^""]*)"""))
            .Where(m => m.Success)
            .Select(m => "/" + m.Groups[1].Value)
            .ToList();

    private static bool Coincide(string ruta, List<string> patrones)
    {
        return patrones.Any(p => Regex.IsMatch(ruta, "^" + Regex.Replace(Regex.Escape(p), @"\\\{[^}]+\}", "[^/]+") + "$", RegexOptions.IgnoreCase));
    }

    [Fact]
    public void Todo_enlace_interno_literal_apunta_a_una_pantalla_existente()
    {
        var patrones = PatronesDePagina();
        var rotos = new List<string>();

        foreach (var archivo in Directory.GetFiles(Path.Combine(Raiz(), "Components"), "*.razor", SearchOption.AllDirectories))
        {
            var texto = File.ReadAllText(archivo);
            var literales = Regex.Matches(texto, @"\bhref=""([^""@#]*)""").Select(m => m.Groups[1].Value)
                .Concat(Regex.Matches(texto, @"NavigateTo\(""([^""]*)""").Select(m => m.Groups[1].Value));

            foreach (var destino in literales)
            {
                if (destino.StartsWith("http") || destino.StartsWith("mailto:") || destino.StartsWith("tel:") || destino.Contains('.') && !destino.Contains('/') && destino != ".")
                {
                    continue;
                }

                var ruta = "/" + destino.TrimStart('/').Split('?')[0].TrimEnd('/');
                if (destino == "." || ruta == "/.")
                {
                    ruta = "/";
                }

                if (!Coincide(ruta, patrones))
                {
                    rotos.Add($"{Path.GetFileName(archivo)}: «{destino}»");
                }
            }
        }

        Assert.Empty(rotos);
    }

    [Fact]
    public void Las_paginas_de_error_estan_en_espanol()
    {
        foreach (var archivo in new[] { "Components/Pages/NotFound.razor", "Components/Pages/Error.razor" })
        {
            var texto = Leer(archivo);
            foreach (var ingles in new[] { "Sorry", "Not Found", "An error occurred", "Request ID", "Development" })
            {
                Assert.DoesNotContain(ingles, texto);
            }
        }
    }

    [Fact]
    public void El_motivo_de_una_ausencia_no_se_muestra_al_cliente_ni_al_publico()
    {
        Assert.DoesNotContain("Motivo", Leer("Components/Pages/ProfesionalDetalle.razor"));
        Assert.DoesNotContain("ausencia.Motivo", Leer("Components/Pages/Reservar.razor"));
    }

    [Fact]
    public void La_bandeja_de_comunicaciones_no_muestra_el_contenido_del_resumen_clinico()
    {
        var bandeja = Leer("Components/Pages/Comunicaciones.razor");

        Assert.Contains("comunicacion.Tipo == ComunicacionesService.ResumenClinico", bandeja);
    }
}
