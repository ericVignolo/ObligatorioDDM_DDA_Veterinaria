namespace Veterinaria.Blazor.Models;

public class Prestacion
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal PrecioDesde { get; set; }
    public bool ReservableEnLinea { get; set; }

    // Duración del turno en minutos; solo para los servicios que se reservan en línea (A-014).
    public int DuracionMinutos { get; set; }
    public bool Publicado { get; set; } = true;

    // Cirugía o procedimiento con anestesia: exige consentimiento informado firmado (RF-CLI-11, RN-10, A-025).
    public bool RequiereConsentimiento { get; set; }
}