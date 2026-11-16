using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Especie configurable: cualquier especie, no solo perros y gatos.
public class Especie
{
    public int Id { get; set; }
    [MaxLength(40)] public string Nombre { get; set; } = "";
}

// Ficha de un animal o de un grupo de animales (RF-CLI-01).
public class Animal
{
    public int Id { get; set; }

    [MaxLength(80)] public string Nombre { get; set; } = "";

    public int EspecieId { get; set; }
    public Especie? Especie { get; set; }

    [MaxLength(80)] public string Raza { get; set; } = "";
    [MaxLength(20)] public string Sexo { get; set; } = "";

    // Columna DATE. En Blazor es DateOnly; se convierte en el Service.
    public DateTime? FechaNacimiento { get; set; }

    // 15 dígitos, único (RN-41). Null si no tiene: MySQL admite varios NULL en un índice único.
    [MaxLength(15)] public string? Microchip { get; set; }
    [MaxLength(30)] public string Caravana { get; set; } = "";

    // Para animales de campo que se atienden en grupo (por ejemplo, una majada).
    public bool EsGrupo { get; set; }
    public int? CantidadGrupo { get; set; }

    [MaxLength(500)] public string SenasParticulares { get; set; } = "";
    [MaxLength(2000)] public string Antecedentes { get; set; } = "";

    public DateTime FechaAlta { get; set; }
    public bool Activo { get; set; } = true;

    public List<Responsabilidad> Responsables { get; set; } = new();
}

// Cliente ↔ Animal con vigencia (modelo de dominio de FLUJO_RESERVA_CONSULTA §7).
// La API autoriza por la relación vigente, no por un Id que manda la pantalla (RN-18).
public class Responsabilidad
{
    public int Id { get; set; }

    public int CuentaId { get; set; }
    public Cuenta? Cuenta { get; set; }

    public int AnimalId { get; set; }
    public Animal? Animal { get; set; }

    public DateTime Desde { get; set; }
    public DateTime? Hasta { get; set; }
}
