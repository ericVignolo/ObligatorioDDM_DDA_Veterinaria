using System.ComponentModel.DataAnnotations;

namespace Veterinaria.Api.Models;

// Datos públicos de la clínica ficticia VITALIA (RF-VIS-03, RF-VIS-06). Una sola fila.
public class Clinica
{
    public int Id { get; set; }
    [MaxLength(80)] public string Nombre { get; set; } = "";
    [MaxLength(120)] public string RazonSocial { get; set; } = "";
    [MaxLength(150)] public string Lema { get; set; } = "";
    [MaxLength(1000)] public string Presentacion { get; set; } = "";
    [MaxLength(150)] public string Direccion { get; set; } = "";
    [MaxLength(80)] public string Ciudad { get; set; } = "";
    [MaxLength(30)] public string Telefono { get; set; } = "";
    [MaxLength(30)] public string TelefonoGuardia { get; set; } = "";
    [MaxLength(150)] public string Correo { get; set; } = "";
    [MaxLength(150)] public string Horario { get; set; } = "";
    [MaxLength(250)] public string Cobertura { get; set; } = "";
}

// Parámetros que configura el Administrador (RF-ADM-10): anticipación, horizonte, cancelación, etc.
public class Parametro
{
    [MaxLength(60)] public string Clave { get; set; } = "";
    [MaxLength(100)] public string Valor { get; set; } = "";
    [MaxLength(250)] public string Descripcion { get; set; } = "";
}

// Feriados y días sin atención de toda la clínica.
public class DiaNoLaborable
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    [MaxLength(120)] public string Motivo { get; set; } = "";
}

// Auditoría de acciones relevantes (RNF de auditoría). Sin contraseñas, códigos, tokens ni texto clínico.
public class Auditoria
{
    public long Id { get; set; }
    public DateTime Fecha { get; set; }

    public int? CuentaId { get; set; }

    [MaxLength(60)] public string Accion { get; set; } = "";
    [MaxLength(60)] public string Entidad { get; set; } = "";
    [MaxLength(40)] public string EntidadId { get; set; } = "";
    [MaxLength(60)] public string Origen { get; set; } = "";
    [MaxLength(500)] public string Detalle { get; set; } = "";
}
