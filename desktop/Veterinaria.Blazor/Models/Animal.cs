namespace Veterinaria.Blazor.Models;

// Ficha de un animal o de un grupo de animales (A-012, RF-CLI-01).
// En la sección C la guarda la API; el cliente solo ve los suyos (RN-18).
public class Animal
{
    public int Id { get; set; }

    // Cuenta del responsable registrado.
    public int ResponsableId { get; set; }

    public string Nombre { get; set; } = "";
    public string Especie { get; set; } = "";
    public string Raza { get; set; } = "";
    public string Sexo { get; set; } = "";
    public DateOnly? FechaNacimiento { get; set; }

    // Identificadores permitidos: microchip (único, RN-41) y caravana de campo.
    public string Microchip { get; set; } = "";
    public string Caravana { get; set; } = "";

    // Para animales de campo que se atienden en grupo (por ejemplo, una majada).
    public bool EsGrupo { get; set; }
    public int? CantidadGrupo { get; set; }

    public string SenasParticulares { get; set; } = "";
    public string Antecedentes { get; set; } = "";

    // Foto para reconocerlo en la ficha y en la consulta (RF-CLI-01). Vacía si no tiene.
    // En la demostración es la imagen achicada en formato data URL; en la sección C la guarda la API y esto es su dirección.
    public string Foto { get; set; } = "";

    public DateTime FechaAlta { get; set; }
}
