namespace Veterinaria.Blazor.Services;

// Guarda qué quería hacer el visitante antes de ingresar, para retomarlo después (A-011, D-REG-05).
// Solo guarda el servicio elegido y la hora; dura 30 minutos.
public class IntencionService
{
    private const int MinutosVigencia = 30;

    private readonly SesionService _sesion;
    private int? _servicioId;
    private DateTime _guardada;

    public IntencionService(SesionService sesion)
    {
        _sesion = sesion;
    }

    // Decide a dónde ir al tocar «Reservar turno».
    public string DestinoParaReservar(int servicioId)
    {
        if (_sesion.Iniciada)
        {
            return $"servicios/{servicioId}?reservar=true";
        }

        // Acción protegida: se guarda la intención y se pide ingresar o registrarse (RF-VIS-07).
        _servicioId = servicioId;
        _guardada = DateTime.Now;
        return "acceso";
    }

    // Servicio pendiente si sigue vigente; si pasaron 30 minutos se descarta (REG-09).
    public int? ServicioPendiente()
    {
        if (_servicioId is not null && DateTime.Now - _guardada > TimeSpan.FromMinutes(MinutosVigencia))
        {
            _servicioId = null;
        }

        return _servicioId;
    }

    // A dónde ir después de ingresar o verificar:
    // al servicio pendiente; al catálogo si la intención venció; al inicio si no había ninguna.
    public string DestinoDespuesDeIngresar()
    {
        var habiaIntencion = _servicioId is not null;
        var servicio = ServicioPendiente();
        _servicioId = null;

        if (servicio is not null)
        {
            return $"servicios/{servicio}?reservar=true";
        }

        return habiaIntencion ? "servicios" : "";
    }
}
