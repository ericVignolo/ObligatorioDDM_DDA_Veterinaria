using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs;

// ---------- Comunicaciones (RF-CLI-18, RF-CLI-20, RF-REC-07, RNF-CON-04, RNF-CON-10) ----------

public class ComunicacionReadDto
{
    public long Id { get; set; }
    public int TipoId { get; set; }
    public string Tipo { get; set; } = "";
    public string Asunto { get; set; } = "";
    public string Cuerpo { get; set; } = "";

    // "Correo" o "En la app".
    public string Canal { get; set; } = "";

    // "Pendiente", "Enviado" o "Fallido".
    public string Estado { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaEnvio { get; set; }
    public bool Leida { get; set; }

    // Solo para el personal.
    public int CuentaId { get; set; }
    public string Cliente { get; set; } = "";
    public string Destino { get; set; } = "";
    public int Intentos { get; set; }
    public string UltimoError { get; set; } = "";
    public long? ReenvioDeId { get; set; }
}

public class BandejaDto
{
    public List<ComunicacionReadDto> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int NoLeidas { get; set; }
}

public class PreferenciaReadDto
{
    public int TipoId { get; set; }
    public string Tipo { get; set; } = "";
    public string Descripcion { get; set; } = "";

    // Un tipo esencial no se puede apagar.
    public bool Esencial { get; set; }

    // "Correo", "Solo en la app" o "No recibir".
    public string Canal { get; set; } = "";
}

public class PreferenciaUpdateDto
{
    [Required(ErrorMessage = "Elegí un canal.")]
    public string Canal { get; set; } = "";
}

public class DifusionDto
{
    [Required(ErrorMessage = "Completá el título (hasta 120 caracteres) y el mensaje (hasta 1000).")]
    [StringLength(120, ErrorMessage = "Completá el título (hasta 120 caracteres) y el mensaje (hasta 1000).")]
    public string Titulo { get; set; } = "";

    [Required(ErrorMessage = "Completá el título (hasta 120 caracteres) y el mensaje (hasta 1000).")]
    [StringLength(1000, ErrorMessage = "Completá el título (hasta 120 caracteres) y el mensaje (hasta 1000).")]
    public string Mensaje { get; set; } = "";

    // Solo en la alerta sanitaria: para los clientes con animales de esa especie; null = todos.
    public int? EspecieId { get; set; }
}

public class DifusionResultadoDto
{
    public int Generados { get; set; }
    public int Omitidos { get; set; }
    public string Mensaje { get; set; } = "";
}

public class ColaProcesadaDto
{
    public int Intentados { get; set; }
    public int Enviados { get; set; }
    public int Fallidos { get; set; }
}
