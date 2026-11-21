using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

// Acceso para cargos, cobros (comprobantes), notas de crédito y señas. Las entidades que devuelve quedan seguidas
// por EF, salvo las listas.
public interface ICobroRepository
{
    Task<Cargo?> ObtenerCargoAsync(int cargoId);

    // Null = de todos los clientes. Del más reciente al más antiguo.
    Task<List<Cargo>> ObtenerCargosAsync(int? clienteId);

    // Lo cobrado de cada cargo, sin los comprobantes anulados.
    Task<Dictionary<int, decimal>> PagadoPorCargoAsync(IEnumerable<int> cargoIds);

    Task<Pago?> ObtenerPagoAsync(int pagoId);
    Task<List<Pago>> ObtenerPagosDeCargoAsync(int cargoId);
    Task<List<Pago>> ObtenerPagosEntreAsync(DateTime desde, DateTime hasta);
    Task<List<NotaCredito>> ObtenerNotasDePagosAsync(IEnumerable<int> pagoIds);
    Task<List<NotaCredito>> ObtenerNotasAsync();

    // RN-35: turnos de cirugía que esperan la seña y todavía no tienen su cargo.
    Task<List<Turno>> ObtenerTurnosSinSenaAsync(int? clienteId);
    Task<bool> ExisteSenaAsync(int turnoId);
    Task<Turno?> ObtenerTurnoAsync(int turnoId);
    Task<TarifaPrestacion?> ObtenerTarifaVigenteAsync(int prestacionId, DateTime fecha);
    Task<Cuenta?> ObtenerCuentaAsync(int cuentaId);
    Task<Dictionary<string, string>> ObtenerParametrosAsync();

    // Siguiente número del correlativo (Comprobante o NotaCredito). Llamar dentro de EnTransaccionAsync con esa clave.
    Task<int> SiguienteNumeroAsync(string clave);

    void Agregar<T>(T entidad) where T : class;
    Task GuardarAsync();

    // Transacción con el cargo y el numerador bloqueados: dos cobros del mismo cargo no pasan juntos el saldo
    // y dos comprobantes no salen con el mismo número.
    Task<T> EnTransaccionAsync<T>(int cargoId, string numerador, Func<Task<T>> accion);
}
