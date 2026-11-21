using System.Text.RegularExpressions;

namespace Veterinaria.Tests;

// A-029: revisión estática de accesibilidad y adaptación de escritorio (RNF-USA-02 a RNF-USA-06, WCAG 2.1 AA).
// Lee los .razor y el CSS del proyecto de escritorio. No reemplaza una prueba con teclado ni con lector de pantalla:
// evita que se pierdan las condiciones básicas ya corregidas.
public class AccesibilidadTests
{
    private static string RaizDeEscritorio()
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

        throw new DirectoryNotFoundException("No se encontró desktop/Veterinaria.Blazor desde " + AppContext.BaseDirectory);
    }

    private static List<(string Archivo, string[] Lineas)> Vistas()
    {
        var raiz = RaizDeEscritorio();
        return Directory.GetFiles(Path.Combine(raiz, "Components"), "*.razor", SearchOption.AllDirectories)
            .Select(f => (Path.GetRelativePath(raiz, f).Replace('\\', '/'), File.ReadAllLines(f)))
            .ToList();
    }

    private static string Leer(string relativo)
    {
        return File.ReadAllText(Path.Combine(RaizDeEscritorio(), relativo));
    }

    private static string Contexto(string[] lineas, int indice, int antes, int despues)
    {
        var desde = Math.Max(0, indice - antes);
        var hasta = Math.Min(lineas.Length - 1, indice + despues);
        return string.Join("\n", lineas[desde..(hasta + 1)]);
    }

    [Fact]
    public void Hay_vistas_para_revisar()
    {
        Assert.True(Vistas().Count > 50);
    }

    [Fact]
    public void Toda_imagen_tiene_alternativa_textual()
    {
        var faltan = new List<string>();
        foreach (var (archivo, lineas) in Vistas())
        {
            for (var i = 0; i < lineas.Length; i++)
            {
                if (Regex.IsMatch(lineas[i], @"<img\b") && !lineas[i].Contains("alt="))
                {
                    faltan.Add($"{archivo}:{i + 1}");
                }
            }
        }

        Assert.Empty(faltan);
    }

    [Fact]
    public void Toda_tabla_es_responsiva_y_tiene_titulo()
    {
        var faltan = new List<string>();
        foreach (var (archivo, lineas) in Vistas())
        {
            for (var i = 0; i < lineas.Length; i++)
            {
                if (!Regex.IsMatch(lineas[i], @"<table\b"))
                {
                    continue;
                }

                if (!Contexto(lineas, i, 3, 0).Contains("table-responsive"))
                {
                    faltan.Add($"{archivo}:{i + 1} sin contenedor table-responsive");
                }

                if (!Contexto(lineas, i, 0, 2).Contains("<caption") && !lineas[i].Contains("aria-label"))
                {
                    faltan.Add($"{archivo}:{i + 1} sin caption");
                }
            }
        }

        Assert.Empty(faltan);
    }

    [Fact]
    public void Los_enlaces_que_abren_otra_pestana_llevan_rel()
    {
        var faltan = new List<string>();
        foreach (var (archivo, lineas) in Vistas())
        {
            for (var i = 0; i < lineas.Length; i++)
            {
                if (lineas[i].Contains("target=\"_blank\"") && !lineas[i].Contains("noopener"))
                {
                    faltan.Add($"{archivo}:{i + 1}");
                }
            }
        }

        Assert.Empty(faltan);
    }

    [Fact]
    public void Todo_campo_tiene_etiqueta_o_nombre_accesible()
    {
        var faltan = new List<string>();
        foreach (var (archivo, lineas) in Vistas())
        {
            for (var i = 0; i < lineas.Length; i++)
            {
                var linea = lineas[i];
                if (!Regex.IsMatch(linea, @"<(input|select|textarea)\b") || linea.Contains("type=\"hidden\""))
                {
                    continue;
                }

                var conId = Regex.IsMatch(linea, @"\bid=""");
                var conNombre = linea.Contains("aria-label") || linea.Contains("aria-labelledby");
                var dentroDeLabel = Contexto(lineas, i, 3, 0).Contains("<label");
                if (!conId && !conNombre && !dentroDeLabel)
                {
                    faltan.Add($"{archivo}:{i + 1}");
                }
            }
        }

        Assert.Empty(faltan);
    }

    [Fact]
    public void Toda_etiqueta_con_for_apunta_a_un_campo_de_la_misma_vista()
    {
        var huerfanas = new List<string>();
        foreach (var (archivo, lineas) in Vistas())
        {
            var texto = string.Join("\n", lineas);
            foreach (Match m in Regex.Matches(texto, @"<label[^>]*\bfor=""([^""@]+)"""))
            {
                var id = m.Groups[1].Value;
                if (!Regex.IsMatch(texto, @"\bid=""" + Regex.Escape(id) + "\""))
                {
                    huerfanas.Add($"{archivo}: for=\"{id}\"");
                }
            }
        }

        Assert.Empty(huerfanas);
    }

    [Fact]
    public void El_layout_tiene_salto_al_contenido_y_puntos_de_referencia()
    {
        var layout = Leer("Components/Layout/MainLayout.razor");
        var menu = Leer("Components/Layout/NavMenu.razor");

        Assert.Contains("class=\"skip-link\"", layout);
        Assert.Contains("<main id=\"contenido-principal\"", layout);
        Assert.Contains("<header", layout);
        Assert.Contains("aria-label=\"Navegación principal\"", menu);
        Assert.Contains("aria-label=\"Abrir o cerrar el menú de navegación\"", menu);
        // El aviso de error es un botón con nombre, no un texto suelto.
        Assert.Contains("<button type=\"button\" class=\"dismiss\" aria-label=", layout);
        Assert.Contains("lang=\"es-UY\"", Leer("Components/App.razor"));
    }

    [Fact]
    public void Los_estilos_garantizan_foco_visible_movimiento_reducido_y_pantallas_angostas()
    {
        var css = Leer("wwwroot/app.css");

        Assert.Contains(":focus-visible", css);
        Assert.Contains("prefers-reduced-motion", css);
        Assert.Contains("max-width: 640.98px", css);
        Assert.Contains("min-height: 44px", css);
        Assert.Contains(".skip-link", css);
    }

    [Fact]
    public void Los_textos_de_sistema_estan_en_espanol_rioplatense()
    {
        var css = Leer("wwwroot/app.css");
        var reconexion = Leer("Components/Layout/ReconnectModal.razor");

        Assert.DoesNotContain("An error has occurred", css);
        foreach (var ingles in new[] { "Rejoin", "Retry", "Resume", "Failed to", "paused by the server" })
        {
            Assert.DoesNotContain(ingles, reconexion);
        }

        Assert.Contains("Reintentá", reconexion);
    }

    [Fact]
    public void Los_mensajes_de_resultado_de_una_accion_se_anuncian_a_los_lectores_de_pantalla()
    {
        // Los mensajes que aparecen después de pulsar un botón llevan role="status" o role="alert".
        var sinRol = new List<string>();
        foreach (var (archivo, lineas) in Vistas())
        {
            for (var i = 0; i < lineas.Length; i++)
            {
                if (!Regex.IsMatch(lineas[i], @"IsNullOrEmpty\((mensaje|error|resultado)\w*\)|IsNullOrWhiteSpace\((mensaje|error)\w*\)"))
                {
                    continue;
                }

                var siguientes = Contexto(lineas, i, 0, 4);
                if (siguientes.Contains("class=\"alert") && !siguientes.Contains("role="))
                {
                    sinRol.Add($"{archivo}:{i + 1}");
                }
            }
        }

        Assert.Empty(sinRol);
    }
}
