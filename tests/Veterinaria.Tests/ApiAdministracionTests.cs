using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Veterinaria.Api.Controllers;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — administración (RF-ADM-01 a RF-ADM-04, RN-33). Reloj fijo: lunes 23/11/2026 10:00.
// Gonzalo (cuenta 5) es el único Administrador; Ferreira (cuenta 2) es veterinaria con la ficha 1.
public class ApiAdministracionTests
{
    private static readonly DateTime Ahora = new(2026, 11, 23, 10, 0, 0);
    private static readonly DateTime Hoy = Ahora.Date;
    private const int Recepcion = 1;
    private const int Ferreira = 2;
    private const int Admin = 5;

    private class Reloj : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Ahora, DateTimeKind.Utc));
    }

    private class AdminEnMemoria : IAdministracionRepository
    {
        public List<Prestacion> Prestaciones { get; } = new();
        public List<TarifaPrestacion> Tarifas { get; } = new();
        public List<Cuenta> Cuentas { get; } = new();
        public List<Rol> Roles { get; } = new();
        public List<Profesional> Profesionales { get; } = new();
        public List<SesionRefresco> Sesiones { get; } = new();
        public List<Ausencia> Ausencias { get; } = new();
        public List<Turno> Turnos { get; } = new();
        public List<DiaNoLaborable> Feriados { get; } = new();
        public List<Parametro> Parametros { get; } = new();
        public List<Articulo> Articulos { get; } = new();
        public List<Auditoria> Auditorias { get; } = new();

        public Task<List<Prestacion>> ObtenerPrestacionesAsync() => Task.FromResult(Prestaciones.OrderBy(p => p.Nombre).ToList());
        public Task<Prestacion?> ObtenerPrestacionAsync(int id) => Task.FromResult(Prestaciones.FirstOrDefault(p => p.Id == id));
        public Task<bool> ExistePrestacionConNombreAsync(string nombre, int exceptoId)
            => Task.FromResult(Prestaciones.Any(p => p.Id != exceptoId && string.Equals(p.Nombre, nombre, StringComparison.OrdinalIgnoreCase)));
        public Task<List<TarifaPrestacion>> ObtenerTarifasAsync(int prestacionId)
            => Task.FromResult(Tarifas.Where(t => t.PrestacionId == prestacionId || t.Prestacion?.Id == prestacionId).OrderByDescending(t => t.VigenteDesde).ToList());

        public Task<List<Cuenta>> ObtenerPersonalAsync()
            => Task.FromResult(Cuentas.Where(c => c.Roles.Any(r => r.Rol!.Nombre != "Cliente")).ToList());
        public Task<Cuenta?> ObtenerCuentaConRolesAsync(int cuentaId) => Task.FromResult(Cuentas.FirstOrDefault(c => c.Id == cuentaId));
        public Task<bool> ExisteCorreoAsync(string correo) => Task.FromResult(Cuentas.Any(c => c.Correo == correo));
        public Task<List<Rol>> ObtenerRolesAsync() => Task.FromResult(Roles);
        public Task<Profesional?> ObtenerProfesionalAsync(int profesionalId) => Task.FromResult(Profesionales.FirstOrDefault(p => p.Id == profesionalId));
        public Task<Profesional?> ObtenerProfesionalDeCuentaAsync(int cuentaId)
            => Task.FromResult(Profesionales.FirstOrDefault(p => p.CuentaId == cuentaId || (p.Cuenta != null && p.Cuenta.Id == cuentaId && cuentaId != 0)));
        public Task<int> ContarAdministradoresActivosAsync(int exceptoCuentaId)
            => Task.FromResult(Cuentas.Count(c => c.Id != exceptoCuentaId && c.Activa && c.Roles.Any(r => r.Rol!.Nombre == "Administrador")));
        public Task<List<SesionRefresco>> ObtenerSesionesActivasAsync(int cuentaId) => Task.FromResult(Sesiones.Where(s => s.CuentaId == cuentaId && s.RevocadaEn == null).ToList());

        public Task<List<Ausencia>> ObtenerAusenciasAsync(int? profesionalId, DateTime desde)
            => Task.FromResult(Ausencias.Where(a => (profesionalId == null || a.ProfesionalId == profesionalId) && a.Hasta >= desde).ToList());
        public Task<Ausencia?> ObtenerAusenciaAsync(int id) => Task.FromResult(Ausencias.FirstOrDefault(a => a.Id == id));
        public Task<int> ContarTurnosActivosAsync(DateTime desde, DateTime hasta, int? profesionalId)
            => Task.FromResult(Turnos.Count(t => t.Inicio < hasta && t.Fin > desde && (profesionalId == null || t.ProfesionalId == profesionalId) && CalculoAgenda.Ocupa(t)));
        public Task<List<DiaNoLaborable>> ObtenerFeriadosAsync(DateTime desde) => Task.FromResult(Feriados.Where(f => f.Fecha >= desde).ToList());
        public Task<DiaNoLaborable?> ObtenerFeriadoAsync(DateTime fecha) => Task.FromResult(Feriados.FirstOrDefault(f => f.Fecha == fecha.Date));

        public Task<List<Parametro>> ObtenerParametrosAsync() => Task.FromResult(Parametros.ToList());
        public Task<Parametro?> ObtenerParametroAsync(string clave) => Task.FromResult(Parametros.FirstOrDefault(p => p.Clave == clave));
        public Task<Articulo?> ObtenerArticuloAsync(int articuloId) => Task.FromResult(Articulos.FirstOrDefault(a => a.Id == articuloId));
        public Task<List<Auditoria>> ObtenerAuditoriaAsync(string? entidad, DateTime? desde, int saltar, int tomar)
            => Task.FromResult(Auditorias.Where(a => (entidad == null || a.Entidad == entidad) && (desde == null || a.Fecha >= desde)).Skip(saltar).Take(tomar).ToList());
        public Task<int> ContarAuditoriaAsync(string? entidad, DateTime? desde)
            => Task.FromResult(Auditorias.Count(a => (entidad == null || a.Entidad == entidad) && (desde == null || a.Fecha >= desde)));
        public Task<Dictionary<int, string>> NombresDeCuentasAsync(IEnumerable<int> ids)
            => Task.FromResult(Cuentas.Where(c => ids.Contains(c.Id)).ToDictionary(c => c.Id, c => $"{c.Nombre} {c.Apellido}"));

        public void Agregar<T>(T entidad) where T : class
        {
            switch (entidad)
            {
                case Prestacion p: p.Id = Prestaciones.Max(x => x.Id) + 1; Prestaciones.Add(p); break;
                case TarifaPrestacion t: t.Id = Tarifas.Count + 1; t.PrestacionId = t.Prestacion?.Id ?? t.PrestacionId; Tarifas.Add(t); break;
                case Cuenta c:
                    c.Id = Cuentas.Max(x => x.Id) + 1;
                    foreach (var r in c.Roles) { r.CuentaId = c.Id; r.Rol = Roles.First(x => x.Id == r.RolId); }
                    Cuentas.Add(c);
                    break;
                case CuentaRol cr: break;
                case Ausencia a: a.Id = Ausencias.Count + 1; Ausencias.Add(a); break;
                case DiaNoLaborable d: d.Id = Feriados.Count + 1; Feriados.Add(d); break;
                case Parametro p: Parametros.Add(p); break;
                case Auditoria a: Auditorias.Add(a); break;
                default: throw new InvalidOperationException($"Tipo no esperado: {typeof(T).Name}");
            }
        }

        public void Quitar<T>(T entidad) where T : class
        {
            switch (entidad)
            {
                case CuentaRol cr: Cuentas.First(c => c.Id == cr.CuentaId).Roles.Remove(cr); break;
                case Ausencia a: Ausencias.Remove(a); break;
                case DiaNoLaborable d: Feriados.Remove(d); break;
                default: throw new InvalidOperationException($"Tipo no esperado: {typeof(T).Name}");
            }
        }

        public Task GuardarAsync()
        {
            // Como EF al guardar: los roles nuevos quedan con su Rol cargado.
            foreach (var r in Cuentas.SelectMany(c => c.Roles))
            {
                r.Rol ??= Roles.First(x => x.Id == r.RolId);
            }

            return Task.CompletedTask;
        }
    }

    private static Cuenta Persona(AdminEnMemoria d, int id, string nombre, string rol)
    {
        var r = d.Roles.First(x => x.Nombre == rol);
        var c = new Cuenta { Id = id, Nombre = nombre, Apellido = "X", Correo = $"{nombre.ToLowerInvariant()}@vitalia.uy", Activa = true };
        c.Roles.Add(new CuentaRol { CuentaId = id, RolId = r.Id, Rol = r });
        d.Cuentas.Add(c);
        return c;
    }

    private static AdminEnMemoria Datos()
    {
        var d = new AdminEnMemoria();
        d.Roles.AddRange(new[] { new Rol { Id = 1, Nombre = "Cliente" }, new Rol { Id = 2, Nombre = "Recepcionista" }, new Rol { Id = 3, Nombre = "Veterinario" }, new Rol { Id = 4, Nombre = "Administrador" } });
        Persona(d, Recepcion, "Laura", "Recepcionista");
        Persona(d, Ferreira, "Lucia", "Veterinario");
        Persona(d, Admin, "Gonzalo", "Administrador");
        d.Profesionales.Add(new Profesional { Id = 1, CuentaId = Ferreira, Nombre = "Dra. Lucía Ferreira" });
        d.Profesionales.Add(new Profesional { Id = 4, Nombre = "Dr. Andrés Pereira" });
        d.Prestaciones.Add(new Prestacion { Id = 1, Nombre = "Consulta clínica general", PrecioDesde = 1200, DuracionMinutos = 30, ReservableEnLinea = true, Publicado = true });
        d.Tarifas.Add(new TarifaPrestacion { Id = 1, PrestacionId = 1, Precio = 1200, VigenteDesde = Hoy.AddYears(-1) });
        d.Parametros.Add(new Parametro { Clave = "agenda.horizonte_dias", Valor = "60", Descripcion = "Días hacia adelante." });
        d.Parametros.Add(new Parametro { Clave = "asistencia.cobertura_km", Valor = "30", Descripcion = "Zona de cobertura." });
        d.Articulos.Add(new Articulo { Id = 8, Nombre = "Jeringa", Unidad = "unidad", StockMinimo = 20 });
        d.Turnos.Add(new Turno { Id = 1, ProfesionalId = 1, Inicio = Hoy.AddDays(3).AddHours(9), Fin = Hoy.AddDays(3).AddHours(9.5), Estado = "Confirmado" });
        return d;
    }

    private static AdministracionService Servicio(AdminEnMemoria d) => new(d, new Reloj());

    [Fact]
    public async Task Servicios_crear_con_tarifa_inicial_editar_sin_precio_y_sin_nombre_repetido()
    {
        var d = Datos();
        var s = Servicio(d);

        var repetido = await s.CrearServicioAsync(Admin, new ServicioFormDto { Nombre = "consulta clínica general", Precio = 900, DuracionMinutos = 30 });
        Assert.Equal("SERVICIO_REPETIDO", repetido.Codigo);
        Assert.Equal("DURACION_CORTA", (await s.CrearServicioAsync(Admin, new ServicioFormDto { Nombre = "Control", Precio = 900, ReservableEnLinea = true, DuracionMinutos = 10 })).Codigo);

        var creado = await s.CrearServicioAsync(Admin, new ServicioFormDto { Nombre = "Control de peso", Precio = 800.4m, DuracionMinutos = 15, ReservableEnLinea = true });
        Assert.Equal(201, creado.Estado);
        Assert.Equal(800m, creado.Valor!.PrecioVigente);
        Assert.Equal(Hoy, d.Tarifas.Last().VigenteDesde);

        var editado = await s.EditarServicioAsync(Admin, creado.Valor.Id, new ServicioFormDto { Nombre = "Control de peso", Precio = 5000, DuracionMinutos = 20, Publicado = false });
        Assert.False(editado.Valor!.Publicado);
        Assert.Equal(800m, editado.Valor.PrecioVigente); // el precio no cambia al editar (RN-33)
        Assert.Equal(2, d.Auditorias.Count);
    }

    [Fact]
    public async Task Tarifa_rige_desde_hoy_o_despues_y_no_repite_fecha()
    {
        var d = Datos();
        var s = Servicio(d);
        var manana = DateOnly.FromDateTime(Hoy.AddDays(1));

        Assert.Equal("TARIFA_PASADA", (await s.ProgramarTarifaAsync(Admin, new TarifaCreateDto { PrestacionId = 1, Precio = 1500, VigenteDesde = DateOnly.FromDateTime(Hoy.AddDays(-1)) })).Codigo);

        var futura = await s.ProgramarTarifaAsync(Admin, new TarifaCreateDto { PrestacionId = 1, Precio = 1500, VigenteDesde = manana });
        Assert.Equal(201, futura.Estado);
        Assert.False(futura.Valor!.Vigente);
        Assert.Equal(1200m, d.Prestaciones[0].PrecioDesde); // todavía no rige

        Assert.Equal("TARIFA_REPETIDA", (await s.ProgramarTarifaAsync(Admin, new TarifaCreateDto { PrestacionId = 1, Precio = 1600, VigenteDesde = manana })).Codigo);

        var tarifas = (await s.TarifasAsync(1)).Valor!;
        Assert.Equal(2, tarifas.Count);
        Assert.True(tarifas.Single(t => t.Precio == 1200).Vigente);
    }

    [Fact]
    public async Task Personal_crear_con_correo_unico_y_veterinario_con_ficha_libre()
    {
        var d = Datos();
        var s = Servicio(d);

        Assert.Equal("CORREO_REPETIDO", (await s.CrearPersonalAsync(Admin, new PersonalCreateDto { Nombre = "Otra", Apellido = "Laura", Correo = "LAURA@vitalia.uy", Rol = "Recepcionista", Contrasena = "Vitalia2026!" })).Codigo);
        Assert.Equal("ROL_INVALIDO", (await s.CrearPersonalAsync(Admin, new PersonalCreateDto { Nombre = "A", Apellido = "B", Correo = "a@vitalia.uy", Rol = "Cliente", Contrasena = "Vitalia2026!" })).Codigo);
        Assert.Equal("FICHA_INVALIDA", (await s.CrearPersonalAsync(Admin, new PersonalCreateDto { Nombre = "A", Apellido = "B", Correo = "a@vitalia.uy", Rol = "Veterinario", ProfesionalId = 1, Contrasena = "Vitalia2026!" })).Codigo);

        var nuevo = await s.CrearPersonalAsync(Admin, new PersonalCreateDto { Nombre = "Andrés", Apellido = "Pereira", Correo = "Pereira@Vitalia.uy", Rol = "Veterinario", ProfesionalId = 4, Contrasena = "Vitalia2026!" });
        Assert.Equal(201, nuevo.Estado);
        Assert.Equal(("pereira@vitalia.uy", "Veterinario", 4), (nuevo.Valor!.Correo, nuevo.Valor.Rol, nuevo.Valor.ProfesionalId!.Value));
        var cuenta = d.Cuentas.Last();
        Assert.True(cuenta.Verificada);
        Assert.True(Seguridad.ContrasenaCoincide("Vitalia2026!", cuenta.HashContrasena));
        Assert.Same(cuenta, d.Profesionales[1].Cuenta);
    }

    [Fact]
    public async Task Rol_y_bloqueo_respetan_al_ultimo_administrador_y_cierran_sesiones()
    {
        var d = Datos();
        d.Sesiones.Add(new SesionRefresco { Id = 1, CuentaId = Ferreira });
        var s = Servicio(d);

        Assert.Equal("PROPIO_ROL", (await s.CambiarRolAsync(Admin, Admin, new CambioRolDto { Rol = "Recepcionista" })).Codigo);
        Assert.Equal("PROPIA_CUENTA", (await s.CambiarBloqueoAsync(Admin, Admin, true)).Codigo);

        // Ferreira pasa a Recepción: deja libre su ficha y se cierran sus sesiones.
        var cambio = await s.CambiarRolAsync(Admin, Ferreira, new CambioRolDto { Rol = "Recepcionista" });
        Assert.Equal("Recepcionista", cambio.Valor!.Rol);
        Assert.Null(d.Profesionales[0].CuentaId);
        Assert.NotNull(d.Sesiones[0].RevocadaEn);
        Assert.Single(d.Cuentas.First(c => c.Id == Ferreira).Roles);

        // Laura pasa a Administradora; después Gonzalo deja de serlo sin problema... salvo que Laura esté bloqueada.
        await s.CambiarRolAsync(Admin, Recepcion, new CambioRolDto { Rol = "Administrador" });
        Assert.True((await s.CambiarBloqueoAsync(Admin, Recepcion, true)).Exito);
        Assert.Equal("SIN_CAMBIO", (await s.CambiarBloqueoAsync(Admin, Recepcion, true)).Codigo);

        var otra = new AdministracionService(d, new Reloj());
        Assert.Equal("ULTIMO_ADMINISTRADOR", (await otra.CambiarRolAsync(Recepcion, Admin, new CambioRolDto { Rol = "Recepcionista" })).Codigo);
    }

    [Fact]
    public async Task Ausencias_y_feriados_no_pisan_turnos_activos()
    {
        var d = Datos();
        var s = Servicio(d);
        var turno = DateOnly.FromDateTime(Hoy.AddDays(3));

        var conTurno = await s.AgregarAusenciaAsync(Admin, new AusenciaCreateDto { ProfesionalId = 1, Desde = turno, Hasta = turno.AddDays(2), Motivo = "Congreso" });
        Assert.Equal("TURNOS_ACTIVOS", conTurno.Codigo);
        Assert.Equal("FECHAS_INVALIDAS", (await s.AgregarAusenciaAsync(Admin, new AusenciaCreateDto { ProfesionalId = 1, Desde = turno.AddDays(5), Hasta = turno.AddDays(4), Motivo = "Licencia" })).Codigo);

        var ok = await s.AgregarAusenciaAsync(Admin, new AusenciaCreateDto { ProfesionalId = 1, Desde = turno.AddDays(5), Hasta = turno.AddDays(9), Motivo = "Licencia" });
        Assert.Equal(201, ok.Estado);
        Assert.Equal("AUSENCIA_SUPERPUESTA", (await s.AgregarAusenciaAsync(Admin, new AusenciaCreateDto { ProfesionalId = 1, Desde = turno.AddDays(9), Hasta = turno.AddDays(10), Motivo = "Licencia" })).Codigo);
        Assert.True((await s.QuitarAusenciaAsync(Admin, ok.Valor!.Id)).Exito);
        Assert.Empty(d.Ausencias);

        Assert.Equal("TURNOS_ACTIVOS", (await s.AgregarFeriadoAsync(Admin, new FeriadoCreateDto { Fecha = turno })).Codigo);
        Assert.Equal("FECHA_PASADA", (await s.AgregarFeriadoAsync(Admin, new FeriadoCreateDto { Fecha = DateOnly.FromDateTime(Hoy.AddDays(-1)) })).Codigo);
        var feriado = DateOnly.FromDateTime(Hoy.AddDays(15));
        Assert.Equal(201, (await s.AgregarFeriadoAsync(Admin, new FeriadoCreateDto { Fecha = feriado, Motivo = "Día de la clínica" })).Estado);
        Assert.Equal("FERIADO_REPETIDO", (await s.AgregarFeriadoAsync(Admin, new FeriadoCreateDto { Fecha = feriado })).Codigo);
        Assert.True((await s.QuitarFeriadoAsync(Admin, feriado)).Exito);
        Assert.Equal(404, (await s.QuitarFeriadoAsync(Admin, feriado)).Estado);
    }

    [Fact]
    public async Task Parametros_con_rango_se_crean_si_faltan_y_los_desconocidos_no_se_cambian()
    {
        var d = Datos();
        var s = Servicio(d);

        var lista = await s.ParametrosAsync();
        Assert.Equal("30", lista.Single(p => p.Clave == "cobro.porcentaje_sena").Valor); // por defecto
        Assert.Contains(lista, p => p.Clave == "asistencia.cobertura_km");

        Assert.Equal("VALOR_FUERA_DE_RANGO", (await s.CambiarParametroAsync(Admin, "cobro.porcentaje_sena", new ParametroUpdateDto { Valor = "5" })).Codigo);
        Assert.Equal("VALOR_FUERA_DE_RANGO", (await s.CambiarParametroAsync(Admin, "agenda.horizonte_dias", new ParametroUpdateDto { Valor = "muchos" })).Codigo);
        Assert.Equal(404, (await s.CambiarParametroAsync(Admin, "asistencia.cobertura_km", new ParametroUpdateDto { Valor = "40" })).Estado);

        Assert.Equal("40", (await s.CambiarParametroAsync(Admin, "cobro.porcentaje_sena", new ParametroUpdateDto { Valor = " 40 " })).Valor!.Valor);
        Assert.Equal("40", d.Parametros.Single(p => p.Clave == "cobro.porcentaje_sena").Valor);
        Assert.Equal("cobro.porcentaje_sena: 30 → 40", d.Auditorias.Single().Detalle);
    }

    [Fact]
    public async Task Stock_minimo_y_auditoria_paginada()
    {
        var d = Datos();
        var s = Servicio(d);

        Assert.Equal(404, (await s.CambiarStockMinimoAsync(Admin, 99, new StockMinimoDto { StockMinimo = 5 })).Estado);
        Assert.True((await s.CambiarStockMinimoAsync(Admin, 8, new StockMinimoDto { StockMinimo = 30 })).Exito);
        Assert.Equal(30, d.Articulos[0].StockMinimo);
        await s.AgregarFeriadoAsync(Admin, new FeriadoCreateDto { Fecha = DateOnly.FromDateTime(Hoy.AddDays(20)) });

        var todo = await s.AuditoriaAsync(null, null, 1, 20);
        Assert.Equal(2, todo.TotalItems);
        Assert.All(todo.Items, a => Assert.Equal("Gonzalo X", a.Persona));
        Assert.Single((await s.AuditoriaAsync("Feriado", null, 1, 20)).Items);
    }

    [Fact]
    public void Controlador_solo_para_el_Administrador()
    {
        Assert.Equal("Administrador", typeof(AdministracionController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }
}
