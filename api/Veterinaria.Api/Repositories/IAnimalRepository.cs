using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

// Acceso a Animales y Responsabilidades. Solo devuelve animales activos con una responsabilidad vigente de la cuenta (RN-18).
public interface IAnimalRepository
{
    Task<int> ContarDeClienteAsync(int cuentaId);
    Task<List<Animal>> ObtenerDeClienteAsync(int cuentaId, int saltar, int tomar);

    // Seguido por EF, para editar o dar de baja. Null si no existe, está de baja o no es de la cuenta.
    Task<Animal?> ObtenerDeClienteAsync(int id, int cuentaId);

    Task<bool> ExisteMicrochipAsync(string microchip, int? excepto);
    Task<Especie?> ObtenerEspecieAsync(int id);
    Task<List<Especie>> ObtenerEspeciesAsync();

    // Turnos que todavía no pasaron y no están cancelados ni finalizados.
    Task<bool> TieneTurnosPendientesAsync(int animalId, DateTime ahora);

    void Agregar(Animal animal);

    // Lanza MicrochipDuplicadoException si choca con el índice único del microchip.
    Task GuardarAsync();
}

public class MicrochipDuplicadoException : Exception
{
    public MicrochipDuplicadoException(Exception interna) : base("El microchip ya existe.", interna)
    {
    }
}
