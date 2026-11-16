using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Indicadores de gestión de demostración (A-027 parte 2, RF-ADM-07): ocupación de agenda, ingresos por servicio,
// ausencias y rotación de stock. Solo lectura. En la sección C los calcula la API con consultas agregadas.
public class IndicadoresService
{
    private readonly AgendaService _agenda;
    private readonly ProfesionalService _profesionales;
    private readonly CobroService _cobros;
    private readonly InventarioService _inventario;

    public IndicadoresService(AgendaService agenda, ProfesionalService profesionales, CobroService cobros, InventarioService inventario)
    {
        _agenda = agenda;
        _profesionales = profesionales;
        _cobros = cobros;
        _inventario = inventario;
    }

    // Minutos con turnos activos sobre minutos de atención del día, por profesional.
    public List<OcupacionProfesional> Ocupacion(DateOnly fecha)
    {
        return _profesionales.ObtenerPublicos()
            .Select(p =>
            {
                var (ocupados, disponibles) = _agenda.Ocupacion(p.Id, fecha);
                return new OcupacionProfesional
                {
                    Profesional = p.Nombre,
                    MinutosOcupados = ocupados,
                    MinutosDisponibles = disponibles,
                    Porcentaje = disponibles > 0 ? (int)Math.Round(100.0 * ocupados / disponibles) : 0
                };
            })
            .ToList();
    }

    // Lo cobrado (sin comprobantes anulados) por servicio en el período.
    public List<IngresoServicio> IngresosPorServicio(DateOnly desde, DateOnly hasta)
    {
        return _cobros.Pagos()
            .Where(p => !_cobros.EstaAnulado(p.Id) && DateOnly.FromDateTime(p.Fecha) >= desde && DateOnly.FromDateTime(p.Fecha) <= hasta)
            .GroupBy(p => _cobros.ObtenerCargo(p.CargoId)?.Descripcion ?? "Otro")
            .Select(g => new IngresoServicio { Servicio = g.Key, Cobros = g.Count(), Total = g.Sum(p => p.Monto) })
            .OrderByDescending(i => i.Total)
            .ToList();
    }

    public ResumenAusencias Ausencias(DateOnly desde, DateOnly hasta)
    {
        var turnos = _agenda.TurnosEntre(desde, hasta);
        var atendidos = turnos.Count(t => t.Estado == "Finalizado");
        var ausentes = turnos.Count(t => t.Estado == "Ausente");
        return new ResumenAusencias
        {
            Turnos = turnos.Count,
            Atendidos = atendidos,
            Ausentes = ausentes,
            Cancelados = turnos.Count(t => t.Estado == "Cancelado"),
            PorcentajeAusencias = atendidos + ausentes > 0 ? (int)Math.Round(100.0 * ausentes / (atendidos + ausentes)) : 0
        };
    }

    // Consumo en las consultas en los últimos días y para cuántos días alcanza el stock a ese ritmo.
    public List<RotacionArticulo> Rotacion(int dias, DateTime? momento = null)
    {
        var ahora = momento ?? DateTime.Now;
        var desde = ahora.AddDays(-dias);
        return _inventario.Articulos()
            .Select(a =>
            {
                var consumo = -_inventario.Movimientos(a.Id)
                    .Where(m => m.Tipo == InventarioService.Consumo && m.Fecha >= desde && m.Fecha <= ahora)
                    .Sum(m => m.Cantidad);
                var stock = _inventario.Stock(a.Id);
                return new RotacionArticulo
                {
                    Articulo = a.Nombre,
                    Unidad = a.Unidad,
                    Consumo = consumo,
                    Stock = stock,
                    DiasCobertura = consumo > 0 ? (int)Math.Floor(stock / (consumo / (double)dias)) : null
                };
            })
            .OrderByDescending(r => r.Consumo)
            .ToList();
    }
}
