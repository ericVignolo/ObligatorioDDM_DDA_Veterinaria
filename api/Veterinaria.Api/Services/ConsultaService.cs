using Microsoft.EntityFrameworkCore;
using Veterinaria.Api.DTOs;
using Veterinaria.Api.Models;
using Veterinaria.Api.Repositories;

namespace Veterinaria.Api.Services;

// Consulta clínica. Reglas y mensajes iguales a ConsultaService y VacunaService de Blazor.
// Borrador (solo su autor, con el turno En curso) → Firmada (inmutable). La firma es una sola transacción.
public class ConsultaService : IConsultaService
{
    // Tipo de comunicación «Resumen clínico» de la semilla.
    public const int TipoResumenClinico = 1;

    private readonly IConsultaRepository _repository;
    private readonly TimeProvider _reloj;

    public ConsultaService(IConsultaRepository repository, TimeProvider reloj)
    {
        _repository = repository;
        _reloj = reloj;
    }

    private DateTime Ahora => _reloj.GetLocalNow().DateTime;
    private DateTime Hoy => Ahora.Date;

    // ===================== Iniciar, leer y guardar =====================

    public async Task<Resultado<ConsultaReadDto>> IniciarAsync(int cuentaId, int turnoId)
    {
        var profesional = await _repository.ObtenerProfesionalDeCuentaAsync(cuentaId);
        if (profesional == null)
        {
            return Error<ConsultaReadDto>(403, "SIN_PROFESIONAL", "Tu cuenta no está asociada a un profesional activo.");
        }

        var turno = await _repository.ObtenerTurnoAsync(turnoId);
        if (turno == null)
        {
            return Error<ConsultaReadDto>(404, "TURNO_NO_ENCONTRADO", "No encontramos ese turno.");
        }

        if (turno.ProfesionalId != profesional.Id)
        {
            return Error<ConsultaReadDto>(403, "TURNO_DE_OTRO", "Este turno no está asignado a vos.");
        }

        // Retomar: un turno tiene una sola consulta (A-RC-01). Tocar dos veces «Iniciar» no crea otra.
        var existente = await _repository.ObtenerPorTurnoAsync(turno.Id);
        if (existente != null)
        {
            return existente.AutorId == cuentaId
                ? Resultado<ConsultaReadDto>.Ok(await ADtoAsync(existente))
                : Error<ConsultaReadDto>(403, "CONSULTA_DE_OTRO", "Esta consulta la está atendiendo otro veterinario.");
        }

        if (turno.Estado != "En espera")
        {
            return Error<ConsultaReadDto>(409, "TURNO_NO_EN_ESPERA", "Solo se inicia la atención de un paciente En espera (Recepción registra la llegada).");
        }

        // RN-10: una cirugía no pasa a En curso sin el consentimiento informado firmado (A-025).
        if (turno.Prestacion!.RequiereConsentimiento && !await _repository.TieneConsentimientoAceptadoAsync(turno.Id))
        {
            return Error<ConsultaReadDto>(409, "FALTA_CONSENTIMIENTO",
                "Este procedimiento necesita el consentimiento informado firmado por el responsable (RN-10). Todavía no está firmado.");
        }

        var alergias = ReglasClinicas.Alergias(await _repository.AlergiasRegistradasAsync(turno.AnimalId));
        var consulta = new Consulta
        {
            TurnoId = turno.Id,
            AnimalId = turno.AnimalId,
            ProfesionalId = profesional.Id,
            AutorId = cuentaId,
            Estado = "Borrador",
            Version = 1,
            FechaInicio = Ahora,
            Alergias = string.Join(", ", alergias)
        };
        _repository.Agregar(consulta);
        turno.Estado = "En curso";
        turno.Version++;

        try
        {
            await _repository.GuardarAsync();
        }
        catch (DbUpdateException)
        {
            // Otro pedido la creó al mismo tiempo (índice único del turno): se devuelve esa.
            var creada = await _repository.ObtenerPorTurnoAsync(turno.Id);
            return creada == null ? throw new InvalidOperationException("No se pudo iniciar la consulta.") : Resultado<ConsultaReadDto>.Ok(await ADtoAsync(creada));
        }

        var nueva = await _repository.ObtenerAsync(consulta.Id) ?? consulta;
        return new Resultado<ConsultaReadDto> { Exito = true, Estado = 201, Valor = await ADtoAsync(nueva) };
    }

    public async Task<Resultado<ConsultaReadDto>> ObtenerAsync(int cuentaId, int consultaId)
    {
        var consulta = await _repository.ObtenerAsync(consultaId);
        if (consulta == null || consulta.AutorId != cuentaId)
        {
            return NoEncontrada<ConsultaReadDto>();
        }

        return Resultado<ConsultaReadDto>.Ok(await ADtoAsync(consulta));
    }

    public async Task<List<ConsultaReadDto>> AbiertasAsync(int cuentaId)
    {
        var lista = new List<ConsultaReadDto>();
        foreach (var c in await _repository.ObtenerAbiertasAsync(cuentaId))
        {
            lista.Add(ADto(c, new List<ReservaStock>(), new List<AplicacionSanitaria>(), new List<string>()));
        }

        return lista;
    }

    public async Task<Resultado<ConsultaReadDto>> GuardarAsync(int cuentaId, int consultaId, ConsultaGuardarDto datos)
    {
        var consulta = await _repository.ObtenerAsync(consultaId);
        var error = Editable<ConsultaReadDto>(consulta, cuentaId) ?? VersionVigente<ConsultaReadDto>(consulta!, datos.Version);
        if (error != null)
        {
            return error;
        }

        Copiar(datos, consulta!);
        consulta!.Version++;
        await _repository.GuardarAsync();
        return Resultado<ConsultaReadDto>.Ok(await ADtoAsync(consulta));
    }

    // ===================== Insumos y vacunas del borrador =====================

    // RF-VET-06: el lote lo elige el inventario por vencimiento (RN-27) y queda apartado hasta la firma.
    // Si no alcanza el stock, no se registra nada (RN-26).
    public async Task<Resultado<ConsultaReadDto>> AgregarInsumoAsync(int cuentaId, int consultaId, InsumoDto datos)
    {
        var articulo = await _repository.ObtenerArticuloAsync(datos.ArticuloId);
        if (articulo == null || articulo.Tipo != "Insumo")
        {
            return Error<ConsultaReadDto>(400, "NO_ES_INSUMO", "Elegí un insumo. Las vacunas y antiparasitarios se registran en su sección.");
        }

        return await _repository.EnTransaccionAsync(consultaId, new[] { articulo.Id }, async () =>
        {
            var consulta = await _repository.ObtenerAsync(consultaId);
            var error = Editable<ConsultaReadDto>(consulta, cuentaId);
            if (error != null)
            {
                return error;
            }

            var reservas = await _repository.ObtenerReservasAsync(consultaId);
            if (reservas.Any(r => r.ArticuloId == articulo.Id && r.AplicacionSanitariaId == null))
            {
                return Error<ConsultaReadDto>(409, "INSUMO_REPETIDO", $"Ya registraste {articulo.Nombre}: quitalo y cargalo con la cantidad total.");
            }

            var lotes = await _repository.ObtenerLotesAsync(articulo.Id);
            var reservado = await _repository.ReservadoPorLoteAsync(articulo.Id);
            var plan = ReglasStock.PlanFefo(lotes, reservado, datos.Cantidad, Hoy);
            if (plan == null)
            {
                return Error<ConsultaReadDto>(409, "SIN_STOCK",
                    $"No alcanza el stock de {articulo.Nombre}: hay {ReglasStock.StockDisponible(lotes, reservado, Hoy)} {articulo.Unidad} disponibles (RN-26).");
            }

            foreach (var (lote, cantidad) in plan)
            {
                _repository.Agregar(new ReservaStock { ConsultaId = consultaId, ArticuloId = articulo.Id, LoteId = lote.Id, Cantidad = cantidad });
            }

            await _repository.GuardarAsync();
            return Resultado<ConsultaReadDto>.Ok(await ADtoAsync(consulta!));
        });
    }

    public async Task<Resultado<ConsultaReadDto>> QuitarInsumoAsync(int cuentaId, int consultaId, int articuloId)
    {
        var consulta = await _repository.ObtenerAsync(consultaId);
        var error = Editable<ConsultaReadDto>(consulta, cuentaId);
        if (error != null)
        {
            return error;
        }

        var reservas = (await _repository.ObtenerReservasAsync(consultaId))
            .Where(r => r.ArticuloId == articuloId && r.AplicacionSanitariaId == null && !r.Confirmada).ToList();
        if (reservas.Count == 0)
        {
            return Error<ConsultaReadDto>(404, "INSUMO_NO_ENCONTRADO", "Ese insumo no está registrado en la consulta.");
        }

        reservas.ForEach(r => _repository.Quitar(r));
        await _repository.GuardarAsync();
        return Resultado<ConsultaReadDto>.Ok(await ADtoAsync(consulta!));
    }

    // RF-VET-04: vacuna o antiparasitario. Se aparta una dosis del lote usable que vence antes (RN-15, RN-27);
    // adelantar al intervalo mínimo exige justificación (RN-16). Cuenta para el carnet cuando se firma.
    public async Task<Resultado<ConsultaReadDto>> AgregarVacunaAsync(int cuentaId, int consultaId, AplicacionDto datos)
    {
        var producto = await _repository.ObtenerProductoAsync(datos.ProductoSanitarioId);
        if (producto == null)
        {
            return Error<ConsultaReadDto>(400, "PRODUCTO_INVALIDO", "Elegí un producto.");
        }

        return await _repository.EnTransaccionAsync(consultaId, new[] { producto.ArticuloId }, async () =>
        {
            var consulta = await _repository.ObtenerAsync(consultaId);
            var error = Editable<ConsultaReadDto>(consulta, cuentaId);
            if (error != null)
            {
                return error;
            }

            var animal = consulta!.Animal!;
            if (!producto.Especies.Any(e => e.EspecieId == animal.EspecieId))
            {
                return Error<ConsultaReadDto>(400, "NO_APLICABLE",
                    $"No aplicable: {producto.Nombre} no está indicado para la especie {animal.Especie?.Nombre.ToLowerInvariant()}.");
            }

            if ((await _repository.ObtenerAplicacionesDeConsultaAsync(consultaId)).Any(a => a.ProductoSanitarioId == producto.Id))
            {
                return Error<ConsultaReadDto>(409, "VACUNA_REPETIDA", $"Ya registraste {producto.Nombre} en esta consulta.");
            }

            var justificacion = (datos.Justificacion ?? "").Trim();
            var anterior = (await _repository.ObtenerAplicacionesQueCuentanAsync(animal.Id)).FirstOrDefault(a => a.ProductoSanitarioId == producto.Id);
            var adelantada = anterior != null && producto.IntervaloMinimoDias > 0 && Hoy < anterior.FechaAplicacion.Date.AddDays(producto.IntervaloMinimoDias);
            if (adelantada && justificacion.Length < 10)
            {
                return Error<ConsultaReadDto>(400, "FALTA_JUSTIFICACION",
                    $"Se está adelantando la dosis: {producto.Nombre} se aplicó el {anterior!.FechaAplicacion:dd/MM/yyyy} y el intervalo mínimo es de {producto.IntervaloMinimoDias} días. Escribí la justificación (al menos 10 caracteres) para registrarla igual (RN-16).");
            }

            var lotes = await _repository.ObtenerLotesAsync(producto.ArticuloId);
            var plan = ReglasStock.PlanFefo(lotes, await _repository.ReservadoPorLoteAsync(producto.ArticuloId), 1, Hoy);
            if (plan == null)
            {
                return Error<ConsultaReadDto>(409, "SIN_STOCK_VIGENTE", $"No hay stock vigente de {producto.Nombre}: los lotes vencidos no se aplican (RN-15).");
            }

            var lote = plan[0].Lote;
            var aplicacion = new AplicacionSanitaria
            {
                AnimalId = animal.Id,
                ConsultaId = consulta.Id,
                ProductoSanitarioId = producto.Id,
                Producto = producto.Nombre,
                Tipo = producto.Tipo,
                Laboratorio = producto.Laboratorio,
                LoteId = lote.Id,
                NumeroLote = lote.Numero,
                LoteVence = lote.Vencimiento,
                FechaAplicacion = Hoy,
                FechaRefuerzo = Hoy.AddDays(producto.IntervaloRefuerzoDias),
                AplicadaPorId = cuentaId,
                JustificacionAdelanto = adelantada ? justificacion : ""
            };
            _repository.Agregar(aplicacion);
            _repository.Agregar(new ReservaStock { ConsultaId = consulta.Id, ArticuloId = producto.ArticuloId, LoteId = lote.Id, Cantidad = 1, AplicacionSanitaria = aplicacion });

            await _repository.GuardarAsync();
            return Resultado<ConsultaReadDto>.Ok(await ADtoAsync(consulta));
        });
    }

    public async Task<Resultado<ConsultaReadDto>> QuitarVacunaAsync(int cuentaId, int consultaId, int aplicacionId)
    {
        var consulta = await _repository.ObtenerAsync(consultaId);
        var error = Editable<ConsultaReadDto>(consulta, cuentaId);
        if (error != null)
        {
            return error;
        }

        var aplicacion = (await _repository.ObtenerAplicacionesDeConsultaAsync(consultaId)).FirstOrDefault(a => a.Id == aplicacionId);
        if (aplicacion == null)
        {
            return Error<ConsultaReadDto>(404, "VACUNA_NO_ENCONTRADA", "Esa aplicación no está en la consulta.");
        }

        (await _repository.ObtenerReservasAsync(consultaId)).Where(r => r.AplicacionSanitariaId == aplicacion.Id).ToList().ForEach(r => _repository.Quitar(r));
        _repository.Quitar(aplicacion);
        await _repository.GuardarAsync();
        return Resultado<ConsultaReadDto>.Ok(await ADtoAsync(consulta!));
    }

    // ===================== Firma =====================

    public async Task<Resultado<FirmaReadDto>> FirmarAsync(int cuentaId, int consultaId, ConsultaFirmaDto datos)
    {
        var articulos = (await _repository.ObtenerReservasAsync(consultaId)).Select(r => r.ArticuloId).ToList();

        return await _repository.EnTransaccionAsync(consultaId, articulos, async () =>
        {
            var consulta = await _repository.ObtenerAsync(consultaId);
            if (consulta == null || consulta.AutorId != cuentaId)
            {
                return NoEncontrada<FirmaReadDto>();
            }

            // Doble envío: ya firmada, se devuelve tal cual, sin repetir stock, cargo ni correo.
            if (consulta.Estado == "Firmada")
            {
                return Resultado<FirmaReadDto>.Ok(new FirmaReadDto { Consulta = await ADtoAsync(consulta), Mensaje = "La consulta ya estaba firmada." });
            }

            var error = Editable<FirmaReadDto>(consulta, cuentaId) ?? VersionVigente<FirmaReadDto>(consulta, datos.Version);
            if (error != null)
            {
                return error;
            }

            var cuenta = await _repository.ObtenerCuentaAsync(cuentaId);
            if (cuenta == null || !Seguridad.ContrasenaCoincide(datos.Contrasena, cuenta.HashContrasena))
            {
                return Error<FirmaReadDto>(400, "CONTRASENA_INCORRECTA", "La contraseña no es correcta. La consulta sigue en borrador.");
            }

            if (!ReglasClinicas.MatriculaVigente(consulta.Profesional, Hoy))
            {
                return Error<FirmaReadDto>(403, "MATRICULA_VENCIDA", "Tu matrícula no está vigente: no podés firmar (RN-13).");
            }

            if (string.IsNullOrWhiteSpace(datos.Motivo) || string.IsNullOrWhiteSpace(datos.Diagnostico))
            {
                return Error<FirmaReadDto>(400, "FALTAN_DATOS", "Para firmar completá el motivo y al menos un diagnóstico presuntivo o definitivo.");
            }

            // RN-17: indicar algo registrado como alergia del animal exige confirmación explícita.
            var alergias = ReglasClinicas.Alergias(await _repository.AlergiasRegistradasAsync(consulta.AnimalId), datos.Alergias);
            var choques = ReglasClinicas.Choques(alergias, datos.Tratamiento, datos.Indicaciones);
            if (choques.Count > 0 && !datos.ConfirmaAlergia)
            {
                return Error<FirmaReadDto>(409, "CONFIRMAR_ALERGIA",
                    $"El tratamiento o las indicaciones incluyen {string.Join(", ", choques)}, registrado como alergia de este animal. Si lo indicás igual, marcá la confirmación (RN-17). La consulta sigue en borrador.");
            }

            // RN-26, RN-29: lo apartado tiene que seguir siendo usable al firmar.
            var reservas = (await _repository.ObtenerReservasAsync(consulta.Id)).Where(r => !r.Confirmada).ToList();
            foreach (var reserva in reservas)
            {
                var porLote = reservas.Where(r => r.LoteId == reserva.LoteId).Sum(r => r.Cantidad);
                if (reserva.Lote == null || !ReglasStock.Usable(reserva.Lote, Hoy) || reserva.Lote.Cantidad < porLote)
                {
                    return Error<FirmaReadDto>(409, "LOTE_NO_USABLE",
                        $"El lote de {reserva.Articulo?.Nombre ?? "un artículo"} ya no se puede usar (venció o no tiene stock). Quitalo y registralo de nuevo. La consulta sigue en borrador.");
                }
            }

            // P-RC-07: tarifa vigente en la fecha de la prestación; responsable económico fijado en el cargo.
            var turno = consulta.Turno!;
            var tarifa = await _repository.ObtenerTarifaVigenteAsync(turno.PrestacionId, turno.Inicio);
            if (tarifa == null)
            {
                return Error<FirmaReadDto>(409, "SIN_TARIFA", "El servicio no tiene una tarifa vigente: avisá al Administrador. La consulta sigue en borrador.");
            }

            var responsable = await _repository.ObtenerResponsableEconomicoAsync(consulta.AnimalId, turno.ClienteId) ?? turno.ClienteId;

            // Desde acá no puede fallar: se aplica todo junto (RNF-CON-03).
            var ahora = Ahora;
            Copiar(datos, consulta);
            consulta.AlergiaConfirmada = string.Join(", ", choques);
            consulta.Estado = "Firmada";
            consulta.FechaFirma = ahora;
            consulta.HashContenido = ReglasClinicas.Huella(ReglasClinicas.Contenido(consulta));
            consulta.Version++;

            turno.Estado = "Finalizado";
            turno.Version++;

            await DescontarStockAsync(consulta.Id, reservas, cuentaId, ahora);

            Cargo? cargo = null;
            if (!await _repository.ExisteCargoDeConsultaAsync(consulta.Id))
            {
                cargo = new Cargo
                {
                    Consulta = consulta,
                    TurnoId = turno.Id,
                    ClienteId = responsable,
                    AnimalId = consulta.AnimalId,
                    PrestacionId = turno.PrestacionId,
                    Descripcion = $"{turno.Prestacion!.Nombre} — {consulta.Animal!.Nombre}",
                    Monto = ReglasClinicas.RedondearPesos(tarifa.Precio),
                    Fecha = ahora
                };
                _repository.Agregar(cargo);
            }

            await EncolarResumenAsync(responsable, consulta, ahora);

            await _repository.GuardarAsync();

            return Resultado<FirmaReadDto>.Ok(new FirmaReadDto
            {
                Consulta = await ADtoAsync(consulta),
                CargoId = cargo?.Id,
                Monto = cargo?.Monto ?? 0,
                Mensaje = "Consulta firmada y turno finalizado. El cargo quedó pendiente de cobro en Recepción."
                    + (choques.Count > 0 ? $" Quedó asentado que se indicó {string.Join(", ", choques)} pese a la alergia registrada." : "")
            });
        });
    }

    // Descuenta lo apartado, deja el movimiento de consumo y avisa si un artículo cruzó el mínimo (RN-28, RN-30).
    private async Task DescontarStockAsync(int consultaId, List<ReservaStock> reservas, int cuentaId, DateTime ahora)
    {
        foreach (var grupo in reservas.GroupBy(r => r.ArticuloId))
        {
            var lotes = await _repository.ObtenerLotesAsync(grupo.Key);
            var antes = ReglasStock.Stock(lotes, Hoy);

            foreach (var reserva in grupo)
            {
                var lote = lotes.First(l => l.Id == reserva.LoteId);
                lote.Cantidad -= reserva.Cantidad;
                reserva.Confirmada = true;
                _repository.Agregar(new MovimientoStock
                {
                    ArticuloId = reserva.ArticuloId,
                    LoteId = lote.Id,
                    Tipo = "Consumo",
                    Cantidad = -reserva.Cantidad,
                    Origen = $"Consulta {consultaId}",
                    Fecha = ahora,
                    PersonaId = cuentaId
                });
            }

            var articulo = grupo.First().Articulo;
            var despues = ReglasStock.Stock(lotes, Hoy);
            if (articulo != null && ReglasStock.CruzaMinimo(antes, despues, articulo.StockMinimo))
            {
                _repository.Agregar(new AlertaStock { ArticuloId = articulo.Id, StockQueQuedo = despues, StockMinimo = articulo.StockMinimo, Origen = $"Consulta {consultaId}", Fecha = ahora });
            }
        }
    }

    // RF-CLI-10: el resumen al responsable queda en la cola por el canal que eligió (un solo mensaje por consulta).
    private async Task EncolarResumenAsync(int clienteId, Consulta consulta, DateTime ahora)
    {
        var cliente = await _repository.ObtenerCuentaAsync(clienteId);
        var tipo = await _repository.ObtenerTipoComunicacionAsync(TipoResumenClinico);
        if (cliente == null || tipo == null || cliente.Eliminada || !cliente.Activa)
        {
            return;
        }

        var canal = await _repository.ObtenerCanalPreferidoAsync(clienteId, tipo.Id) ?? tipo.CanalPorDefecto;
        _repository.Agregar(new Comunicacion
        {
            CuentaId = clienteId,
            TipoComunicacionId = tipo.Id,
            Asunto = $"Resumen de la consulta de {consulta.Animal!.Nombre}",
            Cuerpo = $"Motivo: {consulta.Motivo}\nDiagnóstico ({consulta.TipoDiagnostico.ToLowerInvariant()}): {consulta.Diagnostico}\n"
                + $"Tratamiento: {consulta.Tratamiento}\nIndicaciones: {consulta.Indicaciones}",
            Canal = canal == "Correo" ? "Correo" : "En la app",
            Estado = "Pendiente",
            Destino = canal == "Correo" ? cliente.Correo : "",
            FechaCreacion = ahora,
            Clave = $"consulta-{consulta.Id}"
        });
    }

    // ===================== Cancelar sin atención y enmiendas =====================

    public async Task<Resultado<MensajeDto>> CancelarSinAtencionAsync(int cuentaId, int consultaId, string motivo)
    {
        var consulta = await _repository.ObtenerAsync(consultaId);
        var error = Editable<MensajeDto>(consulta, cuentaId);
        if (error != null)
        {
            return error;
        }

        if ((await _repository.ObtenerReservasAsync(consultaId)).Count > 0 || (await _repository.ObtenerAplicacionesDeConsultaAsync(consultaId)).Count > 0)
        {
            return Error<MensajeDto>(409, "CON_CONSUMOS", "Registraste insumos o vacunas en esta consulta: si no se usaron, quitalos antes de cancelar sin atención.");
        }

        var turno = consulta!.Turno!;
        turno.Estado = "Cancelado";
        turno.MotivoCancelacion = motivo.Trim();
        turno.FechaCancelacion = Ahora;
        turno.CanceladoPor = "Veterinario";
        turno.Version++;
        await _repository.GuardarAsync();
        return Resultado<MensajeDto>.Ok(new MensajeDto { Mensaje = "Turno cancelado sin atención. El borrador queda guardado y no se publica." });
    }

    public async Task<Resultado<ConsultaReadDto>> EnmendarAsync(int cuentaId, int consultaId, EnmiendaDto datos)
    {
        var consulta = await _repository.ObtenerAsync(consultaId);
        if (consulta == null || consulta.Estado != "Firmada")
        {
            return Error<ConsultaReadDto>(409, "NO_FIRMADA", "Solo se enmienda una consulta firmada.");
        }

        if (consulta.AutorId != cuentaId)
        {
            return Error<ConsultaReadDto>(403, "NO_ES_AUTOR", "Solo el veterinario que firmó la consulta puede enmendarla.");
        }

        var cuenta = await _repository.ObtenerCuentaAsync(cuentaId);
        if (cuenta == null || !Seguridad.ContrasenaCoincide(datos.Contrasena, cuenta.HashContrasena))
        {
            return Error<ConsultaReadDto>(400, "CONTRASENA_INCORRECTA", "La contraseña no es correcta.");
        }

        if (!ReglasClinicas.MatriculaVigente(consulta.Profesional, Hoy))
        {
            return Error<ConsultaReadDto>(403, "MATRICULA_VENCIDA", "Tu matrícula no está vigente: no podés firmar (RN-13).");
        }

        var ahora = Ahora;
        var enmienda = new Enmienda
        {
            ConsultaId = consulta.Id,
            Motivo = datos.Motivo.Trim(),
            Texto = datos.Texto.Trim(),
            AutorId = cuentaId,
            Autor = cuenta,
            Fecha = ahora
        };
        enmienda.HashContenido = ReglasClinicas.Huella($"{consulta.Id}|{consulta.HashContenido}|{enmienda.Motivo}|{enmienda.Texto}|{cuentaId}|{ahora:O}");
        _repository.Agregar(enmienda);
        consulta.Enmiendas.Add(enmienda);
        await _repository.GuardarAsync();
        return Resultado<ConsultaReadDto>.Ok(await ADtoAsync(consulta));
    }

    // ===================== Historia =====================

    public async Task<Resultado<HistoriaDto>> HistoriaParaPersonalAsync(int cuentaId, int animalId, string motivo)
    {
        var animal = await _repository.ObtenerAnimalAsync(animalId);
        if (animal == null)
        {
            return Error<HistoriaDto>(404, "ANIMAL_NO_ENCONTRADO", "No encontramos ese animal.");
        }

        // P-RC-05: sin turno del día ni internación abierta, hace falta un motivo, que queda auditado.
        motivo = (motivo ?? "").Trim();
        var conCaso = await _repository.TieneCasoActivoAsync(animalId, Ahora);
        if (!conCaso && motivo.Length < 5)
        {
            return Error<HistoriaDto>(400, "MOTIVO_REQUERIDO", "Este animal no tiene un turno activo: indicá el motivo del acceso.");
        }

        // RN-40: toda lectura queda registrada, sin copiar texto clínico.
        _repository.Agregar(new LecturaHistoria { CuentaId = cuentaId, AnimalId = animalId, Fecha = Ahora, Motivo = conCaso ? "" : motivo });
        await _repository.GuardarAsync();

        return Resultado<HistoriaDto>.Ok(await HistoriaAsync(animal));
    }

    public async Task<Resultado<HistoriaDto>> HistoriaParaClienteAsync(int cuentaId, int animalId)
    {
        var animal = await _repository.ObtenerAnimalAsync(animalId);
        if (animal == null || !await _repository.EsResponsableAsync(animalId, cuentaId))
        {
            return Error<HistoriaDto>(404, "ANIMAL_NO_ENCONTRADO", "No encontramos ese animal entre los tuyos.");
        }

        return Resultado<HistoriaDto>.Ok(await HistoriaAsync(animal));
    }

    private async Task<HistoriaDto> HistoriaAsync(Animal animal)
    {
        var historia = new HistoriaDto
        {
            AnimalId = animal.Id,
            Animal = animal.Nombre,
            Especie = animal.Especie?.Nombre ?? "",
            Alergias = ReglasClinicas.Alergias(await _repository.AlergiasRegistradasAsync(animal.Id)),
            Vacunas = (await _repository.ObtenerAplicacionesQueCuentanAsync(animal.Id)).Select(AplicacionADto).ToList()
        };

        foreach (var c in await _repository.ObtenerFirmadasAsync(animal.Id))
        {
            historia.Consultas.Add(ADto(c, new List<ReservaStock>(), new List<AplicacionSanitaria>(), new List<string>()));
        }

        return historia;
    }

    // ===================== Apoyo =====================

    // Borrador del autor con el turno En curso: la única consulta que se edita (A-021).
    private static Resultado<T>? Editable<T>(Consulta? consulta, int cuentaId)
    {
        if (consulta == null || consulta.AutorId != cuentaId)
        {
            return NoEncontrada<T>();
        }

        if (consulta.Estado != "Borrador" || consulta.Turno?.Estado != "En curso")
        {
            return Error<T>(409, "NO_EDITABLE", "Esta consulta ya no se puede editar.");
        }

        return null;
    }

    private static Resultado<T>? VersionVigente<T>(Consulta consulta, int version)
    {
        return consulta.Version == version
            ? null
            : Error<T>(409, "VERSION_DESACTUALIZADA", "Otra pantalla guardó cambios en esta consulta. Recargá para ver la versión actual.");
    }

    private static void Copiar(ConsultaGuardarDto d, Consulta c)
    {
        c.Motivo = (d.Motivo ?? "").Trim();
        c.Anamnesis = (d.Anamnesis ?? "").Trim();
        c.ExamenFisico = (d.ExamenFisico ?? "").Trim();
        c.TemperaturaC = d.TemperaturaC;
        c.FrecuenciaCardiaca = d.FrecuenciaCardiaca;
        c.FrecuenciaRespiratoria = d.FrecuenciaRespiratoria;
        c.PesoKg = d.PesoKg;
        c.Alergias = (d.Alergias ?? "").Trim();
        c.Diagnostico = (d.Diagnostico ?? "").Trim();
        c.TipoDiagnostico = d.TipoDiagnostico == "Definitivo" ? "Definitivo" : "Presuntivo";
        c.Tratamiento = (d.Tratamiento ?? "").Trim();
        c.Indicaciones = (d.Indicaciones ?? "").Trim();
    }

    private async Task<ConsultaReadDto> ADtoAsync(Consulta consulta)
    {
        var reservas = await _repository.ObtenerReservasAsync(consulta.Id);
        var aplicaciones = await _repository.ObtenerAplicacionesDeConsultaAsync(consulta.Id);
        var alergias = ReglasClinicas.Alergias(await _repository.AlergiasRegistradasAsync(consulta.AnimalId));
        return ADto(consulta, reservas, aplicaciones, alergias);
    }

    private static ConsultaReadDto ADto(Consulta c, List<ReservaStock> reservas, List<AplicacionSanitaria> aplicaciones, List<string> alergias)
    {
        return new ConsultaReadDto
        {
            Id = c.Id,
            TurnoId = c.TurnoId,
            AnimalId = c.AnimalId,
            Animal = c.Animal?.Nombre ?? "",
            Especie = c.Animal?.Especie?.Nombre ?? "",
            Prestacion = c.Turno?.Prestacion?.Nombre ?? "",
            Profesional = c.Profesional?.Nombre ?? "",
            Estado = c.Estado,
            Version = c.Version,
            FechaInicio = c.FechaInicio,
            FechaFirma = c.FechaFirma,
            Motivo = c.Motivo,
            Anamnesis = c.Anamnesis,
            ExamenFisico = c.ExamenFisico,
            TemperaturaC = c.TemperaturaC,
            FrecuenciaCardiaca = c.FrecuenciaCardiaca,
            FrecuenciaRespiratoria = c.FrecuenciaRespiratoria,
            PesoKg = c.PesoKg,
            Alergias = c.Alergias,
            Diagnostico = c.Diagnostico,
            TipoDiagnostico = c.TipoDiagnostico,
            Tratamiento = c.Tratamiento,
            Indicaciones = c.Indicaciones,
            AlergiaConfirmada = c.AlergiaConfirmada,
            AlergiasRegistradas = alergias,
            Insumos = reservas.Where(r => r.AplicacionSanitariaId == null && r.AplicacionSanitaria == null)
                .GroupBy(r => r.ArticuloId)
                .Select(g => new InsumoReadDto
                {
                    ArticuloId = g.Key,
                    Articulo = g.First().Articulo?.Nombre ?? "",
                    Cantidad = g.Sum(r => r.Cantidad),
                    Unidad = g.First().Articulo?.Unidad ?? "",
                    Lotes = string.Join(", ", g.Select(r => r.Lote?.Numero ?? "").Where(n => n != ""))
                }).ToList(),
            Vacunas = aplicaciones.Select(AplicacionADto).ToList(),
            Enmiendas = c.Enmiendas.OrderBy(e => e.Fecha).Select(e => new EnmiendaReadDto
            {
                Id = e.Id,
                Fecha = e.Fecha,
                Autor = e.Autor == null ? "" : $"{e.Autor.Nombre} {e.Autor.Apellido}".Trim(),
                Motivo = e.Motivo,
                Texto = e.Texto
            }).ToList()
        };
    }

    private static AplicacionReadDto AplicacionADto(AplicacionSanitaria a) => new()
    {
        Id = a.Id,
        Producto = a.Producto,
        Tipo = a.Tipo,
        Laboratorio = a.Laboratorio,
        Lote = a.NumeroLote,
        FechaAplicacion = DateOnly.FromDateTime(a.FechaAplicacion),
        FechaRefuerzo = DateOnly.FromDateTime(a.FechaRefuerzo),
        Adelantada = a.JustificacionAdelanto != ""
    };

    private static Resultado<T> NoEncontrada<T>() => Error<T>(404, "CONSULTA_NO_ENCONTRADA", "No encontramos esa consulta entre las tuyas.");

    private static Resultado<T> Error<T>(int estado, string codigo, string mensaje)
        => Resultado<T>.Error(estado, codigo, estado switch { 403 => "Sin permiso", 404 => "No encontrado", 409 => "No se puede", _ => "Datos no válidos" }, mensaje);
}
