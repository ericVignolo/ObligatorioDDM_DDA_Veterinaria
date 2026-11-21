using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

// Acceso a lo que usa la consulta clínica: turno, consulta, stock, vacunas, cargo y comunicación.
// Las entidades que devuelve quedan seguidas por EF: el Service las cambia y llama a GuardarAsync.
public interface IConsultaRepository
{
    // Con servicio y animal (y su especie).
    Task<Turno?> ObtenerTurnoAsync(int turnoId);

    // Con turno, servicio, animal, profesional, autor y enmiendas.
    Task<Consulta?> ObtenerAsync(int consultaId);
    Task<Consulta?> ObtenerPorTurnoAsync(int turnoId);

    // Borradores cuyo turno sigue En curso (P-RC-04). Null = de todos.
    Task<List<Consulta>> ObtenerAbiertasAsync(int? autorId);

    Task<Cuenta?> ObtenerCuentaAsync(int cuentaId);
    Task<Profesional?> ObtenerProfesionalDeCuentaAsync(int cuentaId);

    // RN-10: el último consentimiento del turno está aceptado.
    Task<bool> TieneConsentimientoAceptadoAsync(int turnoId);

    // RN-17: lo escrito como alergias en las consultas firmadas del animal.
    Task<List<string>> AlergiasRegistradasAsync(int animalId);

    // ---------- Stock ----------
    Task<Articulo?> ObtenerArticuloAsync(int articuloId);

    // Lotes del artículo, seguidos por EF, con lo apartado por consultas sin firmar.
    Task<List<LoteProducto>> ObtenerLotesAsync(int articuloId);
    Task<Dictionary<int, int>> ReservadoPorLoteAsync(int articuloId);
    Task<List<ReservaStock>> ObtenerReservasAsync(int consultaId);

    // ---------- Vacunas ----------
    Task<ProductoSanitario?> ObtenerProductoAsync(int productoId);
    Task<List<AplicacionSanitaria>> ObtenerAplicacionesDeConsultaAsync(int consultaId);

    // Las que cuentan: de consultas firmadas o registros anteriores al sistema (sin consulta).
    Task<List<AplicacionSanitaria>> ObtenerAplicacionesQueCuentanAsync(int animalId);

    // ---------- Cargo y comunicación ----------
    Task<TarifaPrestacion?> ObtenerTarifaVigenteAsync(int prestacionId, DateTime fecha);
    Task<bool> ExisteCargoDeConsultaAsync(int consultaId);

    // RN-35: lo pagado de la seña del turno (sin comprobantes anulados), para descontarlo del cargo final.
    Task<decimal> SenaPagadaAsync(int turnoId);

    // P-RC-07: el cliente que reservó si sigue siendo responsable; si no, el titular vigente.
    Task<int?> ObtenerResponsableEconomicoAsync(int animalId, int clientePreferido);

    Task<string?> ObtenerCanalPreferidoAsync(int cuentaId, int tipoComunicacionId);
    Task<TipoComunicacion?> ObtenerTipoComunicacionAsync(int tipoComunicacionId);

    // ---------- Historia ----------
    Task<Animal?> ObtenerAnimalAsync(int animalId);
    Task<bool> EsResponsableAsync(int animalId, int cuentaId);
    Task<List<Consulta>> ObtenerFirmadasAsync(int animalId);

    // P-RC-05: el animal tiene un turno del día o en curso, o una internación abierta.
    Task<bool> TieneCasoActivoAsync(int animalId, DateTime ahora);

    void Agregar<T>(T entidad) where T : class;
    void Quitar<T>(T entidad) where T : class;
    Task GuardarAsync();

    // Transacción con bloqueo de la consulta y de los artículos (en orden), para no apartar dos veces el mismo stock.
    Task<T> EnTransaccionAsync<T>(int consultaId, IEnumerable<int> articuloIds, Func<Task<T>> accion);
}
