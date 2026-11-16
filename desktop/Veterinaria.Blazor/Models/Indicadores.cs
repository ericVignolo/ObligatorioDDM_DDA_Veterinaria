namespace Veterinaria.Blazor.Models;

// Filas de los indicadores de gestión (A-027 parte 2, RF-ADM-07).

public class OcupacionProfesional
{
    public string Profesional { get; set; } = "";
    public int MinutosOcupados { get; set; }
    public int MinutosDisponibles { get; set; }

    // 0 a 100; 0 si ese día no atiende.
    public int Porcentaje { get; set; }
}

public class IngresoServicio
{
    public string Servicio { get; set; } = "";
    public int Cobros { get; set; }
    public decimal Total { get; set; }
}

public class ResumenAusencias
{
    public int Turnos { get; set; }
    public int Atendidos { get; set; }
    public int Ausentes { get; set; }
    public int Cancelados { get; set; }

    // Ausentes sobre turnos que ya debían ocurrir (atendidos + ausentes), 0 a 100.
    public int PorcentajeAusencias { get; set; }
}

public class RotacionArticulo
{
    public string Articulo { get; set; } = "";
    public string Unidad { get; set; } = "";
    public int Consumo { get; set; }
    public int Stock { get; set; }

    // Días que alcanza el stock al ritmo de consumo; null si no hubo consumo.
    public int? DiasCobertura { get; set; }
}
