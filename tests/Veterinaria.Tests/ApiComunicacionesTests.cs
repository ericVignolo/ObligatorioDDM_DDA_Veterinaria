using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Veterinaria.Api.Controllers;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — comunicaciones (RF-CLI-18, RF-CLI-20, RF-REC-07, RNF-CON-04, RNF-CON-10). Reloj fijo: lunes 23/11/2026 10:00.
// Ana (10) tiene a Luna (perro); Pedro (11) a Canela (caballo); Beto (12) no verificó su cuenta.
public class ApiComunicacionesTests
{
    private static readonly DateTime Ahora = new(2026, 11, 23, 10, 0, 0);
    private const int Recepcion = 1;
    private const int Ana = 10;
    private const int Pedro = 11;
    private const int Beto = 12;
    private const int Perro = 1;

    private class Reloj : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(Ahora, DateTimeKind.Utc));
    }

    private class EnvioDePrueba : IEnvioComunicacion
    {
        public bool Falla { get; set; }
        public List<string> Enviados { get; } = new();

        public Task<string?> EnviarAsync(string destino, string asunto, string cuerpo)
        {
            if (Falla)
            {
                return Task.FromResult<string?>("El servidor de correo no respondió.");
            }

            Enviados.Add(destino);
            return Task.FromResult<string?>(null);
        }
    }

    private class ComunicacionesEnMemoria : IComunicacionRepository
    {
        public List<TipoComunicacion> Tipos { get; } = new();
        public List<PreferenciaComunicacion> Preferencias { get; } = new();
        public List<Cuenta> Cuentas { get; } = new();
        public List<Comunicacion> Comunicaciones { get; } = new();
        public List<Pago> Pagos { get; } = new();
        public List<Auditoria> Auditorias { get; } = new();
        public Dictionary<int, int> EspecieDe { get; } = new();

        public Task<List<TipoComunicacion>> ObtenerTiposAsync() => Task.FromResult(Tipos.ToList());
        public Task<TipoComunicacion?> ObtenerTipoAsync(int tipoId) => Task.FromResult(Tipos.FirstOrDefault(t => t.Id == tipoId));
        public Task<List<PreferenciaComunicacion>> ObtenerPreferenciasAsync(int cuentaId) => Task.FromResult(Preferencias.Where(p => p.CuentaId == cuentaId).ToList());
        public Task<PreferenciaComunicacion?> ObtenerPreferenciaAsync(int cuentaId, int tipoId)
            => Task.FromResult(Preferencias.FirstOrDefault(p => p.CuentaId == cuentaId && p.TipoComunicacionId == tipoId));
        public Task<Cuenta?> ObtenerCuentaAsync(int cuentaId) => Task.FromResult(Cuentas.FirstOrDefault(c => c.Id == cuentaId));
        public Task<bool> ExisteConClaveAsync(int cuentaId, int tipoId, string clave)
            => Task.FromResult(Comunicaciones.Any(c => c.CuentaId == cuentaId && c.TipoComunicacionId == tipoId && c.Clave == clave && c.ReenvioDeId == null));

        private Comunicacion ConDatos(Comunicacion c)
        {
            c.Cuenta = Cuentas.First(x => x.Id == c.CuentaId);
            c.TipoComunicacion = Tipos.First(t => t.Id == c.TipoComunicacionId);
            return c;
        }

        public Task<List<Comunicacion>> ObtenerDeCuentaAsync(int cuentaId, int saltar, int tomar)
            => Task.FromResult(Comunicaciones.Where(c => c.CuentaId == cuentaId).OrderByDescending(c => c.Id).Skip(saltar).Take(tomar).Select(ConDatos).ToList());
        public Task<int> ContarDeCuentaAsync(int cuentaId) => Task.FromResult(Comunicaciones.Count(c => c.CuentaId == cuentaId));
        public Task<int> ContarNoLeidasAsync(int cuentaId) => Task.FromResult(Comunicaciones.Count(c => c.CuentaId == cuentaId && !c.Leida));
        public Task<Comunicacion?> ObtenerAsync(long id)
        {
            var c = Comunicaciones.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(c == null ? null : ConDatos(c));
        }

        private IEnumerable<Comunicacion> Busqueda(string? estado, int? tipoId, string? texto)
            => Comunicaciones.Select(ConDatos).Where(c => (estado == null || c.Estado == estado) && (tipoId == null || c.TipoComunicacionId == tipoId)
                && (texto == null || c.Asunto.Contains(texto, StringComparison.OrdinalIgnoreCase) || $"{c.Cuenta!.Nombre} {c.Cuenta.Apellido}".Contains(texto, StringComparison.OrdinalIgnoreCase)));
        public Task<List<Comunicacion>> BuscarAsync(string? estado, int? tipoId, string? texto, int saltar, int tomar)
            => Task.FromResult(Busqueda(estado, tipoId, texto).OrderByDescending(c => c.Id).Skip(saltar).Take(tomar).ToList());
        public Task<int> ContarBusquedaAsync(string? estado, int? tipoId, string? texto) => Task.FromResult(Busqueda(estado, tipoId, texto).Count());
        public Task<List<Comunicacion>> ObtenerPendientesAsync(int tomar) => Task.FromResult(Comunicaciones.Where(c => c.Estado == "Pendiente").Take(tomar).ToList());
        public Task<List<Cuenta>> ObtenerDestinatariosAsync(int? especieId)
            => Task.FromResult(Cuentas.Where(c => c.Verificada && c.Activa && !c.Eliminada && (especieId == null || EspecieDe.GetValueOrDefault(c.Id) == especieId)).ToList());
        public Task<Pago?> ObtenerPagoAsync(int pagoId) => Task.FromResult(Pagos.FirstOrDefault(p => p.Id == pagoId));

        public void Agregar<T>(T entidad) where T : class
        {
            switch (entidad)
            {
                case Comunicacion c: c.Id = Comunicaciones.Count + 1; Comunicaciones.Add(c); break;
                case PreferenciaComunicacion p: Preferencias.Add(p); break;
                case Auditoria a: Auditorias.Add(a); break;
                default: throw new InvalidOperationException($"Tipo no esperado: {typeof(T).Name}");
            }
        }

        public Task GuardarAsync() => Task.CompletedTask;
    }

    private static ComunicacionesEnMemoria Datos()
    {
        var d = new ComunicacionesEnMemoria();
        d.Tipos.Add(new TipoComunicacion { Id = 1, Nombre = "Resumen clínico", Esencial = true, CanalPorDefecto = "Correo" });
        d.Tipos.Add(new TipoComunicacion { Id = 2, Nombre = "Comprobante de cobro", Esencial = true, CanalPorDefecto = "Correo" });
        d.Tipos.Add(new TipoComunicacion { Id = 3, Nombre = "Aviso de la clínica", Esencial = true, CanalPorDefecto = "Correo" });
        d.Tipos.Add(new TipoComunicacion { Id = 4, Nombre = "Aviso de refuerzo", Esencial = false, CanalPorDefecto = "Correo" });
        d.Tipos.Add(new TipoComunicacion { Id = 5, Nombre = "Alerta sanitaria", Esencial = false, CanalPorDefecto = "No recibir" });
        d.Cuentas.Add(new Cuenta { Id = Ana, Nombre = "Ana", Apellido = "Pérez", Correo = "ana@correo.uy", Verificada = true, Activa = true });
        d.Cuentas.Add(new Cuenta { Id = Pedro, Nombre = "Pedro", Apellido = "Gómez", Correo = "pedro@correo.uy", Verificada = true, Activa = true });
        d.Cuentas.Add(new Cuenta { Id = Beto, Nombre = "Beto", Apellido = "Ríos", Correo = "beto@correo.uy", Verificada = false, Activa = true });
        d.EspecieDe[Ana] = Perro;
        d.EspecieDe[Pedro] = 11;
        d.Pagos.Add(new Pago { Id = 3, Numero = "C-000003", Monto = 1200, Medio = "Efectivo", Fecha = Ahora, Cargo = new Cargo { Id = 1, ClienteId = Ana, Descripcion = "Consulta — Luna" } });
        return d;
    }

    private static (ComunicacionService Servicio, ComunicacionesEnMemoria Datos, EnvioDePrueba Envio) Escenario()
    {
        var d = Datos();
        var envio = new EnvioDePrueba();
        return (new ComunicacionService(d, envio, new Reloj()), d, envio);
    }

    [Fact]
    public async Task Preferencias_por_tipo_sin_apagar_los_esenciales()
    {
        var (s, d, _) = Escenario();

        var iniciales = await s.PreferenciasAsync(Ana);
        Assert.Equal("No recibir", iniciales.Single(p => p.TipoId == 5).Canal);

        Assert.Equal("TIPO_ESENCIAL", (await s.CambiarPreferenciaAsync(Ana, 1, "No recibir")).Codigo);
        Assert.Equal("PREFERENCIA_INVALIDA", (await s.CambiarPreferenciaAsync(Ana, 4, "Paloma mensajera")).Codigo);
        Assert.True((await s.CambiarPreferenciaAsync(Ana, 5, "Solo en la app")).Exito);
        Assert.True((await s.CambiarPreferenciaAsync(Ana, 5, "Correo")).Exito);
        Assert.Equal("Correo", Assert.Single(d.Preferencias).Canal);
    }

    [Fact]
    public async Task Comprobante_se_envia_una_vez_y_se_reenvia_como_copia()
    {
        var (s, d, envio) = Escenario();

        var enviado = await s.EnviarComprobanteAsync(Recepcion, "Recepcionista", 3);
        Assert.Equal(201, enviado.Estado);
        Assert.Equal(("Enviado", "Correo", "ana@correo.uy"), (enviado.Valor!.Estado, enviado.Valor.Canal, enviado.Valor.Destino));
        Assert.Contains("C-000003", d.Comunicaciones[0].Cuerpo);
        Assert.Equal("COMPROBANTE_ENVIADO", (await s.EnviarComprobanteAsync(Recepcion, "Recepcionista", 3)).Codigo);
        Assert.Equal(404, (await s.EnviarComprobanteAsync(Recepcion, "Recepcionista", 99)).Estado);

        var copia = await s.ReenviarAsync(Recepcion, "Recepcionista", 1);
        Assert.Equal(201, copia.Estado);
        Assert.Equal(1, copia.Valor!.ReenvioDeId);
        Assert.Equal(2, d.Comunicaciones.Count);
        Assert.Equal(2, envio.Enviados.Count);
        Assert.Equal(2, d.Auditorias.Count);
    }

    [Fact]
    public async Task Cola_reintenta_hasta_tres_veces_y_el_personal_reintenta_el_fallido()
    {
        var (s, d, envio) = Escenario();
        envio.Falla = true;

        await s.EnviarComprobanteAsync(Recepcion, "Recepcionista", 3); // intento 1
        var mensaje = d.Comunicaciones[0];
        Assert.Equal(("Pendiente", 1), (mensaje.Estado, mensaje.Intentos));
        Assert.Equal("NO_FALLIDO", (await s.ReintentarAsync(Recepcion, "Recepcionista", 1)).Codigo);

        await s.ProcesarColaAsync(); // intento 2
        var tercero = await s.ProcesarColaAsync(); // intento 3
        Assert.Equal((1, 0, 1), (tercero.Intentados, tercero.Enviados, tercero.Fallidos));
        Assert.Equal("Fallido", mensaje.Estado);
        Assert.Equal(0, (await s.ProcesarColaAsync()).Intentados);

        envio.Falla = false;
        var reintento = await s.ReintentarAsync(Recepcion, "Recepcionista", 1);
        Assert.Equal("Enviado", reintento.Valor!.Estado);
        Assert.Equal(1, mensaje.Intentos);
    }

    [Fact]
    public async Task Cola_entrega_en_la_app_lo_que_otros_modulos_dejaron_pendiente()
    {
        var (s, d, envio) = Escenario();
        d.Comunicaciones.Add(new Comunicacion { Id = 1, CuentaId = Ana, TipoComunicacionId = 4, Asunto = "Refuerzo", Canal = "En la app", Estado = "Pendiente", FechaCreacion = Ahora });

        var cola = await s.ProcesarColaAsync();
        Assert.Equal((1, 1), (cola.Intentados, cola.Enviados));
        Assert.Equal(Ahora, d.Comunicaciones[0].FechaEnvio);
        Assert.Empty(envio.Enviados);
    }

    [Fact]
    public async Task Bandeja_solo_lo_propio_y_marcar_leida()
    {
        var (s, d, _) = Escenario();
        await s.EnviarComprobanteAsync(Recepcion, "Recepcionista", 3);

        var bandeja = await s.BandejaAsync(Ana, 1, 20);
        Assert.Equal((1, 1), (bandeja.TotalItems, bandeja.NoLeidas));
        Assert.Equal("", bandeja.Items[0].Destino); // el cliente no ve los datos de envío
        Assert.Equal(0, (await s.BandejaAsync(Pedro, 1, 20)).TotalItems);

        Assert.Equal(404, (await s.MarcarLeidaAsync(Pedro, 1)).Estado);
        Assert.True((await s.MarcarLeidaAsync(Ana, 1)).Exito);
        Assert.Equal(0, (await s.BandejaAsync(Ana, 1, 20)).NoLeidas);
    }

    [Fact]
    public async Task Difusion_respeta_preferencias_verificacion_y_especie()
    {
        var (s, d, _) = Escenario();

        // Aviso de la clínica: esencial, a todos los verificados (Beto no).
        var aviso = await s.AvisoOperativoAsync(Recepcion, "Recepcionista", new DifusionDto { Titulo = "Cerramos el 8/12", Mensaje = "Feriado de la clínica." });
        Assert.Equal((2, 0), (aviso.Valor!.Generados, aviso.Valor.Omitidos));

        // Alerta sanitaria: apagada por defecto; Ana la activa solo en la app. Para perros, Pedro ni entra.
        d.Preferencias.Add(new PreferenciaComunicacion { CuentaId = Ana, TipoComunicacionId = 5, Canal = "Solo en la app" });
        var alerta = await s.AlertaSanitariaAsync(Recepcion, "Veterinario", new DifusionDto { Titulo = "Brote de moquillo", Mensaje = "Revisá la vacuna de tu perro.", EspecieId = Perro });
        Assert.Equal((1, 0), (alerta.Valor!.Generados, alerta.Valor.Omitidos));
        var deAna = d.Comunicaciones.Last();
        Assert.Equal(("En la app", "Enviado", ""), (deAna.Canal, deAna.Estado, deAna.Destino));

        var todos = await s.AlertaSanitariaAsync(Recepcion, "Veterinario", new DifusionDto { Titulo = "Calor", Mensaje = "Agua fresca siempre." });
        Assert.Equal((1, 1), (todos.Valor!.Generados, todos.Valor.Omitidos));

        Assert.Equal("DIFUSION_INVALIDA", (await s.AvisoOperativoAsync(Recepcion, "Recepcionista", new DifusionDto { Titulo = " ", Mensaje = "x" })).Codigo);
        Assert.Equal(3, d.Auditorias.Count);
    }

    [Fact]
    public async Task Salida_filtra_por_estado_tipo_y_nombre()
    {
        var (s, d, envio) = Escenario();
        await s.EnviarComprobanteAsync(Recepcion, "Recepcionista", 3);
        envio.Falla = true;
        await s.AvisoOperativoAsync(Recepcion, "Recepcionista", new DifusionDto { Titulo = "Aviso", Mensaje = "Texto" });

        Assert.Equal(3, (await s.SalidaAsync(null, null, null, 1, 20)).TotalItems);
        Assert.Equal(2, (await s.SalidaAsync("Pendiente", null, null, 1, 20)).TotalItems);
        Assert.Equal(1, (await s.SalidaAsync(null, 2, null, 1, 20)).TotalItems);
        var dePedro = await s.SalidaAsync(null, null, "gómez", 1, 20);
        Assert.Equal("Pedro Gómez", Assert.Single(dePedro.Items).Cliente);
    }

    [Fact]
    public void Controladores_tienen_los_roles_correctos()
    {
        Assert.Equal("Cliente", typeof(ComunicacionesController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Recepcionista,Administrador", typeof(SalidaComunicacionesController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
        Assert.Equal("Administrador,Veterinario", typeof(AlertaSanitariaController).GetCustomAttribute<AuthorizeAttribute>()!.Roles);
    }
}
