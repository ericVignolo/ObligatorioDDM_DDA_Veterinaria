using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Veterinaria.Api.Controllers;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — consulta clínica (CU-VET-01, A-019 a A-021, RF-VET-02 a 06, RF-VET-13, P-RC-03 a 07).
// Reloj fijo: lunes 23/11/2026 10:00. Ferreira (cuenta 2) atiende a Luna (perro de Ana, cuenta 10).
public class ApiConsultaTests
{
    private static readonly DateTime Ahora = new(2026, 11, 23, 10, 0, 0);
    private const int Ferreira = 2;
    private const int Silveira = 3;
    private const int Ana = 10;
    private const string Clave = "Vitalia2026!";
    private static readonly string HashClave = Seguridad.DerivarContrasena(Clave);

    private class Reloj : TimeProvider
    {
        public DateTime Ahora { get; set; } = ApiConsultaTests.Ahora;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Ahora, DateTimeKind.Utc));
    }

    private class ConsultasEnMemoria : IConsultaRepository
    {
        public List<Cuenta> Cuentas { get; } = new();
        public List<Profesional> Profesionales { get; } = new();
        public List<Animal> Animales { get; } = new();
        public List<Turno> Turnos { get; } = new();
        public List<Consulta> Consultas { get; } = new();
        public List<Consentimiento> Consentimientos { get; } = new();
        public List<Articulo> Articulos { get; } = new();
        public List<LoteProducto> Lotes { get; } = new();
        public List<ReservaStock> Reservas { get; } = new();
        public List<MovimientoStock> Movimientos { get; } = new();
        public List<AlertaStock> Alertas { get; } = new();
        public List<ProductoSanitario> Productos { get; } = new();
        public List<AplicacionSanitaria> Aplicaciones { get; } = new();
        public List<TarifaPrestacion> Tarifas { get; } = new();
        public List<Cargo> Cargos { get; } = new();
        public List<Comunicacion> Comunicaciones { get; } = new();
        public List<Enmienda> Enmiendas { get; } = new();
        public List<LecturaHistoria> Lecturas { get; } = new();
        public List<Internacion> Internaciones { get; } = new();

        public Task<Turno?> ObtenerTurnoAsync(int turnoId) => Task.FromResult(Turnos.FirstOrDefault(t => t.Id == turnoId));
        public Task<Consulta?> ObtenerAsync(int consultaId) => Task.FromResult(Consultas.FirstOrDefault(c => c.Id == consultaId));
        public Task<Consulta?> ObtenerPorTurnoAsync(int turnoId) => Task.FromResult(Consultas.FirstOrDefault(c => c.TurnoId == turnoId));
        public Task<List<Consulta>> ObtenerAbiertasAsync(int? autorId)
            => Task.FromResult(Consultas.Where(c => c.Estado == "Borrador" && c.Turno?.Estado == "En curso" && (autorId == null || c.AutorId == autorId)).ToList());
        public Task<Cuenta?> ObtenerCuentaAsync(int cuentaId) => Task.FromResult(Cuentas.FirstOrDefault(c => c.Id == cuentaId));
        public Task<Profesional?> ObtenerProfesionalDeCuentaAsync(int cuentaId) => Task.FromResult(Profesionales.FirstOrDefault(p => p.CuentaId == cuentaId && p.Activo));
        public Task<bool> TieneConsentimientoAceptadoAsync(int turnoId)
            => Task.FromResult(Consentimientos.Where(c => c.TurnoId == turnoId).OrderByDescending(c => c.Fecha).ThenByDescending(c => c.Id).FirstOrDefault()?.Respuesta == "Aceptado");
        public Task<List<string>> AlergiasRegistradasAsync(int animalId)
            => Task.FromResult(Consultas.Where(c => c.AnimalId == animalId && c.Estado == "Firmada" && c.Alergias != "").Select(c => c.Alergias).ToList());
        public Task<Articulo?> ObtenerArticuloAsync(int articuloId) => Task.FromResult(Articulos.FirstOrDefault(a => a.Id == articuloId && a.Activo));
        public Task<List<LoteProducto>> ObtenerLotesAsync(int articuloId) => Task.FromResult(Lotes.Where(l => l.ArticuloId == articuloId).OrderBy(l => l.Vencimiento).ToList());
        public Task<Dictionary<int, int>> ReservadoPorLoteAsync(int articuloId)
            => Task.FromResult(Reservas.Where(r => r.ArticuloId == articuloId && !r.Confirmada).GroupBy(r => r.LoteId).ToDictionary(g => g.Key, g => g.Sum(r => r.Cantidad)));
        public Task<List<ReservaStock>> ObtenerReservasAsync(int consultaId) => Task.FromResult(Reservas.Where(r => r.ConsultaId == consultaId).ToList());
        public Task<ProductoSanitario?> ObtenerProductoAsync(int productoId) => Task.FromResult(Productos.FirstOrDefault(p => p.Id == productoId));
        public Task<List<AplicacionSanitaria>> ObtenerAplicacionesDeConsultaAsync(int consultaId) => Task.FromResult(Aplicaciones.Where(a => a.ConsultaId == consultaId).ToList());
        public Task<List<AplicacionSanitaria>> ObtenerAplicacionesQueCuentanAsync(int animalId)
            => Task.FromResult(Aplicaciones.Where(a => a.AnimalId == animalId && (a.ConsultaId == null || Consultas.Any(c => c.Id == a.ConsultaId && c.Estado == "Firmada")))
                .OrderByDescending(a => a.FechaAplicacion).ToList());
        public Task<TarifaPrestacion?> ObtenerTarifaVigenteAsync(int prestacionId, DateTime fecha)
            => Task.FromResult(Tarifas.Where(t => t.PrestacionId == prestacionId && t.VigenteDesde <= fecha.Date).OrderByDescending(t => t.VigenteDesde).FirstOrDefault());
        public Task<bool> ExisteCargoDeConsultaAsync(int consultaId) => Task.FromResult(Cargos.Any(c => c.ConsultaId == consultaId || c.Consulta?.Id == consultaId));
        public decimal SenaPagada { get; set; }
        public Task<decimal> SenaPagadaAsync(int turnoId) => Task.FromResult(SenaPagada);
        public Task<int?> ObtenerResponsableEconomicoAsync(int animalId, int clientePreferido)
        {
            var vigentes = Animales.First(a => a.Id == animalId).Responsables.Where(r => r.Hasta == null).Select(r => r.CuentaId).ToList();
            return Task.FromResult(vigentes.Contains(clientePreferido) ? clientePreferido : vigentes.Cast<int?>().FirstOrDefault());
        }
        public Task<string?> ObtenerCanalPreferidoAsync(int cuentaId, int tipoComunicacionId) => Task.FromResult<string?>(null);
        public Task<TipoComunicacion?> ObtenerTipoComunicacionAsync(int tipoComunicacionId)
            => Task.FromResult<TipoComunicacion?>(new TipoComunicacion { Id = 1, Nombre = "Resumen clínico", Esencial = true, CanalPorDefecto = "Correo" });
        public Task<Animal?> ObtenerAnimalAsync(int animalId) => Task.FromResult(Animales.FirstOrDefault(a => a.Id == animalId));
        public Task<bool> EsResponsableAsync(int animalId, int cuentaId)
            => Task.FromResult(Animales.Any(a => a.Id == animalId && a.Responsables.Any(r => r.CuentaId == cuentaId && r.Hasta == null)));
        public Task<List<Consulta>> ObtenerFirmadasAsync(int animalId) => Task.FromResult(Consultas.Where(c => c.AnimalId == animalId && c.Estado == "Firmada").ToList());
        public Task<bool> TieneCasoActivoAsync(int animalId, DateTime ahora)
            => Task.FromResult(Turnos.Any(t => t.AnimalId == animalId && (t.Estado == "En espera" || t.Estado == "En curso" || (t.Estado == "Confirmado" && t.Inicio.Date == ahora.Date))));

        public void Agregar<T>(T entidad) where T : class
        {
            switch (entidad)
            {
                case Consulta c:
                    c.Id = Consultas.Count + 1;
                    c.Turno = Turnos.First(t => t.Id == c.TurnoId);
                    c.Animal = Animales.First(a => a.Id == c.AnimalId);
                    c.Profesional = Profesionales.First(p => p.Id == c.ProfesionalId);
                    c.Autor = Cuentas.First(x => x.Id == c.AutorId);
                    Consultas.Add(c);
                    break;
                case ReservaStock r:
                    r.Id = Reservas.Count + 1;
                    r.Articulo = Articulos.First(a => a.Id == r.ArticuloId);
                    r.Lote = Lotes.First(l => l.Id == r.LoteId);
                    Reservas.Add(r);
                    break;
                case AplicacionSanitaria a:
                    a.Id = Aplicaciones.Count + 1;
                    Aplicaciones.Add(a);
                    break;
                case MovimientoStock m: Movimientos.Add(m); break;
                case AlertaStock a: Alertas.Add(a); break;
                case Cargo c:
                    c.Id = Cargos.Count + 1;
                    Cargos.Add(c);
                    break;
                case Comunicacion c: Comunicaciones.Add(c); break;
                case Enmienda e:
                    e.Id = Enmiendas.Count + 1;
                    Enmiendas.Add(e);
                    break;
                case LecturaHistoria l: Lecturas.Add(l); break;
                default: throw new InvalidOperationException(typeof(T).Name);
            }
        }

        public void Quitar<T>(T entidad) where T : class
        {
            switch (entidad)
            {
                case ReservaStock r: Reservas.Remove(r); break;
                case AplicacionSanitaria a: Aplicaciones.Remove(a); break;
                default: throw new InvalidOperationException(typeof(T).Name);
            }
        }

        public Task GuardarAsync()
        {
            // Como EF: las claves se completan desde las navegaciones al guardar.
            foreach (var r in Reservas.Where(r => r.AplicacionSanitaria != null))
            {
                r.AplicacionSanitariaId = r.AplicacionSanitaria!.Id;
            }

            foreach (var c in Cargos.Where(c => c.Consulta != null))
            {
                c.ConsultaId = c.Consulta!.Id;
            }

            return Task.CompletedTask;
        }

        public Task<T> EnTransaccionAsync<T>(int consultaId, IEnumerable<int> articuloIds, Func<Task<T>> accion) => accion();
    }

    // ---------- Escenario ----------

    private class Escenario
    {
        public ConsultasEnMemoria Repo { get; } = new();
        public Reloj Reloj { get; } = new();
        public ConsultaService Servicio { get; }

        public Escenario()
        {
            Repo.Cuentas.Add(new Cuenta { Id = Ferreira, Nombre = "Lucía", Apellido = "Ferreira", HashContrasena = HashClave, Activa = true });
            Repo.Cuentas.Add(new Cuenta { Id = Silveira, Nombre = "Martín", Apellido = "Silveira", HashContrasena = HashClave, Activa = true });
            Repo.Cuentas.Add(new Cuenta { Id = Ana, Nombre = "Ana", Apellido = "Pérez", Correo = "ana@correo.uy", Activa = true });
            Repo.Profesionales.Add(new Profesional { Id = 1, CuentaId = Ferreira, Nombre = "Dra. Lucía Ferreira", Activo = true, MatriculaVigenteHasta = new DateTime(2027, 11, 1) });
            Repo.Profesionales.Add(new Profesional { Id = 2, CuentaId = Silveira, Nombre = "Dr. Martín Silveira", Activo = true, MatriculaVigenteHasta = new DateTime(2027, 11, 1) });

            var luna = new Animal { Id = 100, Nombre = "Luna", EspecieId = 1, Especie = new Especie { Id = 1, Nombre = "Perro" }, Activo = true };
            luna.Responsables.Add(new Responsabilidad { CuentaId = Ana, AnimalId = 100 });
            Repo.Animales.Add(luna);

            Repo.Articulos.Add(new Articulo { Id = 1, Nombre = "Vacuna polivalente canina", Tipo = "Vacuna", Unidad = "dosis", StockMinimo = 5, Activo = true });
            Repo.Articulos.Add(new Articulo { Id = 8, Nombre = "Jeringa descartable 3 ml", Tipo = "Insumo", Unidad = "unidad", StockMinimo = 20, Activo = true });
            Repo.Lotes.Add(new LoteProducto { Id = 1, ArticuloId = 1, Numero = "L1-A", Vencimiento = new DateTime(2026, 12, 10), Cantidad = 3 });
            Repo.Lotes.Add(new LoteProducto { Id = 2, ArticuloId = 1, Numero = "L1-B", Vencimiento = new DateTime(2027, 9, 30), Cantidad = 10 });
            Repo.Lotes.Add(new LoteProducto { Id = 3, ArticuloId = 8, Numero = "JER-2611", Vencimiento = new DateTime(2029, 11, 30), Cantidad = 21 });

            var polivalente = new ProductoSanitario { Id = 1, ArticuloId = 1, Nombre = "Vacuna polivalente canina", Tipo = "Vacuna", Laboratorio = "Demo", IntervaloRefuerzoDias = 365, IntervaloMinimoDias = 330 };
            polivalente.Especies.Add(new ProductoSanitarioEspecie { ProductoSanitarioId = 1, EspecieId = 1 });
            var felina = new ProductoSanitario { Id = 3, ArticuloId = 1, Nombre = "Vacuna triple felina", Tipo = "Vacuna", IntervaloRefuerzoDias = 365 };
            felina.Especies.Add(new ProductoSanitarioEspecie { ProductoSanitarioId = 3, EspecieId = 2 });
            Repo.Productos.AddRange(new[] { polivalente, felina });

            Repo.Tarifas.Add(new TarifaPrestacion { Id = 1, PrestacionId = 1, Precio = 1200.40m, VigenteDesde = new DateTime(2026, 11, 1) });
            Repo.Tarifas.Add(new TarifaPrestacion { Id = 8, PrestacionId = 8, Precio = 4500, VigenteDesde = new DateTime(2026, 11, 1) });

            NuevoTurno(1, "En espera");
            Servicio = new ConsultaService(Repo, Reloj);
        }

        public Turno NuevoTurno(int id, string estado, int prestacionId = 1, int profesionalId = 1)
        {
            var prestacion = prestacionId == 8
                ? new Prestacion { Id = 8, Nombre = "Castración", DuracionMinutos = 90, RequiereConsentimiento = true }
                : new Prestacion { Id = 1, Nombre = "Consulta clínica general", DuracionMinutos = 30 };
            var turno = new Turno
            {
                Id = id, AnimalId = 100, Animal = Repo.Animales[0], ClienteId = Ana, ProfesionalId = profesionalId,
                PrestacionId = prestacionId, Prestacion = prestacion, Inicio = Ahora, Fin = Ahora.AddMinutes(30), Estado = estado
            };
            Repo.Turnos.Add(turno);
            return turno;
        }

        public async Task<ConsultaReadDto> IniciarAsync(int turnoId = 1) => (await Servicio.IniciarAsync(Ferreira, turnoId)).Valor!;

        public static ConsultaFirmaDto Firma(int version, string diagnostico = "Otitis externa", string tratamiento = "Limpieza ótica", bool confirma = false) => new()
        {
            Version = version,
            Motivo = "Se rasca la oreja",
            Diagnostico = diagnostico,
            TipoDiagnostico = "Presuntivo",
            Tratamiento = tratamiento,
            Indicaciones = "Control en 10 días",
            Contrasena = Clave,
            ConfirmaAlergia = confirma
        };
    }

    // ---------- Iniciar y guardar ----------

    [Fact]
    public async Task Iniciar_crea_el_borrador_y_pasa_el_turno_a_En_curso_una_sola_vez()
    {
        var e = new Escenario();

        var primero = await e.Servicio.IniciarAsync(Ferreira, 1);
        var segundo = await e.Servicio.IniciarAsync(Ferreira, 1);

        Assert.Equal(201, primero.Estado);
        Assert.Equal("Borrador", primero.Valor!.Estado);
        Assert.Equal("En curso", e.Repo.Turnos[0].Estado);
        Assert.Equal(200, segundo.Estado);
        Assert.Equal(primero.Valor.Id, segundo.Valor!.Id);
        Assert.Single(e.Repo.Consultas);
    }

    [Fact]
    public async Task Solo_inicia_el_veterinario_asignado_y_con_el_paciente_En_espera()
    {
        var e = new Escenario();
        e.NuevoTurno(2, "Confirmado");

        Assert.Equal("TURNO_DE_OTRO", (await e.Servicio.IniciarAsync(Silveira, 1)).Codigo);
        Assert.Equal("TURNO_NO_EN_ESPERA", (await e.Servicio.IniciarAsync(Ferreira, 2)).Codigo);
        Assert.Equal("SIN_PROFESIONAL", (await e.Servicio.IniciarAsync(Ana, 1)).Codigo);
    }

    [Fact]
    public async Task RN10_la_cirugia_no_empieza_sin_consentimiento_aceptado()
    {
        var e = new Escenario();
        e.NuevoTurno(2, "En espera", prestacionId: 8);

        Assert.Equal("FALTA_CONSENTIMIENTO", (await e.Servicio.IniciarAsync(Ferreira, 2)).Codigo);

        e.Repo.Consentimientos.Add(new Consentimiento { Id = 1, TurnoId = 2, Respuesta = "Rechazado", Fecha = Ahora.AddDays(-2) });
        e.Repo.Consentimientos.Add(new Consentimiento { Id = 2, TurnoId = 2, Respuesta = "Aceptado", Fecha = Ahora.AddDays(-1) });
        Assert.True((await e.Servicio.IniciarAsync(Ferreira, 2)).Exito);
    }

    [Fact]
    public async Task Guardar_con_una_version_vieja_no_pisa_los_cambios()
    {
        var e = new Escenario();
        var c = await e.IniciarAsync();

        var guardado = await e.Servicio.GuardarAsync(Ferreira, c.Id, new ConsultaGuardarDto { Version = c.Version, Motivo = "Control" });
        var viejo = await e.Servicio.GuardarAsync(Ferreira, c.Id, new ConsultaGuardarDto { Version = c.Version, Motivo = "Pisado" });

        Assert.Equal(c.Version + 1, guardado.Valor!.Version);
        Assert.Equal("VERSION_DESACTUALIZADA", viejo.Codigo);
        Assert.Equal("Control", e.Repo.Consultas[0].Motivo);
    }

    [Fact]
    public async Task Otro_veterinario_no_ve_ni_edita_el_borrador()
    {
        var e = new Escenario();
        var c = await e.IniciarAsync();

        Assert.Equal(404, (await e.Servicio.ObtenerAsync(Silveira, c.Id)).Estado);
        Assert.Equal(404, (await e.Servicio.GuardarAsync(Silveira, c.Id, new ConsultaGuardarDto { Version = 1 })).Estado);
    }

    // ---------- Insumos y vacunas ----------

    [Fact]
    public async Task Insumo_se_aparta_por_FEFO_sin_pasar_el_stock()
    {
        var e = new Escenario();
        var c = await e.IniciarAsync();

        var ok = await e.Servicio.AgregarInsumoAsync(Ferreira, c.Id, new InsumoDto { ArticuloId = 8, Cantidad = 3 });
        var repetido = await e.Servicio.AgregarInsumoAsync(Ferreira, c.Id, new InsumoDto { ArticuloId = 8, Cantidad = 1 });
        var vacuna = await e.Servicio.AgregarInsumoAsync(Ferreira, c.Id, new InsumoDto { ArticuloId = 1, Cantidad = 1 });

        Assert.Equal(3, Assert.Single(ok.Valor!.Insumos).Cantidad);
        Assert.Equal("INSUMO_REPETIDO", repetido.Codigo);
        Assert.Equal("NO_ES_INSUMO", vacuna.Codigo);
        Assert.Equal(21, e.Repo.Lotes[2].Cantidad); // se descuenta recién al firmar

        await e.Servicio.QuitarInsumoAsync(Ferreira, c.Id, 8);
        var demasiado = await e.Servicio.AgregarInsumoAsync(Ferreira, c.Id, new InsumoDto { ArticuloId = 8, Cantidad = 50 });
        Assert.Equal("SIN_STOCK", demasiado.Codigo);
        Assert.Empty(e.Repo.Reservas);
    }

    [Fact]
    public async Task Vacuna_usa_el_lote_que_vence_antes_y_calcula_el_refuerzo()
    {
        var e = new Escenario();
        var c = await e.IniciarAsync();

        var r = await e.Servicio.AgregarVacunaAsync(Ferreira, c.Id, new AplicacionDto { ProductoSanitarioId = 1 });

        var vacuna = Assert.Single(r.Valor!.Vacunas);
        Assert.Equal("L1-A", vacuna.Lote);
        Assert.Equal(new DateOnly(2027, 11, 23), vacuna.FechaRefuerzo);
        Assert.Equal(1, e.Repo.Reservas.Single().AplicacionSanitariaId);
        Assert.Empty(r.Valor.Insumos); // la dosis no aparece como insumo
    }

    [Fact]
    public async Task Vacuna_de_otra_especie_o_repetida_se_rechaza()
    {
        var e = new Escenario();
        var c = await e.IniciarAsync();
        await e.Servicio.AgregarVacunaAsync(Ferreira, c.Id, new AplicacionDto { ProductoSanitarioId = 1 });

        Assert.Equal("NO_APLICABLE", (await e.Servicio.AgregarVacunaAsync(Ferreira, c.Id, new AplicacionDto { ProductoSanitarioId = 3 })).Codigo);
        Assert.Equal("VACUNA_REPETIDA", (await e.Servicio.AgregarVacunaAsync(Ferreira, c.Id, new AplicacionDto { ProductoSanitarioId = 1 })).Codigo);
    }

    [Fact]
    public async Task RN16_adelantar_la_dosis_exige_justificacion()
    {
        var e = new Escenario();
        e.Repo.Aplicaciones.Add(new AplicacionSanitaria { Id = 50, AnimalId = 100, ProductoSanitarioId = 1, FechaAplicacion = Ahora.Date.AddDays(-30), FechaRefuerzo = Ahora.Date.AddDays(335) });
        var c = await e.IniciarAsync();

        var sin = await e.Servicio.AgregarVacunaAsync(Ferreira, c.Id, new AplicacionDto { ProductoSanitarioId = 1 });
        var con = await e.Servicio.AgregarVacunaAsync(Ferreira, c.Id, new AplicacionDto { ProductoSanitarioId = 1, Justificacion = "Viaja al exterior y le exigen la dosis" });

        Assert.Equal("FALTA_JUSTIFICACION", sin.Codigo);
        Assert.True(con.Valor!.Vacunas.Single().Adelantada);
    }

    // ---------- Firma ----------

    [Fact]
    public async Task Firma_completa_en_una_sola_operacion()
    {
        var e = new Escenario();
        var c = await e.IniciarAsync();
        await e.Servicio.AgregarInsumoAsync(Ferreira, c.Id, new InsumoDto { ArticuloId = 8, Cantidad = 3 });
        await e.Servicio.AgregarVacunaAsync(Ferreira, c.Id, new AplicacionDto { ProductoSanitarioId = 1 });

        var r = await e.Servicio.FirmarAsync(Ferreira, c.Id, Escenario.Firma(c.Version));

        Assert.True(r.Exito);
        var consulta = e.Repo.Consultas[0];
        Assert.Equal("Firmada", consulta.Estado);
        Assert.Equal(Ahora, consulta.FechaFirma);
        Assert.Equal(64, consulta.HashContenido.Length);
        Assert.Equal("Finalizado", e.Repo.Turnos[0].Estado);

        // Stock: se descuenta lo apartado, con su movimiento, y la jeringa cruza el mínimo (21 → 18 < 20).
        Assert.Equal(2, e.Repo.Lotes[0].Cantidad);
        Assert.Equal(18, e.Repo.Lotes[2].Cantidad);
        Assert.All(e.Repo.Reservas, x => Assert.True(x.Confirmada));
        Assert.Equal(2, e.Repo.Movimientos.Count);
        Assert.All(e.Repo.Movimientos, m => Assert.Equal("Consumo", m.Tipo));
        Assert.Equal(8, Assert.Single(e.Repo.Alertas).ArticuloId);

        // Cargo único, con la tarifa vigente redondeada a peso entero (P-RC-07), a nombre de Ana.
        var cargo = Assert.Single(e.Repo.Cargos);
        Assert.Equal(1200m, cargo.Monto);
        Assert.Equal(Ana, cargo.ClienteId);
        Assert.Equal(1200m, r.Valor!.Monto);

        // Resumen al cliente en la cola, una vez.
        var resumen = Assert.Single(e.Repo.Comunicaciones);
        Assert.Equal("Pendiente", resumen.Estado);
        Assert.Equal("ana@correo.uy", resumen.Destino);
        Assert.Equal($"consulta-{c.Id}", resumen.Clave);
    }

    [Fact]
    public async Task Firma_descuenta_la_sena_pagada_del_cargo()
    {
        var e = new Escenario();
        e.Repo.SenaPagada = 360;
        var c = await e.IniciarAsync();

        var r = await e.Servicio.FirmarAsync(Ferreira, c.Id, Escenario.Firma(c.Version));

        Assert.True(r.Exito);
        var cargo = Assert.Single(e.Repo.Cargos);
        Assert.Equal(840m, cargo.Monto);
        Assert.Contains("descontada la seña", cargo.Descripcion);
    }

    [Fact]
    public async Task Firmar_dos_veces_no_repite_stock_cargo_ni_correo()
    {
        var e = new Escenario();
        var c = await e.IniciarAsync();
        await e.Servicio.AgregarInsumoAsync(Ferreira, c.Id, new InsumoDto { ArticuloId = 8, Cantidad = 3 });

        await e.Servicio.FirmarAsync(Ferreira, c.Id, Escenario.Firma(c.Version));
        var segunda = await e.Servicio.FirmarAsync(Ferreira, c.Id, Escenario.Firma(c.Version));

        Assert.True(segunda.Exito);
        Assert.Contains("ya estaba firmada", segunda.Valor!.Mensaje);
        Assert.Single(e.Repo.Cargos);
        Assert.Single(e.Repo.Movimientos);
        Assert.Single(e.Repo.Comunicaciones);
        Assert.Equal(18, e.Repo.Lotes[2].Cantidad);
    }

    [Fact]
    public async Task Firma_rechazada_deja_todo_en_borrador()
    {
        var e = new Escenario();
        var c = await e.IniciarAsync();

        var malaClave = Escenario.Firma(c.Version);
        malaClave.Contrasena = "otra";
        Assert.Equal("CONTRASENA_INCORRECTA", (await e.Servicio.FirmarAsync(Ferreira, c.Id, malaClave)).Codigo);
        Assert.Equal("FALTAN_DATOS", (await e.Servicio.FirmarAsync(Ferreira, c.Id, Escenario.Firma(c.Version, diagnostico: ""))).Codigo);

        e.Repo.Profesionales[0].MatriculaVigenteHasta = Ahora.AddDays(-1);
        Assert.Equal("MATRICULA_VENCIDA", (await e.Servicio.FirmarAsync(Ferreira, c.Id, Escenario.Firma(c.Version))).Codigo);

        Assert.Equal("Borrador", e.Repo.Consultas[0].Estado);
        Assert.Equal("En curso", e.Repo.Turnos[0].Estado);
        Assert.Empty(e.Repo.Cargos);
    }

    [Fact]
    public async Task RN17_indicar_una_alergia_registrada_exige_confirmacion()
    {
        var e = new Escenario();
        e.Repo.Consultas.Add(new Consulta { Id = 99, AnimalId = 100, Estado = "Firmada", Alergias = "Penicilina" });
        var c = await e.IniciarAsync();
        Assert.Contains("Penicilina", c.AlergiasRegistradas);

        var sin = await e.Servicio.FirmarAsync(Ferreira, c.Id, Escenario.Firma(c.Version, tratamiento: "penicilina 1 ml"));
        var con = await e.Servicio.FirmarAsync(Ferreira, c.Id, Escenario.Firma(c.Version, tratamiento: "penicilina 1 ml", confirma: true));

        Assert.Equal("CONFIRMAR_ALERGIA", sin.Codigo);
        Assert.True(con.Exito);
        Assert.Equal("Penicilina", e.Repo.Consultas.Single(x => x.Id == c.Id).AlergiaConfirmada);
    }

    [Fact]
    public async Task Un_lote_que_dejo_de_ser_usable_frena_la_firma()
    {
        var e = new Escenario();
        var c = await e.IniciarAsync();
        await e.Servicio.AgregarVacunaAsync(Ferreira, c.Id, new AplicacionDto { ProductoSanitarioId = 1 });
        e.Repo.Lotes[0].Inhabilitado = true;

        var r = await e.Servicio.FirmarAsync(Ferreira, c.Id, Escenario.Firma(c.Version));

        Assert.Equal("LOTE_NO_USABLE", r.Codigo);
        Assert.Equal("Borrador", e.Repo.Consultas[0].Estado);
    }

    // ---------- Cancelar sin atención y enmiendas ----------

    [Fact]
    public async Task P_RC04_cancelar_sin_atencion_solo_sin_consumos()
    {
        var e = new Escenario();
        var c = await e.IniciarAsync();
        await e.Servicio.AgregarInsumoAsync(Ferreira, c.Id, new InsumoDto { ArticuloId = 8, Cantidad = 1 });

        Assert.Equal("CON_CONSUMOS", (await e.Servicio.CancelarSinAtencionAsync(Ferreira, c.Id, "Se fue")).Codigo);

        await e.Servicio.QuitarInsumoAsync(Ferreira, c.Id, 8);
        Assert.True((await e.Servicio.CancelarSinAtencionAsync(Ferreira, c.Id, "Se fue")).Exito);
        Assert.Equal("Cancelado", e.Repo.Turnos[0].Estado);
        Assert.Equal("Borrador", e.Repo.Consultas[0].Estado); // queda como evidencia
    }

    [Fact]
    public async Task RN12_la_enmienda_no_cambia_el_original()
    {
        var e = new Escenario();
        var c = await e.IniciarAsync();
        var enmienda = new EnmiendaDto { Motivo = "Dosis mal escrita", Texto = "La dosis correcta es 2 ml", Contrasena = Clave };

        Assert.Equal("NO_FIRMADA", (await e.Servicio.EnmendarAsync(Ferreira, c.Id, enmienda)).Codigo);

        await e.Servicio.FirmarAsync(Ferreira, c.Id, Escenario.Firma(c.Version));
        var huella = e.Repo.Consultas[0].HashContenido;

        Assert.Equal("NO_ES_AUTOR", (await e.Servicio.EnmendarAsync(Silveira, c.Id, enmienda)).Codigo);
        var r = await e.Servicio.EnmendarAsync(Ferreira, c.Id, enmienda);

        Assert.Equal("La dosis correcta es 2 ml", Assert.Single(r.Valor!.Enmiendas).Texto);
        Assert.Equal(huella, e.Repo.Consultas[0].HashContenido);
        Assert.Equal("Otitis externa", e.Repo.Consultas[0].Diagnostico);
    }

    // ---------- Historia ----------

    [Fact]
    public async Task P_RC05_sin_caso_activo_la_historia_pide_motivo_y_queda_registrada()
    {
        var e = new Escenario();
        e.Repo.Turnos[0].Estado = "Finalizado";

        var sinMotivo = await e.Servicio.HistoriaParaPersonalAsync(Silveira, 100, "");
        var conMotivo = await e.Servicio.HistoriaParaPersonalAsync(Silveira, 100, "Interconsulta pedida por la Dra. Ferreira");

        Assert.Equal("MOTIVO_REQUERIDO", sinMotivo.Codigo);
        Assert.True(conMotivo.Exito);
        var lectura = Assert.Single(e.Repo.Lecturas);
        Assert.Equal(Silveira, lectura.CuentaId);
        Assert.StartsWith("Interconsulta", lectura.Motivo);
    }

    [Fact]
    public async Task Con_turno_activo_la_historia_no_pide_motivo_pero_igual_registra()
    {
        var e = new Escenario();

        var r = await e.Servicio.HistoriaParaPersonalAsync(Ferreira, 100, "");

        Assert.True(r.Exito);
        Assert.Equal("", Assert.Single(e.Repo.Lecturas).Motivo);
    }

    [Fact]
    public async Task El_cliente_ve_solo_lo_firmado_de_sus_animales()
    {
        var e = new Escenario();
        var c = await e.IniciarAsync();
        await e.Servicio.AgregarVacunaAsync(Ferreira, c.Id, new AplicacionDto { ProductoSanitarioId = 1 });

        var antes = await e.Servicio.HistoriaParaClienteAsync(Ana, 100);
        Assert.Empty(antes.Valor!.Consultas);
        Assert.Empty(antes.Valor.Vacunas); // el borrador no cuenta para el carnet

        await e.Servicio.FirmarAsync(Ferreira, c.Id, Escenario.Firma(c.Version));
        var despues = await e.Servicio.HistoriaParaClienteAsync(Ana, 100);
        Assert.Single(despues.Valor!.Consultas);
        Assert.Single(despues.Valor.Vacunas);

        Assert.Equal(404, (await e.Servicio.HistoriaParaClienteAsync(Silveira, 100)).Estado);
    }

    // ---------- Reglas puras ----------

    [Fact]
    public void FEFO_toma_primero_el_que_vence_antes_y_descuenta_lo_apartado()
    {
        var hoy = Ahora.Date;
        var lotes = new List<LoteProducto>
        {
            new() { Id = 1, Vencimiento = hoy.AddDays(300), Cantidad = 10 },
            new() { Id = 2, Vencimiento = hoy.AddDays(20), Cantidad = 3 },
            new() { Id = 3, Vencimiento = hoy.AddDays(-1), Cantidad = 50 },               // vencido
            new() { Id = 4, Vencimiento = hoy.AddDays(5), Cantidad = 50, Inhabilitado = true }
        };
        var apartado = new Dictionary<int, int> { [2] = 1 };

        var plan = ReglasStock.PlanFefo(lotes, apartado, 4, hoy)!;

        Assert.Equal(new[] { (2, 2), (1, 2) }, plan.Select(p => (p.Lote.Id, p.Cantidad)));
        Assert.Equal(12, ReglasStock.StockDisponible(lotes, apartado, hoy));
        Assert.Null(ReglasStock.PlanFefo(lotes, apartado, 13, hoy));
        Assert.True(ReglasStock.CruzaMinimo(5, 4, 5));
        Assert.False(ReglasStock.CruzaMinimo(4, 3, 5)); // ya estaba debajo: no se repite el aviso
    }

    [Fact]
    public void Alergias_ignoran_ninguna_y_comparan_sin_tildes()
    {
        var alergias = ReglasClinicas.Alergias(new[] { "Ninguna", "Penicilina, ácaros" }, "metamizol");

        Assert.Equal(new[] { "Penicilina", "ácaros", "metamizol" }, alergias);
        Assert.Equal(new[] { "ácaros" }, ReglasClinicas.Choques(alergias, "Evitar contacto con acaros", ""));
        Assert.Empty(ReglasClinicas.Choques(alergias, "Penicilinasa", "")); // palabra completa
        Assert.Equal(1201m, ReglasClinicas.RedondearPesos(1200.5m));
    }

    [Fact]
    public void La_consulta_y_la_historia_del_personal_son_solo_para_veterinarios()
    {
        Assert.Equal("Veterinario", typeof(VeterinarioController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Cliente", typeof(AnimalesController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }
}
