using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

// Acceso a Turnos y a lo que la agenda necesita: profesional con horario, servicio, animal, feriados y parámetros.
public interface ITurnoRepository
{
    // Con franjas, ausencias, especies y servicios.
    Task<Profesional?> ObtenerProfesionalAsync(int id);
    Task<int?> ObtenerProfesionalIdDeCuentaAsync(int cuentaId);
    Task<Prestacion?> ObtenerPrestacionAsync(int id);

    // Activo, con su especie y la responsabilidad vigente de la cuenta. Null si no es de esa cuenta (RN-18).
    Task<Animal?> ObtenerAnimalDeClienteAsync(int animalId, int clienteId);

    // Turnos que se cruzan con [desde, hasta) del profesional o del animal (todos los estados; el cálculo filtra).
    Task<List<Turno>> ObtenerTurnosAsync(int profesionalId, int animalId, DateTime desde, DateTime hasta);

    Task<HashSet<DateOnly>> ObtenerFeriadosAsync(DateOnly desde, DateOnly hasta);
    Task<Dictionary<string, string>> ObtenerParametrosAsync();

    // RN-08: ausencias sin aviso del cliente desde una fecha.
    Task<int> ContarAusenciasAsync(int clienteId, DateTime desde);

    Task<List<Turno>> ObtenerDeClienteAsync(int clienteId, bool proximos, DateTime ahora, int saltar, int tomar);
    Task<int> ContarDeClienteAsync(int clienteId, bool proximos, DateTime ahora);

    // Seguido por EF, con animal, servicio, profesional y cliente.
    Task<Turno?> ObtenerAsync(int id);

    Task<List<Turno>> ObtenerDelDiaAsync(DateOnly fecha, int? profesionalId, string? estado);

    // Recepción: clientes por nombre, apellido, correo o documento, con sus animales activos.
    Task<List<Cuenta>> BuscarClientesAsync(string texto, int saltar, int tomar);
    Task<int> ContarClientesAsync(string texto);
    Task<List<Animal>> ObtenerAnimalesDeClienteAsync(int clienteId);

    void Agregar(Turno turno);
    void AgregarAuditoria(Auditoria registro);
    Task GuardarAsync();

    // Transacción breve READ COMMITTED que bloquea con SELECT … FOR UPDATE la fila del profesional y la del animal,
    // en ese orden fijo, y ejecuta `accion` (que revalida y guarda). Hasta 3 reintentos (50/100/200 ms) ante interbloqueo (DT-BD-02).
    Task<T> EnTransaccionConBloqueoAsync<T>(int profesionalId, int animalId, Func<Task<T>> accion);
}
