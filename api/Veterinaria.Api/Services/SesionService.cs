using Microsoft.Extensions.Options;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Sesiones de clientes y personal (A-008). El mismo ingreso sirve para todos: los roles viajan en el token.
public class SesionService : ISesionService
{
    private readonly ICuentaRepository _repository;
    private readonly ITokenService _tokens;
    private readonly TimeProvider _reloj;
    private readonly OpcionesCuentas _cuentas;
    private readonly OpcionesJwt _jwt;

    public SesionService(ICuentaRepository repository, ITokenService tokens, TimeProvider reloj,
        IOptions<OpcionesCuentas> cuentas, IOptions<OpcionesJwt> jwt)
    {
        _repository = repository;
        _tokens = tokens;
        _reloj = reloj;
        _cuentas = cuentas.Value;
        _jwt = jwt.Value;
    }

    private DateTime Ahora => _reloj.GetUtcNow().UtcDateTime;

    public async Task<Resultado<SesionReadDto>> IniciarAsync(InicioSesionDto datos, string origen)
    {
        var ahora = Ahora;
        var cuenta = await _repository.ObtenerPorCorreoAsync(ValidacionDatos.NormalizarCorreo(datos.Correo));

        if (cuenta == null || cuenta.Eliminada || !cuenta.Activa)
        {
            // Mismo trabajo y misma respuesta que con una contraseña equivocada: no revela si el correo existe.
            Seguridad.GastarTiempoComoComprobacion(datos.Contrasena);
            return CredencialesInvalidas();
        }

        if (cuenta.BloqueadaHasta > ahora)
        {
            return Bloqueada(cuenta.BloqueadaHasta.Value, ahora);
        }

        if (!Seguridad.ContrasenaCoincide(datos.Contrasena, cuenta.HashContrasena))
        {
            // Bloqueo temporal progresivo (RNF-SEG-09): desde el quinto fallo, 1, 2, 4… minutos, hasta 30.
            cuenta.IntentosFallidos++;
            var excedidos = cuenta.IntentosFallidos - _cuentas.IntentosAntesDeBloquear;
            if (excedidos >= 0)
            {
                var minutos = Math.Min(Math.Pow(2, Math.Min(excedidos, 10)), _cuentas.MinutosBloqueoMaximo);
                cuenta.BloqueadaHasta = ahora.AddMinutes(minutos);
            }

            await _repository.GuardarAsync();
            return cuenta.BloqueadaHasta > ahora ? Bloqueada(cuenta.BloqueadaHasta!.Value, ahora) : CredencialesInvalidas();
        }

        // Se informa recién con la contraseña correcta, para no revelar qué cuentas existen (REG-05).
        if (!cuenta.Verificada)
        {
            return Resultado<SesionReadDto>.Error(403, "CUENTA_NO_VERIFICADA", "Cuenta sin verificar",
                "Tenés que verificar tu correo antes de ingresar. Revisá el código que te enviamos.");
        }

        cuenta.IntentosFallidos = 0;
        cuenta.BloqueadaHasta = null;

        var sesion = NuevaSesion(cuenta, ahora, origen);
        await _repository.GuardarAsync();
        return Resultado<SesionReadDto>.Ok(sesion);
    }

    public async Task<Resultado<SesionReadDto>> RefrescarAsync(string tokenRefresco, string origen)
    {
        var ahora = Ahora;
        var guardada = await _repository.ObtenerSesionAsync(Seguridad.HashToken(tokenRefresco));
        if (guardada == null || guardada.Vence <= ahora)
        {
            return SesionInvalida();
        }

        if (guardada.RevocadaEn != null)
        {
            // Un token ya usado o revocado que vuelve a aparecer puede ser robado: se cierran todas las sesiones de la cuenta.
            foreach (var activa in await _repository.ObtenerSesionesActivasAsync(guardada.CuentaId, ahora))
            {
                activa.RevocadaEn = ahora;
            }

            await _repository.GuardarAsync();
            return SesionInvalida();
        }

        var cuenta = await _repository.ObtenerPorIdAsync(guardada.CuentaId);
        guardada.RevocadaEn = ahora; // rotación: cada token de refresco sirve una sola vez

        if (cuenta == null || cuenta.Eliminada || !cuenta.Activa)
        {
            await _repository.GuardarAsync();
            return SesionInvalida();
        }

        var sesion = NuevaSesion(cuenta, ahora, origen);
        await _repository.GuardarAsync();
        return Resultado<SesionReadDto>.Ok(sesion);
    }

    public async Task CerrarAsync(string tokenRefresco)
    {
        var guardada = await _repository.ObtenerSesionAsync(Seguridad.HashToken(tokenRefresco));
        if (guardada != null && guardada.RevocadaEn == null)
        {
            guardada.RevocadaEn = Ahora;
            await _repository.GuardarAsync();
        }
    }

    private SesionReadDto NuevaSesion(Cuenta cuenta, DateTime ahora, string origen)
    {
        var roles = CuentaService.NombresRoles(cuenta);
        var (tokenAcceso, venceAcceso) = _tokens.CrearTokenAcceso(cuenta, roles, ahora);

        var tokenRefresco = Seguridad.GenerarTokenRefresco();
        var venceRefresco = ahora.AddDays(_jwt.DiasRefresco);
        _repository.AgregarSesion(new SesionRefresco
        {
            CuentaId = cuenta.Id,
            HashToken = Seguridad.HashToken(tokenRefresco),
            FechaCreacion = ahora,
            Vence = venceRefresco,
            Origen = origen.Length > 60 ? origen[..60] : origen
        });

        return new SesionReadDto
        {
            TokenAcceso = tokenAcceso,
            VenceTokenAcceso = venceAcceso,
            TokenRefresco = tokenRefresco,
            VenceTokenRefresco = venceRefresco,
            CuentaId = cuenta.Id,
            Nombre = $"{cuenta.Nombre} {cuenta.Apellido}".Trim(),
            Roles = roles
        };
    }

    private static Resultado<SesionReadDto> CredencialesInvalidas()
        => Resultado<SesionReadDto>.Error(401, "CREDENCIALES_INVALIDAS", "No se pudo ingresar", "El correo o la contraseña no son correctos.");

    private static Resultado<SesionReadDto> SesionInvalida()
        => Resultado<SesionReadDto>.Error(401, "SESION_INVALIDA", "Sesión vencida", "Tu sesión venció. Volvé a ingresar.");

    private static Resultado<SesionReadDto> Bloqueada(DateTime hasta, DateTime ahora)
    {
        var minutos = Math.Max(1, (int)Math.Ceiling((hasta - ahora).TotalMinutes));
        return Resultado<SesionReadDto>.Error(423, "CUENTA_BLOQUEADA", "Ingreso bloqueado",
            $"Por seguridad, el ingreso quedó bloqueado por {minutos} minuto{(minutos == 1 ? "" : "s")}. Probá más tarde o recuperá tu contraseña.");
    }
}
