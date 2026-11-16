namespace Veterinaria.Blazor.Models;

// Cargo por una prestación atendida (A-023, RN-32, RN-33). Lo genera la firma de la consulta; el cobro es un paso aparte.
// El monto es el precio vigente al firmar y no cambia si después se modifica la tarifa (RN-33).
// Importes en pesos enteros.
public class Cargo
{
    public int Id { get; set; }
    public int ConsultaId { get; set; }
    public int TurnoId { get; set; }
    public int ClienteId { get; set; }
    public int AnimalId { get; set; }
    public int PrestacionId { get; set; }
    public string Descripcion { get; set; } = "";
    public decimal Monto { get; set; }
    public DateTime Fecha { get; set; }
}
