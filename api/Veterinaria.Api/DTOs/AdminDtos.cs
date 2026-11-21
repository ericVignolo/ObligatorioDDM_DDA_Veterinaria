using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.DTOs;

// ---------- Administración (RF-ADM-01 a RF-ADM-04, RN-33) ----------

// Servicios: el Administrador ve también los no publicados.
public class ServicioAdminDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public decimal PrecioVigente { get; set; }
    public bool ReservableEnLinea { get; set; }
    public int DuracionMinutos { get; set; }
    public bool Publicado { get; set; }
    public bool RequiereConsentimiento { get; set; }
}

public class ServicioFormDto
{
    [Required(ErrorMessage = "Ingresá el nombre.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "El nombre tiene que tener entre 3 y 120 caracteres.")]
    public string Nombre { get; set; } = "";

    [StringLength(500, ErrorMessage = "La descripción puede tener hasta 500 caracteres.")]
    public string Descripcion { get; set; } = "";

    // Solo al crear: después el precio cambia con una tarifa programada (RN-33).
    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "El precio tiene que estar entre $ 1 y $ 1.000.000.")]
    public decimal Precio { get; set; }

    public bool ReservableEnLinea { get; set; }

    [Range(0, 480, ErrorMessage = "La duración puede ser de hasta 480 minutos.")]
    public int DuracionMinutos { get; set; }

    public bool Publicado { get; set; } = true;
    public bool RequiereConsentimiento { get; set; }
}

public class TarifaReadDto
{
    public int Id { get; set; }
    public int PrestacionId { get; set; }
    public decimal Precio { get; set; }
    public DateOnly VigenteDesde { get; set; }
    public bool Vigente { get; set; }
    public string RegistradoPor { get; set; } = "";
}

public class TarifaCreateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí el servicio.")]
    public int PrestacionId { get; set; }

    [Range(typeof(decimal), "1", "1000000", ErrorMessage = "El precio tiene que estar entre $ 1 y $ 1.000.000.")]
    public decimal Precio { get; set; }

    [Required(ErrorMessage = "Elegí la fecha.")]
    public DateOnly? VigenteDesde { get; set; }
}

// Personal: cuentas con rol de Recepción, Veterinario o Administrador.
public class PersonalReadDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Correo { get; set; } = "";
    public string Rol { get; set; } = "";
    public int? ProfesionalId { get; set; }
    public string Profesional { get; set; } = "";
    public bool Activa { get; set; }
}

public class PersonalCreateDto
{
    [Required(ErrorMessage = "Ingresá el nombre.")]
    [StringLength(80, ErrorMessage = "El nombre puede tener hasta 80 caracteres.")]
    public string Nombre { get; set; } = "";

    [Required(ErrorMessage = "Ingresá el apellido.")]
    [StringLength(80, ErrorMessage = "El apellido puede tener hasta 80 caracteres.")]
    public string Apellido { get; set; } = "";

    [Required(ErrorMessage = "Ingresá el correo.")]
    [EmailAddress(ErrorMessage = "El correo no es válido.")]
    [StringLength(150, ErrorMessage = "El correo puede tener hasta 150 caracteres.")]
    public string Correo { get; set; } = "";

    [Required(ErrorMessage = "Elegí el rol.")]
    public string Rol { get; set; } = "";

    // Solo para Veterinario: su ficha de profesional (matrícula, agenda y especies).
    public int? ProfesionalId { get; set; }

    // Contraseña inicial; la persona la cambia después desde su cuenta.
    [Required(ErrorMessage = "Ingresá una contraseña inicial.")]
    [StringLength(64, MinimumLength = 8, ErrorMessage = "La contraseña debe tener entre 8 y 64 caracteres.")]
    public string Contrasena { get; set; } = "";
}

public class CambioRolDto
{
    [Required(ErrorMessage = "Elegí el rol.")]
    public string Rol { get; set; } = "";

    public int? ProfesionalId { get; set; }
}

public class BloqueoDto
{
    public bool Bloquear { get; set; }
}

// Ausencias y feriados (RF-ADM-04).
public class AusenciaReadDto
{
    public int Id { get; set; }
    public int ProfesionalId { get; set; }
    public string Profesional { get; set; } = "";
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public string Motivo { get; set; } = "";
}

public class AusenciaCreateDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Elegí el profesional.")]
    public int ProfesionalId { get; set; }

    [Required(ErrorMessage = "Elegí la fecha de inicio.")]
    public DateOnly? Desde { get; set; }

    [Required(ErrorMessage = "Elegí la fecha de fin.")]
    public DateOnly? Hasta { get; set; }

    [Required(ErrorMessage = "Escribí el motivo.")]
    [StringLength(120, MinimumLength = 3, ErrorMessage = "El motivo tiene que tener entre 3 y 120 caracteres.")]
    public string Motivo { get; set; } = "";
}

public class FeriadoReadDto
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public string Motivo { get; set; } = "";
}

public class FeriadoCreateDto
{
    [Required(ErrorMessage = "Elegí una fecha de hoy en adelante.")]
    public DateOnly? Fecha { get; set; }

    [StringLength(120, ErrorMessage = "El motivo puede tener hasta 120 caracteres.")]
    public string Motivo { get; set; } = "";
}

// Parámetros.
public class ParametroReadDto
{
    public string Clave { get; set; } = "";
    public string Valor { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int Minimo { get; set; }
    public int Maximo { get; set; }
}

public class ParametroUpdateDto
{
    [Required(ErrorMessage = "Ingresá el valor.")]
    [StringLength(100, ErrorMessage = "El valor puede tener hasta 100 caracteres.")]
    public string Valor { get; set; } = "";
}

public class StockMinimoDto
{
    [Range(0, 100000, ErrorMessage = "El mínimo tiene que estar entre 0 y 100.000.")]
    public int StockMinimo { get; set; }
}

public class AuditoriaReadDto
{
    public long Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Persona { get; set; } = "";
    public string Rol { get; set; } = "";
    public string Accion { get; set; } = "";
    public string Entidad { get; set; } = "";
    public string EntidadId { get; set; } = "";
    public string Detalle { get; set; } = "";
}

public class OperacionDto
{
    public string Mensaje { get; set; } = "";
}
