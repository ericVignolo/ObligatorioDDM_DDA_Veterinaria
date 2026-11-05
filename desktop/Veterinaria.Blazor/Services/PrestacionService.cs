using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Datos ficticios de la sección A. En la sección C se reemplaza por PrestacionApiService.
public class PrestacionService
{
    private readonly List<Prestacion> _prestaciones = new()
    {
        new() { Id = 1, Nombre = "Consulta clínica general", Descripcion = "Revisión completa: anamnesis, examen físico, diagnóstico y tratamiento. Animales de compañía, equinos y otras especies.", PrecioDesde = 1200, ReservableEnLinea = true, DuracionMinutos = 30 },
        new() { Id = 2, Nombre = "Vacunación", Descripcion = "Aplicación con registro de producto, lote y fecha de refuerzo.", PrecioDesde = 600, ReservableEnLinea = true, DuracionMinutos = 15 },
        new() { Id = 3, Nombre = "Desparasitación", Descripcion = "Tratamiento antiparasitario indicado por el veterinario.", PrecioDesde = 450, ReservableEnLinea = true, DuracionMinutos = 15 },
        new() { Id = 4, Nombre = "Revisión de ovinos y bovinos", Descripcion = "Revisión individual o por grupo. Se coordina con la clínica.", PrecioDesde = 1500, ReservableEnLinea = false },
        new() { Id = 5, Nombre = "Rehabilitación equina", Descripcion = "Fisioterapia para caballos. Requiere evaluación previa.", PrecioDesde = 2500, ReservableEnLinea = false },
        new() { Id = 6, Nombre = "Baño y estética", Descripcion = "Baño, secado, corte y cuidado estético de perros y gatos.", PrecioDesde = 900, ReservableEnLinea = true, DuracionMinutos = 60 },
        new() { Id = 7, Nombre = "Guardería", Descripcion = "No publicado: no debe aparecer.", PrecioDesde = 700, ReservableEnLinea = false, Publicado = false }
    };

    // Catálogo público: solo publicadas y con precio (D-CAT-02, D-CAT-04).
    public List<Prestacion> ObtenerPublicas()
    {
        return _prestaciones
            .Where(p => p.Publicado && p.PrecioDesde > 0)
            .ToList();
    }

    // Servicios que se pueden reservar en línea (A-015).
    public List<Prestacion> ObtenerReservables()
    {
        return ObtenerPublicas()
            .Where(p => p.ReservableEnLinea && p.DuracionMinutos > 0)
            .ToList();
    }

    public Prestacion? ObtenerPorId(int id)
    {
        return ObtenerPublicas().FirstOrDefault(p => p.Id == id);
    }
}