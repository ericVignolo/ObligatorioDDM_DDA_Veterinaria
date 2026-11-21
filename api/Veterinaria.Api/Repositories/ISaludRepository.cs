using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

// Acceso para el carnet, los avisos de refuerzo, el inventario y el consentimiento informado.
// Las entidades que devuelve quedan seguidas por EF: el Service las cambia y llama a GuardarAsync.
public interface ISaludRepository
{
    // ---------- Animales y cuentas ----------
    Task<Animal?> ObtenerAnimalAsync(int animalId);
    Task<bool> EsResponsableAsync(int animalId, int cuentaId);
    Task<List<Animal>> ObtenerAnimalesDeClienteAsync(int cuentaId);
    Task<Cuenta?> ObtenerCuentaAsync(int cuentaId);
    Task<Dictionary<string, string>> ObtenerParametrosAsync();

    // ---------- Carnet y avisos ----------
    // Las que cuentan (de consultas firmadas o anteriores al sistema), de esos animales.
    Task<List<AplicacionSanitaria>> ObtenerAplicacionesQueCuentanAsync(IEnumerable<int> animalIds);

    // Animales activos con alguna aplicación que cuenta y refuerzo hasta esa fecha, con sus responsables vigentes.
    Task<List<Animal>> ObtenerAnimalesConRefuerzoHastaAsync(DateTime hasta);

    Task<bool> ExisteComunicacionAsync(int cuentaId, int tipoComunicacionId, string clave);
    Task<string?> ObtenerCanalPreferidoAsync(int cuentaId, int tipoComunicacionId);
    Task<TipoComunicacion?> ObtenerTipoComunicacionAsync(int tipoComunicacionId);

    // ---------- Inventario ----------
    Task<List<Articulo>> ObtenerArticulosAsync();
    Task<Articulo?> ObtenerArticuloAsync(int articuloId);

    // Null = de todos los artículos.
    Task<List<LoteProducto>> ObtenerLotesAsync(int? articuloId);
    Task<LoteProducto?> ObtenerLoteAsync(int loteId);
    Task<Dictionary<int, int>> ReservadoPorLoteAsync(int? articuloId);
    Task<bool> ExisteLoteAsync(int articuloId, string numero);

    Task<List<MovimientoStock>> ObtenerMovimientosAsync(int? articuloId, int saltar, int tomar);
    Task<int> ContarMovimientosAsync(int? articuloId);
    Task<List<AlertaStock>> ObtenerAlertasAsync(bool soloPendientes);
    Task<AlertaStock?> ObtenerAlertaAsync(int alertaId);
    Task<List<ProductoSanitario>> ObtenerProductosAsync();

    // ---------- Consentimiento ----------
    Task<Turno?> ObtenerTurnoAsync(int turnoId);
    Task<List<Turno>> ObtenerTurnosConConsentimientoAsync(int clienteId, DateTime ahora);
    Task<DocumentoConsentimiento?> ObtenerDocumentoVigenteAsync(int prestacionId);
    Task<List<DocumentoConsentimiento>> ObtenerDocumentosAsync(int prestacionId);
    Task<Consentimiento?> ObtenerUltimaRespuestaAsync(int turnoId);
    Task<Prestacion?> ObtenerPrestacionAsync(int prestacionId);

    void Agregar<T>(T entidad) where T : class;
    Task GuardarAsync();

    // Transacción con bloqueo de los artículos (en orden), para que dos movimientos no pisen el mismo stock.
    Task<T> EnTransaccionAsync<T>(IEnumerable<int> articuloIds, Func<Task<T>> accion);
}
