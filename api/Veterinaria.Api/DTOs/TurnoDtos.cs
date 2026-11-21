using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs;

// Reserva del cliente en cuatro pasos (CU-CLI-02, RNF-USA-01). La duración la pone la API según el servicio.
public class ReservaCreateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí el animal.")]
    public int AnimalId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Elegí el servicio.")]
    public int PrestacionId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Elegí el profesional.")]
    public int ProfesionalId { get; set; }

    // Fecha y hora local de la clínica, por ejemplo 2026-11-25T09:00:00.
    [Required(ErrorMessage = "Elegí el horario.")]
    public DateTime? Inicio { get; set; }
}

// Turno dado en el mostrador por Recepción o el Administrador (RF-REC-02, RN-09).
public class ReservaMostradorDto : ReservaCreateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí el cliente.")]
    public int ClienteId { get; set; }

    // Sobreturno: fuera de la grilla o en un horario ya tomado del profesional; exige motivo.
    public bool Sobreturno { get; set; }

    [StringLength(250, ErrorMessage = "El motivo puede tener hasta 250 caracteres.")]
    public string Motivo { get; set; } = "";
}

public class ReprogramacionDto
{
    [Required(ErrorMessage = "Elegí el nuevo horario.")]
    public DateTime? Inicio { get; set; }
}

public class CancelacionDto
{
    [StringLength(250, ErrorMessage = "El motivo puede tener hasta 250 caracteres.")]
    public string Motivo { get; set; } = "";
}

public class DisponibilidadDto
{
    public DateOnly Fecha { get; set; }

    // Horarios de inicio libres de ese día.
    public List<DateTime> Horarios { get; set; } = new();

    // Si el día pedido no tiene horarios, el primero que sí tiene (para no mostrar «sin horarios» sin salida).
    public DateOnly? ProximoDiaConHorarios { get; set; }

    // RN-08: aviso por ausencias sin avisar; vacío si no corresponde.
    public string AvisoAusencias { get; set; } = "";
}

public class TurnoReadDto
{
    public int Id { get; set; }
    public string Estado { get; set; } = "";
    public DateTime Inicio { get; set; }
    public DateTime Fin { get; set; }
    public DateTime? InicioAnterior { get; set; }

    public int ClienteId { get; set; }
    public string Cliente { get; set; } = "";
    public int AnimalId { get; set; }
    public string Animal { get; set; } = "";
    public string Especie { get; set; } = "";
    public int PrestacionId { get; set; }
    public string Prestacion { get; set; } = "";
    public int ProfesionalId { get; set; }
    public string Profesional { get; set; } = "";

    public bool EsSobreturno { get; set; }
    public DateTime? HoraLlegada { get; set; }
    public string MotivoCancelacion { get; set; } = "";
    public string CanceladoPor { get; set; } = "";

    // Para el cliente: si todavía puede cancelar o reprogramar por su cuenta (más de 24 h antes, RN-06).
    public bool PuedeGestionar { get; set; }
}

// Cliente encontrado por Recepción, con sus animales activos, para dar un turno en el mostrador.
public class ClienteBusquedaDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string Documento { get; set; } = "";
    public int AusenciasRecientes { get; set; }
    public List<AnimalResumenDto> Animales { get; set; } = new();
}

public class AnimalResumenDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Especie { get; set; } = "";
}
