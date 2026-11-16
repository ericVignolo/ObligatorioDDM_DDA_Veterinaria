using System.ComponentModel.DataAnnotations;
using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Inventario de demostración en memoria (A-022, RF-ADM-05, RF-ADM-06, RF-VET-06).
// Solo material de trabajo de la clínica: no hay venta de productos ni comercio electrónico (DT-ALC-01).
// Reglas: stock nunca negativo (RN-26), FEFO (RN-27), alerta al cruzar el mínimo (RN-28),
// lote vencido inhabilitado y dado de baja (RN-29), todo movimiento con origen (RN-30)
// y ajuste solo del Administrador con motivo (RN-31).
// En la sección C lo reemplaza InventarioApiService: la API descuenta el stock con una actualización
// condicional dentro de la misma transacción que firma la consulta (RNF-CON-03) y la baja por vencimiento
// la hace una tarea programada. Acá los saldos viven solo mientras la aplicación está abierta.
public class InventarioService
{
    public const string Compra = "Compra";
    public const string Consumo = "Consumo";
    public const string Ajuste = "Ajuste";
    public const string Baja = "Baja";

    public const string AvisoDemo = "Demostración: el stock y los movimientos viven solo mientras la aplicación está abierta. En la versión final los guarda la API.";

    // Días con los que un lote cuenta como «próximo a vencer» en el tablero (RF-ADM-06).
    public int DiasProximoAVencer { get; set; } = 30;

    private readonly List<Articulo> _articulos = new();
    private readonly List<LoteProducto> _lotes = new();
    private readonly List<MovimientoStock> _movimientos = new();
    private readonly List<AlertaStock> _alertas = new();
    private readonly List<ReservaStock> _reservas = new();
    private int _ultimoLote;
    private int _ultimoMovimiento;
    private int _ultimaAlerta;
    private int _ultimaReserva;

    public InventarioService()
    {
        CargarDemo();
        ProcesarVencidos();
    }

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);

    // Ids 1 a 7: los productos sanitarios del catálogo de vacunas (VacunaService). Del 8 en adelante, insumos.
    private void CargarDemo()
    {
        _articulos.AddRange(new List<Articulo>
        {
            new() { Id = 1, Nombre = "Vacuna polivalente canina", Tipo = "Vacuna", Unidad = "dosis", StockMinimo = 5 },
            new() { Id = 2, Nombre = "Vacuna antirrábica", Tipo = "Vacuna", Unidad = "dosis", StockMinimo = 5 },
            new() { Id = 3, Nombre = "Vacuna triple felina", Tipo = "Vacuna", Unidad = "dosis", StockMinimo = 4 },
            new() { Id = 4, Nombre = "Vacuna antitetánica equina", Tipo = "Vacuna", Unidad = "dosis", StockMinimo = 3 },
            new() { Id = 5, Nombre = "Vacuna clostridial", Tipo = "Vacuna", Unidad = "dosis", StockMinimo = 20 },
            new() { Id = 6, Nombre = "Antiparasitario interno", Tipo = "Antiparasitario", Unidad = "dosis", StockMinimo = 5 },
            new() { Id = 7, Nombre = "Antiparasitario externo", Tipo = "Antiparasitario", Unidad = "dosis", StockMinimo = 5 },
            new() { Id = 8, Nombre = "Jeringa descartable 3 ml", Tipo = "Insumo", Unidad = "unidad", StockMinimo = 20 },
            new() { Id = 9, Nombre = "Guantes de examen", Tipo = "Insumo", Unidad = "par", StockMinimo = 10 },
            new() { Id = 10, Nombre = "Gasa estéril", Tipo = "Insumo", Unidad = "sobre", StockMinimo = 10 },
            new() { Id = 11, Nombre = "Suero fisiológico 500 ml", Tipo = "Insumo", Unidad = "unidad", StockMinimo = 4 },
            new() { Id = 12, Nombre = "Venda elástica", Tipo = "Insumo", Unidad = "unidad", StockMinimo = 5 }
        });

        // Fechas relativas a hoy para que la demostración no caduque. Cada vacuna tiene un lote que vence
        // pronto (FEFO lo usa primero), uno lejano y uno vencido (se da de baja solo al arrancar, RN-29).
        foreach (var articulo in _articulos.Where(a => a.Tipo != "Insumo"))
        {
            AgregarLoteDemo(articulo.Id, $"L{articulo.Id}-A", Hoy.AddDays(15), 3);
            AgregarLoteDemo(articulo.Id, $"L{articulo.Id}-B", Hoy.AddMonths(10), 10);
            AgregarLoteDemo(articulo.Id, $"L{articulo.Id}-VENC", Hoy.AddDays(-20), 2);
        }

        // La antirrábica arranca por debajo del mínimo y el antiparasitario externo justo encima,
        // para ver el tablero y la alerta al consumir.
        _lotes.First(l => l.Numero == "L2-B").Cantidad = 1;
        _lotes.First(l => l.Numero == "L7-B").Cantidad = 3;

        AgregarLoteDemo(8, "JER-2611", Hoy.AddYears(3), 120);
        AgregarLoteDemo(9, "GUA-2610", Hoy.AddYears(2), 40);
        AgregarLoteDemo(10, "GAS-2609", Hoy.AddDays(25), 8);
        AgregarLoteDemo(10, "GAS-2611", Hoy.AddYears(1), 30);
        AgregarLoteDemo(11, "SUE-2608", Hoy.AddMonths(8), 6);
        AgregarLoteDemo(12, "VEN-2605", Hoy.AddYears(2), 15);
    }

    private void AgregarLoteDemo(int articuloId, string numero, DateOnly vence, int cantidad)
    {
        var lote = new LoteProducto
        {
            Id = ++_ultimoLote,
            ArticuloId = articuloId,
            Numero = numero,
            Vencimiento = vence,
            Cantidad = cantidad,
            FechaIngreso = DateTime.Today.AddDays(-40)
        };
        _lotes.Add(lote);
        Registrar(lote, Compra, cantidad, "Carga inicial de demostración", "", null);
    }

    // ---------- Consultas ----------

    public List<Articulo> Articulos()
    {
        return _articulos.OrderBy(a => a.Tipo).ThenBy(a => a.Nombre).ToList();
    }

    public List<Articulo> Insumos()
    {
        return _articulos.Where(a => a.Tipo == "Insumo").OrderBy(a => a.Nombre).ToList();
    }

    public Articulo? ObtenerArticulo(int id)
    {
        return _articulos.FirstOrDefault(a => a.Id == id);
    }

    public LoteProducto? ObtenerLote(int id)
    {
        return _lotes.FirstOrDefault(l => l.Id == id);
    }

    // Todos los lotes del artículo, del que vence antes al que vence después.
    public List<LoteProducto> LotesDe(int articuloId)
    {
        ProcesarVencidos();
        return _lotes.Where(l => l.ArticuloId == articuloId).OrderBy(l => l.Vencimiento).ThenBy(l => l.Id).ToList();
    }

    // RN-15 y RN-29: un lote vencido no se usa. El día de vencimiento todavía sirve.
    public static bool Vencido(LoteProducto lote)
    {
        return lote.Vencimiento < Hoy;
    }

    public static bool Usable(LoteProducto lote)
    {
        return !lote.Inhabilitado && !Vencido(lote);
    }

    // Unidades apartadas por consultas que todavía no se firmaron.
    public int Reservado(LoteProducto lote)
    {
        return _reservas.Where(r => r.LoteId == lote.Id && !r.Confirmada).Sum(r => r.Cantidad);
    }

    public int Disponible(LoteProducto lote)
    {
        return Usable(lote) ? Math.Max(0, lote.Cantidad - Reservado(lote)) : 0;
    }

    // Stock físico: lo que hay en los lotes habilitados (incluye lo apartado).
    public int Stock(int articuloId)
    {
        ProcesarVencidos();
        return _lotes.Where(l => l.ArticuloId == articuloId && Usable(l)).Sum(l => l.Cantidad);
    }

    // Lo que todavía se puede usar en una consulta nueva.
    public int StockDisponible(int articuloId)
    {
        ProcesarVencidos();
        return _lotes.Where(l => l.ArticuloId == articuloId).Sum(l => Disponible(l));
    }

    // RN-27: lotes y cantidades que se usarían para consumir esa cantidad, primero el que vence antes.
    // null si no alcanza (RN-26).
    public List<(LoteProducto Lote, int Cantidad)>? PlanFefo(int articuloId, int cantidad)
    {
        ProcesarVencidos();
        var plan = new List<(LoteProducto, int)>();
        var falta = cantidad;
        foreach (var lote in _lotes.Where(l => l.ArticuloId == articuloId && Disponible(l) > 0)
                                   .OrderBy(l => l.Vencimiento).ThenBy(l => l.Id))
        {
            var toma = Math.Min(falta, Disponible(lote));
            plan.Add((lote, toma));
            falta -= toma;
            if (falta == 0)
            {
                return plan;
            }
        }

        return null;
    }

    public List<ReservaStock> ReservasDe(int consultaId)
    {
        return _reservas.Where(r => r.ConsultaId == consultaId).OrderBy(r => r.Id).ToList();
    }

    public List<MovimientoStock> Movimientos(int articuloId = 0)
    {
        ProcesarVencidos();
        return _movimientos
            .Where(m => articuloId == 0 || m.ArticuloId == articuloId)
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .ToList();
    }

    public List<AlertaStock> Alertas()
    {
        ProcesarVencidos();
        return _alertas.OrderByDescending(a => a.Fecha).ThenByDescending(a => a.Id).ToList();
    }

    // Tablero (RF-ADM-06): artículos por debajo del mínimo.
    public List<Articulo> BajoMinimo()
    {
        return Articulos().Where(a => Stock(a.Id) < a.StockMinimo).ToList();
    }

    // Tablero (RF-ADM-06): lotes con unidades que vencen dentro de DiasProximoAVencer.
    public List<LoteProducto> ProximosAVencer()
    {
        ProcesarVencidos();
        var limite = Hoy.AddDays(DiasProximoAVencer);
        return _lotes.Where(l => Usable(l) && l.Cantidad > 0 && l.Vencimiento <= limite)
                     .OrderBy(l => l.Vencimiento)
                     .ToList();
    }

    // ---------- Reglas automáticas ----------

    // RN-29: cada vez que se mira el inventario, los lotes vencidos con unidades se inhabilitan
    // y se dan de baja con un movimiento del sistema. En la sección C lo hace una tarea programada.
    public void ProcesarVencidos()
    {
        foreach (var lote in _lotes.Where(l => !l.Inhabilitado && Vencido(l)).ToList())
        {
            // El lote ya no cuenta como usable por la fecha: el stock de antes lo suma a mano.
            var antes = StockSinProcesar(lote.ArticuloId) + lote.Cantidad;
            lote.Inhabilitado = true;
            if (lote.Cantidad > 0)
            {
                var cantidad = lote.Cantidad;
                lote.Cantidad = 0;
                Registrar(lote, Baja, -cantidad, "Vencimiento automático", $"El lote venció el {lote.Vencimiento:dd/MM/yyyy}.", null);
            }

            RevisarMinimo(lote.ArticuloId, antes, "Baja por vencimiento");
        }
    }

    private int StockSinProcesar(int articuloId)
    {
        return _lotes.Where(l => l.ArticuloId == articuloId && !l.Inhabilitado && !Vencido(l)).Sum(l => l.Cantidad);
    }

    // RN-28: la alerta se genera en el mismo paso que el movimiento, cuando el stock pasa
    // de estar en el mínimo o por encima a quedar por debajo.
    private void RevisarMinimo(int articuloId, int stockAntes, string origen)
    {
        var articulo = ObtenerArticulo(articuloId);
        var despues = StockSinProcesar(articuloId);
        if (articulo is null || stockAntes < articulo.StockMinimo || despues >= articulo.StockMinimo)
        {
            return;
        }

        _alertas.Add(new AlertaStock
        {
            Id = ++_ultimaAlerta,
            ArticuloId = articulo.Id,
            Articulo = articulo.Nombre,
            StockQueQuedo = despues,
            StockMinimo = articulo.StockMinimo,
            Origen = origen,
            Fecha = DateTime.Now
        });
    }

    // RN-30: todo cambio de cantidad deja un movimiento con su origen.
    private void Registrar(LoteProducto lote, string tipo, int cantidad, string origen, string motivo, Personal? persona)
    {
        _movimientos.Add(new MovimientoStock
        {
            Id = ++_ultimoMovimiento,
            ArticuloId = lote.ArticuloId,
            LoteId = lote.Id,
            Tipo = tipo,
            Cantidad = cantidad,
            Origen = origen,
            Motivo = motivo,
            Fecha = DateTime.Now,
            PersonaId = persona?.Id ?? 0,
            PersonaNombre = persona?.Nombre ?? "Sistema"
        });
    }

    private static bool EsAdministrador(Personal? persona)
    {
        return persona is not null && persona.Rol == PersonalService.Administrador;
    }

    private static bool Valido(object datos)
    {
        return Validator.TryValidateObject(datos, new ValidationContext(datos), new List<ValidationResult>(), validateAllProperties: true);
    }

    // ---------- Acciones del Administrador (RF-ADM-05) ----------

    public ResultadoOperacion IngresarCompra(IngresoLoteFormulario datos, Personal? persona)
    {
        if (!EsAdministrador(persona))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo el Administrador registra compras." };
        }

        if (!Valido(datos) || datos.Vencimiento is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Corregí los campos marcados." };
        }

        var articulo = ObtenerArticulo(datos.ArticuloId);
        if (articulo is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Elegí un artículo." };
        }

        if (datos.Vencimiento.Value <= Hoy)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "No se ingresa un lote vencido o que vence hoy." };
        }

        var numero = datos.Numero.Trim().ToUpperInvariant();
        if (_lotes.Any(l => l.ArticuloId == articulo.Id && l.Numero.ToUpperInvariant() == numero))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"Ya existe el lote {numero} de {articulo.Nombre}. Para corregir su cantidad usá un ajuste." };
        }

        var lote = new LoteProducto
        {
            Id = ++_ultimoLote,
            ArticuloId = articulo.Id,
            Numero = numero,
            Vencimiento = datos.Vencimiento.Value,
            Cantidad = datos.Cantidad,
            FechaIngreso = DateTime.Now
        };
        _lotes.Add(lote);
        Registrar(lote, Compra, datos.Cantidad, Compra, "", persona);
        return new ResultadoOperacion { Exito = true, Mensaje = $"Ingresaron {datos.Cantidad} {articulo.Unidad} de {articulo.Nombre} (lote {numero})." };
    }

    // RN-31: ajuste manual con motivo, solo el Administrador. RN-26: nunca deja el lote en negativo
    // ni por debajo de lo que tienen apartado las consultas abiertas.
    public ResultadoOperacion Ajustar(AjusteFormulario datos, Personal? persona)
    {
        if (!EsAdministrador(persona))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo el Administrador hace ajustes (RN-31)." };
        }

        if (!Valido(datos) || string.IsNullOrWhiteSpace(datos.Motivo))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Escribí el motivo del ajuste (RN-31)." };
        }

        if (datos.Cantidad == 0)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "La cantidad del ajuste no puede ser 0." };
        }

        var lote = ObtenerLote(datos.LoteId);
        var error = ValidarSalida(lote, datos.Cantidad < 0 ? -datos.Cantidad : 0);
        if (error is not null)
        {
            return error;
        }

        var antes = Stock(lote!.ArticuloId);
        lote.Cantidad += datos.Cantidad;
        Registrar(lote, Ajuste, datos.Cantidad, "Ajuste manual", datos.Motivo.Trim(), persona);
        RevisarMinimo(lote.ArticuloId, antes, "Ajuste manual");
        return new ResultadoOperacion { Exito = true, Mensaje = $"Ajuste registrado en el lote {lote.Numero}: {datos.Cantidad:+#;-#}." };
    }

    // Baja por rotura, pérdida u otro motivo (RF-ADM-05). La baja por vencimiento es automática (RN-29).
    public ResultadoOperacion DarDeBaja(AjusteFormulario datos, Personal? persona)
    {
        if (!EsAdministrador(persona))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo el Administrador da de baja." };
        }

        if (!Valido(datos) || string.IsNullOrWhiteSpace(datos.Motivo))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Escribí el motivo de la baja." };
        }

        if (datos.Cantidad <= 0)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Indicá cuántas unidades se dan de baja." };
        }

        var lote = ObtenerLote(datos.LoteId);
        var error = ValidarSalida(lote, datos.Cantidad);
        if (error is not null)
        {
            return error;
        }

        var antes = Stock(lote!.ArticuloId);
        lote.Cantidad -= datos.Cantidad;
        Registrar(lote, Baja, -datos.Cantidad, "Baja manual", datos.Motivo.Trim(), persona);
        RevisarMinimo(lote.ArticuloId, antes, "Baja manual");
        return new ResultadoOperacion { Exito = true, Mensaje = $"Baja registrada: {datos.Cantidad} del lote {lote.Numero}." };
    }

    private ResultadoOperacion? ValidarSalida(LoteProducto? lote, int sale)
    {
        if (lote is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Elegí un lote." };
        }

        ProcesarVencidos();
        if (lote.Inhabilitado)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"El lote {lote.Numero} está vencido e inhabilitado (RN-29)." };
        }

        if (sale > Disponible(lote))
        {
            var apartado = Reservado(lote);
            var detalle = apartado > 0 ? $" ({apartado} apartadas por consultas abiertas)" : "";
            return new ResultadoOperacion { Exito = false, Mensaje = $"El lote {lote.Numero} tiene {Disponible(lote)} disponibles{detalle}: el stock no puede quedar negativo (RN-26)." };
        }

        return null;
    }

    // ---------- Consumo en la consulta (RF-VET-06) ----------

    // Aparta la cantidad por FEFO para una consulta abierta. Si no alcanza, no aparta nada (RN-26).
    // Devuelve las reservas creadas, o null.
    public List<ReservaStock>? Reservar(int consultaId, int articuloId, int cantidad, int aplicacionId)
    {
        if (cantidad <= 0)
        {
            return null;
        }

        var plan = PlanFefo(articuloId, cantidad);
        if (plan is null)
        {
            return null;
        }

        var creadas = plan.Select(p => new ReservaStock
        {
            Id = ++_ultimaReserva,
            ConsultaId = consultaId,
            ArticuloId = articuloId,
            LoteId = p.Lote.Id,
            Cantidad = p.Cantidad,
            AplicacionId = aplicacionId
        }).ToList();
        _reservas.AddRange(creadas);
        return creadas;
    }

    // Libera lo apartado (al quitar un insumo o una vacuna del borrador).
    public void Liberar(int consultaId, int articuloId, int aplicacionId)
    {
        _reservas.RemoveAll(r => r.ConsultaId == consultaId && r.ArticuloId == articuloId
                                 && r.AplicacionId == aplicacionId && !r.Confirmada);
    }

    // Antes de firmar: todo lo apartado tiene que seguir siendo usable. Si un lote venció mientras la
    // consulta estaba abierta, la firma no sigue y el veterinario vuelve a registrar ese ítem.
    public ResultadoOperacion? ValidarConsumo(int consultaId)
    {
        ProcesarVencidos();
        foreach (var reserva in _reservas.Where(r => r.ConsultaId == consultaId && !r.Confirmada))
        {
            var lote = ObtenerLote(reserva.LoteId);
            if (lote is null || !Usable(lote) || lote.Cantidad < reserva.Cantidad)
            {
                var nombre = ObtenerArticulo(reserva.ArticuloId)?.Nombre ?? "un artículo";
                return new ResultadoOperacion { Exito = false, Mensaje = $"El lote de {nombre} ya no se puede usar (venció o no tiene stock). Quitalo y registralo de nuevo. La consulta sigue en borrador." };
            }
        }

        return null;
    }

    // Al firmar (después de ValidarConsumo): descuenta lo apartado, deja los movimientos y revisa el mínimo.
    public void ConfirmarConsumo(int consultaId, Personal? veterinario)
    {
        var reservas = _reservas.Where(r => r.ConsultaId == consultaId && !r.Confirmada).ToList();
        foreach (var articuloId in reservas.Select(r => r.ArticuloId).Distinct())
        {
            var antes = Stock(articuloId);
            foreach (var reserva in reservas.Where(r => r.ArticuloId == articuloId))
            {
                var lote = ObtenerLote(reserva.LoteId)!;
                lote.Cantidad -= reserva.Cantidad;
                reserva.Confirmada = true;
                Registrar(lote, Consumo, -reserva.Cantidad, $"Consulta {consultaId}", "", veterinario);
            }

            RevisarMinimo(articuloId, antes, $"Consulta {consultaId}");
        }
    }

    public bool TieneConsumos(int consultaId)
    {
        return _reservas.Any(r => r.ConsultaId == consultaId);
    }
}
