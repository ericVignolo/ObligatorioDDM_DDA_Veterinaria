using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Datos ficticios de la sección A. En la sección C se reemplaza por ClinicaApiService.
public class ClinicaService
{
    private readonly Clinica _clinica = new()
    {
        Nombre = "VITALIA",
        Lema = "Clínica veterinaria para animales de compañía y de campo",
        Direccion = "Calle 18 de Julio 1234",
        Ciudad = "Nueva Palmira, Colonia",
        Telefono = "+598 4544 0000",
        TelefonoGuardia = "+598 99 000 000",
        Correo = "contacto@vitalia.uy",
        Horario = "Lunes a viernes de 9 a 19 h · Sábados de 9 a 13 h"
    };

    public Clinica Obtener()
    {
        return _clinica;
    }

    // Convierte "+598 4544 0000" en "tel:+59845440000" para los botones de llamada.
    public string EnlaceTelefono(string telefono)
    {
        return "tel:" + telefono.Replace(" ", "");
    }
}
