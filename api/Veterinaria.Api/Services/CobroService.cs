using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Cobro, igual a CobroService de Blazor. Solo Recepción cobra; el monto no pasa el saldo; las transferencias llevan
// el número de operación; un comprobante no se cambia y se anula con nota de crédito del Administrador (RN-34).
// Cobro y anulación bloquean el cargo y el numerador, así dos pedidos a la vez no pasan el saldo ni repiten número.
public class CobroService : ICobroService
{
    public const string Efectivo = "Efectivo";
    public const string Debito = "Débito";
    public const string Credito = "Crédito";
    public const string Transferencia = "Transferencia";
    public static readonly string[] Medios = { Efectivo, Debito, Credito, Transferencia };

    public const string NumeradorComprobante = "Comprobante";
    public const string NumeradorNotaCredito = "NotaCredito";

    // Valores por defecto si la tabla Parametros no los tiene (decisión de Eric, 2026-11-17: seña del 30 %).
    public const int PorcentajeSenaPorDefecto = 30;
    public const int DiasParaVencer = 30;
    public const decimal LimiteDeudaPorDefecto = 3000;

    private readonly ICobroRepository _repository;
    private readonly TimeProvider _reloj;

    public CobroService(ICobroRepository repository, TimeProvider reloj)
    {
        _repository = repository;
        _reloj = reloj;
    }

    private DateTime Ahora => _reloj.GetLocalNow().DateTime;

    public static string Estado(decimal monto, decimal pagado) => pagado <= 0 ? "Pendiente" : pagado >= monto ? "Pagado" : "Pago parcial";

    // ===================== Cargos =====================

    public async Task<PaginaDto<CargoReadDto>> MisCargosAsync(int cuentaId, int page, int pageSize)
    {
        await GenerarSenasAsync(cuentaId);
        return Paginar(await ListaAsync(cuentaId), page, pageSize);
    }

    public async Task<PaginaDto<CargoReadDto>> CargosAsync(int? clienteId, bool soloConSaldo, int page, int pageSize)
    {
        await GenerarSenasAsync(clienteId);
        var lista = await ListaAsync(clienteId);
        return Paginar(soloConSaldo ? lista.Where(c => c.Saldo > 0).ToList() : lista, page, pageSize);
    }

    public async Task<Resultado<CargoReadDto>> CargoAsync(int cargoId)
    {
        var cargo = await _repository.ObtenerCargoAsync(cargoId);
        if (cargo == null)
        {
            return Resultado<CargoReadDto>.Error(404, "CARGO_NO_ENCONTRADO", "No encontrado", "Ese cargo no existe.");
        }

        return Resultado<CargoReadDto>.Ok(await DetalleAsync(cargo));
    }

    public async Task<DeudaClienteDto> DeudaAsync(int clienteId)
    {
        var parametros = await _repository.ObtenerParametrosAsync();
        var limite = parametros.TryGetValue("cobro.limite_deuda", out var valor) && decimal.TryParse(valor, out var l) && l >= 0 ? l : LimiteDeudaPorDefecto;
        var vencimiento = Ahora.AddDays(-DiasParaVencer);

        // Las señas no son deuda: el turno todavía no se confirmó.
        var deuda = (await ListaAsync(clienteId)).Where(c => !c.EsSena && c.Fecha <= vencimiento).Sum(c => c.Saldo);
        return new DeudaClienteDto
        {
            ClienteId = clienteId,
            DeudaVencida = deuda,
            Aviso = deuda > limite
                ? $"Hay $ {deuda:N0} de saldo vencido (más de {DiasParaVencer} días). Se puede agendar igual; conviene regularizarlo en Recepción."
                : ""
        };
    }

    // ===================== Cobro (Recepción) =====================

    public async Task<Resultado<CobroRegistradoDto>> CobrarAsync(int personaId, CobroCreateDto datos)
    {
        if (!Medios.Contains(datos.Medio))
        {
            return Error<CobroRegistradoDto>(400, "MEDIO_INVALIDO", "Elegí un medio de pago de la lista.");
        }

        if (datos.Medio == Transferencia && string.IsNullOrWhiteSpace(datos.Referencia))
        {
            return Error<CobroRegistradoDto>(400, "FALTA_REFERENCIA", "Para una transferencia escribí el número de operación.");
        }

        if (await _repository.ObtenerCargoAsync(datos.CargoId) == null)
        {
            return Error<CobroRegistradoDto>(404, "CARGO_NO_ENCONTRADO", "Ese cargo no existe.");
        }

        return await _repository.EnTransaccionAsync(datos.CargoId, NumeradorComprobante, async () =>
        {
            var cargo = (await _repository.ObtenerCargoAsync(datos.CargoId))!;
            var saldo = cargo.Monto - await PagadoAsync(cargo.Id);
            if (saldo <= 0)
            {
                return Error<CobroRegistradoDto>(409, "CARGO_PAGADO", "Este cargo ya está pagado.");
            }

            if (datos.Monto > saldo)
            {
                return Error<CobroRegistradoDto>(400, "MONTO_MAYOR_AL_SALDO", $"El monto no puede superar el saldo del cargo ($ {saldo:N0}).");
            }

            var numero = await _repository.SiguienteNumeroAsync(NumeradorComprobante);
            var pago = new Pago
            {
                CargoId = cargo.Id,
                Numero = $"C-{numero:000000}",
                Monto = datos.Monto,
                Medio = datos.Medio,
                Referencia = (datos.Referencia ?? "").Trim(),
                Fecha = Ahora,
                CobradoPorId = personaId
            };
            _repository.Agregar(pago);

            var queda = saldo - datos.Monto;
            var detalle = queda <= 0 ? "El cargo quedó pagado." : $"Queda un saldo de $ {queda:N0}.";

            // RN-35: con la seña pagada, la cirugía queda confirmada.
            if (cargo.EsSena && queda <= 0 && cargo.TurnoId is int turnoId)
            {
                var turno = await _repository.ObtenerTurnoAsync(turnoId);
                if (turno != null && turno.Estado == CalculoAgenda.PendienteSena)
                {
                    turno.Estado = "Confirmado";
                    turno.Version++;
                    detalle += " La cirugía quedó confirmada.";
                }
            }

            await _repository.GuardarAsync();

            var cobrador = await _repository.ObtenerCuentaAsync(personaId);
            return new Resultado<CobroRegistradoDto>
            {
                Exito = true,
                Estado = 201,
                Valor = new CobroRegistradoDto
                {
                    Pago = PagoADto(pago, cargo, cobrador, null),
                    Cargo = CargoADto(cargo, cargo.Monto - queda),
                    Mensaje = $"Cobro registrado: comprobante {pago.Numero} por $ {pago.Monto:N0}. {detalle}"
                }
            };
        });
    }

    public async Task<List<PagoReadDto>> ComprobantesDelDiaAsync(DateOnly fecha)
    {
        var desde = fecha.ToDateTime(TimeOnly.MinValue);
        var pagos = await _repository.ObtenerPagosEntreAsync(desde, desde.AddDays(1));
        var notas = await _repository.ObtenerNotasDePagosAsync(pagos.Select(p => p.Id));
        return pagos.Select(p => PagoADto(p, p.Cargo, p.CobradoPor, notas.FirstOrDefault(n => n.PagoId == p.Id))).ToList();
    }

    public async Task<Resultado<PagoReadDto>> ComprobanteAsync(int pagoId)
    {
        var pago = await _repository.ObtenerPagoAsync(pagoId);
        if (pago == null)
        {
            return Error<PagoReadDto>(404, "COMPROBANTE_NO_ENCONTRADO", "Ese comprobante no existe.");
        }

        var nota = (await _repository.ObtenerNotasDePagosAsync(new[] { pago.Id })).FirstOrDefault();
        return Resultado<PagoReadDto>.Ok(PagoADto(pago, pago.Cargo, pago.CobradoPor, nota));
    }

    public async Task<ResumenCajaDto> ResumenAsync(DateOnly fecha)
    {
        var comprobantes = (await ComprobantesDelDiaAsync(fecha)).Where(p => !p.Anulado).ToList();
        return new ResumenCajaDto
        {
            Fecha = fecha,
            CobradoEnElDia = comprobantes.Sum(p => p.Monto),
            Comprobantes = comprobantes.Count,
            TotalPendiente = (await ListaAsync(null)).Sum(c => c.Saldo)
        };
    }

    // ===================== Anulación (Administrador) =====================

    public async Task<Resultado<AnulacionRegistradaDto>> AnularAsync(int personaId, int pagoId, AnulacionDto datos)
    {
        var previo = await _repository.ObtenerPagoAsync(pagoId);
        if (previo == null)
        {
            return Error<AnulacionRegistradaDto>(404, "COMPROBANTE_NO_ENCONTRADO", "Ese comprobante no existe.");
        }

        return await _repository.EnTransaccionAsync(previo.CargoId, NumeradorNotaCredito, async () =>
        {
            var pago = (await _repository.ObtenerPagoAsync(pagoId))!;
            if ((await _repository.ObtenerNotasDePagosAsync(new[] { pago.Id })).Count > 0)
            {
                return Error<AnulacionRegistradaDto>(409, "COMPROBANTE_ANULADO", "Ese comprobante ya está anulado.");
            }

            // Lo cobrado del cargo sin este comprobante, antes de agregar la nota.
            var cargo = pago.Cargo!;
            var pagadoDespues = await PagadoAsync(cargo.Id) - pago.Monto;

            var numero = await _repository.SiguienteNumeroAsync(NumeradorNotaCredito);
            var nota = new NotaCredito
            {
                Numero = $"NC-{numero:000000}",
                PagoId = pago.Id,
                Monto = pago.Monto,
                Motivo = datos.Motivo.Trim(),
                Fecha = Ahora,
                EmitidaPorId = personaId
            };
            _repository.Agregar(nota);
            _repository.Agregar(new Auditoria
            {
                Fecha = Ahora,
                CuentaId = personaId,
                Rol = "Administrador",
                Accion = "Anulación de comprobante",
                Entidad = "Pago",
                EntidadId = pago.Id.ToString(),
                Origen = "API",
                Detalle = $"{pago.Numero} con {nota.Numero}: {nota.Motivo}"
            });

            // RN-35: si se anula la seña, la cirugía vuelve a esperar el pago.
            var extra = "";
            if (cargo.EsSena && cargo.Monto - pagadoDespues > 0 && cargo.TurnoId is int turnoId)
            {
                var turno = await _repository.ObtenerTurnoAsync(turnoId);
                if (turno != null && turno.Estado == "Confirmado")
                {
                    turno.Estado = CalculoAgenda.PendienteSena;
                    turno.Version++;
                    extra = " La cirugía vuelve a quedar pendiente de seña.";
                }
            }

            await _repository.GuardarAsync();

            var emisor = await _repository.ObtenerCuentaAsync(personaId);
            return new Resultado<AnulacionRegistradaDto>
            {
                Exito = true,
                Estado = 201,
                Valor = new AnulacionRegistradaDto
                {
                    Nota = NotaADto(nota, pago.Numero, emisor),
                    Mensaje = $"Comprobante {pago.Numero} anulado con la nota de crédito {nota.Numero}.{extra}"
                }
            };
        });
    }

    public async Task<List<NotaCreditoReadDto>> NotasAsync()
    {
        return (await _repository.ObtenerNotasAsync()).Select(n => NotaADto(n, n.Pago?.Numero ?? "", n.EmitidaPor)).ToList();
    }

    // ===================== Señas (RN-35) =====================

    public async Task<int> GenerarSenasAsync(int? clienteId)
    {
        var turnos = await _repository.ObtenerTurnosSinSenaAsync(clienteId);
        if (turnos.Count == 0)
        {
            return 0;
        }

        var parametros = await _repository.ObtenerParametrosAsync();
        var porcentaje = parametros.TryGetValue("cobro.porcentaje_sena", out var valor) && int.TryParse(valor, out var p) && p is >= 10 and <= 100
            ? p
            : PorcentajeSenaPorDefecto;

        var generadas = 0;
        foreach (var turno in turnos)
        {
            if (turno.Prestacion == null || await _repository.ExisteSenaAsync(turno.Id))
            {
                continue;
            }

            var tarifa = await _repository.ObtenerTarifaVigenteAsync(turno.PrestacionId, turno.Inicio);
            var precio = tarifa?.Precio ?? turno.Prestacion.PrecioDesde;
            _repository.Agregar(new Cargo
            {
                TurnoId = turno.Id,
                ClienteId = turno.ClienteId,
                AnimalId = turno.AnimalId,
                PrestacionId = turno.PrestacionId,
                Descripcion = $"Seña de {turno.Prestacion.Nombre.ToLowerInvariant()} ({porcentaje} %)",
                Monto = ReglasClinicas.RedondearPesos(precio * porcentaje / 100m),
                Fecha = Ahora,
                EsSena = true
            });
            generadas++;
        }

        await _repository.GuardarAsync();
        return generadas;
    }

    // ===================== Apoyo =====================

    private async Task<decimal> PagadoAsync(int cargoId)
        => (await _repository.PagadoPorCargoAsync(new[] { cargoId })).GetValueOrDefault(cargoId);

    private async Task<List<CargoReadDto>> ListaAsync(int? clienteId)
    {
        var cargos = await _repository.ObtenerCargosAsync(clienteId);
        var pagado = await _repository.PagadoPorCargoAsync(cargos.Select(c => c.Id));
        return cargos.Select(c => CargoADto(c, pagado.GetValueOrDefault(c.Id))).ToList();
    }

    private async Task<CargoReadDto> DetalleAsync(Cargo cargo)
    {
        var pagos = await _repository.ObtenerPagosDeCargoAsync(cargo.Id);
        var notas = await _repository.ObtenerNotasDePagosAsync(pagos.Select(p => p.Id));
        var dto = CargoADto(cargo, pagos.Where(p => notas.All(n => n.PagoId != p.Id)).Sum(p => p.Monto));
        dto.Pagos = pagos.Select(p => PagoADto(p, cargo, p.CobradoPor, notas.FirstOrDefault(n => n.PagoId == p.Id))).ToList();
        return dto;
    }

    private static PaginaDto<CargoReadDto> Paginar(List<CargoReadDto> lista, int page, int pageSize) => new()
    {
        Items = lista.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
        Page = page,
        PageSize = pageSize,
        TotalItems = lista.Count
    };

    private static CargoReadDto CargoADto(Cargo c, decimal pagado) => new()
    {
        Id = c.Id,
        ClienteId = c.ClienteId,
        Cliente = c.Cliente == null ? "" : $"{c.Cliente.Nombre} {c.Cliente.Apellido}".Trim(),
        AnimalId = c.AnimalId,
        Animal = c.Animal?.Nombre ?? "",
        TurnoId = c.TurnoId,
        ConsultaId = c.ConsultaId,
        Descripcion = c.Descripcion,
        Monto = c.Monto,
        Pagado = pagado,
        Saldo = c.Monto - pagado,
        Estado = Estado(c.Monto, pagado),
        Fecha = c.Fecha,
        EsSena = c.EsSena
    };

    private static PagoReadDto PagoADto(Pago p, Cargo? cargo, Cuenta? cobrador, NotaCredito? nota) => new()
    {
        Id = p.Id,
        Numero = p.Numero,
        CargoId = p.CargoId,
        Descripcion = cargo?.Descripcion ?? "",
        Cliente = cargo?.Cliente == null ? "" : $"{cargo.Cliente.Nombre} {cargo.Cliente.Apellido}".Trim(),
        Monto = p.Monto,
        Medio = p.Medio,
        Referencia = p.Referencia,
        Fecha = p.Fecha,
        CobradoPor = cobrador == null ? "" : $"{cobrador.Nombre} {cobrador.Apellido}".Trim(),
        Anulado = nota != null,
        NotaCredito = nota?.Numero ?? ""
    };

    private static NotaCreditoReadDto NotaADto(NotaCredito n, string comprobante, Cuenta? emisor) => new()
    {
        Id = n.Id,
        Numero = n.Numero,
        PagoId = n.PagoId,
        Comprobante = comprobante,
        Monto = n.Monto,
        Motivo = n.Motivo,
        Fecha = n.Fecha,
        EmitidaPor = emisor == null ? "" : $"{emisor.Nombre} {emisor.Apellido}".Trim()
    };

    private static Resultado<T> Error<T>(int estado, string codigo, string mensaje)
        => Resultado<T>.Error(estado, codigo, estado switch { 404 => "No encontrado", 409 => "No se puede", _ => "Datos no válidos" }, mensaje);
}
