using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

// Acceso para internaciones, boxes y novedades. Las entidades que devuelve quedan seguidas por EF, salvo las listas.
public interface IInternacionRepository
{
    // Con especie y responsables vigentes.
    Task<Animal?> ObtenerAnimalAsync(int animalId);
    Task<bool> EsResponsableAsync(int animalId, int cuentaId);

    // RN-21: la internación abierta (Admitida o Internado) del animal, si hay.
    Task<Internacion?> ObtenerAbiertaDeAnimalAsync(int animalId);

    // Con animal (y especie), cliente y box.
    Task<Internacion?> ObtenerAsync(int id);
    Task<List<Internacion>> ObtenerAbiertasAsync();
    Task<List<Internacion>> ObtenerAltasAsync(int tomar);
    Task<List<Internacion>> ObtenerDeClienteAsync(int clienteId);

    Task<List<Box>> ObtenerBoxesAsync();
    Task<Box?> ObtenerBoxAsync(int boxId);
    Task<bool> ExisteBoxConNombreAsync(string nombre);

    // RN-22: la internación que ocupa el box (Internado), si hay.
    Task<Internacion?> ObtenerOcupanteAsync(int boxId);
    Task<List<Internacion>> ObtenerOcupantesAsync();

    Task<List<NovedadInternacion>> ObtenerNovedadesAsync(int internacionId, bool soloVisibles);
    Task<NovedadInternacion?> ObtenerNovedadAsync(int novedadId);

    void Agregar<T>(T entidad) where T : class;
    Task GuardarAsync();

    // Transacción con el animal (admisión, RN-21) o el box (ingreso clínico, RN-22) bloqueado.
    Task<T> EnTransaccionConAnimalAsync<T>(int animalId, Func<Task<T>> accion);
    Task<T> EnTransaccionConBoxAsync<T>(int boxId, Func<Task<T>> accion);
}
