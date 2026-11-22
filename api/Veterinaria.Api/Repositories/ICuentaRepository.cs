using Veterinaria.Api.Models;

namespace Veterinaria.Api.Repositories;

// Acceso a Cuentas, CodigosCuenta, AceptacionesLegales y SesionesRefresco.
// Las entidades que devuelve quedan seguidas por EF: el Service las cambia y llama a GuardarAsync.
public interface ICuentaRepository
{
    // Con sus roles.
    Task<Cuenta?> ObtenerPorCorreoAsync(string correo);
    Task<Cuenta?> ObtenerPorIdAsync(int id);

    Task<bool> ExisteDocumentoAsync(string tipo, string numero);

    // La versión vigente de cada documento legal (terminos, privacidad, cookies).
    Task<List<DocumentoLegal>> ObtenerDocumentosVigentesAsync(DateTime hoy);

    Task<int> ContarCodigosDesdeAsync(int cuentaId, string tipo, DateTime desde);
    Task<CodigoCuenta?> ObtenerUltimoCodigoAsync(int cuentaId, string tipo);
    Task<List<CodigoCuenta>> ObtenerCodigosSinUsarAsync(int cuentaId, string tipo);

    Task<SesionRefresco?> ObtenerSesionAsync(string hashToken);
    Task<List<SesionRefresco>> ObtenerSesionesActivasAsync(int cuentaId, DateTime ahora);

    void AgregarCuenta(Cuenta cuenta);
    void AgregarCodigo(CodigoCuenta codigo);
    void AgregarSesion(SesionRefresco sesion);

    // Borra una cuenta que nunca se verificó, con sus códigos, roles y aceptaciones (D-REG-02, 7 días).
    Task EliminarNoVerificadaAsync(Cuenta cuenta);

    // D-REG-02: cuentas que nunca se verificaron, creadas antes de la fecha límite.
    Task<List<Cuenta>> ObtenerNoVerificadasAntesDeAsync(DateTime limite);

    // Lanza CuentaDuplicadaException si choca con el índice único del correo.
    Task GuardarAsync();
}

public class CuentaDuplicadaException : Exception
{
    public CuentaDuplicadaException(Exception interna) : base("El correo ya tiene una cuenta.", interna)
    {
    }
}
