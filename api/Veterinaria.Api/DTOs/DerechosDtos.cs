namespace Veterinaria.Api.DTOs;

// ---------- Derechos del titular (A-028, RF-CLI-19, RNF-LEG-01, RNF-LEG-02, RNF-LEG-06) ----------

public class PoliticaConservacionDto
{
    public string Categoria { get; set; } = "";
    public string Finalidad { get; set; } = "";
    public string Acceso { get; set; } = "";
    public string Conservacion { get; set; } = "";
    public string AlEliminar { get; set; } = "";
}

public class SolicitudDerechoReadDto
{
    public int Id { get; set; }
    public int CuentaId { get; set; }
    public string Titular { get; set; } = "";

    // "Exportación" o "Eliminación".
    public string Tipo { get; set; } = "";

    // "Recibida", "Completada", "Rechazada" o "Cancelada".
    public string Estado { get; set; } = "";
    public DateTime Fecha { get; set; }
    public DateTime? FechaResolucion { get; set; }
    public string ResueltaPor { get; set; } = "";
    public string Detalle { get; set; } = "";
    public string Mensaje { get; set; } = "";
}

public class PedidoEliminacionDto
{
    // Tiene que venir en true: la eliminación no se puede deshacer.
    public bool Confirmado { get; set; }
}

public class ResolucionDerechoDto
{
    public bool Aprobar { get; set; }

    // Obligatorio para rechazar.
    public string Motivo { get; set; } = "";
}

public class ObstaculosDto
{
    public bool SePuedeEliminar { get; set; }
    public List<string> Obstaculos { get; set; } = new();
}
