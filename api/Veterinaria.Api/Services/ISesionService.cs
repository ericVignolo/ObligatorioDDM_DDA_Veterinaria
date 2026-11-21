using Veterinaria.Api.DTOs;

namespace Veterinaria.Api.Services;

// Inicio y cierre de sesión con token corto y token de refresco revocable (A-008, RNF-SEG-03, RNF-SEG-09).
public interface ISesionService
{
    Task<Resultado<SesionReadDto>> IniciarAsync(InicioSesionDto datos, string origen);
    Task<Resultado<SesionReadDto>> RefrescarAsync(string tokenRefresco, string origen);

    // Siempre termina bien, aunque el token ya no exista (cerrar dos veces no es un error).
    Task CerrarAsync(string tokenRefresco);
}
