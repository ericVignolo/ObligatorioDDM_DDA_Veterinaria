using System.ComponentModel.DataAnnotations;
using Veterinaria.Blazor.Models;

namespace Veterinaria.Blazor.Services;

// Cargos, cobros presenciales y comprobantes de demostración en memoria (A-023, RF-REC-05, RN-32 a RN-34).
// No hay pago en línea: el cliente paga en Recepción (decisión de Eric, 2026-11-16, DT-ALC-02).
// En la sección C lo reemplaza CobroApiService: la API genera el cargo en la misma transacción que la firma
// (RNF-CON-03) y numera los comprobantes con una secuencia de la base.
public class CobroService
{
    public const string Efectivo = "Efectivo";
    public const string Debito = "Débito";
    public const string Credito = "Crédito";
    public const string Transferencia = "Transferencia";

    public const string AvisoDemo = "Demostración: los cargos y comprobantes viven solo mientras la aplicación está abierta. En la versión final los guarda la API.";

    private readonly AgendaService _agenda;
    private readonly PrestacionService _prestaciones;
    private readonly List<Cargo> _cargos = new();
    private readonly List<Pago> _pagos = new();
    private readonly List<NotaCredito> _notas = new();
    private int _ultimoCargo;
    private int _ultimoPago;
    private int _ultimaNota;

    public CobroService(AgendaService agenda, PrestacionService prestaciones)
    {
        _agenda = agenda;
        _prestaciones = prestaciones;
    }

    public List<string> Medios { get; } = new() { Efectivo, Debito, Credito, Transferencia };

    // RN-32: solo una consulta firmada genera cargo. Se llama al firmar; si ya existe el cargo de esa consulta
    // lo devuelve (idempotente: un reintento no duplica el cargo). Una consulta sin turno (historia anterior) no cobra.
    public Cargo? GenerarCargo(Consulta consulta)
    {
        if (consulta.Estado != "Firmada" || consulta.TurnoId == 0)
        {
            return null;
        }

        var existente = _cargos.FirstOrDefault(c => c.ConsultaId == consulta.Id);
        if (existente is not null)
        {
            return existente;
        }

        var turno = _agenda.ObtenerPorId(consulta.TurnoId);
        var prestacion = turno is null ? null : _prestaciones.ObtenerPorId(turno.PrestacionId);
        if (turno is null || prestacion is null)
        {
            return null;
        }

        var cargo = new Cargo
        {
            Id = ++_ultimoCargo,
            ConsultaId = consulta.Id,
            TurnoId = turno.Id,
            ClienteId = turno.ClienteId,
            AnimalId = turno.AnimalId,
            PrestacionId = prestacion.Id,
            Descripcion = prestacion.Nombre,
            // RN-33: el precio de hoy queda fijado en el cargo.
            Monto = prestacion.PrecioDesde,
            Fecha = DateTime.Now
        };
        _cargos.Add(cargo);
        return cargo;
    }

    // RN-58: la asistencia finalizada genera su cargo con el costo informado. Idempotente.
    public Cargo? GenerarCargoAsistencia(SolicitudAsistencia solicitud)
    {
        if (solicitud.Costo <= 0)
        {
            return null;
        }

        var existente = _cargos.FirstOrDefault(c => c.AsistenciaId == solicitud.Id);
        if (existente is not null)
        {
            return existente;
        }

        var cargo = new Cargo
        {
            Id = ++_ultimoCargo,
            AsistenciaId = solicitud.Id,
            ClienteId = solicitud.ClienteId,
            AnimalId = solicitud.AnimalId,
            Descripcion = $"Asistencia a domicilio ({solicitud.Especie}, {solicitud.DistanciaKm} km)",
            Monto = solicitud.Costo,
            Fecha = DateTime.Now
        };
        _cargos.Add(cargo);
        return cargo;
    }

    public Cargo? ObtenerCargo(int id)
    {
        return _cargos.FirstOrDefault(c => c.Id == id);
    }

    public Cargo? CargoDeConsulta(int consultaId)
    {
        return _cargos.FirstOrDefault(c => c.ConsultaId == consultaId);
    }

    // Todos los cargos, del más reciente al más antiguo.
    public List<Cargo> Cargos()
    {
        return _cargos.OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id).ToList();
    }

    // Los cargos de un cliente (RN-18: cada cliente ve solo los suyos).
    public List<Cargo> CargosDe(int clienteId)
    {
        return Cargos().Where(c => c.ClienteId == clienteId).ToList();
    }

    public Pago? ObtenerPago(int id)
    {
        return _pagos.FirstOrDefault(p => p.Id == id);
    }

    // Comprobantes (cobros), del más reciente al más antiguo.
    public List<Pago> Pagos()
    {
        return _pagos.OrderByDescending(p => p.Fecha).ThenByDescending(p => p.Id).ToList();
    }

    public List<Pago> PagosDe(int cargoId)
    {
        return Pagos().Where(p => p.CargoId == cargoId).ToList();
    }

    public List<NotaCredito> Notas()
    {
        return _notas.OrderByDescending(n => n.Fecha).ThenByDescending(n => n.Id).ToList();
    }

    // Un comprobante está anulado si existe su nota de crédito; el comprobante en sí nunca cambia (RN-34).
    public NotaCredito? NotaDe(int pagoId)
    {
        return _notas.FirstOrDefault(n => n.PagoId == pagoId);
    }

    public bool EstaAnulado(int pagoId)
    {
        return NotaDe(pagoId) is not null;
    }

    // Lo cobrado de un cargo, sin contar los comprobantes anulados.
    public decimal Pagado(int cargoId)
    {
        return _pagos.Where(p => p.CargoId == cargoId && !EstaAnulado(p.Id)).Sum(p => p.Monto);
    }

    public decimal Saldo(Cargo cargo)
    {
        return cargo.Monto - Pagado(cargo.Id);
    }

    // Pendiente (nada cobrado), Pago parcial o Pagado.
    public string Estado(Cargo cargo)
    {
        var pagado = Pagado(cargo.Id);
        if (pagado <= 0)
        {
            return "Pendiente";
        }

        return pagado >= cargo.Monto ? "Pagado" : "Pago parcial";
    }

    public decimal TotalPendiente()
    {
        return _cargos.Sum(Saldo);
    }

    // Cobrado hoy en el mostrador, sin los comprobantes anulados.
    public decimal CobradoHoy()
    {
        return _pagos.Where(p => p.Fecha.Date == DateTime.Today && !EstaAnulado(p.Id)).Sum(p => p.Monto);
    }

    // RF-REC-05: solo Recepción cobra. El monto no supera el saldo, no se cobra dos veces un cargo ya pagado
    // y las transferencias llevan su número de operación.
    public ResultadoOperacion Cobrar(CobroFormulario datos, Personal? persona)
    {
        if (persona is null || persona.Rol != PersonalService.Recepcion)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo Recepción registra cobros." };
        }

        if (!Validator.TryValidateObject(datos, new ValidationContext(datos), new List<ValidationResult>(), validateAllProperties: true))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Corregí los campos marcados: monto entre $ 1 y $ 1.000.000 y medio de pago." };
        }

        var cargo = ObtenerCargo(datos.CargoId);
        if (cargo is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ese cargo no existe." };
        }

        if (!Medios.Contains(datos.Medio))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Elegí un medio de pago de la lista." };
        }

        if (datos.Medio == Transferencia && string.IsNullOrWhiteSpace(datos.Referencia))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Para una transferencia escribí el número de operación." };
        }

        var saldo = Saldo(cargo);
        if (saldo <= 0)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Este cargo ya está pagado." };
        }

        if (datos.Monto > saldo)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = $"El monto no puede superar el saldo del cargo ($ {saldo:N0})." };
        }

        var pago = new Pago
        {
            Id = ++_ultimoPago,
            CargoId = cargo.Id,
            Numero = $"C-{_ultimoPago:000000}",
            Monto = datos.Monto,
            Medio = datos.Medio,
            Referencia = datos.Referencia.Trim(),
            Fecha = DateTime.Now,
            PersonalId = persona.Id,
            PersonalNombre = persona.Nombre
        };
        _pagos.Add(pago);

        var queda = Saldo(cargo);
        var detalle = queda <= 0 ? "El cargo quedó pagado." : $"Queda un saldo de $ {queda:N0}.";
        return new ResultadoOperacion { Exito = true, Mensaje = $"Cobro registrado: comprobante {pago.Numero} por $ {pago.Monto:N0}. {detalle}" };
    }

    // RN-34: un comprobante emitido no se edita ni se borra; se anula con una nota de crédito trazable.
    // Solo el Administrador, con motivo. El cargo vuelve a tener saldo y se puede cobrar de nuevo.
    public ResultadoOperacion Anular(int pagoId, AnulacionFormulario datos, Personal? persona)
    {
        if (persona is null || persona.Rol != PersonalService.Administrador)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Solo el Administrador anula comprobantes." };
        }

        if (!Validator.TryValidateObject(datos, new ValidationContext(datos), new List<ValidationResult>(), validateAllProperties: true))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Escribí el motivo de la anulación (hasta 200 caracteres)." };
        }

        var pago = ObtenerPago(pagoId);
        if (pago is null)
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ese comprobante no existe." };
        }

        if (EstaAnulado(pago.Id))
        {
            return new ResultadoOperacion { Exito = false, Mensaje = "Ese comprobante ya está anulado." };
        }

        var nota = new NotaCredito
        {
            Id = ++_ultimaNota,
            Numero = $"NC-{_ultimaNota:000000}",
            PagoId = pago.Id,
            Monto = pago.Monto,
            Motivo = datos.Motivo.Trim(),
            Fecha = DateTime.Now,
            PersonalId = persona.Id,
            PersonalNombre = persona.Nombre
        };
        _notas.Add(nota);

        return new ResultadoOperacion { Exito = true, Mensaje = $"Comprobante {pago.Numero} anulado con la nota de crédito {nota.Numero}." };
    }
}
