using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Solicitud de asistencia que Recepción registra cuando el cliente llama (A-026). La misma información que pide el móvil (RF-CLI-21).
public class SolicitudTelefonicaFormulario
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí el cliente.")]
    public int ClienteId { get; set; }

    // 0 = animal no registrado o grupo.
    public int AnimalId { get; set; }

    [StringLength(40)]
    public string Especie { get; set; } = "";

    [Range(1, 1000, ErrorMessage = "La cantidad debe estar entre 1 y 1000.")]
    public int Cantidad { get; set; } = 1;

    [Required(ErrorMessage = "Describí los síntomas.")]
    [StringLength(1000, ErrorMessage = "Los síntomas pueden tener hasta 1000 caracteres.")]
    public string Sintomas { get; set; } = "";

    [Required(ErrorMessage = "Elegí la gravedad que percibe el cliente.")]
    public string Nivel { get; set; } = "";

    [Required(ErrorMessage = "Elegí la modalidad.")]
    public string Modalidad { get; set; } = "";

    [StringLength(200, ErrorMessage = "La dirección puede tener hasta 200 caracteres.")]
    public string Direccion { get; set; } = "";

    [StringLength(300, ErrorMessage = "Las referencias pueden tener hasta 300 caracteres.")]
    public string Referencias { get; set; } = "";

    [Range(0, 500, ErrorMessage = "La distancia debe estar entre 0 y 500 km.")]
    public int DistanciaKm { get; set; }

    [Required(ErrorMessage = "Escribí un teléfono de contacto.")]
    [StringLength(30, ErrorMessage = "El teléfono puede tener hasta 30 caracteres.")]
    public string TelefonoContacto { get; set; } = "";

    // Evita duplicar la solicitud si se envía dos veces (prueba 10 de la especificación).
    public string ClaveIdempotencia { get; set; } = Guid.NewGuid().ToString("N");
}
