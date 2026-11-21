using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

// Inventario (A-022, RF-ADM-05, RN-26 a RN-31). Las acciones que cambian cantidades son del Administrador.
public interface IInventarioService
{
    Task<List<ArticuloStockDto>> ArticulosAsync();
    Task<Resultado<List<LoteReadDto>>> LotesAsync(int articuloId);
    Task<PaginaDto<MovimientoReadDto>> MovimientosAsync(int? articuloId, int page, int pageSize);
    Task<List<AlertaStockReadDto>> AlertasAsync(bool soloPendientes);
    Task<Resultado<AlertaStockReadDto>> MarcarAlertaAtendidaAsync(int alertaId);

    Task<Resultado<LoteReadDto>> RegistrarCompraAsync(int personaId, CompraDto datos);
    Task<Resultado<LoteReadDto>> AjustarAsync(int personaId, AjusteDto datos);
    Task<Resultado<LoteReadDto>> DarDeBajaAsync(int personaId, AjusteDto datos);

    // RN-29: inhabilita los lotes vencidos y da de baja su saldo, con su movimiento.
    Task<VencidosProcesadosDto> ProcesarVencidosAsync();

    // Para el formulario de la consulta.
    Task<List<InsumoDisponibleDto>> InsumosParaConsultaAsync();
    Task<Resultado<List<ProductoDisponibleDto>>> ProductosParaAnimalAsync(int animalId);
}
