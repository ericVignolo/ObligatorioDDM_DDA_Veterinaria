using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Configuración, igual a AdministracionService de Blazor: servicios y tarifas con fecha (RN-33), personal y roles
// (siempre queda un Administrador activo; nadie se cambia ni se bloquea a sí mismo), ausencias y feriados sin pisar
// turnos activos, parámetros con su rango, stock mínimo y auditoría. Cada cambio deja su registro.
public class AdministracionService : IAdministracionService
{
    public const string Recepcionista = "Recepcionista";
    public const string Veterinario = "Veterinario";
    public const string Administrador = "Administrador";
    public static readonly string[] RolesDePersonal = { Recepcionista, Veterinario, Administrador };

    // Parámetros que la API usa, con su rango. Los que faltan en la tabla se crean al cambiarlos.
    public static readonly Dictionary<string, (int Minimo, int Maximo, int PorDefecto, string Descripcion)> Catalogo = new()
    {
        ["agenda.anticipacion_minima_horas"] = (0, 72, 2, "Horas mínimas entre la reserva y el turno."),
        ["agenda.horizonte_dias"] = (7, 365, 60, "Días hacia adelante en que se puede reservar."),
        ["agenda.intervalo_inicio_minutos"] = (5, 60, 15, "Cada cuántos minutos puede empezar un turno."),
        ["agenda.cancelacion_sin_costo_horas"] = (0, 168, 24, "Horas antes del turno hasta las que se cancela sin costo."),
        ["agenda.tolerancia_ausencia_minutos"] = (5, 60, 15, "Minutos después del inicio para marcar la ausencia."),
        ["agenda.aviso_ausencias"] = (1, 10, 2, "Ausencias en 12 meses a partir de las que se avisa al reservar."),
        ["agenda.limite_ausencias"] = (2, 10, 3, "Ausencias en 12 meses a partir de las que se reserva solo por Recepción (RN-08)."),
        ["avisos.refuerzo_dias_antes"] = (0, 30, 5, "Días de anticipación del aviso de refuerzo de vacunas y desparasitaciones."),
        ["cobro.porcentaje_sena"] = (10, 100, 30, "Porcentaje del precio de la cirugía que se paga como seña (RN-35)."),
        ["cobro.limite_deuda"] = (0, 1_000_000, 3000, "Deuda vencida a partir de la que se avisa al agendar (RN-36)."),
        ["asistencia.cobertura_km"] = (1, 200, 30, "Zona de cobertura desde la clínica."),
        ["asistencia.escalado_rojo_minutos"] = (1, 60, 10, "Espera de aceptación de un rojo antes de escalar a Recepción (RN-55)."),
        ["asistencia.tarifa_base"] = (0, 100_000, 1500, "Tarifa base de la visita a domicilio (RN-58)."),
        ["asistencia.tarifa_km"] = (0, 10_000, 40, "Pesos por kilómetro de la visita a domicilio."),
        ["asistencia.tarifa_animal_extra"] = (0, 100_000, 300, "Pesos por cada animal extra de la visita.")
    };

    private readonly IAdministracionRepository _repository;
    private readonly TimeProvider _reloj;

    public AdministracionService(IAdministracionRepository repository, TimeProvider reloj)
    {
        _repository = repository;
        _reloj = reloj;
    }

    private DateTime Ahora => _reloj.GetLocalNow().DateTime;
    private DateTime Hoy => Ahora.Date;

    // ===================== Servicios y tarifas (RF-ADM-02) =====================

    public async Task<List<ServicioAdminDto>> ServiciosAsync()
    {
        var lista = new List<ServicioAdminDto>();
        foreach (var p in await _repository.ObtenerPrestacionesAsync())
        {
            lista.Add(ServicioADto(p, await PrecioVigenteAsync(p)));
        }

        return lista;
    }

    public async Task<Resultado<ServicioAdminDto>> CrearServicioAsync(int adminId, ServicioFormDto datos)
    {
        if (datos.Precio < 1)
        {
            return Error<ServicioAdminDto>(400, "PRECIO_INVALIDO", "El precio tiene que estar entre $ 1 y $ 1.000.000.");
        }

        var error = await ValidarServicioAsync(datos, 0);
        if (error != null)
        {
            return Error<ServicioAdminDto>(error.Value.Estado, error.Value.Codigo, error.Value.Mensaje);
        }

        var prestacion = new Prestacion();
        Copiar(datos, prestacion);
        prestacion.PrecioDesde = ReglasClinicas.RedondearPesos(datos.Precio);
        _repository.Agregar(prestacion);
        _repository.Agregar(new TarifaPrestacion { Prestacion = prestacion, Precio = prestacion.PrecioDesde, VigenteDesde = Hoy, RegistradoPorId = adminId });
        Auditar(adminId, "Servicio creado", "Servicio", "", $"{prestacion.Nombre}, $ {prestacion.PrecioDesde:N0}");
        await _repository.GuardarAsync();

        return new Resultado<ServicioAdminDto> { Exito = true, Estado = 201, Valor = ServicioADto(prestacion, prestacion.PrecioDesde) };
    }

    // El precio no se edita acá: se programa una tarifa con fecha (RN-33).
    public async Task<Resultado<ServicioAdminDto>> EditarServicioAsync(int adminId, int id, ServicioFormDto datos)
    {
        var prestacion = await _repository.ObtenerPrestacionAsync(id);
        if (prestacion == null)
        {
            return Error<ServicioAdminDto>(404, "SERVICIO_NO_ENCONTRADO", "Ese servicio no existe.");
        }

        var error = await ValidarServicioAsync(datos, id);
        if (error != null)
        {
            return Error<ServicioAdminDto>(error.Value.Estado, error.Value.Codigo, error.Value.Mensaje);
        }

        Copiar(datos, prestacion);
        Auditar(adminId, "Servicio editado", "Servicio", prestacion.Id.ToString(),
            $"{prestacion.Nombre}: {(prestacion.Publicado ? "publicado" : "no publicado")}, {(prestacion.ReservableEnLinea ? $"reservable en línea ({prestacion.DuracionMinutos} min)" : "no reservable en línea")}");
        await _repository.GuardarAsync();
        return Resultado<ServicioAdminDto>.Ok(ServicioADto(prestacion, await PrecioVigenteAsync(prestacion)));
    }

    private async Task<(int Estado, string Codigo, string Mensaje)?> ValidarServicioAsync(ServicioFormDto datos, int idActual)
    {
        if (datos.ReservableEnLinea && datos.DuracionMinutos < 15)
        {
            return (400, "DURACION_CORTA", "Un servicio que se reserva en línea necesita una duración de al menos 15 minutos.");
        }

        if (await _repository.ExistePrestacionConNombreAsync(datos.Nombre.Trim(), idActual))
        {
            return (409, "SERVICIO_REPETIDO", "Ya hay un servicio con ese nombre.");
        }

        return null;
    }

    public async Task<Resultado<List<TarifaReadDto>>> TarifasAsync(int prestacionId)
    {
        if (await _repository.ObtenerPrestacionAsync(prestacionId) == null)
        {
            return Error<List<TarifaReadDto>>(404, "SERVICIO_NO_ENCONTRADO", "Ese servicio no existe.");
        }

        var tarifas = await _repository.ObtenerTarifasAsync(prestacionId);
        var vigente = tarifas.Where(t => t.VigenteDesde.Date <= Hoy).OrderByDescending(t => t.VigenteDesde).FirstOrDefault();
        return Resultado<List<TarifaReadDto>>.Ok(tarifas.Select(t => new TarifaReadDto
        {
            Id = t.Id,
            PrestacionId = t.PrestacionId,
            Precio = t.Precio,
            VigenteDesde = DateOnly.FromDateTime(t.VigenteDesde),
            Vigente = t == vigente,
            RegistradoPor = t.RegistradoPor == null ? "Carga inicial" : $"{t.RegistradoPor.Nombre} {t.RegistradoPor.Apellido}".Trim()
        }).ToList());
    }

    // RN-33: el precio nuevo rige desde hoy o una fecha futura; lo ya atendido conserva su precio.
    public async Task<Resultado<TarifaReadDto>> ProgramarTarifaAsync(int adminId, TarifaCreateDto datos)
    {
        var prestacion = await _repository.ObtenerPrestacionAsync(datos.PrestacionId);
        if (prestacion == null)
        {
            return Error<TarifaReadDto>(404, "SERVICIO_NO_ENCONTRADO", "Ese servicio no existe.");
        }

        var desde = datos.VigenteDesde!.Value.ToDateTime(TimeOnly.MinValue);
        if (desde < Hoy)
        {
            return Error<TarifaReadDto>(400, "TARIFA_PASADA", "La tarifa nueva rige desde hoy o una fecha futura: no se cambian precios pasados (RN-33).");
        }

        var precio = ReglasClinicas.RedondearPesos(datos.Precio);
        var existente = (await _repository.ObtenerTarifasAsync(prestacion.Id)).FirstOrDefault(t => t.VigenteDesde.Date == desde);
        if (existente != null)
        {
            return Error<TarifaReadDto>(409, "TARIFA_REPETIDA", $"Ya hay una tarifa desde el {desde:dd/MM/yyyy} ($ {existente.Precio:N0}). Elegí otra fecha.");
        }

        var tarifa = new TarifaPrestacion { PrestacionId = prestacion.Id, Precio = precio, VigenteDesde = desde, RegistradoPorId = adminId };
        _repository.Agregar(tarifa);
        if (desde == Hoy)
        {
            prestacion.PrecioDesde = precio;
        }

        Auditar(adminId, "Tarifa programada", "Tarifa", prestacion.Id.ToString(), $"{prestacion.Nombre}: $ {precio:N0} desde el {desde:dd/MM/yyyy}");
        await _repository.GuardarAsync();

        return new Resultado<TarifaReadDto>
        {
            Exito = true,
            Estado = 201,
            Valor = new TarifaReadDto { Id = tarifa.Id, PrestacionId = prestacion.Id, Precio = precio, VigenteDesde = DateOnly.FromDateTime(desde), Vigente = desde == Hoy }
        };
    }

    // ===================== Personal y roles (RF-ADM-01) =====================

    public async Task<List<PersonalReadDto>> PersonalAsync()
    {
        var lista = new List<PersonalReadDto>();
        foreach (var cuenta in await _repository.ObtenerPersonalAsync())
        {
            lista.Add(await PersonalADtoAsync(cuenta));
        }

        return lista;
    }

    public async Task<Resultado<PersonalReadDto>> CrearPersonalAsync(int adminId, PersonalCreateDto datos)
    {
        if (!RolesDePersonal.Contains(datos.Rol))
        {
            return Error<PersonalReadDto>(400, "ROL_INVALIDO", "Elegí un rol: Recepcionista, Veterinario o Administrador.");
        }

        var correo = ValidacionDatos.NormalizarCorreo(datos.Correo);
        if (await _repository.ExisteCorreoAsync(correo))
        {
            return Error<PersonalReadDto>(409, "CORREO_REPETIDO", "Ya hay una cuenta con ese correo.");
        }

        Profesional? ficha = null;
        if (datos.Rol == Veterinario)
        {
            var (f, error) = await FichaLibreAsync(datos.ProfesionalId, 0);
            if (error != null)
            {
                return Error<PersonalReadDto>(400, "FICHA_INVALIDA", error);
            }

            ficha = f;
        }

        var roles = await _repository.ObtenerRolesAsync();
        var cuenta = new Cuenta
        {
            Nombre = datos.Nombre.Trim(),
            Apellido = datos.Apellido.Trim(),
            Correo = correo,
            HashContrasena = Seguridad.DerivarContrasena(datos.Contrasena),
            Verificada = true,
            Activa = true,
            FechaCreacion = Ahora
        };
        cuenta.Roles.Add(new CuentaRol { Cuenta = cuenta, RolId = roles.First(r => r.Nombre == datos.Rol).Id });
        _repository.Agregar(cuenta);
        if (ficha != null)
        {
            ficha.Cuenta = cuenta;
        }

        Auditar(adminId, "Cuenta creada", "Personal", "", $"{cuenta.Nombre} {cuenta.Apellido} ({datos.Rol}, {cuenta.Correo})");
        await _repository.GuardarAsync();

        return new Resultado<PersonalReadDto> { Exito = true, Estado = 201, Valor = await PersonalADtoAsync(cuenta, datos.Rol, ficha) };
    }

    public async Task<Resultado<PersonalReadDto>> CambiarRolAsync(int adminId, int cuentaId, CambioRolDto datos)
    {
        var cuenta = await _repository.ObtenerCuentaConRolesAsync(cuentaId);
        var actual = cuenta == null ? null : RolDePersonal(cuenta);
        if (cuenta == null || actual == null || !RolesDePersonal.Contains(datos.Rol))
        {
            return Error<PersonalReadDto>(400, "ROL_INVALIDO", "Elegí una cuenta del personal y un rol válido.");
        }

        if (cuenta.Id == adminId)
        {
            return Error<PersonalReadDto>(409, "PROPIO_ROL", "No podés cambiar tu propio rol.");
        }

        if (actual == Administrador && datos.Rol != Administrador && cuenta.Activa && await _repository.ContarAdministradoresActivosAsync(cuenta.Id) == 0)
        {
            return Error<PersonalReadDto>(409, "ULTIMO_ADMINISTRADOR", "Tiene que quedar al menos un Administrador activo.");
        }

        Profesional? nueva = null;
        if (datos.Rol == Veterinario)
        {
            var (f, error) = await FichaLibreAsync(datos.ProfesionalId, cuenta.Id);
            if (error != null)
            {
                return Error<PersonalReadDto>(400, "FICHA_INVALIDA", error);
            }

            nueva = f;
        }

        // Deja la ficha anterior libre y asocia la nueva, si corresponde.
        var anterior = await _repository.ObtenerProfesionalDeCuentaAsync(cuenta.Id);
        if (anterior != null && anterior != nueva)
        {
            anterior.CuentaId = null;
        }

        if (nueva != null)
        {
            nueva.CuentaId = cuenta.Id;
        }

        if (actual != datos.Rol)
        {
            var roles = await _repository.ObtenerRolesAsync();
            // Se quita solo el rol de personal; si además es cliente, lo sigue siendo.
            foreach (var r in cuenta.Roles.Where(r => r.Rol?.Nombre != "Cliente").ToList())
            {
                _repository.Quitar(r);
            }

            cuenta.Roles.Add(new CuentaRol { CuentaId = cuenta.Id, RolId = roles.First(r => r.Nombre == datos.Rol).Id });
            await RevocarSesionesAsync(cuenta.Id);
        }

        Auditar(adminId, "Rol cambiado", "Personal", cuenta.Id.ToString(), $"{cuenta.Nombre} {cuenta.Apellido}: {actual} → {datos.Rol}");
        await _repository.GuardarAsync();
        return Resultado<PersonalReadDto>.Ok(await PersonalADtoAsync(cuenta, datos.Rol, nueva));
    }

    // Bloquear impide ingresar y cierra sus sesiones; no borra la cuenta ni su historial.
    public async Task<Resultado<PersonalReadDto>> CambiarBloqueoAsync(int adminId, int cuentaId, bool bloquear)
    {
        var cuenta = await _repository.ObtenerCuentaConRolesAsync(cuentaId);
        var rol = cuenta == null ? null : RolDePersonal(cuenta);
        if (cuenta == null || rol == null)
        {
            return Error<PersonalReadDto>(404, "CUENTA_NO_ENCONTRADA", "Esa cuenta no existe.");
        }

        if (bloquear && cuenta.Id == adminId)
        {
            return Error<PersonalReadDto>(409, "PROPIA_CUENTA", "No podés bloquear tu propia cuenta.");
        }

        if (bloquear && rol == Administrador && await _repository.ContarAdministradoresActivosAsync(cuenta.Id) == 0)
        {
            return Error<PersonalReadDto>(409, "ULTIMO_ADMINISTRADOR", "Tiene que quedar al menos un Administrador activo.");
        }

        if (cuenta.Activa == !bloquear)
        {
            return Error<PersonalReadDto>(409, "SIN_CAMBIO", bloquear ? "La cuenta ya está bloqueada." : "La cuenta ya está activa.");
        }

        cuenta.Activa = !bloquear;
        if (bloquear)
        {
            await RevocarSesionesAsync(cuenta.Id);
        }

        Auditar(adminId, bloquear ? "Cuenta bloqueada" : "Cuenta desbloqueada", "Personal", cuenta.Id.ToString(), $"{cuenta.Nombre} {cuenta.Apellido}");
        await _repository.GuardarAsync();
        return Resultado<PersonalReadDto>.Ok(await PersonalADtoAsync(cuenta));
    }

    // Un veterinario necesita una ficha de profesional que no esté asociada a otra cuenta.
    private async Task<(Profesional? Ficha, string? Error)> FichaLibreAsync(int? profesionalId, int cuentaId)
    {
        var ficha = profesionalId is int id ? await _repository.ObtenerProfesionalAsync(id) : null;
        if (ficha == null)
        {
            return (null, "Un veterinario necesita su ficha de profesional (matrícula, agenda y especies).");
        }

        if (ficha.CuentaId != null && ficha.CuentaId != cuentaId)
        {
            return (null, "Esa ficha de profesional ya está asociada a otra cuenta.");
        }

        return (ficha, null);
    }

    private async Task RevocarSesionesAsync(int cuentaId)
    {
        foreach (var sesion in await _repository.ObtenerSesionesActivasAsync(cuentaId))
        {
            sesion.RevocadaEn = Ahora;
        }
    }

    private static string? RolDePersonal(Cuenta cuenta)
        => cuenta.Roles.Select(r => r.Rol?.Nombre).FirstOrDefault(n => n != null && RolesDePersonal.Contains(n));

    // ===================== Ausencias y feriados (RF-ADM-04) =====================

    public async Task<List<AusenciaReadDto>> AusenciasAsync(int? profesionalId)
    {
        return (await _repository.ObtenerAusenciasAsync(profesionalId, Hoy)).Select(AusenciaADto).ToList();
    }

    // No se carga una ausencia sobre turnos activos: primero se reprograman o cancelan.
    public async Task<Resultado<AusenciaReadDto>> AgregarAusenciaAsync(int adminId, AusenciaCreateDto datos)
    {
        var profesional = await _repository.ObtenerProfesionalAsync(datos.ProfesionalId);
        if (profesional == null)
        {
            return Error<AusenciaReadDto>(404, "PROFESIONAL_NO_ENCONTRADO", "Ese profesional no existe.");
        }

        var desde = datos.Desde!.Value.ToDateTime(TimeOnly.MinValue);
        var hasta = datos.Hasta!.Value.ToDateTime(TimeOnly.MinValue);
        if (hasta < desde || desde < Hoy)
        {
            return Error<AusenciaReadDto>(400, "FECHAS_INVALIDAS", "Las fechas tienen que ser de hoy en adelante y «hasta» no puede ser antes que «desde».");
        }

        if ((await _repository.ObtenerAusenciasAsync(profesional.Id, Hoy)).Any(a => desde <= a.Hasta.Date && hasta >= a.Desde.Date))
        {
            return Error<AusenciaReadDto>(409, "AUSENCIA_SUPERPUESTA", "Se superpone con otra ausencia de este profesional.");
        }

        var turnos = await _repository.ContarTurnosActivosAsync(desde, hasta.AddDays(1), profesional.Id);
        if (turnos > 0)
        {
            return Error<AusenciaReadDto>(409, "TURNOS_ACTIVOS", $"Hay {turnos} turno(s) activo(s) en esas fechas: reprogramalos o cancelalos antes.");
        }

        var ausencia = new Ausencia { ProfesionalId = profesional.Id, Profesional = profesional, Desde = desde, Hasta = hasta, Motivo = datos.Motivo.Trim() };
        _repository.Agregar(ausencia);
        Auditar(adminId, "Ausencia agregada", "Horario", profesional.Id.ToString(), $"{profesional.Nombre}: {desde:dd/MM/yyyy} a {hasta:dd/MM/yyyy} ({ausencia.Motivo})");
        await _repository.GuardarAsync();
        return new Resultado<AusenciaReadDto> { Exito = true, Estado = 201, Valor = AusenciaADto(ausencia) };
    }

    public async Task<Resultado<OperacionDto>> QuitarAusenciaAsync(int adminId, int id)
    {
        var ausencia = await _repository.ObtenerAusenciaAsync(id);
        if (ausencia == null)
        {
            return Error<OperacionDto>(404, "AUSENCIA_NO_ENCONTRADA", "Esa ausencia no existe.");
        }

        _repository.Quitar(ausencia);
        var detalle = $"{ausencia.Profesional?.Nombre}: {ausencia.Desde:dd/MM/yyyy} a {ausencia.Hasta:dd/MM/yyyy}";
        Auditar(adminId, "Ausencia quitada", "Horario", ausencia.ProfesionalId.ToString(), detalle);
        await _repository.GuardarAsync();
        return Resultado<OperacionDto>.Ok(new OperacionDto { Mensaje = $"Ausencia quitada: {detalle}" });
    }

    public async Task<List<FeriadoReadDto>> FeriadosAsync()
    {
        return (await _repository.ObtenerFeriadosAsync(Hoy)).Select(FeriadoADto).ToList();
    }

    public async Task<Resultado<FeriadoReadDto>> AgregarFeriadoAsync(int adminId, FeriadoCreateDto datos)
    {
        var fecha = datos.Fecha!.Value.ToDateTime(TimeOnly.MinValue);
        if (fecha < Hoy)
        {
            return Error<FeriadoReadDto>(400, "FECHA_PASADA", "Elegí una fecha de hoy en adelante.");
        }

        if (await _repository.ObtenerFeriadoAsync(fecha) != null)
        {
            return Error<FeriadoReadDto>(409, "FERIADO_REPETIDO", "Ese día ya es feriado.");
        }

        var turnos = await _repository.ContarTurnosActivosAsync(fecha, fecha.AddDays(1), null);
        if (turnos > 0)
        {
            return Error<FeriadoReadDto>(409, "TURNOS_ACTIVOS", $"Hay {turnos} turno(s) activo(s) ese día: reprogramalos o cancelalos antes.");
        }

        var feriado = new DiaNoLaborable { Fecha = fecha, Motivo = (datos.Motivo ?? "").Trim() };
        _repository.Agregar(feriado);
        Auditar(adminId, "Feriado agregado", "Feriado", "", $"{fecha:dd/MM/yyyy}");
        await _repository.GuardarAsync();
        return new Resultado<FeriadoReadDto> { Exito = true, Estado = 201, Valor = FeriadoADto(feriado) };
    }

    public async Task<Resultado<OperacionDto>> QuitarFeriadoAsync(int adminId, DateOnly fecha)
    {
        var feriado = await _repository.ObtenerFeriadoAsync(fecha.ToDateTime(TimeOnly.MinValue));
        if (feriado == null)
        {
            return Error<OperacionDto>(404, "FERIADO_NO_ENCONTRADO", "Ese día no es feriado.");
        }

        _repository.Quitar(feriado);
        Auditar(adminId, "Feriado quitado", "Feriado", feriado.Id.ToString(), $"{fecha:dd/MM/yyyy}");
        await _repository.GuardarAsync();
        return Resultado<OperacionDto>.Ok(new OperacionDto { Mensaje = $"Feriado quitado: {fecha:dd/MM/yyyy}" });
    }

    // ===================== Parámetros, stock mínimo y auditoría =====================

    public async Task<List<ParametroReadDto>> ParametrosAsync()
    {
        var guardados = (await _repository.ObtenerParametrosAsync()).ToDictionary(p => p.Clave);
        var claves = guardados.Keys.Union(Catalogo.Keys).OrderBy(c => c);
        return claves.Select(clave =>
        {
            guardados.TryGetValue(clave, out var p);
            var conocido = Catalogo.TryGetValue(clave, out var c);
            return new ParametroReadDto
            {
                Clave = clave,
                Valor = p?.Valor ?? c.PorDefecto.ToString(),
                Descripcion = p?.Descripcion is { Length: > 0 } d ? d : c.Descripcion ?? "",
                Minimo = conocido ? c.Minimo : 0,
                Maximo = conocido ? c.Maximo : 0
            };
        }).ToList();
    }

    public async Task<Resultado<ParametroReadDto>> CambiarParametroAsync(int adminId, string clave, ParametroUpdateDto datos)
    {
        if (!Catalogo.TryGetValue(clave, out var regla))
        {
            return Error<ParametroReadDto>(404, "PARAMETRO_NO_EDITABLE", "Ese parámetro no existe o todavía no se puede cambiar desde la API.");
        }

        if (!int.TryParse(datos.Valor.Trim(), out var valor) || valor < regla.Minimo || valor > regla.Maximo)
        {
            return Error<ParametroReadDto>(400, "VALOR_FUERA_DE_RANGO", $"El valor tiene que ser un número entero entre {regla.Minimo} y {regla.Maximo}.");
        }

        var parametro = await _repository.ObtenerParametroAsync(clave);
        var anterior = parametro?.Valor ?? regla.PorDefecto.ToString();
        if (parametro == null)
        {
            parametro = new Parametro { Clave = clave, Descripcion = regla.Descripcion };
            _repository.Agregar(parametro);
        }

        parametro.Valor = valor.ToString();
        Auditar(adminId, "Parámetro cambiado", "Parámetro", clave, $"{clave}: {anterior} → {valor}");
        await _repository.GuardarAsync();

        return Resultado<ParametroReadDto>.Ok(new ParametroReadDto { Clave = clave, Valor = parametro.Valor, Descripcion = parametro.Descripcion, Minimo = regla.Minimo, Maximo = regla.Maximo });
    }

    public async Task<Resultado<OperacionDto>> CambiarStockMinimoAsync(int adminId, int articuloId, StockMinimoDto datos)
    {
        var articulo = await _repository.ObtenerArticuloAsync(articuloId);
        if (articulo == null)
        {
            return Error<OperacionDto>(404, "ARTICULO_NO_ENCONTRADO", "No encontramos ese artículo.");
        }

        var anterior = articulo.StockMinimo;
        articulo.StockMinimo = datos.StockMinimo;
        Auditar(adminId, "Stock mínimo cambiado", "Artículo", articulo.Id.ToString(), $"{articulo.Nombre}: {anterior} → {datos.StockMinimo}");
        await _repository.GuardarAsync();
        return Resultado<OperacionDto>.Ok(new OperacionDto { Mensaje = $"Stock mínimo de {articulo.Nombre}: {datos.StockMinimo} {articulo.Unidad}." });
    }

    public async Task<PaginaDto<AuditoriaReadDto>> AuditoriaAsync(string? entidad, DateOnly? desde, int page, int pageSize)
    {
        var fecha = desde?.ToDateTime(TimeOnly.MinValue);
        var filtro = string.IsNullOrWhiteSpace(entidad) ? null : entidad.Trim();
        var registros = await _repository.ObtenerAuditoriaAsync(filtro, fecha, (page - 1) * pageSize, pageSize);
        var nombres = await _repository.NombresDeCuentasAsync(registros.Where(r => r.CuentaId != null).Select(r => r.CuentaId!.Value));

        return new PaginaDto<AuditoriaReadDto>
        {
            Items = registros.Select(r => new AuditoriaReadDto
            {
                Id = r.Id,
                Fecha = r.Fecha,
                Persona = r.CuentaId is int id && nombres.TryGetValue(id, out var n) ? n.Trim() : "Sistema",
                Rol = r.Rol,
                Accion = r.Accion,
                Entidad = r.Entidad,
                EntidadId = r.EntidadId,
                Detalle = r.Detalle
            }).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalItems = await _repository.ContarAuditoriaAsync(filtro, fecha)
        };
    }

    // ===================== Apoyo =====================

    private void Auditar(int adminId, string accion, string entidad, string entidadId, string detalle)
    {
        _repository.Agregar(new Auditoria
        {
            Fecha = Ahora,
            CuentaId = adminId,
            Rol = Administrador,
            Accion = accion,
            Entidad = entidad,
            EntidadId = entidadId,
            Origen = "API",
            Detalle = detalle.Length > 500 ? detalle[..500] : detalle
        });
    }

    private async Task<decimal> PrecioVigenteAsync(Prestacion p)
        => (await _repository.ObtenerTarifasAsync(p.Id)).Where(t => t.VigenteDesde.Date <= Hoy).OrderByDescending(t => t.VigenteDesde).FirstOrDefault()?.Precio ?? p.PrecioDesde;

    private static void Copiar(ServicioFormDto datos, Prestacion p)
    {
        p.Nombre = datos.Nombre.Trim();
        p.Descripcion = (datos.Descripcion ?? "").Trim();
        p.ReservableEnLinea = datos.ReservableEnLinea;
        p.DuracionMinutos = datos.DuracionMinutos;
        p.Publicado = datos.Publicado;
        p.RequiereConsentimiento = datos.RequiereConsentimiento;
    }

    private static ServicioAdminDto ServicioADto(Prestacion p, decimal precio) => new()
    {
        Id = p.Id,
        Nombre = p.Nombre,
        Descripcion = p.Descripcion,
        PrecioVigente = precio,
        ReservableEnLinea = p.ReservableEnLinea,
        DuracionMinutos = p.DuracionMinutos,
        Publicado = p.Publicado,
        RequiereConsentimiento = p.RequiereConsentimiento
    };

    private async Task<PersonalReadDto> PersonalADtoAsync(Cuenta c, string? rol = null, Profesional? ficha = null)
    {
        ficha ??= await _repository.ObtenerProfesionalDeCuentaAsync(c.Id);
        return new PersonalReadDto
        {
            Id = c.Id,
            Nombre = c.Nombre,
            Apellido = c.Apellido,
            Correo = c.Correo,
            Rol = rol ?? RolDePersonal(c) ?? "",
            ProfesionalId = ficha?.Id,
            Profesional = ficha?.Nombre ?? "",
            Activa = c.Activa
        };
    }

    private static AusenciaReadDto AusenciaADto(Ausencia a) => new()
    {
        Id = a.Id,
        ProfesionalId = a.ProfesionalId,
        Profesional = a.Profesional?.Nombre ?? "",
        Desde = DateOnly.FromDateTime(a.Desde),
        Hasta = DateOnly.FromDateTime(a.Hasta),
        Motivo = a.Motivo
    };

    private static FeriadoReadDto FeriadoADto(DiaNoLaborable d) => new() { Id = d.Id, Fecha = DateOnly.FromDateTime(d.Fecha), Motivo = d.Motivo };

    private static Resultado<T> Error<T>(int estado, string codigo, string mensaje)
        => Resultado<T>.Error(estado, codigo, estado switch { 404 => "No encontrado", 409 => "No se puede", _ => "Datos no válidos" }, mensaje);
}
