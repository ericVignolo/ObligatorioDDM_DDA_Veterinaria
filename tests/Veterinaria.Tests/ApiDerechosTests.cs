using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Veterinaria.Api.Controllers;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — derechos del titular (A-028, RF-CLI-19, RNF-LEG-01/02/06). Reloj fijo: domingo 22/11/2026 15:00.
// Ana (cuenta 10) tiene a Luna; el Administrador es la cuenta 5.
public class ApiDerechosTests
{
    private static readonly DateTime Ahora = new(2026, 11, 22, 15, 0, 0);
    private const int Ana = 10;
    private const int Admin = 5;

    private class Reloj : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Ahora, DateTimeKind.Utc));
    }

    private class DerechosEnMemoria : IDerechosRepository
    {
        public Cuenta Ana { get; } = new()
        {
            Id = ApiDerechosTests.Ana, Nombre = "Ana", Apellido = "Pérez", Correo = "ana@correo.uy", Telefono = "099123456",
            TipoDocumento = "CI", NumeroDocumento = "12345672", Calle = "Rivera", NumeroPuerta = "1234", Localidad = "Nueva Palmira",
            HashContrasena = "hash", Activa = true, Verificada = true, FechaCreacion = Ahora.AddYears(-1)
        };

        public List<Cargo> Cargos { get; } = new();
        public List<Pago> Pagos { get; } = new();
        public List<int> Anulados { get; } = new();
        public List<Turno> Turnos { get; } = new();
        public bool Internado { get; set; }
        public List<Comunicacion> Comunicaciones { get; } = new();
        public List<PreferenciaComunicacion> Preferencias { get; } = new();
        public List<CodigoCuenta> Codigos { get; } = new();
        public List<SesionRefresco> Sesiones { get; } = new();
        public List<SolicitudDerecho> Solicitudes { get; } = new();
        public List<Auditoria> Auditorias { get; } = new();

        public Task<Cuenta?> ObtenerCuentaAsync(int cuentaId) => Task.FromResult(cuentaId == Ana.Id ? Ana : null);
        public Task<List<PoliticaConservacion>> ObtenerPoliticasAsync() => Task.FromResult(new List<PoliticaConservacion> { new() { Id = 1, Categoria = "Datos de la cuenta" } });
        public Task<List<Animal>> ObtenerAnimalesAsync(int cuentaId) => Task.FromResult(new List<Animal> { new() { Id = 1, Nombre = "Luna", Especie = new Especie { Nombre = "Perro" } } });
        public Task<List<Turno>> ObtenerTurnosAsync(int clienteId) => Task.FromResult(Turnos.ToList());
        public Task<List<Cargo>> ObtenerCargosAsync(int clienteId) => Task.FromResult(Cargos.ToList());
        public Task<List<Pago>> ObtenerPagosAsync(IEnumerable<int> cargoIds) => Task.FromResult(Pagos.Where(p => cargoIds.Contains(p.CargoId)).ToList());
        public Task<List<int>> ObtenerPagosAnuladosAsync(IEnumerable<int> pagoIds) => Task.FromResult(Anulados.Where(pagoIds.Contains).ToList());
        public Task<List<Consentimiento>> ObtenerConsentimientosAsync(int clienteId) => Task.FromResult(new List<Consentimiento>());
        public Task<List<Comunicacion>> ObtenerComunicacionesAsync(int cuentaId) => Task.FromResult(Comunicaciones.ToList());
        public Task<List<PreferenciaComunicacion>> ObtenerPreferenciasAsync(int cuentaId) => Task.FromResult(Preferencias.ToList());

        public Task<decimal> SaldoPendienteAsync(int clienteId)
            => Task.FromResult(Cargos.Sum(c => c.Monto) - Pagos.Where(p => !Anulados.Contains(p.Id)).Sum(p => p.Monto));
        public Task<int> ContarTurnosProximosAsync(int clienteId, DateTime ahora) => Task.FromResult(Turnos.Count(t => t.Inicio > ahora && t.Estado == "Confirmado"));
        public Task<bool> TieneInternacionAbiertaAsync(int clienteId) => Task.FromResult(Internado);

        public Task<List<SolicitudDerecho>> ObtenerSolicitudesAsync(int? cuentaId)
            => Task.FromResult(Solicitudes.Where(s => cuentaId == null || s.CuentaId == cuentaId).OrderByDescending(s => s.Id).ToList());
        public Task<SolicitudDerecho?> ObtenerSolicitudAsync(int id) => Task.FromResult(Solicitudes.FirstOrDefault(s => s.Id == id));

        public Task<List<Comunicacion>> ObtenerComunicacionesParaBorrarAsync(int cuentaId) => Task.FromResult(Comunicaciones.ToList());
        public Task<List<PreferenciaComunicacion>> ObtenerPreferenciasParaBorrarAsync(int cuentaId) => Task.FromResult(Preferencias.ToList());
        public Task<List<CodigoCuenta>> ObtenerCodigosAsync(int cuentaId) => Task.FromResult(Codigos.ToList());
        public Task<List<SesionRefresco>> ObtenerSesionesActivasAsync(int cuentaId) => Task.FromResult(Sesiones.Where(s => s.RevocadaEn == null).ToList());

        public void Agregar<T>(T entidad) where T : class
        {
            switch (entidad)
            {
                case SolicitudDerecho s: s.Id = Solicitudes.Count + 1; Solicitudes.Add(s); break;
                case Auditoria a: Auditorias.Add(a); break;
                default: throw new InvalidOperationException($"Tipo no esperado: {typeof(T).Name}");
            }
        }

        public void Quitar<T>(T entidad) where T : class
        {
            switch (entidad)
            {
                case Comunicacion c: Comunicaciones.Remove(c); break;
                case PreferenciaComunicacion p: Preferencias.Remove(p); break;
                case CodigoCuenta c: Codigos.Remove(c); break;
                default: throw new InvalidOperationException($"Tipo no esperado: {typeof(T).Name}");
            }
        }

        public Task GuardarAsync() => Task.CompletedTask;
    }

    private static (DerechosService S, DerechosEnMemoria D) Escenario()
    {
        var d = new DerechosEnMemoria();
        return (new DerechosService(d, new Reloj()), d);
    }

    [Fact]
    public async Task Exportar_entrega_json_con_los_datos_y_queda_registrado()
    {
        var (s, d) = Escenario();
        d.Cargos.Add(new Cargo { Id = 1, ClienteId = Ana, Descripcion = "Consulta", Monto = 1200, Fecha = Ahora.AddDays(-3) });
        d.Pagos.Add(new Pago { Id = 1, CargoId = 1, Numero = "C-000001", Monto = 1200, Medio = "Efectivo", Fecha = Ahora.AddDays(-3) });
        d.Anulados.Add(1);

        var resultado = await s.ExportarAsync(Ana);
        using var json = JsonDocument.Parse(resultado.Valor!);
        var raiz = json.RootElement;
        Assert.Equal("ana@correo.uy", raiz.GetProperty("titular").GetProperty("correo").GetString());
        Assert.Equal("Luna", raiz.GetProperty("animales")[0].GetProperty("nombre").GetString());
        var cargo = raiz.GetProperty("cargos")[0];
        Assert.Equal(1200m, cargo.GetProperty("saldo").GetDecimal()); // el pago anulado no cuenta
        Assert.True(cargo.GetProperty("pagos")[0].GetProperty("anulado").GetBoolean());

        var registro = Assert.Single(d.Solicitudes);
        Assert.Equal((DerechosService.Exportacion, DerechosService.Completada), (registro.Tipo, registro.Estado));
        Assert.Equal(404, (await s.ExportarAsync(99)).Estado);
    }

    [Fact]
    public async Task Eliminacion_pide_confirmar_y_no_tener_pendientes()
    {
        var (s, d) = Escenario();
        Assert.Equal("FALTA_CONFIRMAR", (await s.PedirEliminacionAsync(Ana, false)).Codigo);

        d.Cargos.Add(new Cargo { Id = 1, ClienteId = Ana, Monto = 500 });
        d.Turnos.Add(new Turno { Id = 1, Inicio = Ahora.AddDays(2), Estado = "Confirmado" });
        d.Internado = true;
        var obstaculos = await s.ObstaculosAsync(Ana);
        Assert.False(obstaculos.SePuedeEliminar);
        Assert.Equal(3, obstaculos.Obstaculos.Count);
        var conSaldo = await s.PedirEliminacionAsync(Ana, true);
        Assert.Equal("HAY_PENDIENTES", conSaldo.Codigo);
        Assert.StartsWith("Tenés cargos con saldo pendiente", conSaldo.Mensaje);

        d.Cargos.Clear();
        d.Turnos.Clear();
        d.Internado = false;
        var pedido = await s.PedirEliminacionAsync(Ana, true);
        Assert.Equal(201, pedido.Estado);
        Assert.Equal("PEDIDO_EN_CURSO", (await s.PedirEliminacionAsync(Ana, true)).Codigo);

        Assert.True((await s.CancelarAsync(Ana, pedido.Valor!.Id)).Exito);
        Assert.Equal("NO_CANCELABLE", (await s.CancelarAsync(Ana, pedido.Valor.Id)).Codigo);
        Assert.Equal(DerechosService.Cancelada, d.Solicitudes[0].Estado);
    }

    [Fact]
    public async Task Aprobar_anonimiza_borra_comunicaciones_codigos_y_cierra_sesiones()
    {
        var (s, d) = Escenario();
        d.Comunicaciones.Add(new Comunicacion { Id = 1, CuentaId = Ana, Asunto = "Hola" });
        d.Preferencias.Add(new PreferenciaComunicacion { CuentaId = Ana, TipoComunicacionId = 4, Canal = "Correo" });
        d.Codigos.Add(new CodigoCuenta { Id = 1, CuentaId = Ana, Tipo = "Verificación" });
        d.Sesiones.Add(new SesionRefresco { Id = 1, CuentaId = Ana });
        var pedido = (await s.PedirEliminacionAsync(Ana, true)).Valor!;

        var aprobado = await s.ResolverAsync(Admin, pedido.Id, new ResolucionDerechoDto { Aprobar = true });
        Assert.Equal(("Cuenta anonimizada.", DerechosService.Completada, "Administrador"), (aprobado.Valor!.Mensaje, aprobado.Valor.Estado, aprobado.Valor.ResueltaPor));
        Assert.True(d.Ana.Eliminada);
        Assert.False(d.Ana.Activa);
        Assert.Equal(("Titular", "eliminado", "eliminada-10@eliminada.invalid", "", ""), (d.Ana.Nombre, d.Ana.Apellido, d.Ana.Correo, d.Ana.NumeroDocumento, d.Ana.HashContrasena));
        Assert.Empty(d.Comunicaciones);
        Assert.Empty(d.Preferencias);
        Assert.Empty(d.Codigos);
        Assert.NotNull(d.Sesiones[0].RevocadaEn);
        Assert.Equal("Eliminación aprobada", Assert.Single(d.Auditorias).Accion);
        Assert.Equal("NO_PENDIENTE", (await s.ResolverAsync(Admin, pedido.Id, new ResolucionDerechoDto { Aprobar = true })).Codigo);
    }

    [Fact]
    public async Task Rechazar_pide_motivo_y_aprobar_revisa_pendientes_nuevos()
    {
        var (s, d) = Escenario();
        var pedido = (await s.PedirEliminacionAsync(Ana, true)).Valor!;

        // Después del pedido apareció un turno: no se puede aprobar.
        d.Turnos.Add(new Turno { Id = 1, Inicio = Ahora.AddDays(1), Estado = "Confirmado" });
        var noSePuede = await s.ResolverAsync(Admin, pedido.Id, new ResolucionDerechoDto { Aprobar = true });
        Assert.Equal("HAY_PENDIENTES", noSePuede.Codigo);
        Assert.False(d.Ana.Eliminada);

        Assert.Equal("FALTA_MOTIVO", (await s.ResolverAsync(Admin, pedido.Id, new ResolucionDerechoDto { Aprobar = false, Motivo = " " })).Codigo);
        var rechazo = await s.ResolverAsync(Admin, pedido.Id, new ResolucionDerechoDto { Aprobar = false, Motivo = "Tiene un turno el lunes." });
        Assert.Equal((DerechosService.Rechazada, "Tiene un turno el lunes."), (rechazo.Valor!.Estado, rechazo.Valor.Detalle));
        Assert.Single(await s.TodasAsync());
    }

    [Fact]
    public void Controladores_tienen_los_roles_correctos()
    {
        Assert.Equal("Cliente", typeof(PrivacidadController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.NotNull(typeof(PrivacidadController).GetMethod(nameof(PrivacidadController.Politicas))!.GetCustomAttribute<AllowAnonymousAttribute>());
        Assert.Equal("Administrador", typeof(AdminDerechosController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }
}
