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
        new() { Id = 8, Nombre = "Castración", Descripcion = "Cirugía programada con anestesia general. Requiere firmar el consentimiento informado antes del día del turno.", PrecioDesde = 4500, ReservableEnLinea = true, DuracionMinutos = 90, RequiereConsentimiento = true },
        new() { Id = 7, Nombre = "Guardería", Descripcion = "No publicado: no debe aparecer.", PrecioDesde = 700, ReservableEnLinea = false, Publicado = false }
    };

    // Historial de precios (A-027, RF-ADM-02). Cada servicio arranca con su precio de siempre.
    private readonly List<TarifaPrestacion> _tarifas = new();

    public PrestacionService()
    {
        foreach (var prestacion in _prestaciones)
        {
            _tarifas.Add(new TarifaPrestacion { PrestacionId = prestacion.Id, Precio = prestacion.PrecioDesde, VigenteDesde = DateOnly.MinValue, RegistradoPor = "Inicial" });
        }
    }

    // RN-33: precio vigente en una fecha.
    public decimal PrecioVigente(int prestacionId, DateOnly fecha)
    {
        return _tarifas
            .Where(t => t.PrestacionId == prestacionId && t.VigenteDesde <= fecha)
            .OrderByDescending(t => t.VigenteDesde)
            .Select(t => t.Precio)
            .FirstOrDefault();
    }

    public List<TarifaPrestacion> TarifasDe(int prestacionId)
    {
        return _tarifas.Where(t => t.PrestacionId == prestacionId).OrderByDescending(t => t.VigenteDesde).ToList();
    }

    // El precio que se muestra es el vigente hoy.
    private void AplicarVigentes()
    {
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        foreach (var prestacion in _prestaciones)
        {
            prestacion.PrecioDesde = PrecioVigente(prestacion.Id, hoy);
        }
    }

    // Para el Administrador: todas, publicadas o no.
    public List<Prestacion> ObtenerTodas()
    {
        AplicarVigentes();
        return _prestaciones.OrderBy(p => p.Id).ToList();
    }

    // Cualquier servicio, publicado o no (para cargos y consentimientos de turnos ya reservados).
    public Prestacion? ObtenerFicha(int id)
    {
        AplicarVigentes();
        return _prestaciones.FirstOrDefault(p => p.Id == id);
    }

    // Alta y tarifas: las validaciones y el permiso los aplica AdministracionService.
    public Prestacion Agregar(PrestacionFormulario datos, string autor)
    {
        var prestacion = new Prestacion { Id = _prestaciones.Max(p => p.Id) + 1 };
        CopiarDatos(datos, prestacion);
        prestacion.PrecioDesde = datos.Precio;
        _prestaciones.Add(prestacion);
        _tarifas.Add(new TarifaPrestacion { PrestacionId = prestacion.Id, Precio = datos.Precio, VigenteDesde = DateOnly.MinValue, RegistradoPor = autor });
        return prestacion;
    }

    public static void CopiarDatos(PrestacionFormulario datos, Prestacion prestacion)
    {
        prestacion.Nombre = datos.Nombre.Trim();
        prestacion.Descripcion = datos.Descripcion.Trim();
        prestacion.DuracionMinutos = datos.DuracionMinutos;
        prestacion.ReservableEnLinea = datos.ReservableEnLinea;
        prestacion.Publicado = datos.Publicado;
    }

    // Una tarifa nueva para la misma fecha reemplaza a la anterior de esa fecha.
    public void ProgramarTarifa(int prestacionId, decimal precio, DateOnly desde, string autor)
    {
        _tarifas.RemoveAll(t => t.PrestacionId == prestacionId && t.VigenteDesde == desde);
        _tarifas.Add(new TarifaPrestacion { PrestacionId = prestacionId, Precio = precio, VigenteDesde = desde, RegistradoPor = autor });
    }

    // Catálogo público: solo publicadas y con precio (D-CAT-02, D-CAT-04).
    public List<Prestacion> ObtenerPublicas()
    {
        AplicarVigentes();
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