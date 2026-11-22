using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;
using Veterinaria.Api.Services;

namespace Veterinaria.Tests;

// API — registro, verificación, sesiones y recuperación (ESPECIFICACION_REGISTRO_CLIENTE.md, REG-02 a REG-08;
// D-REG-02, D-REG-03, RNF-SEG-03, RNF-SEG-09). Repositorio, reloj y correo de prueba, sin MySQL.
public class ApiCuentasTests
{
    // ---------- Dobles de prueba ----------

    private class Reloj : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = DateTimeOffset.UtcNow;
        public override DateTimeOffset GetUtcNow() => Ahora;
        public void Avanzar(TimeSpan t) => Ahora = Ahora.Add(t);
    }

    private class CorreoDePrueba : IEnvioCorreo
    {
        public List<(string Destino, string Tipo, string Codigo)> Enviados { get; } = new();

        public Task EnviarCodigoAsync(string destino, string tipo, string codigo, DateTime vence)
        {
            Enviados.Add((destino, tipo, codigo));
            return Task.CompletedTask;
        }

        public string UltimoCodigo => Enviados[^1].Codigo;
    }

    private class CuentasEnMemoria : ICuentaRepository
    {
        public List<Cuenta> Cuentas { get; } = new();
        public List<CodigoCuenta> Codigos { get; } = new();
        public List<SesionRefresco> Sesiones { get; } = new();
        public List<DocumentoLegal> Legales { get; } = new()
        {
            new DocumentoLegal { Id = 1, Codigo = "privacidad", Version = "1.0", VigenteDesde = new DateTime(2026, 11, 2) },
            new DocumentoLegal { Id = 2, Codigo = "terminos", Version = "1.0", VigenteDesde = new DateTime(2026, 11, 2) }
        };

        private static readonly Rol Cliente = new() { Id = 1, Nombre = "Cliente" };

        public Task<Cuenta?> ObtenerPorCorreoAsync(string correo) => Task.FromResult(Cuentas.FirstOrDefault(c => c.Correo == correo));
        public Task<Cuenta?> ObtenerPorIdAsync(int id) => Task.FromResult(Cuentas.FirstOrDefault(c => c.Id == id));
        public Task<bool> ExisteDocumentoAsync(string tipo, string numero)
            => Task.FromResult(Cuentas.Any(c => c.TipoDocumento == tipo && c.NumeroDocumento == numero && !c.Eliminada));
        public Task<List<DocumentoLegal>> ObtenerDocumentosVigentesAsync(DateTime hoy)
            => Task.FromResult(Legales.Where(d => d.VigenteDesde <= hoy).ToList());
        public Task<int> ContarCodigosDesdeAsync(int cuentaId, string tipo, DateTime desde)
            => Task.FromResult(Codigos.Count(c => c.CuentaId == cuentaId && c.Tipo == tipo && c.FechaCreacion >= desde));
        public Task<CodigoCuenta?> ObtenerUltimoCodigoAsync(int cuentaId, string tipo)
            => Task.FromResult(Codigos.Where(c => c.CuentaId == cuentaId && c.Tipo == tipo).OrderByDescending(c => c.FechaCreacion).ThenByDescending(c => c.Id).FirstOrDefault());
        public Task<List<CodigoCuenta>> ObtenerCodigosSinUsarAsync(int cuentaId, string tipo)
            => Task.FromResult(Codigos.Where(c => c.CuentaId == cuentaId && c.Tipo == tipo && !c.Usado).ToList());
        public Task<SesionRefresco?> ObtenerSesionAsync(string hashToken) => Task.FromResult(Sesiones.FirstOrDefault(s => s.HashToken == hashToken));
        public Task<List<SesionRefresco>> ObtenerSesionesActivasAsync(int cuentaId, DateTime ahora)
            => Task.FromResult(Sesiones.Where(s => s.CuentaId == cuentaId && s.RevocadaEn == null && s.Vence > ahora).ToList());

        public void AgregarCuenta(Cuenta cuenta)
        {
            cuenta.Id = Cuentas.Count == 0 ? 10 : Cuentas.Max(c => c.Id) + 1;
            foreach (var rol in cuenta.Roles)
            {
                rol.CuentaId = cuenta.Id;
                rol.Rol ??= Cliente;
            }

            Cuentas.Add(cuenta);
        }

        public void AgregarCodigo(CodigoCuenta codigo)
        {
            codigo.Id = Codigos.Count + 1;
            Codigos.Add(codigo);
        }

        public void AgregarSesion(SesionRefresco sesion)
        {
            sesion.Id = Sesiones.Count + 1;
            Sesiones.Add(sesion);
        }

        public Task<List<Cuenta>> ObtenerNoVerificadasAntesDeAsync(DateTime limite)
            => Task.FromResult(Cuentas.Where(c => !c.Verificada && !c.Eliminada && c.FechaCreacion < limite).ToList());

        public Task EliminarNoVerificadaAsync(Cuenta cuenta)
        {
            Codigos.RemoveAll(c => c.CuentaId == cuenta.Id);
            Cuentas.Remove(cuenta);
            return Task.CompletedTask;
        }

        public Task GuardarAsync()
        {
            // Como EF: el código creado junto con la cuenta recibe el Id de la cuenta al guardar.
            foreach (var codigo in Codigos.Where(c => c.Cuenta != null))
            {
                codigo.CuentaId = codigo.Cuenta!.Id;
            }

            return Task.CompletedTask;
        }
    }

    private class Escenario
    {
        public CuentasEnMemoria Repo { get; } = new();
        public CorreoDePrueba Correo { get; } = new();
        public Reloj Reloj { get; } = new();
        public OpcionesJwt Jwt { get; } = new() { Clave = "clave-de-prueba-de-al-menos-32-caracteres!!" };
        public CuentaService Cuentas { get; }
        public SesionService Sesiones { get; }

        public Escenario(bool mostrarCodigo = false)
        {
            var opciones = Options.Create(new OpcionesCuentas { MostrarCodigoEnRespuesta = mostrarCodigo });
            Cuentas = new CuentaService(Repo, Correo, Reloj, opciones);
            Sesiones = new SesionService(Repo, new TokenService(Options.Create(Jwt)), Reloj, opciones, Options.Create(Jwt));
        }

        public async Task<Cuenta> ClienteVerificadoAsync(string correo = "ana@correo.uy", string contrasena = "Secreta123")
        {
            await Cuentas.RegistrarAsync(Registro(correo, contrasena));
            await Cuentas.VerificarAsync(new VerificacionDto { Correo = correo, Codigo = Correo.UltimoCodigo });
            return Repo.Cuentas.Single(c => c.Correo == correo);
        }
    }

    [Fact]
    public async Task Tarea_diaria_borra_solo_las_cuentas_sin_verificar_de_mas_de_7_dias()
    {
        var e = new Escenario();
        await e.ClienteVerificadoAsync("ana@correo.uy");
        await e.Cuentas.RegistrarAsync(Registro("beto@correo.uy", cedula: "4.444.444-4"));

        e.Reloj.Avanzar(TimeSpan.FromDays(6));
        Assert.Equal(0, await e.Cuentas.EliminarNoVerificadasAsync());

        e.Reloj.Avanzar(TimeSpan.FromDays(2));
        Assert.Equal(1, await e.Cuentas.EliminarNoVerificadasAsync());
        Assert.Equal("ana@correo.uy", Assert.Single(e.Repo.Cuentas).Correo);
    }

    [Fact]
    public async Task Dos_pasos_activar_ingresar_con_codigo_sin_repetirlo_y_desactivar()
    {
        var e = new Escenario();
        var proteccion = new ProteccionSecretos(new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider());
        var sesiones = new SesionService(e.Repo, new TokenService(Options.Create(e.Jwt)), e.Reloj, Options.Create(new OpcionesCuentas()), Options.Create(e.Jwt), proteccion);
        var dosPasos = new SegundoFactorService(e.Repo, proteccion, e.Reloj);
        var cuenta = await e.ClienteVerificadoAsync();
        string CodigoAhora(string secreto) => Totp.Codigo(secreto, Totp.Paso(e.Reloj.GetUtcNow().UtcDateTime));

        var config = (await dosPasos.ConfigurarAsync(cuenta.Id)).Valor!;
        Assert.DoesNotContain(config.Secreto, cuenta.SecretoSegundoFactor); // se guarda cifrado
        Assert.Equal("CODIGO_INVALIDO", (await dosPasos.ActivarAsync(cuenta.Id, "000000")).Codigo);
        Assert.True((await dosPasos.ActivarAsync(cuenta.Id, CodigoAhora(config.Secreto))).Exito);
        Assert.True(cuenta.SegundoFactorActivo);

        // Sin código: pide el segundo factor y no entrega tokens.
        var sinCodigo = await sesiones.IniciarAsync(new InicioSesionDto { Correo = "ana@correo.uy", Contrasena = "Secreta123" }, "prueba");
        Assert.True(sinCodigo.Valor!.RequiereSegundoFactor);
        Assert.Equal("", sinCodigo.Valor.TokenAcceso);

        // El mismo código de la activación no sirve otra vez.
        var repetido = await sesiones.IniciarAsync(new InicioSesionDto { Correo = "ana@correo.uy", Contrasena = "Secreta123", CodigoSegundoFactor = CodigoAhora(config.Secreto) }, "prueba");
        Assert.Equal("CODIGO_SEGUNDO_FACTOR_INVALIDO", repetido.Codigo);
        Assert.Equal(1, cuenta.IntentosFallidos);

        e.Reloj.Avanzar(TimeSpan.FromSeconds(30));
        var ok = await sesiones.IniciarAsync(new InicioSesionDto { Correo = "ana@correo.uy", Contrasena = "Secreta123", CodigoSegundoFactor = CodigoAhora(config.Secreto) }, "prueba");
        Assert.NotEqual("", ok.Valor!.TokenAcceso);
        Assert.False(ok.Valor.RequiereSegundoFactor);
        Assert.Equal(0, cuenta.IntentosFallidos);

        e.Reloj.Avanzar(TimeSpan.FromSeconds(30));
        Assert.Equal("DATOS_INCORRECTOS", (await dosPasos.DesactivarAsync(cuenta.Id, new DesactivacionSegundoFactorDto { Contrasena = "Otra", Codigo = CodigoAhora(config.Secreto) })).Codigo);
        Assert.True((await dosPasos.DesactivarAsync(cuenta.Id, new DesactivacionSegundoFactorDto { Contrasena = "Secreta123", Codigo = CodigoAhora(config.Secreto) })).Exito);
        Assert.False(cuenta.SegundoFactorActivo);
        Assert.Equal("", cuenta.SecretoSegundoFactor);
    }

    private static RegistroCreateDto Registro(string correo = "ana@correo.uy", string contrasena = "Secreta123", string cedula = "1.234.567-2") => new()
    {
        Nombre = "Ana",
        Apellido = "Pérez",
        Correo = correo,
        Telefono = "099 123 456",
        TipoDocumento = "CI",
        NumeroDocumento = cedula,
        Calle = "Rambla",
        NumeroPuerta = "100",
        Localidad = "Nueva Palmira",
        Departamento = "Colonia",
        Contrasena = contrasena,
        AceptaTerminos = true,
        AceptaPrivacidad = true
    };

    private static List<ValidationResult> Validar(object dto)
    {
        var errores = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), errores, validateAllProperties: true);
        return errores;
    }

    // ---------- Registro ----------

    [Fact]
    public async Task Registro_crea_cliente_sin_verificar_con_aceptaciones_y_codigo()
    {
        var e = new Escenario();

        var r = await e.Cuentas.RegistrarAsync(Registro(correo: "  Ana@Correo.UY "));

        Assert.True(r.Exito);
        Assert.Equal(201, r.Estado);
        var cuenta = Assert.Single(e.Repo.Cuentas);
        Assert.Equal("ana@correo.uy", cuenta.Correo);
        Assert.Equal("+59899123456", cuenta.Telefono);
        Assert.Equal("12345672", cuenta.NumeroDocumento);
        Assert.False(cuenta.Verificada);
        Assert.Equal(CuentaService.RolClienteId, Assert.Single(cuenta.Roles).RolId);
        Assert.Equal(new[] { 1, 2 }, cuenta.Aceptaciones.Select(a => a.DocumentoLegalId).OrderBy(i => i));
        Assert.True(Seguridad.ContrasenaCoincide("Secreta123", cuenta.HashContrasena));
        Assert.DoesNotContain("Secreta123", cuenta.HashContrasena);

        // D-REG-02: el código se manda por correo y se guarda solo su hash.
        var codigo = e.Correo.UltimoCodigo;
        Assert.Matches(@"^\d{6}$", codigo);
        var guardado = Assert.Single(e.Repo.Codigos);
        Assert.DoesNotContain(codigo, guardado.HashCodigo);
        Assert.Null(r.Valor!.CodigoDesarrollo);
    }

    [Fact]
    public async Task En_desarrollo_el_codigo_vuelve_en_la_respuesta()
    {
        var e = new Escenario(mostrarCodigo: true);

        var r = await e.Cuentas.RegistrarAsync(Registro());

        Assert.Equal(e.Correo.UltimoCodigo, r.Valor!.CodigoDesarrollo);
    }

    [Fact]
    public async Task REG02_correo_o_documento_ya_registrado_no_duplica()
    {
        var e = new Escenario();
        await e.Cuentas.RegistrarAsync(Registro());

        var mismoCorreo = await e.Cuentas.RegistrarAsync(Registro(correo: "ANA@correo.uy", cedula: "4.444.444-4"));
        var mismoDocumento = await e.Cuentas.RegistrarAsync(Registro(correo: "otra@correo.uy"));

        Assert.Equal(409, mismoCorreo.Estado);
        Assert.Equal("CUENTA_NO_DISPONIBLE", mismoCorreo.Codigo);
        Assert.Equal(409, mismoDocumento.Estado);
        Assert.Single(e.Repo.Cuentas);
    }

    [Fact]
    public async Task Una_cuenta_sin_verificar_de_mas_de_7_dias_libera_el_correo()
    {
        var e = new Escenario();
        await e.Cuentas.RegistrarAsync(Registro());
        e.Reloj.Avanzar(TimeSpan.FromDays(8));

        var r = await e.Cuentas.RegistrarAsync(Registro());

        Assert.True(r.Exito);
        Assert.Single(e.Repo.Cuentas);
    }

    [Fact]
    public void REG03_falta_una_aceptacion_obligatoria()
    {
        var sinTerminos = Registro();
        sinTerminos.AceptaTerminos = false;
        var sinPrivacidad = Registro();
        sinPrivacidad.AceptaPrivacidad = false;

        Assert.Contains(Validar(sinTerminos), v => v.ErrorMessage!.Contains("Términos"));
        Assert.Contains(Validar(sinPrivacidad), v => v.ErrorMessage!.Contains("Privacidad"));
    }

    [Theory]
    [InlineData("CI", "1.234.567-3", "099 123 456")]   // dígito verificador mal
    [InlineData("Pasaporte", "AB1", "099 123 456")]   // documento corto
    [InlineData("CI", "1.234.567-2", "12")]           // teléfono inválido
    public void Datos_invalidos_no_pasan_la_validacion(string tipo, string documento, string telefono)
    {
        var dto = Registro();
        dto.TipoDocumento = tipo;
        dto.NumeroDocumento = documento;
        dto.Telefono = telefono;

        Assert.NotEmpty(Validar(dto));
    }

    [Fact]
    public void Un_registro_completo_es_valido()
    {
        Assert.Empty(Validar(Registro()));
    }

    // ---------- Verificación ----------

    [Fact]
    public async Task Verificar_con_el_codigo_correcto()
    {
        var e = new Escenario();
        await e.Cuentas.RegistrarAsync(Registro());

        var r = await e.Cuentas.VerificarAsync(new VerificacionDto { Correo = "ana@correo.uy", Codigo = e.Correo.UltimoCodigo });

        Assert.True(r.Exito);
        Assert.True(e.Repo.Cuentas[0].Verificada);
    }

    [Fact]
    public async Task REG07_sexto_intento_con_un_codigo_lo_encuentra_invalidado()
    {
        var e = new Escenario();
        await e.Cuentas.RegistrarAsync(Registro());
        var correcto = e.Correo.UltimoCodigo;
        var incorrecto = correcto == "000000" ? "111111" : "000000";

        var respuestas = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            respuestas.Add((await e.Cuentas.VerificarAsync(new VerificacionDto { Correo = "ana@correo.uy", Codigo = incorrecto })).Codigo);
        }

        var sexto = await e.Cuentas.VerificarAsync(new VerificacionDto { Correo = "ana@correo.uy", Codigo = correcto });

        Assert.Equal(new[] { "CODIGO_INCORRECTO", "CODIGO_INCORRECTO", "CODIGO_INCORRECTO", "CODIGO_INCORRECTO", "CODIGO_INVALIDADO" }, respuestas);
        Assert.Equal("CODIGO_INVALIDADO", sexto.Codigo);
        Assert.False(e.Repo.Cuentas[0].Verificada);
    }

    [Fact]
    public async Task El_codigo_vence_a_los_15_minutos()
    {
        var e = new Escenario();
        await e.Cuentas.RegistrarAsync(Registro());
        e.Reloj.Avanzar(TimeSpan.FromMinutes(16));

        var r = await e.Cuentas.VerificarAsync(new VerificacionDto { Correo = "ana@correo.uy", Codigo = e.Correo.UltimoCodigo });

        Assert.Equal("CODIGO_VENCIDO", r.Codigo);
    }

    [Fact]
    public async Task Reenvio_invalida_el_anterior_y_tiene_limite_de_3_por_hora()
    {
        var e = new Escenario();
        await e.Cuentas.RegistrarAsync(Registro());
        var primero = e.Correo.UltimoCodigo;

        var reenvios = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            reenvios.Add((await e.Cuentas.ReenviarCodigoAsync("ana@correo.uy")).Estado);
        }

        Assert.Equal(new[] { 200, 200, 200, 429 }, reenvios);

        if (primero != e.Correo.UltimoCodigo)
        {
            var conElViejo = await e.Cuentas.VerificarAsync(new VerificacionDto { Correo = "ana@correo.uy", Codigo = primero });
            Assert.False(conElViejo.Exito);
        }

        var conElNuevo = await e.Cuentas.VerificarAsync(new VerificacionDto { Correo = "ana@correo.uy", Codigo = e.Correo.UltimoCodigo });
        Assert.True(conElNuevo.Exito);
    }

    // ---------- Inicio de sesión ----------

    [Fact]
    public async Task REG05_una_cuenta_sin_verificar_no_puede_ingresar()
    {
        var e = new Escenario();
        await e.Cuentas.RegistrarAsync(Registro());

        var r = await e.Sesiones.IniciarAsync(new InicioSesionDto { Correo = "ana@correo.uy", Contrasena = "Secreta123" }, "127.0.0.1");

        Assert.Equal(403, r.Estado);
        Assert.Equal("CUENTA_NO_VERIFICADA", r.Codigo);
        Assert.Empty(e.Repo.Sesiones);
    }

    [Fact]
    public async Task Ingreso_correcto_devuelve_token_con_roles_y_refresco_guardado_como_hash()
    {
        var e = new Escenario();
        var cuenta = await e.ClienteVerificadoAsync();

        var r = await e.Sesiones.IniciarAsync(new InicioSesionDto { Correo = "ANA@correo.uy", Contrasena = "Secreta123" }, "127.0.0.1");

        Assert.True(r.Exito);
        var sesion = r.Valor!;
        Assert.Equal(cuenta.Id, sesion.CuentaId);
        Assert.Equal(new[] { "Cliente" }, sesion.Roles);
        Assert.Equal(e.Reloj.Ahora.UtcDateTime.AddMinutes(15), sesion.VenceTokenAcceso);

        var guardada = Assert.Single(e.Repo.Sesiones);
        Assert.NotEqual(sesion.TokenRefresco, guardada.HashToken);
        Assert.Equal(Seguridad.HashToken(sesion.TokenRefresco), guardada.HashToken);

        // El token está firmado con la clave y lleva la cuenta y el rol.
        var validacion = await new JsonWebTokenHandler().ValidateTokenAsync(sesion.TokenAcceso, new TokenValidationParameters
        {
            ValidIssuer = e.Jwt.Emisor,
            ValidAudience = e.Jwt.Audiencia,
            IssuerSigningKey = TokenService.Clave(e.Jwt)
        });
        Assert.True(validacion.IsValid);
        Assert.Equal(cuenta.Id.ToString(), validacion.Claims[TokenService.ClaimId]);
        Assert.Equal("Cliente", validacion.Claims[TokenService.ClaimRol]);
    }

    [Fact]
    public async Task Correo_inexistente_y_contrasena_mal_dan_la_misma_respuesta()
    {
        var e = new Escenario();
        await e.ClienteVerificadoAsync();

        var malaContrasena = await e.Sesiones.IniciarAsync(new InicioSesionDto { Correo = "ana@correo.uy", Contrasena = "otra-cosa" }, "");
        var inexistente = await e.Sesiones.IniciarAsync(new InicioSesionDto { Correo = "nadie@correo.uy", Contrasena = "otra-cosa" }, "");

        Assert.Equal(401, malaContrasena.Estado);
        Assert.Equal(malaContrasena.Codigo, inexistente.Codigo);
        Assert.Equal(malaContrasena.Mensaje, inexistente.Mensaje);
    }

    [Fact]
    public async Task Al_quinto_fallo_se_bloquea_y_el_bloqueo_crece()
    {
        var e = new Escenario();
        await e.ClienteVerificadoAsync();
        var mal = new InicioSesionDto { Correo = "ana@correo.uy", Contrasena = "equivocada" };
        var bien = new InicioSesionDto { Correo = "ana@correo.uy", Contrasena = "Secreta123" };

        var estados = new List<int>();
        for (var i = 0; i < 5; i++)
        {
            estados.Add((await e.Sesiones.IniciarAsync(mal, "")).Estado);
        }

        Assert.Equal(new[] { 401, 401, 401, 401, 423 }, estados);

        // Bloqueada: ni con la contraseña correcta.
        Assert.Equal(423, (await e.Sesiones.IniciarAsync(bien, "")).Estado);

        // Pasado el minuto, otro fallo bloquea 2 minutos.
        e.Reloj.Avanzar(TimeSpan.FromMinutes(1.5));
        Assert.Equal(423, (await e.Sesiones.IniciarAsync(mal, "")).Estado);
        Assert.Equal(e.Reloj.Ahora.UtcDateTime.AddMinutes(2), e.Repo.Cuentas[0].BloqueadaHasta);

        // Vencido el bloqueo, la contraseña correcta entra y reinicia el contador.
        e.Reloj.Avanzar(TimeSpan.FromMinutes(3));
        Assert.True((await e.Sesiones.IniciarAsync(bien, "")).Exito);
        Assert.Equal(0, e.Repo.Cuentas[0].IntentosFallidos);
    }

    [Fact]
    public async Task Refresco_rota_el_token_y_reusar_el_viejo_cierra_todas_las_sesiones()
    {
        var e = new Escenario();
        await e.ClienteVerificadoAsync();
        var inicio = (await e.Sesiones.IniciarAsync(new InicioSesionDto { Correo = "ana@correo.uy", Contrasena = "Secreta123" }, "")).Valor!;

        var renovada = await e.Sesiones.RefrescarAsync(inicio.TokenRefresco, "");
        Assert.True(renovada.Exito);
        Assert.NotEqual(inicio.TokenRefresco, renovada.Valor!.TokenRefresco);

        var reusoDelViejo = await e.Sesiones.RefrescarAsync(inicio.TokenRefresco, "");
        Assert.Equal("SESION_INVALIDA", reusoDelViejo.Codigo);

        // Por el reúso, también se cerró la sesión nueva.
        Assert.False((await e.Sesiones.RefrescarAsync(renovada.Valor.TokenRefresco, "")).Exito);
    }

    [Fact]
    public async Task Cerrar_sesion_revoca_el_refresco_y_cerrar_dos_veces_no_falla()
    {
        var e = new Escenario();
        await e.ClienteVerificadoAsync();
        var inicio = (await e.Sesiones.IniciarAsync(new InicioSesionDto { Correo = "ana@correo.uy", Contrasena = "Secreta123" }, "")).Valor!;

        await e.Sesiones.CerrarAsync(inicio.TokenRefresco);
        await e.Sesiones.CerrarAsync(inicio.TokenRefresco);

        Assert.False((await e.Sesiones.RefrescarAsync(inicio.TokenRefresco, "")).Exito);
    }

    [Fact]
    public async Task El_refresco_vence_a_los_7_dias()
    {
        var e = new Escenario();
        await e.ClienteVerificadoAsync();
        var inicio = (await e.Sesiones.IniciarAsync(new InicioSesionDto { Correo = "ana@correo.uy", Contrasena = "Secreta123" }, "")).Valor!;
        e.Reloj.Avanzar(TimeSpan.FromDays(7.1));

        Assert.Equal("SESION_INVALIDA", (await e.Sesiones.RefrescarAsync(inicio.TokenRefresco, "")).Codigo);
    }

    // ---------- Recuperación ----------

    [Fact]
    public async Task REG08_recuperacion_responde_igual_exista_o_no_el_correo()
    {
        var e = new Escenario();
        await e.ClienteVerificadoAsync();
        var enviadosAntes = e.Correo.Enviados.Count;

        var existe = await e.Cuentas.PedirRecuperacionAsync("ana@correo.uy");
        var noExiste = await e.Cuentas.PedirRecuperacionAsync("nadie@correo.uy");

        Assert.Equal(existe.Estado, noExiste.Estado);
        Assert.Equal(existe.Valor!.Mensaje, noExiste.Valor!.Mensaje);
        Assert.Equal(enviadosAntes + 1, e.Correo.Enviados.Count);
    }

    [Fact]
    public async Task Cambiar_la_contrasena_cierra_todas_las_sesiones()
    {
        var e = new Escenario();
        await e.ClienteVerificadoAsync();
        var dato = new InicioSesionDto { Correo = "ana@correo.uy", Contrasena = "Secreta123" };
        var sesion1 = (await e.Sesiones.IniciarAsync(dato, "")).Valor!;
        var sesion2 = (await e.Sesiones.IniciarAsync(dato, "")).Valor!;

        await e.Cuentas.PedirRecuperacionAsync("ana@correo.uy");
        var cambio = await e.Cuentas.ConfirmarRecuperacionAsync(new NuevaContrasenaDto { Correo = "ana@correo.uy", Codigo = e.Correo.UltimoCodigo, Contrasena = "NuevaClave456" });

        Assert.True(cambio.Exito);
        Assert.False((await e.Sesiones.RefrescarAsync(sesion1.TokenRefresco, "")).Exito);
        Assert.False((await e.Sesiones.RefrescarAsync(sesion2.TokenRefresco, "")).Exito);
        Assert.Equal(401, (await e.Sesiones.IniciarAsync(dato, "")).Estado);
        Assert.True((await e.Sesiones.IniciarAsync(new InicioSesionDto { Correo = "ana@correo.uy", Contrasena = "NuevaClave456" }, "")).Exito);
    }

    [Fact]
    public async Task Confirmar_recuperacion_con_correo_inexistente_da_codigo_no_valido()
    {
        var e = new Escenario();

        var r = await e.Cuentas.ConfirmarRecuperacionAsync(new NuevaContrasenaDto { Correo = "nadie@correo.uy", Codigo = "123456", Contrasena = "NuevaClave456" });

        Assert.Equal("CODIGO_INVALIDO", r.Codigo);
    }

    [Fact]
    public async Task El_codigo_de_verificacion_no_sirve_para_recuperar()
    {
        var e = new Escenario();
        await e.Cuentas.RegistrarAsync(Registro());
        var codigoVerificacion = e.Correo.UltimoCodigo;
        await e.Cuentas.VerificarAsync(new VerificacionDto { Correo = "ana@correo.uy", Codigo = codigoVerificacion });

        var r = await e.Cuentas.ConfirmarRecuperacionAsync(new NuevaContrasenaDto { Correo = "ana@correo.uy", Codigo = codigoVerificacion, Contrasena = "NuevaClave456" });

        Assert.False(r.Exito);
    }

    // ---------- Perfil y personal de la semilla ----------

    [Fact]
    public async Task El_perfil_no_incluye_hashes()
    {
        var e = new Escenario();
        var cuenta = await e.ClienteVerificadoAsync();

        var perfil = (await e.Cuentas.ObtenerPerfilAsync(cuenta.Id)).Valor!;

        var json = System.Text.Json.JsonSerializer.Serialize(perfil);
        Assert.DoesNotContain("Hash", json);
        Assert.DoesNotContain("Intentos", json);
        Assert.Equal("ana@correo.uy", perfil.Correo);
    }

    [Fact]
    public void El_hash_del_personal_de_la_semilla_sirve_en_la_api()
    {
        Assert.True(Seguridad.ContrasenaCoincide("Vitalia2026!", Veterinaria.Api.Data.ConfiguracionModulos.HashContrasenaDemo));
    }
}
