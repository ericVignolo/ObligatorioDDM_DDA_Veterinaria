using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Inventario, igual a InventarioService de Blazor: compras, ajustes y bajas del Administrador con motivo (RN-31),
// stock nunca negativo ni por debajo de lo apartado (RN-26), alerta al cruzar el mínimo (RN-28), todo cambio con
// su movimiento (RN-30) y baja automática de vencidos (RN-29). Cada cambio bloquea la fila del artículo.
public class InventarioService : IInventarioService
{
    public const int DiasProximoAVencer = 30;

    private readonly ISaludRepository _repository;
    private readonly TimeProvider _reloj;

    public InventarioService(ISaludRepository repository, TimeProvider reloj)
    {
        _repository = repository;
        _reloj = reloj;
    }

    private DateTime Ahora => _reloj.GetLocalNow().DateTime;
    private DateTime Hoy => Ahora.Date;

    // ===================== Consultas =====================

    public async Task<List<ArticuloStockDto>> ArticulosAsync()
    {
        var hoy = Hoy;
        var lotes = await _repository.ObtenerLotesAsync(null);
        var reservado = await _repository.ReservadoPorLoteAsync(null);

        return (await _repository.ObtenerArticulosAsync()).Select(a =>
        {
            var propios = lotes.Where(l => l.ArticuloId == a.Id).ToList();
            var stock = ReglasStock.Stock(propios, hoy);
            return new ArticuloStockDto
            {
                Id = a.Id,
                Nombre = a.Nombre,
                Tipo = a.Tipo,
                Unidad = a.Unidad,
                StockMinimo = a.StockMinimo,
                Stock = stock,
                Disponible = ReglasStock.StockDisponible(propios, reservado, hoy),
                BajoMinimo = stock < a.StockMinimo,
                LotesProximosAVencer = propios.Count(l => ReglasStock.Usable(l, hoy) && l.Cantidad > 0 && l.Vencimiento.Date <= hoy.AddDays(DiasProximoAVencer))
            };
        }).ToList();
    }

    public async Task<Resultado<List<LoteReadDto>>> LotesAsync(int articuloId)
    {
        if (await _repository.ObtenerArticuloAsync(articuloId) == null)
        {
            return Error<List<LoteReadDto>>(404, "ARTICULO_NO_ENCONTRADO", "No encontramos ese artículo.");
        }

        var reservado = await _repository.ReservadoPorLoteAsync(articuloId);
        return Resultado<List<LoteReadDto>>.Ok((await _repository.ObtenerLotesAsync(articuloId)).Select(l => LoteADto(l, reservado)).ToList());
    }

    public async Task<PaginaDto<MovimientoReadDto>> MovimientosAsync(int? articuloId, int page, int pageSize)
    {
        var total = await _repository.ContarMovimientosAsync(articuloId);
        var movimientos = await _repository.ObtenerMovimientosAsync(articuloId, (page - 1) * pageSize, pageSize);

        return new PaginaDto<MovimientoReadDto>
        {
            Items = movimientos.Select(m => new MovimientoReadDto
            {
                Id = m.Id,
                ArticuloId = m.ArticuloId,
                Articulo = m.Articulo?.Nombre ?? "",
                Lote = m.Lote?.Numero ?? "",
                Tipo = m.Tipo,
                Cantidad = m.Cantidad,
                Origen = m.Origen,
                Motivo = m.Motivo,
                Fecha = m.Fecha,
                Persona = m.Persona == null ? "Sistema" : $"{m.Persona.Nombre} {m.Persona.Apellido}".Trim()
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = total
        };
    }

    public async Task<List<AlertaStockReadDto>> AlertasAsync(bool soloPendientes)
    {
        return (await _repository.ObtenerAlertasAsync(soloPendientes)).Select(AlertaADto).ToList();
    }

    public async Task<Resultado<AlertaStockReadDto>> MarcarAlertaAtendidaAsync(int alertaId)
    {
        var alerta = await _repository.ObtenerAlertaAsync(alertaId);
        if (alerta == null)
        {
            return Error<AlertaStockReadDto>(404, "ALERTA_NO_ENCONTRADA", "No encontramos esa alerta.");
        }

        alerta.Atendida = true;
        await _repository.GuardarAsync();
        return Resultado<AlertaStockReadDto>.Ok(AlertaADto(alerta));
    }

    // ===================== Cambios del Administrador =====================

    public async Task<Resultado<LoteReadDto>> RegistrarCompraAsync(int personaId, CompraDto datos)
    {
        var articulo = await _repository.ObtenerArticuloAsync(datos.ArticuloId);
        if (articulo == null)
        {
            return Error<LoteReadDto>(400, "ARTICULO_INVALIDO", "Elegí un artículo.");
        }

        var vencimiento = datos.Vencimiento!.Value.ToDateTime(TimeOnly.MinValue);
        if (vencimiento <= Hoy)
        {
            return Error<LoteReadDto>(400, "LOTE_VENCIDO", "No se ingresa un lote vencido o que vence hoy.");
        }

        var numero = datos.Numero.Trim().ToUpperInvariant();

        return await _repository.EnTransaccionAsync(new[] { articulo.Id }, async () =>
        {
            if (await _repository.ExisteLoteAsync(articulo.Id, numero))
            {
                return Error<LoteReadDto>(409, "LOTE_REPETIDO", $"Ya existe el lote {numero} de {articulo.Nombre}. Para corregir su cantidad usá un ajuste.");
            }

            var lote = new LoteProducto { ArticuloId = articulo.Id, Numero = numero, Vencimiento = vencimiento, Cantidad = datos.Cantidad, FechaIngreso = Ahora };
            _repository.Agregar(lote);
            _repository.Agregar(Movimiento(lote, "Compra", datos.Cantidad, "Compra", "", personaId));
            await _repository.GuardarAsync();
            return new Resultado<LoteReadDto> { Exito = true, Estado = 201, Valor = LoteADto(lote, new Dictionary<int, int>()) };
        });
    }

    public async Task<Resultado<LoteReadDto>> AjustarAsync(int personaId, AjusteDto datos)
    {
        if (datos.Cantidad == 0)
        {
            return Error<LoteReadDto>(400, "AJUSTE_CERO", "La cantidad del ajuste no puede ser 0.");
        }

        return await CambiarLoteAsync(datos.LoteId, sale: datos.Cantidad < 0 ? -datos.Cantidad : 0, (lote, articulo) =>
        {
            lote.Cantidad += datos.Cantidad;
            _repository.Agregar(Movimiento(lote, "Ajuste", datos.Cantidad, "Ajuste manual", datos.Motivo.Trim(), personaId));
            return "Ajuste manual";
        });
    }

    public async Task<Resultado<LoteReadDto>> DarDeBajaAsync(int personaId, AjusteDto datos)
    {
        if (datos.Cantidad <= 0)
        {
            return Error<LoteReadDto>(400, "BAJA_SIN_CANTIDAD", "Indicá cuántas unidades se dan de baja.");
        }

        return await CambiarLoteAsync(datos.LoteId, sale: datos.Cantidad, (lote, articulo) =>
        {
            lote.Cantidad -= datos.Cantidad;
            _repository.Agregar(Movimiento(lote, "Baja", -datos.Cantidad, "Baja manual", datos.Motivo.Trim(), personaId));
            return "Baja manual";
        });
    }

    public async Task<VencidosProcesadosDto> ProcesarVencidosAsync()
    {
        var hoy = Hoy;
        var resultado = new VencidosProcesadosDto();
        var articulos = (await _repository.ObtenerLotesAsync(null)).Where(l => !l.Inhabilitado && l.Vencimiento.Date < hoy).Select(l => l.ArticuloId).Distinct().ToList();
        if (articulos.Count == 0)
        {
            return resultado;
        }

        return await _repository.EnTransaccionAsync(articulos, async () =>
        {
            foreach (var articuloId in articulos)
            {
                var articulo = await _repository.ObtenerArticuloAsync(articuloId);
                var lotes = await _repository.ObtenerLotesAsync(articuloId);

                // El lote vencido ya no cuenta como usable por la fecha: el stock de antes lo suma a mano.
                var vencidos = lotes.Where(l => !l.Inhabilitado && l.Vencimiento.Date < hoy).ToList();
                var antes = ReglasStock.Stock(lotes, hoy) + vencidos.Sum(l => l.Cantidad);

                foreach (var lote in vencidos)
                {
                    lote.Inhabilitado = true;
                    resultado.LotesInhabilitados++;
                    if (lote.Cantidad > 0)
                    {
                        resultado.UnidadesDadasDeBaja += lote.Cantidad;
                        _repository.Agregar(Movimiento(lote, "Baja", -lote.Cantidad, "Vencimiento automático", $"El lote venció el {lote.Vencimiento:dd/MM/yyyy}.", null));
                        lote.Cantidad = 0;
                    }
                }

                RevisarMinimo(articulo, antes, ReglasStock.Stock(lotes, hoy), "Baja por vencimiento");
            }

            await _repository.GuardarAsync();
            return resultado;
        });
    }

    // ===================== Para la consulta =====================

    public async Task<List<InsumoDisponibleDto>> InsumosParaConsultaAsync()
    {
        var hoy = Hoy;
        var lotes = await _repository.ObtenerLotesAsync(null);
        var reservado = await _repository.ReservadoPorLoteAsync(null);

        return (await _repository.ObtenerArticulosAsync())
            .Where(a => a.Tipo == "Insumo")
            .Select(a => new InsumoDisponibleDto
            {
                ArticuloId = a.Id,
                Nombre = a.Nombre,
                Unidad = a.Unidad,
                Disponible = ReglasStock.StockDisponible(lotes.Where(l => l.ArticuloId == a.Id), reservado, hoy)
            }).ToList();
    }

    public async Task<Resultado<List<ProductoDisponibleDto>>> ProductosParaAnimalAsync(int animalId)
    {
        var animal = await _repository.ObtenerAnimalAsync(animalId);
        if (animal == null)
        {
            return Error<List<ProductoDisponibleDto>>(404, "ANIMAL_NO_ENCONTRADO", "No encontramos ese animal.");
        }

        var hoy = Hoy;
        var lotes = await _repository.ObtenerLotesAsync(null);
        var reservado = await _repository.ReservadoPorLoteAsync(null);

        return Resultado<List<ProductoDisponibleDto>>.Ok((await _repository.ObtenerProductosAsync())
            .Where(p => p.Especies.Any(e => e.EspecieId == animal.EspecieId))
            .Select(p =>
            {
                var propios = lotes.Where(l => l.ArticuloId == p.ArticuloId).ToList();
                var plan = ReglasStock.PlanFefo(propios, reservado, 1, hoy);
                return new ProductoDisponibleDto
                {
                    Id = p.Id,
                    Nombre = p.Nombre,
                    Tipo = p.Tipo,
                    Laboratorio = p.Laboratorio,
                    IntervaloRefuerzoDias = p.IntervaloRefuerzoDias,
                    Disponible = ReglasStock.StockDisponible(propios, reservado, hoy),
                    LoteQueSeUsara = plan?[0].Lote.Numero ?? ""
                };
            }).ToList());
    }

    // ===================== Apoyo =====================

    // Ajuste o baja: bloquea el artículo del lote, valida que no quede negativo ni debajo de lo apartado y registra.
    private async Task<Resultado<LoteReadDto>> CambiarLoteAsync(int loteId, int sale, Func<LoteProducto, Articulo, string> cambio)
    {
        var previo = await _repository.ObtenerLoteAsync(loteId);
        if (previo == null)
        {
            return Error<LoteReadDto>(404, "LOTE_NO_ENCONTRADO", "Elegí un lote.");
        }

        return await _repository.EnTransaccionAsync(new[] { previo.ArticuloId }, async () =>
        {
            var hoy = Hoy;
            var articulo = await _repository.ObtenerArticuloAsync(previo.ArticuloId);
            var lotes = await _repository.ObtenerLotesAsync(previo.ArticuloId);
            var lote = lotes.First(l => l.Id == loteId);
            var reservado = await _repository.ReservadoPorLoteAsync(previo.ArticuloId);

            if (!ReglasStock.Usable(lote, hoy))
            {
                return Error<LoteReadDto>(409, "LOTE_INHABILITADO", $"El lote {lote.Numero} está vencido e inhabilitado (RN-29).");
            }

            var disponible = ReglasStock.Disponible(lote, reservado, hoy);
            if (sale > disponible)
            {
                var apartado = reservado.GetValueOrDefault(lote.Id);
                var detalle = apartado > 0 ? $" ({apartado} apartadas por consultas abiertas)" : "";
                return Error<LoteReadDto>(409, "STOCK_INSUFICIENTE", $"El lote {lote.Numero} tiene {disponible} disponibles{detalle}: el stock no puede quedar negativo (RN-26).");
            }

            var antes = ReglasStock.Stock(lotes, hoy);
            var origen = cambio(lote, articulo!);
            RevisarMinimo(articulo, antes, ReglasStock.Stock(lotes, hoy), origen);
            await _repository.GuardarAsync();
            return Resultado<LoteReadDto>.Ok(LoteADto(lote, reservado));
        });
    }

    private void RevisarMinimo(Articulo? articulo, int antes, int despues, string origen)
    {
        if (articulo != null && ReglasStock.CruzaMinimo(antes, despues, articulo.StockMinimo))
        {
            _repository.Agregar(new AlertaStock { ArticuloId = articulo.Id, StockQueQuedo = despues, StockMinimo = articulo.StockMinimo, Origen = origen, Fecha = Ahora });
        }
    }

    private MovimientoStock Movimiento(LoteProducto lote, string tipo, int cantidad, string origen, string motivo, int? personaId) => new()
    {
        ArticuloId = lote.ArticuloId,
        Lote = lote,
        LoteId = lote.Id,
        Tipo = tipo,
        Cantidad = cantidad,
        Origen = origen,
        Motivo = motivo,
        Fecha = Ahora,
        PersonaId = personaId
    };

    private LoteReadDto LoteADto(LoteProducto l, IReadOnlyDictionary<int, int> reservado) => new()
    {
        Id = l.Id,
        ArticuloId = l.ArticuloId,
        Numero = l.Numero,
        Vencimiento = DateOnly.FromDateTime(l.Vencimiento),
        Cantidad = l.Cantidad,
        Apartado = reservado.GetValueOrDefault(l.Id),
        Disponible = ReglasStock.Disponible(l, reservado, Hoy),
        Inhabilitado = l.Inhabilitado,
        Vencido = l.Vencimiento.Date < Hoy
    };

    private static AlertaStockReadDto AlertaADto(AlertaStock a) => new()
    {
        Id = a.Id,
        ArticuloId = a.ArticuloId,
        Articulo = a.Articulo?.Nombre ?? "",
        StockQueQuedo = a.StockQueQuedo,
        StockMinimo = a.StockMinimo,
        Origen = a.Origen,
        Fecha = a.Fecha,
        Atendida = a.Atendida
    };

    private static Resultado<T> Error<T>(int estado, string codigo, string mensaje)
        => Resultado<T>.Error(estado, codigo, estado switch { 404 => "No encontrado", 409 => "No se puede", _ => "Datos no válidos" }, mensaje);
}
