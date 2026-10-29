using Veterinaria.Contracts.Catalogo;
using Veterinaria.Contracts.Comun;

namespace Veterinaria.Blazor.Datos.Demo;

/// <summary>
/// Catálogo con datos ficticios para el prototipo de la sección A.
/// Reproduce las respuestas que dará la API (filtro de publicación, vigencia
/// e importe «Desde»; paginación) para validar la pantalla. No es la
/// implementación real de esas reglas: en la sección C las aplica la API
/// (ver docs/contexto-base/CONTEXTO_BASE.md, sección 3.1).
/// </summary>
public sealed class CatalogoServiciosDemo(TimeProvider reloj) : ICatalogoServicios
{
    private const string Moneda = "UYU";

    public Task<PaginaDto<ServicioPublicoDto>> ListarAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (page < 1)
            throw new ArgumentOutOfRangeException(nameof(page), "La página empieza en 1.");
        if (pageSize < 1 || pageSize > PaginaDto<ServicioPublicoDto>.PageSizeMaximo)
            throw new ArgumentOutOfRangeException(nameof(pageSize), $"El tamaño de página va de 1 a {PaginaDto<ServicioPublicoDto>.PageSizeMaximo}.");

        var publicos = Publicos().ToList();
        var items = publicos.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(new PaginaDto<ServicioPublicoDto>(items, page, pageSize, publicos.Count));
    }

    public Task<ServicioPublicoDto?> ObtenerAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Publicos().FirstOrDefault(s => s.Id == id));

    /// <summary>D-CAT-02 y D-CAT-04: publicado, vigente hoy y con al menos una tarifa vigente.</summary>
    private IEnumerable<ServicioPublicoDto> Publicos()
    {
        var hoy = DateOnly.FromDateTime(reloj.GetLocalNow().DateTime);
        return Servicios
            .Where(s => s.Publicado && s.VigenteDesde <= hoy && (s.VigenteHasta is null || s.VigenteHasta >= hoy))
            .Select(s => (Servicio: s, Tarifas: s.Tarifas.Where(t => t.VigenteDesde <= hoy).ToList()))
            .Where(x => x.Tarifas.Count > 0)
            .OrderBy(x => x.Servicio.Orden)
            .Select(x => new ServicioPublicoDto(
                x.Servicio.Id,
                x.Servicio.Nombre,
                x.Servicio.Descripcion,
                x.Servicio.ReservableEnLinea,
                new PrecioReferenciaDto(x.Tarifas.Min(t => t.Importe), Moneda)));
    }

    private sealed record TarifaDemo(string Variante, decimal Importe, DateOnly VigenteDesde);

    private sealed record ServicioDemo(
        Guid Id,
        int Orden,
        string Nombre,
        string Descripcion,
        bool Publicado,
        bool ReservableEnLinea,
        DateOnly VigenteDesde,
        DateOnly? VigenteHasta,
        IReadOnlyList<TarifaDemo> Tarifas);

    private static readonly DateOnly Inicio = new(2026, 1, 1);

    // Datos ficticios: nombres, descripciones e importes no son tarifas ni protocolos reales.
    private static readonly IReadOnlyList<ServicioDemo> Servicios =
    [
        new(Guid.Parse("4b2f1c3e-0001-4a1b-9c1d-000000000001"), 1,
            "Consulta clínica general",
            "Revisión completa del animal en la clínica: anamnesis, examen físico, diagnóstico y plan de tratamiento. Para animales de compañía, equinos y otras especies.",
            Publicado: true, ReservableEnLinea: true, Inicio, null,
            [new("Animales de compañía", 1200m, Inicio), new("Equinos", 1800m, Inicio)]),

        new(Guid.Parse("4b2f1c3e-0002-4a1b-9c1d-000000000002"), 2,
            "Vacunación",
            "Aplicación de vacunas con registro de producto, lote y fecha de refuerzo. El plan lo define el veterinario según especie y edad.",
            Publicado: true, ReservableEnLinea: true, Inicio, null,
            [new("Por dosis", 600m, Inicio)]),

        new(Guid.Parse("4b2f1c3e-0003-4a1b-9c1d-000000000003"), 3,
            "Desparasitación",
            "Tratamiento antiparasitario indicado por el veterinario, separado de la vacunación, con producto e indicaciones registrados.",
            Publicado: true, ReservableEnLinea: true, Inicio, null,
            [new("Por animal", 450m, Inicio)]),

        new(Guid.Parse("4b2f1c3e-0004-4a1b-9c1d-000000000004"), 4,
            "Revisión de ovinos y bovinos",
            "Revisión clínica de animales de producción, individual o por grupo. Se coordina con la clínica según cantidad de animales y lugar.",
            Publicado: true, ReservableEnLinea: false, Inicio, null,
            [new("Individual", 1500m, Inicio), new("Por grupo", 4000m, Inicio)]),

        new(Guid.Parse("4b2f1c3e-0005-4a1b-9c1d-000000000005"), 5,
            "Rehabilitación equina",
            "Fisioterapia y rehabilitación para caballos, con un profesional con formación específica. Requiere evaluación previa.",
            Publicado: true, ReservableEnLinea: false, Inicio, null,
            [new("Sesión", 2500m, Inicio)]),

        // Casos que NO deben aparecer: sirven para comprobar CAT-02 y CAT-02A.
        new(Guid.Parse("4b2f1c3e-0006-4a1b-9c1d-000000000006"), 6,
            "Cirugía programada (sin tarifa publicable)",
            "Oculto por no tener importe publicable (D-CAT-04).",
            Publicado: true, ReservableEnLinea: false, Inicio, null,
            []),

        new(Guid.Parse("4b2f1c3e-0007-4a1b-9c1d-000000000007"), 7,
            "Estética (no publicado)",
            "Oculto por no estar publicado (D-CAT-02).",
            Publicado: false, ReservableEnLinea: true, Inicio, null,
            [new("Baño", 900m, Inicio)]),

        new(Guid.Parse("4b2f1c3e-0008-4a1b-9c1d-000000000008"), 8,
            "Ecografía (vigencia vencida)",
            "Oculto por estar fuera de vigencia (D-CAT-02).",
            Publicado: true, ReservableEnLinea: true, Inicio, new DateOnly(2026, 3, 31),
            [new("Estudio", 2200m, Inicio)]),
    ];
}
