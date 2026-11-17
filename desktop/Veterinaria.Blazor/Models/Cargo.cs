namespace Veterinaria.Blazor.Models;

// Cargo por una prestación atendida (A-023, RN-32, RN-33). Lo genera la firma de la consulta; el cobro es un paso aparte.
// El monto es el precio vigente en la fecha del turno y no cambia si después se modifica la tarifa (RN-33, A-027).
// Importes en pesos enteros.
public class Cargo
{
    public int Id { get; set; }
    public int ConsultaId { get; set; }

    // Si el cargo es de una asistencia a domicilio (A-026, RN-58); 0 si es de una consulta.
    public int AsistenciaId { get; set; }
    public int TurnoId { get; set; }
    public int ClienteId { get; set; }
    public int AnimalId { get; set; }
    public int PrestacionId { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }

    // RN-35: seña de una cirugía programada. Se cobra antes del turno y se descuenta del cargo final.
    public bool EsSena { get; set; }
}
