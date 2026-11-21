using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs;

// ---------- Asistencia veterinaria (A-026, ESPECIFICACION_ALERTAS_ASISTENCIA, RN-46 a RN-59) ----------

public class SolicitudAsistenciaReadDto
{
    public int Id { get; set; }
    public int ClienteId { get; set; }
    public string Cliente { get; set; } = "";
    public int? AnimalId { get; set; }
    public string Animal { get; set; } = "";
    public string Especie { get; set; } = "";
    public int Cantidad { get; set; }
    public string Sintomas { get; set; } = "";
    public string Modalidad { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Referencias { get; set; } = "";
    public bool UbicacionGps { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }
    public int DistanciaKm { get; set; }
    public string TelefonoContacto { get; set; } = "";
    public string Canal { get; set; } = "";
    public string NivelCliente { get; set; } = "";

    // El nivel nunca va solo por color: nombre y descripción (sección 4).
    public string Nivel { get; set; } = "";
    public string DescripcionNivel { get; set; } = "";
    public string Estado { get; set; } = "";
    public DateTime FechaCreacion { get; set; }
    public int? VeterinarioId { get; set; }
    public string Veterinario { get; set; } = "";
    public DateTime? FechaAceptacion { get; set; }

    // RN-53: solo después de aceptar.
    public int? EtaMinutos { get; set; }
    public decimal Costo { get; set; }
    public bool ReevaluacionPedida { get; set; }
    public string MotivoCoordinacion { get; set; } = "";
    public string MotivoCierre { get; set; } = "";
    public string Resultado { get; set; } = "";

    // RN-48: fuera del horario, lo no rojo y sin aceptar queda para el próximo día.
    public bool ParaProximoDia { get; set; }

    // RN-57: el veterinario comparte su ubicación solo «En camino».
    public bool CompartiendoUbicacion { get; set; }

    // Solo para el personal: a quiénes está ofrecida.
    public List<string> OfrecidaA { get; set; } = new();
    public int Version { get; set; }
    public List<EventoAsistenciaReadDto> Eventos { get; set; } = new();
    public string Mensaje { get; set; } = "";
}

public class EventoAsistenciaReadDto
{
    public long Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Tipo { get; set; } = "";
    public string Texto { get; set; } = "";
    public string Autor { get; set; } = "";
    public string NivelAnterior { get; set; } = "";
    public string NivelNuevo { get; set; } = "";
}

// Datos comunes de una solicitud nueva.
public class SolicitudAsistenciaBaseDto
{
    // 0 o null = animal no registrado o grupo: se indica la especie.
    public int? AnimalId { get; set; }
    public int? EspecieId { get; set; }

    [Range(1, 1000, ErrorMessage = "La cantidad debe estar entre 1 y 1000.")]
    public int Cantidad { get; set; } = 1;

    [Required(ErrorMessage = "Describí los síntomas.")]
    [StringLength(1000, ErrorMessage = "Los síntomas pueden tener hasta 1000 caracteres.")]
    public string Sintomas { get; set; } = "";

    // Gravedad que percibe el cliente: Rojo, Naranja, Amarillo o Verde.
    [Required(ErrorMessage = "Elegí la gravedad que percibe el cliente.")]
    public string Nivel { get; set; } = "";

    // "Visita" u "Orientación y traslado".
    [Required(ErrorMessage = "Elegí la modalidad.")]
    public string Modalidad { get; set; } = "";

    [StringLength(200, ErrorMessage = "La dirección puede tener hasta 200 caracteres.")]
    public string Direccion { get; set; } = "";

    [StringLength(300, ErrorMessage = "Las referencias pueden tener hasta 300 caracteres.")]
    public string Referencias { get; set; } = "";

    // Distancia aproximada desde la clínica (la carga Recepción o la estima el mapa del móvil).
    [Range(0, 500, ErrorMessage = "La distancia debe estar entre 0 y 500 km.")]
    public int DistanciaKm { get; set; }

    [Required(ErrorMessage = "Escribí un teléfono de contacto.")]
    [StringLength(30, ErrorMessage = "El teléfono puede tener hasta 30 caracteres.")]
    public string TelefonoContacto { get; set; } = "";

    // Evita duplicar la solicitud si se envía dos veces (prueba 10 de la especificación).
    [StringLength(80, ErrorMessage = "La clave puede tener hasta 80 caracteres.")]
    public string ClaveIdempotencia { get; set; } = "";
}

// Recepción registra la que llega por teléfono.
public class SolicitudTelefonicaDto : SolicitudAsistenciaBaseDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí el cliente.")]
    public int ClienteId { get; set; }
}

// El cliente la crea desde la app, con GPS solo si lo consiente (RNF-LEG-09).
public class SolicitudAppDto : SolicitudAsistenciaBaseDto
{
    public bool UbicacionGps { get; set; }

    [Range(-90, 90, ErrorMessage = "La latitud no es válida.")]
    public decimal? Latitud { get; set; }

    [Range(-180, 180, ErrorMessage = "La longitud no es válida.")]
    public decimal? Longitud { get; set; }
}

public class AsignacionDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí el veterinario.")]
    public int VeterinarioId { get; set; }
}

public class MotivoAsistenciaDto
{
    [Required(ErrorMessage = "Escribí el motivo del rechazo (hasta 200 caracteres).")]
    [StringLength(200, ErrorMessage = "Escribí el motivo del rechazo (hasta 200 caracteres).")]
    public string Motivo { get; set; } = "";
}

public class ResultadoAtencionDto
{
    [Required(ErrorMessage = "Escribí el resultado de la atención (hasta 1000 caracteres).")]
    [StringLength(1000, ErrorMessage = "Escribí el resultado de la atención (hasta 1000 caracteres).")]
    public string Resultado { get; set; } = "";
}

public class CambioNivelDto
{
    [Required(ErrorMessage = "Elegí el nivel.")]
    public string Nivel { get; set; } = "";

    [Required(ErrorMessage = "Escribí el motivo o lo que informó el cliente.")]
    [StringLength(300, ErrorMessage = "Hasta 300 caracteres.")]
    public string Motivo { get; set; } = "";
}

public class CierreAsistenciaDto
{
    // "Cancelada por la clínica" o "No concretada".
    [Required(ErrorMessage = "Elegí cómo se cierra.")]
    public string Estado { get; set; } = "";

    [Required(ErrorMessage = "Escribí el motivo y el contacto con el cliente (hasta 300 caracteres).")]
    [StringLength(300, ErrorMessage = "Escribí el motivo y el contacto con el cliente (hasta 300 caracteres).")]
    public string Motivo { get; set; } = "";
}

public class VeterinarioElegibleDto
{
    public int CuentaId { get; set; }
    public string Nombre { get; set; } = "";
    public bool DeGuardia { get; set; }
}

public class GuardiaDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí el veterinario de guardia.")]
    public int CuentaId { get; set; }
}

public class PlazosRevisadosDto
{
    public int RojosEscalados { get; set; }
    public int ReevaluacionesPedidas { get; set; }
}
