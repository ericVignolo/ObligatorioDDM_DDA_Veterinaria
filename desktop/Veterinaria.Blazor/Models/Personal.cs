namespace Veterinaria.Blazor.Models;

// Cuenta del personal de la clínica (A-010, RF-ADM-01). En la sección C la crea el Administrador en la API.
public class Personal
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";

    // Recepción, Veterinario o Administrador.
    public string Rol { get; set; } = "";

    // Para Veterinario: su ficha de profesional (agenda, especies, servicios).
    public int? ProfesionalId { get; set; }

    public string HashContrasena { get; set; } = "";
}
