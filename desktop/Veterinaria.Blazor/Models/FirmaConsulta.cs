using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Blazor.Models;

// Para firmar una consulta o una enmienda el veterinario vuelve a ingresar su contraseña (P-RC-06).
public class FirmaConsulta
{
    [Required(ErrorMessage = "Ingresá tu contraseña para firmar.")]
    public string Contrasena { get; set; } = "";
}
