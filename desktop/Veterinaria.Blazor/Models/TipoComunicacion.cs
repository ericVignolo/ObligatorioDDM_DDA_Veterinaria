namespace Veterinaria.Blazor.Models;

// Catálogo de tipos de comunicación con su finalidad (RNF-LEG-06) y si el cliente puede dejar de recibirla.
public class TipoComunicacion
{
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";

    // Esencial: parte del servicio contratado; se puede elegir correo o solo la app, pero no dejar de recibirla.
    public bool Esencial { get; set; }

    // Canal que rige si el cliente no eligió nada. Los opcionales arrancan sin recibir (RN-43: nada premarcado).
    public string CanalPorDefecto { get; set; } = "";
}
