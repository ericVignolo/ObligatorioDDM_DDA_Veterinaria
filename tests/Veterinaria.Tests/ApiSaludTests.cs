using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Veterinaria.Api.Controllers;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — carnet y avisos de refuerzo (A-021, RF-CLI-06/07), inventario (A-022, RN-26 a RN-31) y consentimiento
// informado (A-025, RN-10). Reloj fijo: lunes 23/11/2026 10:00. Ana (cuenta 10) tiene a Luna (perro, id 1).
public class ApiSaludTests
{
    private static readonly DateTime Ahora = new(2026, 11, 23, 10, 0, 0);
    private static readonly DateTime Hoy = Ahora.Date;
    private const int Ana = 10;
    private const int Otro = 11;
    private const int Admin = 5;

    private class Reloj : TimeProvider
    {
        public DateTime Ahora { get; set; } = ApiSaludTests.Ahora;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Ahora, DateTimeKind.Utc));
    }

    private class SaludEnMemoria : ISaludRepository
    {
        public List<Cuenta> Cuentas { get; } = new();
        public List<Animal> Animales { get; } = new();
        public List<AplicacionSanitaria> Aplicaciones { get; } = new();
        public List<TipoComunicacion> Tipos { get; } = new();
        public List<PreferenciaComunicacion> Preferencias { get; } = new();
        public List<Comunicacion> Comunicaciones { get; } = new();
        public Dictionary<string, string> Parametros { get; } = new();
        public List<Articulo> Articulos { get; } = new();
        public List<LoteProducto> Lotes { get; } = new();
        public List<ReservaStock> Reservas { get; } = new();
        public List<MovimientoStock> Movimientos { get; } = new();
        public List<AlertaStock> Alertas { get; } = new();
        public List<ProductoSanitario> Productos { get; } = new();
        public List<Prestacion> Prestaciones { get; } = new();
        public List<Turno> Turnos { get; } = new();
        public List<DocumentoConsentimiento> Documentos { get; } = new();
        public List<Consentimiento> Consentimientos { get; } = new();
        public int Transacciones { get; private set; }

        public Task<Animal?> ObtenerAnimalAsync(int animalId) => Task.FromResult(Animales.FirstOrDefault(a => a.Id == animalId));
        public Task<bool> EsResponsableAsync(int animalId, int cuentaId)
            => Task.FromResult(Animales.Any(a => a.Id == animalId && a.Responsables.Any(r => r.CuentaId == cuentaId && r.Hasta == null)));
        public Task<List<Animal>> ObtenerAnimalesDeClienteAsync(int cuentaId)
            => Task.FromResult(Animales.Where(a => a.Activo && a.Responsables.Any(r => r.CuentaId == cuentaId && r.Hasta == null)).ToList());
        public Task<Cuenta?> ObtenerCuentaAsync(int cuentaId) => Task.FromResult(Cuentas.FirstOrDefault(c => c.Id == cuentaId));
        public Task<Dictionary<string, string>> ObtenerParametrosAsync() => Task.FromResult(Parametros);

        public Task<List<AplicacionSanitaria>> ObtenerAplicacionesQueCuentanAsync(IEnumerable<int> animalIds)
        {
            var ids = animalIds.ToList();
            return Task.FromResult(Aplicaciones.Where(a => ids.Contains(a.AnimalId)).OrderByDescending(a => a.FechaAplicacion).ToList());
        }

        public Task<List<Animal>> ObtenerAnimalesConRefuerzoHastaAsync(DateTime hasta)
            => Task.FromResult(Animales.Where(a => a.Activo && Aplicaciones.Any(x => x.AnimalId == a.Id && x.FechaRefuerzo <= hasta)).ToList());
        public Task<bool> ExisteComunicacionAsync(int cuentaId, int tipoComunicacionId, string clave)
            => Task.FromResult(Comunicaciones.Any(c => c.CuentaId == cuentaId && c.TipoComunicacionId == tipoComunicacionId && c.Clave == clave));
        public Task<string?> ObtenerCanalPreferidoAsync(int cuentaId, int tipoComunicacionId)
            => Task.FromResult(Preferencias.FirstOrDefault(p => p.CuentaId == cuentaId && p.TipoComunicacionId == tipoComunicacionId)?.Canal);
        public Task<TipoComunicacion?> ObtenerTipoComunicacionAsync(int tipoComunicacionId) => Task.FromResult(Tipos.FirstOrDefault(t => t.Id == tipoComunicacionId));

        public Task<List<Articulo>> ObtenerArticulosAsync() => Task.FromResult(Articulos.Where(a => a.Activo).ToList());
        public Task<Articulo?> ObtenerArticuloAsync(int articuloId) => Task.FromResult(Articulos.FirstOrDefault(a => a.Id == articuloId && a.Activo));
        public Task<List<LoteProducto>> ObtenerLotesAsync(int? articuloId)
            => Task.FromResult(Lotes.Where(l => articuloId == null || l.ArticuloId == articuloId).OrderBy(l => l.Vencimiento).ThenBy(l => l.Id).ToList());
        public Task<LoteProducto?> ObtenerLoteAsync(int loteId) => Task.FromResult(Lotes.FirstOrDefault(l => l.Id == loteId));
        public Task<Dictionary<int, int>> ReservadoPorLoteAsync(int? articuloId)
            => Task.FromResult(Reservas.Where(r => !r.Confirmada && (articuloId == null || r.ArticuloId == articuloId))
                .GroupBy(r => r.LoteId).ToDictionary(g => g.Key, g => g.Sum(r => r.Cantidad)));
        public Task<bool> ExisteLoteAsync(int articuloId, string numero) => Task.FromResult(Lotes.Any(l => l.ArticuloId == articuloId && l.Numero == numero));
        public Task<List<MovimientoStock>> ObtenerMovimientosAsync(int? articuloId, int saltar, int tomar)
            => Task.FromResult(Movimientos.Where(m => articuloId == null || m.ArticuloId == articuloId).OrderByDescending(m => m.Fecha).ThenByDescending(m => m.Id).Skip(saltar).Take(tomar).ToList());
        public Task<int> ContarMovimientosAsync(int? articuloId) => Task.FromResult(Movimientos.Count(m => articuloId == null || m.ArticuloId == articuloId));
        public Task<List<AlertaStock>> ObtenerAlertasAsync(bool soloPendientes) => Task.FromResult(Alertas.Where(a => !soloPendientes || !a.Atendida).ToList());
        public Task<AlertaStock?> ObtenerAlertaAsync(int alertaId) => Task.FromResult(Alertas.FirstOrDefault(a => a.Id == alertaId));
        public Task<List<ProductoSanitario>> ObtenerProductosAsync() => Task.FromResult(Productos.ToList());

        public Task<Turno?> ObtenerTurnoAsync(int turnoId) => Task.FromResult(Turnos.FirstOrDefault(t => t.Id == turnoId));
        public Task<List<Turno>> ObtenerTurnosConConsentimientoAsync(int clienteId, DateTime ahora)
            => Task.FromResult(Turnos.Where(t => t.ClienteId == clienteId && t.Fin > ahora && t.Prestacion!.RequiereConsentimiento
                && new[] { "Confirmado", "En espera", "Pendiente de seña" }.Contains(t.Estado)).ToList());
        public Task<DocumentoConsentimiento?> ObtenerDocumentoVigenteAsync(int prestacionId)
            => Task.FromResult(Documentos.Where(d => d.PrestacionId == prestacionId && d.Vigente).OrderByDescending(d => d.FechaVigencia).ThenByDescending(d => d.Id).FirstOrDefault());
        public Task<List<DocumentoConsentimiento>> ObtenerDocumentosAsync(int prestacionId) => Task.FromResult(Documentos.Where(d => d.PrestacionId == prestacionId).ToList());
        public Task<Consentimiento?> ObtenerUltimaRespuestaAsync(int turnoId)
            => Task.FromResult(Consentimientos.Where(c => c.TurnoId == turnoId).OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id).FirstOrDefault());
        public Task<Prestacion?> ObtenerPrestacionAsync(int prestacionId) => Task.FromResult(Prestaciones.FirstOrDefault(p => p.Id == prestacionId));

        public void Agregar<T>(T entidad) where T : class
        {
            switch (entidad)
            {
                case Comunicacion c: c.Id = Comunicaciones.Count + 1; Comunicaciones.Add(c); break;
                case LoteProducto l: l.Id = Lotes.Count == 0 ? 1 : Lotes.Max(x => x.Id) + 1; Lotes.Add(l); break;
                case MovimientoStock m: m.Id = Movimientos.Count + 1; m.LoteId = m.Lote?.Id ?? m.LoteId; Movimientos.Add(m); break;
                case AlertaStock a: a.Id = Alertas.Count + 1; Alertas.Add(a); break;
                case Consentimiento c: c.Id = Consentimientos.Count + 1; Consentimientos.Add(c); break;
                case DocumentoConsentimiento d: d.Id = Documentos.Count == 0 ? 1 : Documentos.Max(x => x.Id) + 1; Documentos.Add(d); break;
                default: throw new InvalidOperationException($"Tipo no esperado: {typeof(T).Name}");
            }
        }

        public Task GuardarAsync() => Task.CompletedTask;

        public async Task<T> EnTransaccionAsync<T>(IEnumerable<int> articuloIds, Func<Task<T>> accion)
        {
            Transacciones++;
            return await accion();
        }
    }

    // ===================== Escenario =====================

    private static SaludEnMemoria Datos()
    {
        var datos = new SaludEnMemoria();
        datos.Cuentas.Add(new Cuenta { Id = Ana, Nombre = "Ana", Apellido = "Pérez", Correo = "ana@correo.uy", Activa = true });
        datos.Cuentas.Add(new Cuenta { Id = Otro, Nombre = "Bruno", Apellido = "Sosa", Correo = "bruno@correo.uy", Activa = true });

        var luna = new Animal { Id = 1, Nombre = "Luna", EspecieId = 1, Activo = true };
        luna.Responsables.Add(new Responsabilidad { CuentaId = Ana, AnimalId = 1, Desde = Hoy.AddYears(-1) });
        var michi = new Animal { Id = 2, Nombre = "Michi", EspecieId = 2, Activo = true };
        michi.Responsables.Add(new Responsabilidad { CuentaId = Otro, AnimalId = 2, Desde = Hoy.AddYears(-1) });
        datos.Animales.AddRange(new[] { luna, michi });

        datos.Tipos.Add(new TipoComunicacion { Id = CarnetService.TipoAvisoRefuerzo, Nombre = "Aviso de refuerzo", CanalPorDefecto = "En la app" });
        datos.Parametros["avisos.refuerzo_dias_antes"] = "5";

        // Inventario: vacuna (id 1, mínimo 5) con dos lotes y un insumo (id 2).
        datos.Articulos.Add(new Articulo { Id = 1, Nombre = "Vacuna séxtuple", Tipo = "Producto", StockMinimo = 5, Activo = true });
        datos.Articulos.Add(new Articulo { Id = 2, Nombre = "Jeringa 3 ml", Tipo = "Insumo", StockMinimo = 10, Activo = true });
        datos.Lotes.Add(new LoteProducto { Id = 1, ArticuloId = 1, Numero = "VS-001", Vencimiento = Hoy.AddDays(20), Cantidad = 4 });
        datos.Lotes.Add(new LoteProducto { Id = 2, ArticuloId = 1, Numero = "VS-002", Vencimiento = Hoy.AddDays(200), Cantidad = 6 });
        datos.Lotes.Add(new LoteProducto { Id = 3, ArticuloId = 2, Numero = "JE-001", Vencimiento = Hoy.AddDays(400), Cantidad = 30 });
        datos.Productos.Add(new ProductoSanitario
        {
            Id = 1, Nombre = "Séxtuple canina", Tipo = "Vacuna", Laboratorio = "Lab", ArticuloId = 1, IntervaloRefuerzoDias = 365,
            Especies = new List<ProductoSanitarioEspecie> { new() { ProductoSanitarioId = 1, EspecieId = 1 } }
        });

        // Consentimiento: castración (id 4) lo pide; consulta general (id 1) no.
        datos.Prestaciones.Add(new Prestacion { Id = 1, Nombre = "Consulta general" });
        datos.Prestaciones.Add(new Prestacion { Id = 4, Nombre = "Castración", RequiereConsentimiento = true });
        datos.Documentos.Add(new DocumentoConsentimiento { Id = 1, PrestacionId = 4, Version = "1.0", Titulo = "Consentimiento de cirugía", Texto = new string('x', 60), FechaVigencia = Hoy.AddMonths(-2), Vigente = true });
        datos.Turnos.Add(NuevoTurno(1, 4, "Confirmado", Ahora.AddDays(3), datos));
        datos.Turnos.Add(NuevoTurno(2, 1, "Confirmado", Ahora.AddDays(4), datos));
        return datos;
    }

    private static Turno NuevoTurno(int id, int prestacionId, string estado, DateTime inicio, SaludEnMemoria datos) => new()
    {
        Id = id,
        ClienteId = Ana,
        AnimalId = 1,
        Animal = datos.Animales[0],
        PrestacionId = prestacionId,
        Prestacion = datos.Prestaciones.First(p => p.Id == prestacionId),
        Estado = estado,
        Inicio = inicio,
        Fin = inicio.AddMinutes(60)
    };

    private static AplicacionSanitaria Aplicacion(int id, int animalId, int productoId, string producto, DateTime aplicada, DateTime refuerzo) => new()
    {
        Id = id, AnimalId = animalId, ProductoSanitarioId = productoId, Producto = producto, Tipo = "Vacuna", NumeroLote = "L-" + id,
        FechaAplicacion = aplicada, FechaRefuerzo = refuerzo
    };

    private static CarnetService ServicioCarnet(SaludEnMemoria d) => new(d, new Reloj());
    private static InventarioService ServicioInventario(SaludEnMemoria d) => new(d, new Reloj());
    private static ConsentimientoService ServicioConsentimiento(SaludEnMemoria d) => new(d, new Reloj());

    // ===================== Carnet y avisos =====================

    [Fact]
    public async Task Carnet_SoloDelResponsableYConLaUltimaAplicacionPorProducto()
    {
        var d = Datos();
        d.Aplicaciones.Add(Aplicacion(1, 1, 1, "Séxtuple", Hoy.AddYears(-2), Hoy.AddYears(-1)));   // vieja, reemplazada
        d.Aplicaciones.Add(Aplicacion(2, 1, 1, "Séxtuple", Hoy.AddDays(-362), Hoy.AddDays(3)));    // refuerzo próximo
        d.Aplicaciones.Add(Aplicacion(3, 1, 2, "Antirrábica", Hoy.AddDays(-400), Hoy.AddDays(-2))); // vencida
        d.Aplicaciones.Add(Aplicacion(4, 1, 3, "Desparasitario", Hoy.AddDays(-10), Hoy.AddDays(40))); // vigente

        var ajeno = await ServicioCarnet(d).CarnetAsync(Otro, 1);
        Assert.Equal(404, ajeno.Estado);

        var carnet = (await ServicioCarnet(d).CarnetAsync(Ana, 1)).Valor!;
        Assert.Equal(new[] { 3, 2, 4 }, carnet.Select(i => i.AplicacionId));
        Assert.Equal(new[] { CarnetService.Vencida, CarnetService.RefuerzoProximo, CarnetService.Vigente }, carnet.Select(i => i.Estado));

        var alertas = await ServicioCarnet(d).AlertasAsync(Ana);
        Assert.Equal(new[] { 3, 2 }, alertas.Select(a => a.AplicacionId));
    }

    [Fact]
    public async Task Avisos_UnoPorAplicacionSinRepetirYRespetandoNoRecibir()
    {
        var d = Datos();
        d.Aplicaciones.Add(Aplicacion(1, 1, 1, "Séxtuple", Hoy.AddDays(-362), Hoy.AddDays(3)));
        d.Aplicaciones.Add(Aplicacion(2, 1, 2, "Antirrábica", Hoy.AddDays(-400), Hoy.AddDays(-2))); // vencida: no se avisa
        d.Aplicaciones.Add(Aplicacion(3, 2, 1, "Séxtuple", Hoy.AddDays(-362), Hoy.AddDays(2)));     // de Bruno
        d.Preferencias.Add(new PreferenciaComunicacion { CuentaId = Otro, TipoComunicacionId = CarnetService.TipoAvisoRefuerzo, Canal = "No recibir" });

        var primero = await ServicioCarnet(d).GenerarAvisosAsync();
        Assert.Equal(1, primero.Generados);
        Assert.Equal(1, primero.SinRecibir);
        var aviso = Assert.Single(d.Comunicaciones);
        Assert.Equal(Ana, aviso.CuentaId);
        Assert.Equal("refuerzo-1", aviso.Clave);
        Assert.Equal("En la app", aviso.Canal);

        var segundo = await ServicioCarnet(d).GenerarAvisosAsync();
        Assert.Equal(0, segundo.Generados);
        Assert.Equal(1, segundo.YaExistian);
        Assert.Single(d.Comunicaciones);
    }

    // ===================== Inventario =====================

    [Fact]
    public async Task Compra_RechazaVencidoYLoteRepetidoYRegistraElMovimiento()
    {
        var d = Datos();
        var servicio = ServicioInventario(d);

        var vencido = await servicio.RegistrarCompraAsync(Admin, new CompraDto { ArticuloId = 1, Numero = "VS-009", Vencimiento = DateOnly.FromDateTime(Hoy), Cantidad = 5 });
        Assert.Equal("LOTE_VENCIDO", vencido.Codigo);

        var repetido = await servicio.RegistrarCompraAsync(Admin, new CompraDto { ArticuloId = 1, Numero = "vs-001", Vencimiento = DateOnly.FromDateTime(Hoy.AddDays(90)), Cantidad = 5 });
        Assert.Equal(409, repetido.Estado);
        Assert.Equal("LOTE_REPETIDO", repetido.Codigo);

        var ok = await servicio.RegistrarCompraAsync(Admin, new CompraDto { ArticuloId = 1, Numero = "vs-010", Vencimiento = DateOnly.FromDateTime(Hoy.AddDays(90)), Cantidad = 5 });
        Assert.Equal(201, ok.Estado);
        Assert.Equal("VS-010", ok.Valor!.Numero);
        var movimiento = Assert.Single(d.Movimientos);
        Assert.Equal(("Compra", 5, Admin), (movimiento.Tipo, movimiento.Cantidad, movimiento.PersonaId!.Value));
    }

    [Fact]
    public async Task Baja_NoDejaElStockDebajoDeLoApartadoYAvisaAlCruzarElMinimo()
    {
        var d = Datos();
        d.Reservas.Add(new ReservaStock { Id = 1, ConsultaId = 1, ArticuloId = 1, LoteId = 1, Cantidad = 3 });
        var servicio = ServicioInventario(d);

        var demasiado = await servicio.DarDeBajaAsync(Admin, new AjusteDto { LoteId = 1, Cantidad = 2, Motivo = "Se rompió" });
        Assert.Equal("STOCK_INSUFICIENTE", demasiado.Codigo);
        Assert.Equal("El lote VS-001 tiene 1 disponibles (3 apartadas por consultas abiertas): el stock no puede quedar negativo (RN-26).", demasiado.Mensaje);
        Assert.Empty(d.Movimientos);

        // Stock 10 → 5: todavía no cruza el mínimo (5). Luego 5 → 4: cruza y deja una alerta.
        Assert.True((await servicio.DarDeBajaAsync(Admin, new AjusteDto { LoteId = 2, Cantidad = 5, Motivo = "Frasco roto" })).Exito);
        Assert.Empty(d.Alertas);
        Assert.True((await servicio.DarDeBajaAsync(Admin, new AjusteDto { LoteId = 1, Cantidad = 1, Motivo = "Frasco roto" })).Exito);
        var alerta = Assert.Single(d.Alertas);
        Assert.Equal((4, 5), (alerta.StockQueQuedo, alerta.StockMinimo));
        Assert.All(d.Movimientos, m => Assert.Equal("Baja", m.Tipo));
        Assert.Equal(3, d.Transacciones); // la rechazada también valida dentro del bloqueo
    }

    [Fact]
    public async Task Ajuste_RechazaCeroYLotesVencidos()
    {
        var d = Datos();
        d.Lotes.Add(new LoteProducto { Id = 9, ArticuloId = 2, Numero = "JE-OLD", Vencimiento = Hoy.AddDays(-1), Cantidad = 5 });
        var servicio = ServicioInventario(d);

        Assert.Equal("AJUSTE_CERO", (await servicio.AjustarAsync(Admin, new AjusteDto { LoteId = 3, Cantidad = 0, Motivo = "Conteo" })).Codigo);
        Assert.Equal("LOTE_INHABILITADO", (await servicio.AjustarAsync(Admin, new AjusteDto { LoteId = 9, Cantidad = -1, Motivo = "Conteo" })).Codigo);

        var suma = await servicio.AjustarAsync(Admin, new AjusteDto { LoteId = 3, Cantidad = 2, Motivo = "Conteo físico" });
        Assert.Equal(32, suma.Valor!.Cantidad);
        Assert.Equal("Conteo físico", Assert.Single(d.Movimientos).Motivo);
    }

    [Fact]
    public async Task Vencidos_SeInhabilitanYSeDanDeBajaConSuMovimiento()
    {
        var d = Datos();
        d.Lotes[0].Vencimiento = Hoy.AddDays(-1); // VS-001, 4 unidades

        var resultado = await ServicioInventario(d).ProcesarVencidosAsync();
        Assert.Equal((1, 4), (resultado.LotesInhabilitados, resultado.UnidadesDadasDeBaja));
        Assert.True(d.Lotes[0].Inhabilitado);
        Assert.Equal(0, d.Lotes[0].Cantidad);
        var movimiento = Assert.Single(d.Movimientos);
        Assert.Equal(("Baja", -4, "Vencimiento automático"), (movimiento.Tipo, movimiento.Cantidad, movimiento.Origen));
        Assert.Null(movimiento.PersonaId);
        Assert.Empty(d.Alertas); // de 10 a 6, sigue sobre el mínimo

        var otraVez = await ServicioInventario(d).ProcesarVencidosAsync();
        Assert.Equal(0, otraVez.LotesInhabilitados);
    }

    [Fact]
    public async Task Productos_SoloDeLaEspecieYConElLoteQueVenceAntes()
    {
        var d = Datos();
        var perro = (await ServicioInventario(d).ProductosParaAnimalAsync(1)).Valor!;
        Assert.Equal("VS-001", Assert.Single(perro).LoteQueSeUsara);
        Assert.Equal(10, perro[0].Disponible);

        Assert.Empty((await ServicioInventario(d).ProductosParaAnimalAsync(2)).Valor!);

        var articulos = await ServicioInventario(d).ArticulosAsync();
        Assert.Equal(1, articulos.First(a => a.Id == 1).LotesProximosAVencer);
        Assert.Single(await ServicioInventario(d).InsumosParaConsultaAsync());
    }

    // ===================== Consentimiento =====================

    [Fact]
    public async Task Consentimiento_FirmaConElNombreDeLaCuentaYSePideDeNuevoConVersionNueva()
    {
        var d = Datos();
        var servicio = ServicioConsentimiento(d);

        Assert.Equal(ConsentimientoService.Pendiente, (await servicio.EstadoAsync(Ana, 1)).Valor!.Estado);
        Assert.Single(await servicio.PendientesAsync(Ana));

        Assert.Equal("FALTA_NOMBRE", (await servicio.ResponderAsync(Ana, 1, new RespuestaConsentimientoDto { Acepta = true })).Codigo);
        Assert.Equal("NOMBRE_NO_COINCIDE", (await servicio.ResponderAsync(Ana, 1, new RespuestaConsentimientoDto { Acepta = true, NombreCompleto = "Ana Gómez" })).Codigo);

        var firmado = await servicio.ResponderAsync(Ana, 1, new RespuestaConsentimientoDto { Acepta = true, NombreCompleto = "  ana   PEREZ " });
        Assert.Equal(ConsentimientoService.Firmado, firmado.Valor!.Estado);
        Assert.Equal("Consentimiento firmado. Queda guardado junto al turno.", firmado.Valor.Mensaje);
        var guardado = Assert.Single(d.Consentimientos);
        Assert.Equal(("Aceptado", "1.0", "En línea", 1), (guardado.Respuesta, guardado.Version, guardado.Canal, guardado.DocumentoConsentimientoId));
        Assert.Equal(64, guardado.Hash.Length);
        Assert.Empty(await servicio.PendientesAsync(Ana));

        Assert.Equal("YA_FIRMADO", (await servicio.ResponderAsync(Ana, 1, new RespuestaConsentimientoDto { Acepta = true, NombreCompleto = "Ana Pérez" })).Codigo);

        var publicado = await servicio.PublicarAsync(new DocumentoConsentimientoCreateDto { PrestacionId = 4, Version = "2.0", Titulo = "Cirugía v2", Texto = new string('y', 60) });
        Assert.Equal(201, publicado.Estado);
        Assert.False(d.Documentos.First(x => x.Id == 1).Vigente);
        Assert.Equal(ConsentimientoService.VersionNueva, (await servicio.EstadoAsync(Ana, 1)).Valor!.Estado);
        Assert.Single(await servicio.PendientesAsync(Ana));
    }

    [Fact]
    public async Task Consentimiento_RechazoTurnoAjenoServicioSinConsentimientoYTurnoEmpezado()
    {
        var d = Datos();
        var servicio = ServicioConsentimiento(d);

        Assert.Equal(404, (await servicio.ResponderAsync(Otro, 1, new RespuestaConsentimientoDto { Acepta = false })).Estado);
        Assert.Equal("NO_REQUIERE_CONSENTIMIENTO", (await servicio.ResponderAsync(Ana, 2, new RespuestaConsentimientoDto { Acepta = false })).Codigo);
        Assert.Equal(ConsentimientoService.NoRequiere, (await servicio.EstadoAsync(Ana, 2)).Valor!.Estado);

        var rechazo = await servicio.ResponderAsync(Ana, 1, new RespuestaConsentimientoDto { Acepta = false });
        Assert.Equal(ConsentimientoService.Rechazado, rechazo.Valor!.Estado);
        Assert.Equal("", d.Consentimientos[0].NombreEscrito);
        Assert.Equal("YA_RECHAZADO", (await servicio.ResponderAsync(Ana, 1, new RespuestaConsentimientoDto { Acepta = false })).Codigo);

        d.Turnos[0].Estado = "En curso";
        Assert.Equal("TURNO_CERRADO", (await servicio.ResponderAsync(Ana, 1, new RespuestaConsentimientoDto { Acepta = true, NombreCompleto = "Ana Pérez" })).Codigo);
    }

    [Fact]
    public async Task PublicarVersion_SoloEnServiciosQueLoPidenYSinRepetirVersion()
    {
        var d = Datos();
        var servicio = ServicioConsentimiento(d);
        var texto = new string('z', 60);

        Assert.Equal("SERVICIO_SIN_CONSENTIMIENTO", (await servicio.PublicarAsync(new DocumentoConsentimientoCreateDto { PrestacionId = 1, Version = "1.0", Titulo = "T", Texto = texto })).Codigo);
        Assert.Equal("VERSION_REPETIDA", (await servicio.PublicarAsync(new DocumentoConsentimientoCreateDto { PrestacionId = 4, Version = "1.0", Titulo = "T", Texto = texto })).Codigo);
        Assert.Single(d.Documentos);
        Assert.Equal(404, (await servicio.DocumentosAsync(99)).Estado);
    }

    // ===================== Roles =====================

    [Fact]
    public void Controladores_TienenLosRolesCorrectos()
    {
        Assert.Equal("Cliente", typeof(SaludController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Veterinario", typeof(StockVeterinarioController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Administrador", typeof(InventarioController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Administrador", typeof(AdminSaludController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }
}
