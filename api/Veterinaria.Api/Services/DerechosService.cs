using System.Text.Encodings.Web;
using System.Text.Json;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Derechos del titular, igual a DerechosService de Blazor. Exportar entrega los datos del cliente en JSON (formato
// abierto) y queda registrado. Eliminar reemplaza los datos personales por valores neutros, borra sus
// comunicaciones, códigos y sesiones, y conserva animales, historia clínica, cargos y consentimientos sin
// identificar al titular. Todo en un solo guardado, con auditoría.
public class DerechosService : IDerechosService
{
    public const string Exportacion = "Exportación";
    public const string Eliminacion = "Eliminación";
    public const string Recibida = "Recibida";
    public const string Completada = "Completada";
    public const string Rechazada = "Rechazada";
    public const string Cancelada = "Cancelada";

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private readonly IDerechosRepository _repository;
    private readonly TimeProvider _reloj;

    public DerechosService(IDerechosRepository repository, TimeProvider reloj)
    {
        _repository = repository;
        _reloj = reloj;
    }

    private DateTime Ahora => _reloj.GetLocalNow().DateTime;

    // RNF-LEG-06: los plazos legales no se inventan; quedan «a confirmar» en la tabla.
    public async Task<List<PoliticaConservacionDto>> PoliticasAsync()
    {
        return (await _repository.ObtenerPoliticasAsync()).Select(p => new PoliticaConservacionDto
        {
            Categoria = p.Categoria,
            Finalidad = p.Finalidad,
            Acceso = p.Acceso,
            Conservacion = p.Conservacion,
            AlEliminar = p.AlEliminar
        }).ToList();
    }

    // RNF-LEG-02: no incluye el texto de las consultas; la historia clínica se ve en Mi cuenta y se conserva.
    public async Task<Resultado<string>> ExportarAsync(int cuentaId)
    {
        var cuenta = await _repository.ObtenerCuentaAsync(cuentaId);
        if (cuenta == null || cuenta.Eliminada)
        {
            return Resultado<string>.Error(404, "CUENTA_NO_ENCONTRADA", "No encontrado", "Iniciá sesión para pedir tus datos.");
        }

        var ahora = Ahora;
        var cargos = await _repository.ObtenerCargosAsync(cuentaId);
        var pagos = await _repository.ObtenerPagosAsync(cargos.Select(c => c.Id));
        var anulados = await _repository.ObtenerPagosAnuladosAsync(pagos.Select(p => p.Id));
        decimal Pagado(int cargoId) => pagos.Where(p => p.CargoId == cargoId && !anulados.Contains(p.Id)).Sum(p => p.Monto);

        var solicitudes = await _repository.ObtenerSolicitudesAsync(cuentaId);
        var datos = new
        {
            generado = ahora.ToString("s"),
            titular = new
            {
                nombre = cuenta.Nombre,
                apellido = cuenta.Apellido,
                correo = cuenta.Correo,
                telefono = cuenta.Telefono,
                documento = $"{cuenta.TipoDocumento} {cuenta.NumeroDocumento}".Trim(),
                domicilio = new
                {
                    calle = cuenta.Calle,
                    numero = cuenta.NumeroPuerta,
                    apartamento = cuenta.Apartamento,
                    localidad = cuenta.Localidad,
                    departamento = cuenta.Departamento,
                    referencias = cuenta.Referencias
                },
                cuentaCreada = cuenta.FechaCreacion.ToString("s")
            },
            aceptaciones = cuenta.Aceptaciones.OrderBy(a => a.FechaAceptacion).Select(a => new
            {
                documento = a.DocumentoLegal?.Titulo ?? "",
                version = a.DocumentoLegal?.Version ?? "",
                fecha = a.FechaAceptacion.ToString("s")
            }).ToList(),
            animales = (await _repository.ObtenerAnimalesAsync(cuentaId)).Select(a => new
            {
                nombre = a.Nombre,
                especie = a.Especie?.Nombre ?? "",
                raza = a.Raza,
                sexo = a.Sexo,
                nacimiento = a.FechaNacimiento?.ToString("yyyy-MM-dd") ?? "",
                microchip = a.Microchip,
                senasParticulares = a.SenasParticulares
            }).ToList(),
            turnos = (await _repository.ObtenerTurnosAsync(cuentaId)).Select(t => new
            {
                fecha = t.Inicio.ToString("s"),
                estado = t.Estado,
                animal = t.Animal?.Nombre ?? "",
                servicio = t.Prestacion?.Nombre ?? ""
            }).ToList(),
            cargos = cargos.Select(c => new
            {
                fecha = c.Fecha.ToString("s"),
                descripcion = c.Descripcion,
                monto = c.Monto,
                saldo = c.Monto - Pagado(c.Id),
                pagos = pagos.Where(p => p.CargoId == c.Id).Select(p => new
                {
                    numero = p.Numero,
                    fecha = p.Fecha.ToString("s"),
                    monto = p.Monto,
                    medio = p.Medio,
                    anulado = anulados.Contains(p.Id)
                }).ToList()
            }).ToList(),
            consentimientos = (await _repository.ObtenerConsentimientosAsync(cuentaId)).Select(c => new
            {
                fecha = c.Fecha.ToString("s"),
                animal = c.Animal?.Nombre ?? "",
                respuesta = c.Respuesta,
                version = c.Version,
                canal = c.Canal
            }).ToList(),
            comunicaciones = (await _repository.ObtenerComunicacionesAsync(cuentaId)).Select(c => new
            {
                fecha = c.FechaCreacion.ToString("s"),
                tipo = c.TipoComunicacion?.Nombre ?? "",
                asunto = c.Asunto,
                cuerpo = c.Cuerpo,
                canal = c.Canal,
                estado = c.Estado
            }).ToList(),
            preferenciasDeComunicacion = (await _repository.ObtenerPreferenciasAsync(cuentaId))
                .Select(p => new { tipo = p.TipoComunicacion?.Nombre ?? "", canal = p.Canal }).ToList(),
            solicitudesDeDatos = solicitudes.Select(s => new { fecha = s.Fecha.ToString("s"), tipo = s.Tipo, estado = s.Estado }).ToList(),
            nota = "Este archivo no incluye el texto de las consultas. La historia clínica de tus animales se conserva y la ves en Mi cuenta."
        };

        // RF-CLI-19: el pedido de exportación se atiende en el acto y queda registrado.
        _repository.Agregar(new SolicitudDerecho { CuentaId = cuentaId, Tipo = Exportacion, Estado = Completada, Fecha = ahora, FechaResolucion = ahora, Detalle = "Atendida en el acto." });
        await _repository.GuardarAsync();
        return Resultado<string>.Ok(JsonSerializer.Serialize(datos, Json));
    }

    public async Task<List<SolicitudDerechoReadDto>> SolicitudesAsync(int cuentaId)
        => (await _repository.ObtenerSolicitudesAsync(cuentaId)).Select(ADto).ToList();

    public async Task<ObstaculosDto> ObstaculosAsync(int cuentaId)
    {
        var lista = await ObstaculosDeAsync(cuentaId);
        return new ObstaculosDto { SePuedeEliminar = lista.Count == 0, Obstaculos = lista };
    }

    // Lo pendiente se atiende antes de eliminar.
    private async Task<List<string>> ObstaculosDeAsync(int cuentaId)
    {
        var obstaculos = new List<string>();
        if (await _repository.SaldoPendienteAsync(cuentaId) > 0)
        {
            obstaculos.Add("Tenés cargos con saldo pendiente: se cobran en Recepción antes de eliminar la cuenta.");
        }

        if (await _repository.ContarTurnosProximosAsync(cuentaId, Ahora) > 0)
        {
            obstaculos.Add("Tenés turnos próximos: cancelalos o esperá a que pasen.");
        }

        if (await _repository.TieneInternacionAbiertaAsync(cuentaId))
        {
            obstaculos.Add("Alguno de tus animales está internado: la cuenta se puede eliminar después del alta.");
        }

        return obstaculos;
    }

    public async Task<Resultado<SolicitudDerechoReadDto>> PedirEliminacionAsync(int cuentaId, bool confirmado)
    {
        var cuenta = await _repository.ObtenerCuentaAsync(cuentaId);
        if (cuenta == null || cuenta.Eliminada)
        {
            return Error(404, "CUENTA_NO_ENCONTRADA", "Iniciá sesión para pedir la eliminación.");
        }

        if (!confirmado)
        {
            return Error(400, "FALTA_CONFIRMAR", "Confirmá que entendés que la eliminación no se puede deshacer.");
        }

        if ((await _repository.ObtenerSolicitudesAsync(cuentaId)).Any(s => s.Tipo == Eliminacion && s.Estado == Recibida))
        {
            return Error(409, "PEDIDO_EN_CURSO", "Ya tenés un pedido de eliminación en curso.");
        }

        var obstaculos = await ObstaculosDeAsync(cuentaId);
        if (obstaculos.Count > 0)
        {
            return Error(409, "HAY_PENDIENTES", obstaculos[0]);
        }

        var solicitud = new SolicitudDerecho { CuentaId = cuentaId, Tipo = Eliminacion, Estado = Recibida, Fecha = Ahora };
        _repository.Agregar(solicitud);
        await _repository.GuardarAsync();

        var dto = ADto(solicitud);
        dto.Mensaje = "Recibimos tu pedido. El Administrador lo resuelve y te lo muestra acá.";
        return new Resultado<SolicitudDerechoReadDto> { Exito = true, Estado = 201, Valor = dto };
    }

    // El cliente puede arrepentirse mientras nadie resolvió su pedido.
    public async Task<Resultado<SolicitudDerechoReadDto>> CancelarAsync(int cuentaId, int id)
    {
        var solicitud = await _repository.ObtenerSolicitudAsync(id);
        if (solicitud == null || solicitud.CuentaId != cuentaId || solicitud.Estado != Recibida)
        {
            return Error(409, "NO_CANCELABLE", "Ese pedido ya no se puede cancelar.");
        }

        solicitud.Estado = Cancelada;
        solicitud.FechaResolucion = Ahora;
        solicitud.Detalle = "Cancelado por el titular.";
        await _repository.GuardarAsync();

        var dto = ADto(solicitud);
        dto.Mensaje = "Cancelaste tu pedido de eliminación.";
        return Resultado<SolicitudDerechoReadDto>.Ok(dto);
    }

    public async Task<List<SolicitudDerechoReadDto>> TodasAsync()
        => (await _repository.ObtenerSolicitudesAsync(null)).Select(ADto).ToList();

    // El Administrador aprueba (anonimiza la cuenta) o rechaza con motivo.
    public async Task<Resultado<SolicitudDerechoReadDto>> ResolverAsync(int adminId, int id, ResolucionDerechoDto datos)
    {
        var solicitud = await _repository.ObtenerSolicitudAsync(id);
        if (solicitud == null || solicitud.Tipo != Eliminacion || solicitud.Estado != Recibida)
        {
            return Error(409, "NO_PENDIENTE", "Ese pedido ya no está pendiente.");
        }

        var ahora = Ahora;
        if (!datos.Aprobar)
        {
            var motivo = (datos.Motivo ?? "").Trim();
            if (motivo.Length == 0 || motivo.Length > 500)
            {
                return Error(400, "FALTA_MOTIVO", "Escribí el motivo del rechazo.");
            }

            solicitud.Estado = Rechazada;
            solicitud.Detalle = motivo;
        }
        else
        {
            // Entre el pedido y la resolución puede haber aparecido algo pendiente.
            var obstaculos = await ObstaculosDeAsync(solicitud.CuentaId);
            if (obstaculos.Count > 0)
            {
                return Error(409, "HAY_PENDIENTES", $"No se puede aprobar: {obstaculos[0]} Podés rechazar el pedido con ese motivo.");
            }

            await AnonimizarAsync(solicitud.CuentaId, ahora);
            solicitud.Estado = Completada;
            solicitud.Detalle = "Datos personales reemplazados. Animales, historia clínica, cargos y consentimientos se conservan sin identificar al titular.";
        }

        solicitud.FechaResolucion = ahora;
        solicitud.ResueltaPorId = adminId;
        _repository.Agregar(new Auditoria
        {
            Fecha = ahora,
            CuentaId = adminId,
            Rol = "Administrador",
            Accion = datos.Aprobar ? "Eliminación aprobada" : "Eliminación rechazada",
            Entidad = "Datos personales",
            EntidadId = solicitud.Id.ToString(),
            Origen = "API",
            Detalle = $"Pedido {solicitud.Id} de la cuenta {solicitud.CuentaId}."
        });
        await _repository.GuardarAsync();

        var dto = ADto(solicitud);
        dto.Mensaje = datos.Aprobar ? "Cuenta anonimizada." : "Pedido rechazado.";
        return Resultado<SolicitudDerechoReadDto>.Ok(dto);
    }

    // Reemplaza los datos personales por valores neutros (igual a CuentaService.Anonimizar de Blazor).
    private async Task AnonimizarAsync(int cuentaId, DateTime ahora)
    {
        var cuenta = await _repository.ObtenerCuentaAsync(cuentaId);
        if (cuenta == null || cuenta.Eliminada)
        {
            return;
        }

        cuenta.Eliminada = true;
        cuenta.Activa = false;
        cuenta.FechaEliminacion = ahora;
        cuenta.Nombre = "Titular";
        cuenta.Apellido = "eliminado";
        cuenta.Correo = $"eliminada-{cuenta.Id}@eliminada.invalid";
        cuenta.Telefono = "";
        cuenta.TipoDocumento = "";
        cuenta.NumeroDocumento = "";
        cuenta.Calle = "";
        cuenta.NumeroPuerta = "";
        cuenta.Apartamento = "";
        cuenta.Localidad = "";
        cuenta.Departamento = "";
        cuenta.Referencias = "";
        cuenta.HashContrasena = "";
        cuenta.CorreoNuevoPendiente = "";

        foreach (var c in await _repository.ObtenerComunicacionesParaBorrarAsync(cuentaId))
        {
            _repository.Quitar(c);
        }

        foreach (var p in await _repository.ObtenerPreferenciasParaBorrarAsync(cuentaId))
        {
            _repository.Quitar(p);
        }

        foreach (var codigo in await _repository.ObtenerCodigosAsync(cuentaId))
        {
            _repository.Quitar(codigo);
        }

        foreach (var sesion in await _repository.ObtenerSesionesActivasAsync(cuentaId))
        {
            sesion.RevocadaEn = ahora;
        }
    }

    private static SolicitudDerechoReadDto ADto(SolicitudDerecho s) => new()
    {
        Id = s.Id,
        CuentaId = s.CuentaId,
        Titular = s.Cuenta == null ? "" : $"{s.Cuenta.Nombre} {s.Cuenta.Apellido}".Trim(),
        Tipo = s.Tipo,
        Estado = s.Estado,
        Fecha = s.Fecha,
        FechaResolucion = s.FechaResolucion,
        ResueltaPor = s.ResueltaPor != null ? $"{s.ResueltaPor.Nombre} {s.ResueltaPor.Apellido}".Trim()
            : s.ResueltaPorId != null ? "Administrador"
            : s.FechaResolucion == null ? "" : s.Tipo == Exportacion ? "Sistema" : "Titular",
        Detalle = s.Detalle
    };

    private static Resultado<SolicitudDerechoReadDto> Error(int estado, string codigo, string mensaje)
        => Resultado<SolicitudDerechoReadDto>.Error(estado, codigo, estado switch { 404 => "No encontrado", 409 => "No se puede", _ => "Datos no válidos" }, mensaje);
}
