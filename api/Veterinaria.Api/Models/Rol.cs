using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Cliente, Recepcionista, Veterinario y Administrador. Una persona puede tener varios (CuentaRol).
public class Rol
{
    public int Id { get; set; }
    [MaxLength(30)] public string Nombre { get; set; } = "";
}

public class CuentaRol
{
    public int CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }

    public int RolId { get; set; }
    public Rol? Rol { get; set; }
}
