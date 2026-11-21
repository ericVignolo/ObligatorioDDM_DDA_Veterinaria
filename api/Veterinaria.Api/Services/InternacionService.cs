using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Internación, igual a InternacionService de Blazor. Un animal tiene a lo sumo una abierta (RN-21) y un box un solo
// paciente (RN-22): la admisión bloquea la fila del animal y el ingreso la del box. Las novedades no se editan: se
// rectifican (RN-24); el cliente ve solo las visibles (RN-23). Sin evolución final e indicaciones no hay alta (RN-25).
public class InternacionService : IInternacionService
{
    public const string Admitida = "Admitida";
    public const string Internado = "Internado";
    public const string Alta = "Alta";
    public const string Anulada = "Anulada";

    public const string Novedad = "Novedad";
    public const string Evolucion = "Evolución";
    public const string Rectificacion = "Rectificación";

    public const string PequenosAnimales = "Pequeños animales";
    public const string GrandesAnimales = "Grandes animales";

    // Especies que van a un box de grandes animales (las mismas de Blazor).
    private static readonly string[] EspeciesGrandes = { "Caballo", "Vaca" };

    private readonly IInternacionRepository _repository;
    private readonly TimeProvider _reloj;

    public InternacionService(IInternacionRepository repository, TimeProvider reloj)
    {
        _repository = repository;
        _reloj = reloj;
    }

    private DateTime Ahora => _reloj.GetLocalNow().DateTime;

    public static string TipoDeBoxPara(string especie) => EspeciesGrandes.Contains(especie) ? GrandesAnimales : PequenosAnimales;

    // ===================== Personal =====================

    public async Task<List<InternacionReadDto>> AbiertasAsync() => (await _repository.ObtenerAbiertasAsync()).Select(i => ADto(i)).ToList();

    public async Task<List<InternacionReadDto>> AltasAsync() => (await _repository.ObtenerAltasAsync(50)).Select(i => ADto(i)).ToList();

    public async Task<Resultado<InternacionReadDto>> DetalleAsync(int id, bool esVeterinario)
    {
        var internacion = await _repository.ObtenerAsync(id);
        if (internacion == null)
        {
            return NoEncontrada();
        }

        return Resultado<InternacionReadDto>.Ok(await ConNovedadesAsync(internacion, soloVisibles: !esVeterinario));
    }

    public async Task<List<BoxReadDto>> BoxesAsync()
    {
        var ocupantes = await _repository.ObtenerOcupantesAsync();
        return (await _repository.ObtenerBoxesAsync()).Select(b => BoxADto(b, ocupantes.FirstOrDefault(o => o.BoxId == b.Id))).ToList();
    }

    // Boxes en servicio, libres y del tipo que corresponde a la especie del animal.
    public async Task<Resultado<List<BoxReadDto>>> BoxesLibresAsync(int internacionId)
    {
        var internacion = await _repository.ObtenerAsync(internacionId);
        if (internacion == null)
        {
            return Resultado<List<BoxReadDto>>.Error(404, "INTERNACION_NO_ENCONTRADA", "No encontrado", "Esa internación no existe.");
        }

        var tipo = TipoDeBoxPara(internacion.Animal?.Especie?.Nombre ?? "");
        return Resultado<List<BoxReadDto>>.Ok((await BoxesAsync()).Where(b => b.Activo && !b.Ocupado && b.Tipo == tipo).ToList());
    }

    // ===================== Recepción =====================

    // El animal no puede tener otra internación abierta (RN-21) y un grupo de animales no se interna.
    public async Task<Resultado<InternacionReadDto>> AdmitirAsync(int personaId, AdmisionDto datos)
    {
        var animal = await _repository.ObtenerAnimalAsync(datos.AnimalId);
        if (animal == null)
        {
            return Error(404, "ANIMAL_NO_ENCONTRADO", "Ese animal no existe.");
        }

        if (animal.EsGrupo)
        {
            return Error(400, "ES_GRUPO", $"{animal.Nombre} es un grupo de animales: no se interna. Registrá cada animal por separado.");
        }

        var responsable = animal.Responsables.Where(r => r.Hasta == null).OrderBy(r => r.Desde).FirstOrDefault();
        if (responsable == null)
        {
            return Error(409, "SIN_RESPONSABLE", $"{animal.Nombre} no tiene un responsable vigente.");
        }

        return await _repository.EnTransaccionConAnimalAsync(animal.Id, async () =>
        {
            if (await _repository.ObtenerAbiertaDeAnimalAsync(animal.Id) != null)
            {
                return Error(409, "INTERNACION_ABIERTA", $"{animal.Nombre} ya tiene una internación abierta (RN-21).");
            }

            var internacion = new Internacion
            {
                AnimalId = animal.Id,
                ClienteId = responsable.CuentaId,
                Estado = Admitida,
                Motivo = datos.Motivo.Trim(),
                TelefonoContacto = datos.TelefonoContacto.Trim(),
                Observaciones = (datos.Observaciones ?? "").Trim(),
                FechaAdmision = Ahora,
                AdmitidoPorId = personaId
            };
            _repository.Agregar(internacion);
            await _repository.GuardarAsync();

            var dto = ADto(internacion, animal);
            dto.Mensaje = $"{animal.Nombre} quedó admitido. Falta que un veterinario le asigne box y plan.";
            return new Resultado<InternacionReadDto> { Exito = true, Estado = 201, Valor = dto };
        });
    }

    // Una admisión que no llegó a ingresar se anula con motivo (por ejemplo, el responsable se lo llevó).
    public async Task<Resultado<InternacionReadDto>> AnularAdmisionAsync(int personaId, int id, AnulacionAdmisionDto datos)
    {
        var internacion = await _repository.ObtenerAsync(id);
        if (internacion == null)
        {
            return NoEncontrada();
        }

        if (internacion.Estado != Admitida)
        {
            return Error(409, "NO_ES_ADMISION", "Solo se anula una admisión que todavía no tiene ingreso clínico.");
        }

        internacion.Estado = Anulada;
        internacion.MotivoAnulacion = datos.Motivo.Trim();
        internacion.Version++;
        await _repository.GuardarAsync();

        var dto = ADto(internacion);
        dto.Mensaje = "Admisión anulada.";
        return Resultado<InternacionReadDto>.Ok(dto);
    }

    // ===================== Veterinario =====================

    // RF-VET-09: box libre del tipo adecuado (RN-22) y plan terapéutico.
    public async Task<Resultado<InternacionReadDto>> IngresarAsync(int personaId, int id, IngresoClinicoDto datos)
    {
        var previa = await _repository.ObtenerAsync(id);
        if (previa == null)
        {
            return NoEncontrada();
        }

        if (await _repository.ObtenerBoxAsync(datos.BoxId) == null)
        {
            return Error(404, "BOX_NO_ENCONTRADO", "Ese box no existe.");
        }

        return await _repository.EnTransaccionConBoxAsync(datos.BoxId, async () =>
        {
            var internacion = (await _repository.ObtenerAsync(id))!;
            var box = (await _repository.ObtenerBoxAsync(datos.BoxId))!;
            if (internacion.Estado != Admitida)
            {
                return Error(409, "NO_ESPERA_INGRESO", "Esta internación ya no espera el ingreso clínico.");
            }

            if (!box.Activo)
            {
                return Error(409, "BOX_FUERA_DE_SERVICIO", $"{box.Nombre} está fuera de servicio.");
            }

            if (await _repository.ObtenerOcupanteAsync(box.Id) != null)
            {
                return Error(409, "BOX_OCUPADO", $"{box.Nombre} está ocupado: elegí otro (RN-22).");
            }

            var especie = internacion.Animal?.Especie?.Nombre ?? "";
            if (box.Tipo != TipoDeBoxPara(especie))
            {
                return Error(400, "BOX_INADECUADO", $"{box.Nombre} es de {box.Tipo.ToLower()}: no sirve para un {especie.ToLower()}.");
            }

            internacion.BoxId = box.Id;
            internacion.Box = box;
            internacion.PlanTerapeutico = datos.PlanTerapeutico.Trim();
            internacion.FechaIngreso = Ahora;
            internacion.VeterinarioIngresoId = personaId;
            internacion.Estado = Internado;
            internacion.Version++;
            await _repository.GuardarAsync();

            var dto = ADto(internacion);
            dto.Mensaje = $"{internacion.Animal?.Nombre} internado en {box.Nombre}.";
            return Resultado<InternacionReadDto>.Ok(dto);
        });
    }

    // RF-VET-10, RF-VET-11: novedades (visibles o internas) y evolución diaria (RN-23).
    public async Task<Resultado<InternacionReadDto>> PublicarAsync(int personaId, int id, NovedadCreateDto datos)
    {
        if (datos.Tipo != Novedad && datos.Tipo != Evolucion)
        {
            return Error(400, "TIPO_INVALIDO", "Elegí si es una novedad o una evolución.");
        }

        var (internacion, error) = await EnCursoAsync(id);
        if (error != null)
        {
            return error;
        }

        AgregarNovedad(internacion!.Id, datos.Tipo, datos.Texto, datos.Visible, null, personaId);
        await _repository.GuardarAsync();

        var dto = await ConNovedadesAsync(internacion, soloVisibles: false);
        dto.Mensaje = datos.Visible ? "Publicado: el cliente lo ve." : "Nota interna guardada: el cliente no la ve.";
        return new Resultado<InternacionReadDto> { Exito = true, Estado = 201, Valor = dto };
    }

    // RN-24: una entrada no se edita ni se borra; se corrige con una rectificación con su misma visibilidad.
    public async Task<Resultado<InternacionReadDto>> RectificarAsync(int personaId, int id, int novedadId, RectificacionDto datos)
    {
        var (internacion, error) = await EnCursoAsync(id);
        if (error != null)
        {
            return error;
        }

        var original = await _repository.ObtenerNovedadAsync(novedadId);
        if (original == null || original.InternacionId != id)
        {
            return Error(404, "NOVEDAD_NO_ENCONTRADA", "Esa entrada no existe.");
        }

        AgregarNovedad(id, Rectificacion, datos.Texto, original.Visible, original.Id, personaId);
        await _repository.GuardarAsync();

        var dto = await ConNovedadesAsync(internacion!, soloVisibles: false);
        dto.Mensaje = "Rectificación publicada. La entrada original se conserva.";
        return new Resultado<InternacionReadDto> { Exito = true, Estado = 201, Valor = dto };
    }

    // RF-VET-11, RN-25: con evolución final e indicaciones. El box queda libre.
    public async Task<Resultado<InternacionReadDto>> DarAltaAsync(int personaId, int id, AltaDto datos)
    {
        var internacion = await _repository.ObtenerAsync(id);
        if (internacion == null)
        {
            return NoEncontrada();
        }

        if (internacion.Estado != Internado)
        {
            return Error(409, "NO_INTERNADO", "Solo se da el alta a un animal internado.");
        }

        if (string.IsNullOrWhiteSpace(datos.EvolucionFinal) || string.IsNullOrWhiteSpace(datos.IndicacionesDomicilio))
        {
            return Error(400, "ALTA_INCOMPLETA", "Para dar el alta escribí la evolución final y las indicaciones para el domicilio (RN-25).");
        }

        internacion.EvolucionFinal = datos.EvolucionFinal.Trim();
        internacion.IndicacionesDomicilio = datos.IndicacionesDomicilio.Trim();
        internacion.FechaAlta = Ahora;
        internacion.VeterinarioAltaId = personaId;
        internacion.Estado = Alta;
        internacion.Version++;
        await _repository.GuardarAsync();

        var dto = ADto(internacion);
        dto.Mensaje = "Alta registrada. El box quedó libre y el cliente ve las indicaciones.";
        return Resultado<InternacionReadDto>.Ok(dto);
    }

    // ===================== Cliente =====================

    public async Task<List<InternacionReadDto>> MiasAsync(int cuentaId)
        => (await _repository.ObtenerDeClienteAsync(cuentaId)).Select(i => ParaCliente(ADto(i))).ToList();

    public async Task<Resultado<InternacionReadDto>> MiaAsync(int cuentaId, int id)
    {
        var internacion = await _repository.ObtenerAsync(id);
        if (internacion == null || internacion.Estado == Anulada || !await _repository.EsResponsableAsync(internacion.AnimalId, cuentaId))
        {
            return Error(404, "INTERNACION_NO_ENCONTRADA", "No encontramos esa internación entre las de tus animales.");
        }

        return Resultado<InternacionReadDto>.Ok(ParaCliente(await ConNovedadesAsync(internacion, soloVisibles: true)));
    }

    // El cliente no ve el plan terapéutico ni las observaciones internas: solo lo publicado y el alta.
    private static InternacionReadDto ParaCliente(InternacionReadDto dto)
    {
        dto.PlanTerapeutico = "";
        dto.Observaciones = "";
        return dto;
    }

    // ===================== Boxes (Administrador) =====================

    public async Task<Resultado<BoxReadDto>> AgregarBoxAsync(int adminId, BoxCreateDto datos)
    {
        var nombre = datos.Nombre.Trim();
        if (nombre.Length == 0 || (datos.Tipo != PequenosAnimales && datos.Tipo != GrandesAnimales))
        {
            return Resultado<BoxReadDto>.Error(400, "BOX_INVALIDO", "Datos no válidos", "Escribí el nombre (hasta 40 caracteres) y elegí el tipo.");
        }

        if (await _repository.ExisteBoxConNombreAsync(nombre))
        {
            return Resultado<BoxReadDto>.Error(409, "BOX_REPETIDO", "No se puede", "Ya hay un box con ese nombre.");
        }

        var box = new Box { Nombre = nombre, Tipo = datos.Tipo, Activo = true };
        _repository.Agregar(box);
        Auditar(adminId, "Box agregado", box.Id, $"{box.Nombre} ({box.Tipo})");
        await _repository.GuardarAsync();
        return new Resultado<BoxReadDto> { Exito = true, Estado = 201, Valor = BoxADto(box, null) };
    }

    // Un box ocupado no se saca de servicio.
    public async Task<Resultado<BoxReadDto>> CambiarServicioBoxAsync(int adminId, int boxId, bool activo)
    {
        var box = await _repository.ObtenerBoxAsync(boxId);
        if (box == null)
        {
            return Resultado<BoxReadDto>.Error(404, "BOX_NO_ENCONTRADO", "No encontrado", "Ese box no existe.");
        }

        var ocupante = await _repository.ObtenerOcupanteAsync(box.Id);
        if (!activo && ocupante != null)
        {
            return Resultado<BoxReadDto>.Error(409, "BOX_OCUPADO", "No se puede", $"{box.Nombre} tiene un animal internado: dale el alta antes.");
        }

        box.Activo = activo;
        Auditar(adminId, activo ? "Box en servicio" : "Box fuera de servicio", box.Id, box.Nombre);
        await _repository.GuardarAsync();
        return Resultado<BoxReadDto>.Ok(BoxADto(box, ocupante));
    }

    // ===================== Apoyo =====================

    private async Task<(Internacion? Internacion, Resultado<InternacionReadDto>? Error)> EnCursoAsync(int id)
    {
        var internacion = await _repository.ObtenerAsync(id);
        if (internacion == null)
        {
            return (null, NoEncontrada());
        }

        if (internacion.Estado != Internado)
        {
            return (null, Error(409, "NO_EN_CURSO", "Solo se publica en una internación en curso."));
        }

        return (internacion, null);
    }

    private void AgregarNovedad(int internacionId, string tipo, string texto, bool visible, int? rectificaId, int autorId)
    {
        _repository.Agregar(new NovedadInternacion
        {
            InternacionId = internacionId,
            Tipo = tipo,
            Texto = texto.Trim(),
            Visible = visible,
            RectificaId = rectificaId,
            Fecha = Ahora,
            AutorId = autorId
        });
    }

    private void Auditar(int adminId, string accion, int boxId, string detalle)
    {
        _repository.Agregar(new Auditoria
        {
            Fecha = Ahora,
            CuentaId = adminId,
            Rol = "Administrador",
            Accion = accion,
            Entidad = "Box",
            EntidadId = boxId == 0 ? "" : boxId.ToString(),
            Origen = "API",
            Detalle = detalle
        });
    }

    private async Task<InternacionReadDto> ConNovedadesAsync(Internacion internacion, bool soloVisibles)
    {
        var dto = ADto(internacion);
        dto.Novedades = (await _repository.ObtenerNovedadesAsync(internacion.Id, soloVisibles)).Select(n => new NovedadReadDto
        {
            Id = n.Id,
            Tipo = n.Tipo,
            Texto = n.Texto,
            Visible = n.Visible,
            RectificaId = n.RectificaId,
            Fecha = n.Fecha,
            Autor = n.Autor == null ? "" : $"{n.Autor.Nombre} {n.Autor.Apellido}".Trim()
        }).ToList();
        return dto;
    }

    private static InternacionReadDto ADto(Internacion i, Animal? animal = null)
    {
        animal ??= i.Animal;
        return new InternacionReadDto
        {
            Id = i.Id,
            AnimalId = i.AnimalId,
            Animal = animal?.Nombre ?? "",
            Especie = animal?.Especie?.Nombre ?? "",
            ClienteId = i.ClienteId,
            Cliente = i.Cliente == null ? "" : $"{i.Cliente.Nombre} {i.Cliente.Apellido}".Trim(),
            Estado = i.Estado,
            Motivo = i.Motivo,
            TelefonoContacto = i.TelefonoContacto,
            Observaciones = i.Observaciones,
            FechaAdmision = i.FechaAdmision,
            BoxId = i.BoxId,
            Box = i.Box?.Nombre ?? "",
            PlanTerapeutico = i.PlanTerapeutico,
            FechaIngreso = i.FechaIngreso,
            EvolucionFinal = i.EvolucionFinal,
            IndicacionesDomicilio = i.IndicacionesDomicilio,
            FechaAlta = i.FechaAlta,
            MotivoAnulacion = i.MotivoAnulacion,
            Version = i.Version
        };
    }

    private static BoxReadDto BoxADto(Box b, Internacion? ocupante) => new()
    {
        Id = b.Id,
        Nombre = b.Nombre,
        Tipo = b.Tipo,
        Activo = b.Activo,
        Ocupado = ocupante != null,
        InternacionId = ocupante?.Id,
        Animal = ocupante?.Animal?.Nombre ?? ""
    };

    private static Resultado<InternacionReadDto> NoEncontrada()
        => Resultado<InternacionReadDto>.Error(404, "INTERNACION_NO_ENCONTRADA", "No encontrado", "Esa internación no existe.");

    private static Resultado<InternacionReadDto> Error(int estado, string codigo, string mensaje)
        => Resultado<InternacionReadDto>.Error(estado, codigo, estado switch { 404 => "No encontrado", 409 => "No se puede", _ => "Datos no válidos" }, mensaje);
}
