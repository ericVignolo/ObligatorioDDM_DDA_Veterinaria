using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Cuentas de demostración del personal (A-010). En la sección C las gestiona el Administrador (RF-ADM-01)
// y la API controla los permisos de cada rol; acá solo cambia lo que se muestra.
public class PersonalService
{
    // SOLO DEMOSTRACIÓN: misma contraseña para todas las cuentas del personal, mostrada en /ingresar.
    public const string ContrasenaDemo = "Vitalia2026!";

    public const string Recepcion = "Recepción";
    public const string Veterinario = "Veterinario";
    public const string Administrador = "Administrador";

    private readonly List<Personal> _personal;

    public PersonalService()
    {
        var hash = CuentaService.DerivarContrasena(ContrasenaDemo);
        _personal = new()
        {
            new() { Id = 1, Nombre = "Laura Benítez", Correo = "recepcion@vitalia.uy", Rol = Recepcion, HashContrasena = hash },
            new() { Id = 2, Nombre = "Dra. Lucía Ferreira", Correo = "ferreira@vitalia.uy", Rol = Veterinario, ProfesionalId = 1, HashContrasena = hash },
            new() { Id = 3, Nombre = "Dr. Martín Silveira", Correo = "silveira@vitalia.uy", Rol = Veterinario, ProfesionalId = 2, HashContrasena = hash },
            new() { Id = 4, Nombre = "Dra. Valentina Acosta", Correo = "acosta@vitalia.uy", Rol = Veterinario, ProfesionalId = 3, HashContrasena = hash },
            new() { Id = 5, Nombre = "Gonzalo Rivas", Correo = "admin@vitalia.uy", Rol = Administrador, HashContrasena = hash }
        };
    }

    public List<Personal> ObtenerTodos()
    {
        return _personal;
    }

    public Personal? ValidarCredenciales(string correo, string contrasena)
    {
        var buscado = (correo ?? "").Trim().ToLowerInvariant();
        var persona = _personal.FirstOrDefault(p => p.Correo == buscado);
        return persona is not null && CuentaService.ContrasenaCoincide(contrasena, persona.HashContrasena) ? persona : null;
    }
}
