using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Veterinaria.Api.Controllers;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — cobro (A-023, RF-REC-05, RN-32 a RN-36). Reloj fijo: lunes 23/11/2026 10:00.
// Ana (cuenta 10) debe la consulta de Luna (cargo 1, $ 1200). Recepción es la cuenta 1 y el Administrador la 5.
public class ApiCobroTests
{
    private static readonly DateTime Ahora = new(2026, 11, 23, 10, 0, 0);
    private const int Recepcion = 1;
    private const int Admin = 5;
    private const int Ana = 10;
    private const int Bruno = 11;

    private class Reloj : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Ahora, DateTimeKind.Utc));
    }

    private class CobrosEnMemoria : ICobroRepository
    {
        public List<Cuenta> Cuentas { get; } = new();
        public List<Cargo> Cargos { get; } = new();
        public List<Pago> Pagos { get; } = new();
        public List<NotaCredito> Notas { get; } = new();
        public List<Turno> Turnos { get; } = new();
        public List<TarifaPrestacion> Tarifas { get; } = new();
        public List<Auditoria> Auditorias { get; } = new();
        public Dictionary<string, string> Parametros { get; } = new();
        public Dictionary<string, int> Numeradores { get; } = new() { ["Comprobante"] = 0, ["NotaCredito"] = 0 };
        public List<string> Bloqueos { get; } = new();

        public Task<Cargo?> ObtenerCargoAsync(int cargoId) => Task.FromResult(Cargos.FirstOrDefault(c => c.Id == cargoId));
        public Task<List<Cargo>> ObtenerCargosAsync(int? clienteId)
            => Task.FromResult(Cargos.Where(c => clienteId == null || c.ClienteId == clienteId).OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id).ToList());
        public Task<Dictionary<int, decimal>> PagadoPorCargoAsync(IEnumerable<int> cargoIds)
        {
            var ids = cargoIds.ToList();
            return Task.FromResult(Pagos.Where(p => ids.Contains(p.CargoId) && Notas.All(n => n.PagoId != p.Id))
                .GroupBy(p => p.CargoId).ToDictionary(g => g.Key, g => g.Sum(p => p.Monto)));
        }
        public Task<Pago?> ObtenerPagoAsync(int pagoId)
        {
            var pago = Pagos.FirstOrDefault(p => p.Id == pagoId);
            if (pago != null)
            {
                pago.Cargo = Cargos.First(c => c.Id == pago.CargoId);
            }
            return Task.FromResult(pago);
        }
        public Task<List<Pago>> ObtenerPagosDeCargoAsync(int cargoId) => Task.FromResult(Pagos.Where(p => p.CargoId == cargoId).ToList());
        public Task<List<Pago>> ObtenerPagosEntreAsync(DateTime desde, DateTime hasta)
            => Task.FromResult(Pagos.Where(p => p.Fecha >= desde && p.Fecha < hasta).ToList());
        public Task<List<NotaCredito>> ObtenerNotasDePagosAsync(IEnumerable<int> pagoIds)
        {
            var ids = pagoIds.ToList();
            return Task.FromResult(Notas.Where(n => ids.Contains(n.PagoId)).ToList());
        }
        public Task<List<NotaCredito>> ObtenerNotasAsync() => Task.FromResult(Notas.ToList());
        public Task<List<Turno>> ObtenerTurnosSinSenaAsync(int? clienteId)
            => Task.FromResult(Turnos.Where(t => t.Estado == CalculoAgenda.PendienteSena && (clienteId == null || t.ClienteId == clienteId)
                && !Cargos.Any(c => c.EsSena && c.TurnoId == t.Id)).ToList());
        public Task<bool> ExisteSenaAsync(int turnoId) => Task.FromResult(Cargos.Any(c => c.EsSena && c.TurnoId == turnoId));
        public Task<Turno?> ObtenerTurnoAsync(int turnoId) => Task.FromResult(Turnos.FirstOrDefault(t => t.Id == turnoId));
        public Task<TarifaPrestacion?> ObtenerTarifaVigenteAsync(int prestacionId, DateTime fecha)
            => Task.FromResult(Tarifas.Where(t => t.PrestacionId == prestacionId && t.VigenteDesde <= fecha.Date).OrderByDescending(t => t.VigenteDesde).FirstOrDefault());
        public Task<Cuenta?> ObtenerCuentaAsync(int cuentaId) => Task.FromResult(Cuentas.FirstOrDefault(c => c.Id == cuentaId));
        public Task<Dictionary<string, string>> ObtenerParametrosAsync() => Task.FromResult(Parametros);
        public Task<int> SiguienteNumeroAsync(string clave) => Task.FromResult(++Numeradores[clave]);

        public void Agregar<T>(T entidad) where T : class
        {
            switch (entidad)
            {
                case Cargo c: c.Id = Cargos.Count + 1; Cargos.Add(c); break;
                case Pago p: p.Id = Pagos.Count + 1; Pagos.Add(p); break;
                case NotaCredito n: n.Id = Notas.Count + 1; Notas.Add(n); break;
                case Auditoria a: Auditorias.Add(a); break;
                default: throw new InvalidOperationException($"Tipo no esperado: {typeof(T).Name}");
            }
        }

        public Task GuardarAsync() => Task.CompletedTask;

        public async Task<T> EnTransaccionAsync<T>(int cargoId, string numerador, Func<Task<T>> accion)
        {
            Bloqueos.Add($"Cargos:{cargoId}+{numerador}");
            return await accion();
        }
    }

    private static CobrosEnMemoria Datos()
    {
        var d = new CobrosEnMemoria();
        d.Cuentas.Add(new Cuenta { Id = Recepcion, Nombre = "Lucía", Apellido = "Méndez" });
        d.Cuentas.Add(new Cuenta { Id = Admin, Nombre = "Gonzalo", Apellido = "Rivas" });
        d.Cuentas.Add(new Cuenta { Id = Ana, Nombre = "Ana", Apellido = "Pérez" });
        d.Cargos.Add(new Cargo { Id = 1, ClienteId = Ana, AnimalId = 1, TurnoId = 50, ConsultaId = 7, Descripcion = "Consulta clínica general — Luna", Monto = 1200, Fecha = Ahora.AddDays(-1) });
        d.Cargos.Add(new Cargo { Id = 2, ClienteId = Bruno, Descripcion = "Vacunación — Michi", Monto = 600, Fecha = Ahora.AddDays(-40) });
        return d;
    }

    private static Turno Cirugia(int id, string estado = CalculoAgenda.PendienteSena) => new()
    {
        Id = id,
        ClienteId = Ana,
        AnimalId = 1,
        PrestacionId = 8,
        Prestacion = new Prestacion { Id = 8, Nombre = "Castración", PrecioDesde = 4500, RequiereConsentimiento = true },
        Estado = estado,
        Inicio = Ahora.AddDays(5),
        Fin = Ahora.AddDays(5).AddMinutes(90)
    };

    private static CobroService Servicio(CobrosEnMemoria d) => new(d, new Reloj());

    private static CobroCreateDto Cobro(int cargoId, decimal monto, string medio = CobroService.Efectivo, string referencia = "")
        => new() { CargoId = cargoId, Monto = monto, Medio = medio, Referencia = referencia };

    [Fact]
    public async Task Cobro_parcial_y_total_con_comprobantes_correlativos()
    {
        var d = Datos();
        var s = Servicio(d);

        var primero = await s.CobrarAsync(Recepcion, Cobro(1, 500));
        Assert.Equal(201, primero.Estado);
        Assert.Equal("C-000001", primero.Valor!.Pago.Numero);
        Assert.Equal("Pago parcial", primero.Valor.Cargo.Estado);
        Assert.Equal(700m, primero.Valor.Cargo.Saldo);
        Assert.Equal("Lucía Méndez", primero.Valor.Pago.CobradoPor);

        var segundo = await s.CobrarAsync(Recepcion, Cobro(1, 700, CobroService.Debito));
        Assert.Equal("C-000002", segundo.Valor!.Pago.Numero);
        Assert.Equal("Pagado", segundo.Valor.Cargo.Estado);
        Assert.EndsWith("El cargo quedó pagado.", segundo.Valor.Mensaje);
        Assert.All(d.Bloqueos, b => Assert.Equal("Cargos:1+Comprobante", b));

        var otra = await s.CobrarAsync(Recepcion, Cobro(1, 1));
        Assert.Equal(409, otra.Estado);
        Assert.Equal("CARGO_PAGADO", otra.Codigo);
    }

    [Fact]
    public async Task Cobro_rechaza_monto_mayor_medio_invalido_y_transferencia_sin_numero()
    {
        var d = Datos();
        var s = Servicio(d);

        var mayor = await s.CobrarAsync(Recepcion, Cobro(1, 1500));
        Assert.Equal("MONTO_MAYOR_AL_SALDO", mayor.Codigo);
        Assert.Contains("saldo del cargo", mayor.Mensaje);
        Assert.Equal("MEDIO_INVALIDO", (await s.CobrarAsync(Recepcion, Cobro(1, 100, "Cheque"))).Codigo);
        Assert.Equal("FALTA_REFERENCIA", (await s.CobrarAsync(Recepcion, Cobro(1, 100, CobroService.Transferencia))).Codigo);
        Assert.Equal(404, (await s.CobrarAsync(Recepcion, Cobro(99, 100))).Estado);
        Assert.Empty(d.Pagos);
        Assert.Equal(0, d.Numeradores["Comprobante"]);

        Assert.True((await s.CobrarAsync(Recepcion, Cobro(1, 100, CobroService.Transferencia, " OP-123 "))).Exito);
        Assert.Equal("OP-123", d.Pagos[0].Referencia);
    }

    [Fact]
    public async Task Anulacion_con_nota_de_credito_devuelve_el_saldo_y_no_se_repite()
    {
        var d = Datos();
        var s = Servicio(d);
        await s.CobrarAsync(Recepcion, Cobro(1, 1200));

        var anulada = await s.AnularAsync(Admin, 1, new AnulacionDto { Motivo = "Se cobró al cliente equivocado" });
        Assert.Equal(201, anulada.Estado);
        Assert.Equal("NC-000001", anulada.Valor!.Nota.Numero);
        Assert.Equal("Comprobante C-000001 anulado con la nota de crédito NC-000001.", anulada.Valor.Mensaje);
        Assert.Equal(1200m, d.Pagos[0].Monto); // el comprobante no cambia (RN-34)
        Assert.Single(d.Auditorias);

        var cargo = (await s.CargoAsync(1)).Valor!;
        Assert.Equal(("Pendiente", 1200m), (cargo.Estado, cargo.Saldo));
        Assert.True(Assert.Single(cargo.Pagos).Anulado);

        Assert.Equal("COMPROBANTE_ANULADO", (await s.AnularAsync(Admin, 1, new AnulacionDto { Motivo = "Otra vez" })).Codigo);
        Assert.Equal(404, (await s.AnularAsync(Admin, 9, new AnulacionDto { Motivo = "No existe" })).Estado);

        // Se puede volver a cobrar, con el número siguiente.
        Assert.Equal("C-000002", (await s.CobrarAsync(Recepcion, Cobro(1, 1200))).Valor!.Pago.Numero);
    }

    [Fact]
    public async Task Sena_se_genera_una_vez_confirma_la_cirugia_y_vuelve_atras_si_se_anula()
    {
        var d = Datos();
        d.Turnos.Add(Cirugia(60));
        d.Tarifas.Add(new TarifaPrestacion { PrestacionId = 8, Precio = 4800, VigenteDesde = Ahora.AddMonths(-1) });
        var s = Servicio(d);

        var mios = await s.MisCargosAsync(Ana, 1, 20);
        var sena = Assert.Single(mios.Items, c => c.EsSena);
        Assert.Equal(1440m, sena.Monto); // 30 % de 4800
        Assert.Equal("Seña de castración (30 %)", sena.Descripcion);
        Assert.Equal(0, await s.GenerarSenasAsync(null)); // idempotente

        var cobro = await s.CobrarAsync(Recepcion, Cobro(sena.Id, 1440));
        Assert.EndsWith("La cirugía quedó confirmada.", cobro.Valor!.Mensaje);
        Assert.Equal("Confirmado", d.Turnos[0].Estado);

        var anulada = await s.AnularAsync(Admin, cobro.Valor.Pago.Id, new AnulacionDto { Motivo = "Error de caja" });
        Assert.EndsWith("La cirugía vuelve a quedar pendiente de seña.", anulada.Valor!.Mensaje);
        Assert.Equal(CalculoAgenda.PendienteSena, d.Turnos[0].Estado);
    }

    [Fact]
    public async Task Sena_usa_el_porcentaje_del_parametro_y_el_precio_desde_sin_tarifa()
    {
        var d = Datos();
        d.Parametros["cobro.porcentaje_sena"] = "50";
        d.Turnos.Add(Cirugia(61));
        d.Turnos.Add(Cirugia(62, "Confirmado"));

        Assert.Equal(1, await Servicio(d).GenerarSenasAsync(null));
        Assert.Equal(2250m, d.Cargos.Single(c => c.EsSena).Monto);
    }

    [Fact]
    public async Task Listas_cada_cliente_ve_lo_suyo_y_recepcion_filtra_pendientes()
    {
        var d = Datos();
        var s = Servicio(d);
        await s.CobrarAsync(Recepcion, Cobro(2, 600));

        var deAna = await s.MisCargosAsync(Ana, 1, 20);
        Assert.Equal(1, Assert.Single(deAna.Items).Id);

        var pendientes = await s.CargosAsync(null, true, 1, 20);
        Assert.Equal(new[] { 1 }, pendientes.Items.Select(c => c.Id));
        Assert.Equal(2, (await s.CargosAsync(null, false, 1, 20)).TotalItems);

        var resumen = await s.ResumenAsync(DateOnly.FromDateTime(Ahora));
        Assert.Equal((600m, 1, 1200m), (resumen.CobradoEnElDia, resumen.Comprobantes, resumen.TotalPendiente));
        Assert.Single(await s.ComprobantesDelDiaAsync(DateOnly.FromDateTime(Ahora)));
    }

    [Fact]
    public async Task Deuda_vencida_avisa_pasado_el_limite_sin_contar_senas()
    {
        var d = Datos();
        d.Cargos.Add(new Cargo { Id = 3, ClienteId = Ana, Descripcion = "Internación", Monto = 3500, Fecha = Ahora.AddDays(-31) });
        d.Cargos.Add(new Cargo { Id = 4, ClienteId = Ana, Descripcion = "Seña", Monto = 2000, Fecha = Ahora.AddDays(-60), EsSena = true });
        var s = Servicio(d);

        var deuda = await s.DeudaAsync(Ana);
        Assert.Equal(3500m, deuda.DeudaVencida);
        Assert.StartsWith("Hay $ ", deuda.Aviso);

        Assert.Equal("", (await s.DeudaAsync(Bruno)).Aviso); // 600 vencidos, debajo del límite
    }

    [Fact]
    public void Controladores_tienen_los_roles_correctos()
    {
        Assert.Equal("Cliente", typeof(CobrosController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Recepcionista,Administrador", typeof(CajaController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Recepcionista", typeof(CajaController).GetMethod(nameof(CajaController.Cobrar))!.GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Administrador", typeof(AdminCobrosController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }
}
