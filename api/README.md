# API compartida

`Veterinaria.Api`: un solo proyecto ASP.NET Core (.NET 10) con el estilo del profesor (Controller → Service → Repository → `AppDbContext`). Usa MySQL 8.4 con EF Core 10 y el proveedor de Oracle `MySql.EntityFrameworkCore` (DT-BD-02 y DT-API-01 en `CONTEXTO_BASE.md`).

Las reglas que aplican están en los [principios vigentes](../docs/contexto-base/CONTEXTO_BASE.md#31-principios-vigentes). El primer contrato es el [catálogo de servicios](../docs/analisis/ESPECIFICACION_CATALOGO_SERVICIOS_API.md).

## Estructura

```text
api/Veterinaria.Api/
├── Data/AppDbContext.cs     tablas, índices y semilla (HasData)
├── Models/                  entidades de la base
├── Properties/launchSettings.json
├── Program.cs               conexión a MySQL (UseMySQL)
├── appsettings.json         cadena de conexión sin contraseña
└── appsettings.Development.example.json
```

Capas (estilo del profesor): `Controllers/` → `Services/` (`IXxxService`) → `Repositories/` (`IXxxRepository`) → `AppDbContext`; `DTOs/` con mapeo explícito en el Service.

## Endpoints

| Método y ruta | Acceso | Respuesta |
|---|---|---|
| `GET /api/v1/servicios?page=1&pageSize=20` | Público | `200` con `items`, `page`, `pageSize` y `totalItems`; `400` (Problem Details, `PAGINACION_INVALIDA`) si `page` < 1 o `pageSize` no está entre 1 y 50 |
| `GET /api/v1/servicios/{id}` | Público | `200` con el servicio; `404` (Problem Details, `SERVICIO_NO_ENCONTRADO`) si no existe o no es público |
| `GET /api/v1/profesionales?page=1&pageSize=20` | Público | `200` paginado: nombre, especialidad, descripción, especies y servicios públicos; `400` si la paginación es inválida |
| `GET /api/v1/profesionales/{id}` | Público | `200` con lo anterior más `horario` por día (lunes a domingo, `jornadas` y `descansos` en `HH:mm`); `404` (`PROFESIONAL_NO_ENCONTRADO`) si no existe o no está publicado y activo |
| `POST /api/v1/cuentas` | Público | Registro de cliente: `201` (crea la cuenta sin verificar y manda el código); `400` con los errores de cada campo; `409` (`CUENTA_NO_DISPONIBLE`) si el correo o el documento ya tienen cuenta |
| `POST /api/v1/cuentas/verificacion` | Público | `200`; `400` (`CODIGO_INCORRECTO`, `CODIGO_VENCIDO`, `CODIGO_INVALIDADO`, `SIN_CODIGO`); `404` si no hay cuenta pendiente |
| `POST /api/v1/cuentas/verificacion/reenvio` | Público | `200` con un código nuevo; `429` (`DEMASIADOS_CODIGOS`) después de 3 reenvíos en una hora |
| `POST /api/v1/sesiones` | Público | Ingreso de clientes y personal: `200` con `tokenAcceso` (15 min), `tokenRefresco` (7 días) y `roles`; `401` (`CREDENCIALES_INVALIDAS`); `403` (`CUENTA_NO_VERIFICADA`); `423` (`CUENTA_BLOQUEADA`) |
| `POST /api/v1/sesiones/refresco` | Público | `200` con tokens nuevos (el de refresco usado deja de servir); `401` (`SESION_INVALIDA`) |
| `DELETE /api/v1/sesiones` | Público | Cierra la sesión del token de refresco: `204` |
| `POST /api/v1/cuentas/recuperacion` | Público | Siempre `200` con el mismo mensaje, exista o no el correo (D-REG-03) |
| `POST /api/v1/cuentas/recuperacion/confirmar` | Público | `200` (cambia la contraseña y cierra todas las sesiones); `400` (`CODIGO_INVALIDO`, `CODIGO_INCORRECTO`) |
| `GET /api/v1/cuentas/yo` | Con token | `200` con los datos de la cuenta; `401` sin token o con token vencido |
| `GET /api/v1/especies` | Público | `200` con las especies (id y nombre) para los formularios |
| `GET /api/v1/animales?page=1&pageSize=20` | Cliente | `200` paginado, solo los animales activos del cliente del token (RN-18) |
| `GET /api/v1/animales/{id}` | Cliente | `200`; `404` (`ANIMAL_NO_ENCONTRADO`) si no existe, está de baja o es de otro cliente |
| `POST /api/v1/animales` | Cliente | `201`, con el cliente como responsable; `400` con los errores de cada campo o `ESPECIE_INVALIDA`; `409` (`MICROCHIP_REPETIDO`, RN-41) |
| `PUT /api/v1/animales/{id}` | Cliente | `200`; `404` si es ajeno; `409` si el microchip es de otro animal |
| `DELETE /api/v1/animales/{id}` | Cliente | Baja lógica: `200` (la historia se conserva); `409` (`ANIMAL_CON_TURNOS`) si tiene turnos pendientes |
| `GET /api/v1/turnos/disponibilidad?profesionalId=&prestacionId=&animalId=&fecha=` | Cliente | `200` con los horarios libres del día, `proximoDiaConHorarios` si no hay y `avisoAusencias` (RN-08); `400` si el profesional no hace ese servicio o no atiende esa especie |
| `POST /api/v1/turnos` | Cliente | `201` Confirmado; `409` (`HORARIO_NO_DISPONIBLE`) si se ocupó; `403` (`RESERVA_POR_RECEPCION`) con 3 ausencias en 12 meses |
| `GET /api/v1/turnos?vista=proximos\|historial` | Cliente | `200` paginado con los turnos propios y `puedeGestionar` |
| `GET /api/v1/turnos/{id}` | Cliente | `200`; `404` si es ajeno |
| `POST /api/v1/turnos/{id}/cancelacion` | Cliente | `200`; `409` (`CANCELAR_POR_RECEPCION`) a menos de 24 h (RN-06) |
| `PUT /api/v1/turnos/{id}/horario` | Cliente | Reprograma el mismo turno: `200`; `409` si el horario no está libre o faltan menos de 24 h |
| `GET /api/v1/recepcion/clientes?buscar=` | Recepción, Administrador | Clientes con sus animales y ausencias recientes |
| `GET /api/v1/recepcion/disponibilidad?clienteId=&animalId=&profesionalId=&prestacionId=&fecha=` | Recepción, Administrador | Igual que la del cliente, sin exigir que el servicio sea reservable en línea |
| `GET /api/v1/recepcion/turnos?fecha=&profesionalId=&estado=` | Recepción, Administrador | Agenda del día |
| `POST /api/v1/recepcion/turnos` | Recepción, Administrador | Turno por mostrador; con `sobreturno: true` y motivo, fuera de la grilla o sobre un horario tomado del profesional (RN-09, queda auditado) |
| `POST /api/v1/recepcion/turnos/{id}/llegada` | Recepción | Confirmado → En espera, el mismo día |
| `POST /api/v1/recepcion/turnos/{id}/ausencia` | Recepción | Confirmado → Ausente, 15 min después del inicio |
| `POST /api/v1/recepcion/turnos/{id}/cancelacion` | Recepción | En cualquier momento, con motivo; también el retiro de alguien En espera |
| `GET /api/v1/veterinario/agenda?fecha=` | Veterinario | Turnos del día del profesional de la cuenta |
| `POST /api/v1/veterinario/turnos/{turnoId}/consulta` | Veterinario | Inicia (En espera → En curso) o retoma la consulta del turno: `201`/`200`; `403` si el turno es de otro; `409` si no está En espera o falta el consentimiento (RN-10) |
| `GET /api/v1/veterinario/consultas/abiertas` | Veterinario | Borradores propios con el turno En curso (P-RC-04) |
| `GET /api/v1/veterinario/consultas/{id}` | Veterinario | La consulta, con insumos, vacunas, enmiendas y alergias registradas (solo el autor) |
| `PUT /api/v1/veterinario/consultas/{id}` | Veterinario | Guarda el borrador con `version`: `409` (`VERSION_DESACTUALIZADA`) si otro guardó antes |
| `POST/DELETE /api/v1/veterinario/consultas/{id}/insumos` | Veterinario | Aparta o libera un insumo por FEFO (RN-26, RN-27); se descuenta al firmar |
| `POST/DELETE /api/v1/veterinario/consultas/{id}/vacunas` | Veterinario | Vacuna o antiparasitario: especie, lote vigente (RN-15), dosis adelantada con justificación (RN-16), refuerzo calculado |
| `POST /api/v1/veterinario/consultas/{id}/firma` | Veterinario | Con la contraseña (P-RC-06). En una transacción: motivo y diagnóstico (RN-20), matrícula (RN-13), alergias (RN-17), lotes usables, tarifa vigente; descuenta stock, movimientos y alerta de mínimo, cargo único (RN-32), turno Finalizado y resumen en la cola. Firmar dos veces no repite nada |
| `POST /api/v1/veterinario/consultas/{id}/cancelacion` | Veterinario | Cancela el turno sin atención si no hay consumos (P-RC-04) |
| `POST /api/v1/veterinario/consultas/{id}/enmiendas` | Veterinario | Solo el autor, con la contraseña; el original no cambia (RN-12) |
| `GET /api/v1/veterinario/animales/{animalId}/historia?motivo=` | Veterinario | Historia completa; sin caso activo exige motivo. Cada lectura queda en `LecturasHistoria` (RN-40, P-RC-05) |
| `GET /api/v1/animales/{id}/historia` | Cliente | Consultas firmadas, enmiendas y vacunas de su animal |
| `GET /api/v1/animales/{id}/carnet` | Cliente | Última aplicación de cada producto con su estado: Vigente, Refuerzo próximo (5 días, parámetro) o Vencida |
| `GET /api/v1/alertas-sanitarias` | Cliente | Refuerzos próximos o vencidos de todos sus animales |
| `GET /api/v1/turnos/consentimientos-pendientes` | Cliente | Turnos próximos que piden consentimiento y no lo tienen firmado en la versión vigente |
| `GET/POST /api/v1/turnos/{id}/consentimiento` | Cliente | Estado y texto; firmar (`acepta: true` con el nombre de la cuenta) o rechazar. `409` si ya respondió o el turno empezó (RN-10) |
| `GET /api/v1/veterinario/insumos` | Veterinario | Insumos con lo disponible (sin lo apartado) |
| `GET /api/v1/veterinario/productos-sanitarios?animalId=` | Veterinario | Productos de la especie del animal con el lote que se usaría (FEFO) |
| `GET /api/v1/admin/inventario/articulos` | Administrador | Stock, disponible, bajo mínimo y lotes que vencen en 30 días |
| `GET /api/v1/admin/inventario/articulos/{id}/lotes` | Administrador | Lotes con lo apartado, vencidos e inhabilitados |
| `GET /api/v1/admin/inventario/movimientos?articuloId=&page=` | Administrador | Historial paginado (RN-30) |
| `POST /api/v1/admin/inventario/compras` | Administrador | Lote nuevo: `201`; vencimiento futuro, número sin repetir |
| `POST /api/v1/admin/inventario/ajustes` y `/bajas` | Administrador | Con motivo (RN-31); nunca negativo ni debajo de lo apartado (RN-26); alerta si cruza el mínimo (RN-28) |
| `POST /api/v1/admin/inventario/vencidos` | Administrador | Inhabilita y da de baja los lotes vencidos (RN-29) |
| `GET /api/v1/admin/inventario/alertas` y `POST .../alertas/{id}/atendida` | Administrador | Alertas de stock mínimo |
| `POST /api/v1/admin/avisos-refuerzo` | Administrador | Encola los avisos de refuerzo; repetirlo no duplica |
| `GET/POST /api/v1/admin/consentimientos` | Administrador | Versiones del texto de un servicio; publicar una nueva deja la anterior sin vigencia |
| `GET /api/v1/cobros` | Cliente | Sus cargos con lo pagado, el saldo y el estado (Pendiente, Pago parcial, Pagado) |
| `GET /api/v1/recepcion/cobros/cargos?clienteId=&pendientes=true` y `/cargos/{id}` | Recepción, Administrador | Cargos (por defecto, con saldo); el detalle trae sus comprobantes |
| `POST /api/v1/recepcion/cobros/pagos` | Recepción | Comprobante `C-000001`: `201`; monto hasta el saldo, transferencia con número, `409` si ya está pagado. Si paga la seña, confirma la cirugía |
| `GET /api/v1/recepcion/cobros/pagos?fecha=`, `/pagos/{id}` y `/resumen` | Recepción, Administrador | Comprobantes del día (con los anulados marcados) y caja |
| `GET /api/v1/recepcion/cobros/deuda/{clienteId}` | Recepción, Administrador | Deuda vencida (más de 30 días) y aviso si pasa el límite (RN-36); nunca bloquea |
| `POST /api/v1/admin/cobros/pagos/{id}/anulacion` | Administrador | Nota de crédito `NC-000001` con motivo y auditoría; el comprobante no cambia (RN-34) |
| `GET /api/v1/admin/cobros/notas` | Administrador | Notas de crédito emitidas |
| `GET/POST /api/v1/admin/servicios` y `PUT /servicios/{id}` | Administrador | Todos los servicios (también los no publicados); crear arma la tarifa inicial; editar no cambia el precio |
| `GET /api/v1/admin/servicios/{id}/tarifas` y `POST /api/v1/admin/tarifas` | Administrador | Tarifas con fecha; una nueva rige desde hoy o después, sin repetir fecha (RN-33) |
| `GET/POST /api/v1/admin/personal` | Administrador | Cuentas del personal; correo único, contraseña inicial; un veterinario necesita una ficha de profesional libre |
| `PUT /api/v1/admin/personal/{id}/rol` y `/bloqueo` | Administrador | Cambia el rol o bloquea; cierra sus sesiones; nadie se cambia a sí mismo y siempre queda un Administrador activo |
| `GET/POST/DELETE /api/v1/admin/ausencias` y `/feriados` | Administrador | Licencias y feriados de hoy en adelante; `409` si hay turnos activos esas fechas |
| `GET /api/v1/admin/parametros` y `PUT /parametros/{clave}` | Administrador | Con rango; se crean si faltan (`cobro.porcentaje_sena`, `cobro.limite_deuda`) |
| `PUT /api/v1/admin/inventario/articulos/{id}/minimo` | Administrador | Stock mínimo del artículo |
| `GET /api/v1/admin/auditoria?entidad=&desde=&page=` | Administrador | Registro de cambios, del más reciente al más antiguo |
| `GET /api/v1/internaciones`, `/altas`, `/{id}` y `/boxes` | Personal | Tablero de abiertas, altas, detalle (el veterinario ve también las notas internas) y boxes con su ocupante |
| `POST /api/v1/internaciones` y `/{id}/anulacion` | Recepción | Admisión (una abierta por animal, RN-21; un grupo no se interna) y anulación antes del ingreso |
| `GET /api/v1/internaciones/{id}/boxes-libres` y `POST /{id}/ingreso` | Personal / Veterinario | Box libre, en servicio y del tipo de la especie (RN-22) con el plan terapéutico |
| `POST /api/v1/internaciones/{id}/novedades` y `/novedades/{n}/rectificacion` | Veterinario | Novedad o evolución, visible o interna (RN-23); la rectificación conserva la original (RN-24) |
| `POST /api/v1/internaciones/{id}/alta` | Veterinario | Con evolución final e indicaciones (RN-25); libera el box |
| `GET /api/v1/mis-internaciones` y `/{id}` | Cliente | Las de sus animales, con lo visible y el alta, sin el plan ni las notas internas |
| `POST /api/v1/admin/boxes` y `PUT /{id}/servicio` | Administrador | Agregar un box; sacarlo de servicio si no está ocupado |
| `GET /api/v1/comunicaciones` y `POST /{id}/leida` | Cliente | Bandeja propia, con la cantidad de no leídas |
| `GET /api/v1/comunicaciones/preferencias` y `PUT /preferencias/{tipoId}` | Cliente | Canal por tipo (Correo, Solo en la app, No recibir); los esenciales no se apagan |
| `GET /api/v1/recepcion/comunicaciones?estado=&tipoId=&texto=` | Recepción, Administrador | Bandeja de salida |
| `POST /api/v1/recepcion/comunicaciones/cola` | Recepción, Administrador | Procesa los pendientes: hasta 3 intentos por correo y después Fallido |
| `POST /api/v1/recepcion/comunicaciones/{id}/reintento` y `/{id}/reenvio` | Recepción, Administrador | Reintentar un Fallido; reenviar como copia nueva por correo |
| `POST /api/v1/recepcion/comunicaciones/comprobantes/{pagoId}` y `/avisos` | Recepción, Administrador | Comprobante de un pago (una vez) y aviso de la clínica a todos los clientes |
| `POST /api/v1/difusion/alerta-sanitaria` | Administrador, Veterinario | Alerta para los clientes con animales de una especie que la activaron |
| `POST /api/v1/asistencias`, `GET /` y `GET /{id}` | Cliente | Solicitud de asistencia (GPS consentido o dirección; idempotente por `claveIdempotencia`) y su seguimiento |
| `POST /api/v1/asistencias/{id}/empeoramiento` | Cliente | El nivel solo sube (RN-51); a rojo activa la guardia |
| `GET /api/v1/recepcion/asistencias`, `/cerradas`, `/{id}` y `/{id}/elegibles` | Recepción, Administrador | Cola por nivel y antigüedad (revisa los plazos), historial y veterinarios elegibles |
| `POST /api/v1/recepcion/asistencias`, `/{id}/asignacion`, `/{id}/empeoramiento` y `/{id}/cierre` | Recepción | Solicitud telefónica, asignación, empeoramiento por teléfono y cierre sin atención |
| `POST /api/v1/recepcion/asistencias/plazos` | Recepción, Administrador | Rojo sin aceptar en 10 min → Recepción; 24 h → reevaluación |
| `GET /api/v1/veterinario/asistencias` y `/{id}` | Veterinario | Ofrecidas a mí y las que acepté |
| `POST /api/v1/veterinario/asistencias/{id}/aceptacion`, `/rechazo`, `/avance`, `/finalizacion` y `/nivel` | Veterinario | El primero que acepta se la queda; traslado; finalizar genera el cargo; nivel con motivo (RN-52) |
| `PUT /api/v1/admin/guardia` | Administrador | Veterinario de guardia (RF-ADM-13) |
| `GET /api/v1/clinica` | Público | `200` con nombre, dirección, teléfonos, guardia, correo, horario y cobertura; `404` (`CLINICA_NO_CONFIGURADA`) si no hay datos |

Un servicio es público si está publicado y tiene precio (`ReglasCatalogo.EsPublica`); un profesional, si está publicado y activo (`ReglasProfesionales.EsPublico`). A los profesionales no se les expone la matrícula, la cuenta ni las ausencias. Los errores salen de `Controllers/Problemas.cs`. Pruebas: `ApiCatalogoTests.cs` y `ApiProfesionalesTests.cs` en `tests/Veterinaria.Tests/`. El contrato OpenAPI se ve en `/openapi/v1.json` al correr en Development.

## Tablas (52)

Migraciones `Inicial` (2026-11-10, 22 tablas), `ModulosClinicos` (2026-11-19, 29 tablas) y `Sesiones` (2026-11-19: tabla `SesionesRefresco` y bloqueo por intentos en `Cuentas`). La configuración de la segunda está en `Data/ConfiguracionModulos.cs`.

| Grupo | Tablas |
|---|---|
| Identidad | `Cuentas` (cliente y personal), `Roles`, `CuentaRoles`, `CodigosCuenta`, `SesionesRefresco`, `DocumentosLegales`, `AceptacionesLegales` |
| Animales | `Especies`, `Animales`, `Responsabilidades` (cliente ↔ animal con vigencia) |
| Catálogo y agenda | `Prestaciones`, `Profesionales`, `ProfesionalEspecies`, `ProfesionalPrestaciones`, `FranjasHorarias`, `Ausencias`, `Recursos`, `Turnos`, `TurnoRecursos` |
| Consulta e historia | `Consultas`, `Enmiendas`, `LecturasHistoria`, `DocumentosConsentimiento`, `Consentimientos` |
| Vacunas e inventario | `Articulos`, `Lotes`, `MovimientosStock`, `ReservasStock`, `AlertasStock`, `ProductosSanitarios`, `ProductoSanitarioEspecies`, `AplicacionesSanitarias` |
| Cobro | `TarifasPrestacion`, `Cargos`, `Pagos` (comprobantes), `NotasCredito`, `Numeradores` |
| Internación | `Boxes`, `Internaciones`, `NovedadesInternacion` |
| Asistencia | `SolicitudesAsistencia`, `OfertasAsistencia`, `EventosAsistencia` |
| Comunicaciones y derechos | `TiposComunicacion`, `PreferenciasComunicacion`, `Comunicaciones` (cola de envío), `SolicitudesDerecho`, `PoliticasConservacion` |
| Configuración | `Clinicas`, `Parametros`, `DiasNoLaborables`, `Auditorias` |

El personal no tiene tabla propia: es una `Cuenta` con rol Recepcionista, Veterinario o Administrador, y el Veterinario se enlaza con su `Profesional`. Los autores (quién firmó, cobró, admitió, etc.) son claves a `Cuentas`.

Semilla (mismos datos de demostración que Blazor): roles, documentos legales 1.0, 11 especies, 8 prestaciones (con castración y su consentimiento), 5 profesionales con horarios, 4 recursos, VITALIA, parámetros y feriados; **personal**: `recepcion@`, `ferreira@`, `silveira@`, `acosta@` y `admin@vitalia.uy`, todos con la contraseña `Vitalia2026!`; 4 boxes; 12 artículos con 20 lotes y su movimiento de compra; 7 productos sanitarios; tarifas iniciales; tipos de comunicación y política de conservación. No trae clientes, animales ni turnos: se crean desde la API.

Decisiones del esquema:
- No hay borrado en cascada: las bajas son lógicas (`Activa`/`Activo`) y la historia se conserva.
- Restricciones `CHECK`: el stock de un lote no queda negativo (RN-26) y un pago es mayor que cero. Índices únicos: un cargo por consulta (RN-32), número de comprobante y de nota de crédito, una nota de crédito por comprobante (RN-34), un turno con una sola consulta.
- Lo que MySQL no puede expresar con un índice (una internación abierta por animal, RN-21; un paciente por box, RN-22) lo controla la API dentro de la transacción.
- Los textos clínicos largos son `LONGTEXT`, sin `MaxLength`, para no pasar el límite de tamaño de fila de MySQL. Los límites de largo se validan en los DTO.
- `Consultas`, `Turnos`, `Internaciones` y `SolicitudesAsistencia` tienen `Version` como control de concurrencia.
- `Microchip` es único y puede quedar vacío (`NULL`) (RN-41).
- Los índices `(ProfesionalId, Inicio, Fin)`, `(AnimalId, Inicio, Fin)` y `(RecursoId, Inicio, Fin)` sirven para controlar los solapamientos con `SELECT … FOR UPDATE` (DT-BD-02).
- Las fechas sin hora se guardan como `DATETIME` en C# con columna `DATE`, y las horas como `TimeSpan` (`TIME`). En Blazor son `DateOnly` y `TimeOnly`, y la conversión se hace en el Service. Así no dependemos de cómo trate el proveedor de Oracle a `DateOnly`/`TimeOnly`.

## Cuentas y sesiones

- **Token de acceso** (JWT, 15 minutos) en el encabezado `Authorization: Bearer …`. Lleva `sub` (id de la cuenta), `name`, `email` y un `role` por cada rol. **Token de refresco** (7 días), guardado solo como hash en `SesionesRefresco`; cada uno sirve una vez, y si un token ya usado vuelve a aparecer se cierran todas las sesiones de la cuenta.
- Contraseñas con PBKDF2-SHA256 (el mismo formato que Blazor). Códigos de 6 dígitos: valen 15 minutos, admiten 5 intentos y se pueden reenviar hasta 3 veces por hora. Se guarda solo el hash, con sal.
- Bloqueo progresivo: desde el quinto ingreso fallido, 1, 2, 4… minutos, hasta 30 (RNF-SEG-09). Además, hay un límite de 20 pedidos por minuto por dirección de origen en `cuentas` y `sesiones`; pasado ese límite responde `429`.
- Una cuenta sin verificar no puede ingresar. Una cuenta sin verificar de más de 7 días se borra cuando alguien se registra con el mismo correo.
- **Correo:** todavía no se envían correos (`EnvioCorreoDesarrollo`). En **Development** el código vuelve en `codigoDesarrollo`; fuera de Development no aparece. Para el envío real se reemplaza `IEnvioCorreo` en `Program.cs`.
- La clave `Jwt:Clave` de `appsettings.json` es **solo de desarrollo**. En producción se reemplaza por una secreta.
- Pendiente: segundo factor del Administrador (RNF-SEG-04) y borrado automático de las cuentas sin verificar.
- Para probar: `Veterinaria.Api.http`, que se abre en Visual Studio y tiene un botón «Enviar solicitud» en cada pedido.

## Turnos

- Mismas reglas que `AgendaService` de Blazor: horarios cada 15 min, 2 h de antelación, 60 días de horizonte, cancelación hasta 24 h antes, ausencia pasados 15 min y 3 ausencias en 12 meses para derivar a Recepción (P-RC-02). Se leen de `Parametros` y se pueden cambiar.
- Un turno ocupa toda su duración; ni el profesional ni el animal pueden tener dos turnos a la vez. `CalculoAgenda` hace el cálculo sin tocar la base.
- **Concurrencia (DT-BD-02):** reservar, reprogramar y el mostrador abren una transacción `READ COMMITTED`, bloquean con `SELECT … FOR UPDATE` la fila del profesional y la del animal (siempre en ese orden), revalidan el horario y guardan. Ante un interbloqueo reintentan hasta 3 veces (50, 100 y 200 ms). Los consultorios todavía no se bloquean: falta definir qué consultorio usa cada servicio (Diego, 2026-11-21).
- Cambio simultáneo de un mismo registro: el segundo responde `409` (`CONFLICTO_DE_VERSION`, `Controllers/ManejadorConcurrencia.cs`).
- Las cirugías (servicios que piden consentimiento) se reservan *Pendiente de seña* (RN-35) y se confirman al pagar la seña en Recepción.
- Hora: la local del servidor, que es la de la clínica.

## Consulta clínica

- Mismas reglas y mensajes que `ConsultaService` y `VacunaService` de Blazor. Solo el autor edita y firma su borrador, y únicamente mientras el turno está En curso.
- **Firma atómica (FLUJO §8.3):** una sola transacción, con la consulta y los artículos bloqueados en orden (`Data/Transacciones.cs`). Si algo falla antes de guardar, todo queda en borrador.
- El cargo usa la tarifa vigente en la fecha del turno, se redondea a peso entero (P-RC-07) y queda a nombre del cliente que reservó, si sigue siendo responsable. Los insumos no se cobran aparte: los artículos todavía no tienen precio.
- El resumen al cliente queda en `Comunicaciones` como Pendiente, una sola vez por consulta (`Clave = consulta-{id}`). El envío real llega con el servicio de correo.
- Reglas puras, sin base: `ReglasStock` (FEFO, disponible, mínimo) y `ReglasClinicas` (alergias, matrícula, huella, redondeo).

## Carnet, inventario y consentimiento

- Mismas reglas y mensajes que `VacunaService`, `InventarioService` y `ConsentimientoService` de Blazor.
- **Carnet:** cuentan las aplicaciones de consultas firmadas o anteriores al sistema. Los avisos de refuerzo quedan en `Comunicaciones` (tipo 4, `Clave = refuerzo-{aplicación}`) por el canal que eligió el cliente; si eligió «No recibir», no se genera. Pensado para correr una vez por día; mientras no haya tarea programada, lo dispara el Administrador.
- **Inventario:** cada compra, ajuste o baja bloquea la fila del artículo (`Transacciones`) y deja su movimiento con la persona. La baja de vencidos queda a nombre del sistema.
- **Consentimiento:** vale solo la última respuesta del turno y solo si es de la versión vigente; la firma de la consulta lo comprueba igual. Se guarda el nombre escrito, la versión, el canal «En línea», la fecha y la huella SHA-256 del texto.

## Cobro

- Mismas reglas y mensajes que `CobroService` de Blazor. No hay pago en línea: se cobra en Recepción (DT-ALC-02).
- Cobrar y anular bloquean la fila del cargo y el numerador (`Numeradores`, claves `Comprobante` y `NotaCredito`): dos cobros a la vez no pasan el saldo y los números no se repiten ni saltean.
- **Seña (RN-35):** el cargo de seña se genera la primera vez que se listan los cargos del cliente o de Recepción, una sola vez por turno. Es el 30 % de la tarifa vigente en la fecha del turno (o del precio desde). Los parámetros `cobro.porcentaje_sena` y `cobro.limite_deuda` (3000 por defecto) los cambia el Administrador desde `PUT /api/v1/admin/parametros/{clave}`, sin migración.
- Un turno *Pendiente de seña* conserva el horario. Todavía no se cancela solo si no se paga.

## Comunicaciones

- Mismas reglas y mensajes que `ComunicacionesService` de Blazor.
- **Envío:** `IEnvioComunicacion`. La versión de desarrollo (`EnvioComunicacionDesarrollo`) no manda nada y da el envío por hecho, para probar la cola. Para el correo real se cambia esa clase en `Program.cs`.
- **Cola:** lo que va solo a la app queda entregado en la bandeja. `POST recepcion/comunicaciones/cola` también entrega los avisos de refuerzo y los resúmenes clínicos que dejan pendientes los otros módulos. Todavía no hay una tarea que procese la cola sola.

## Asistencia

- Mismas reglas que `AsistenciaService` de Blazor y las decisiones D-ASI-01 a 11 de `docs/analisis/ESPECIFICACION_ALERTAS_ASISTENCIA.md`.
- **Parámetros** (Administrador, `admin/parametros`):
  - `asistencia.cobertura_km` (30) y `asistencia.escalado_rojo_minutos` (10);
  - `asistencia.tarifa_base` (1500), `asistencia.tarifa_km` (40) y `asistencia.tarifa_animal_extra` (300);
  - la guardia queda en `asistencia.guardia_cuenta_id`; por defecto es la cuenta 2, la Dra. Ferreira.
- **Plazos:** se revisan al abrir la cola de Recepción o la lista del veterinario, y con `POST recepcion/asistencias/plazos`. Todavía no hay una tarea programada.
- **Todavía no está:**
  - la distancia la manda el móvil o la carga Recepción: no hay proveedor de mapas;
  - la ubicación en vivo del veterinario y las notificaciones de cada estado;
  - el cuestionario clínico por especie (D-ASI-01).

## Crear la base en tu computadora

1. Tener MySQL 8.4 corriendo (MySQL Workbench o el servicio de Windows).
2. Copiar `appsettings.Development.example.json` como `appsettings.Development.json`, en la misma carpeta, y poner tu contraseña de MySQL. Ese archivo no se sube a GitHub.
3. Abrir `api/Veterinaria.Api/Veterinaria.Api.slnx` en Visual Studio y compilar. NuGet descarga `MySql.EntityFrameworkCore` 10.0.7 y `Microsoft.EntityFrameworkCore.Tools`. Si NuGet avisa de un conflicto de versiones, actualizá `Microsoft.EntityFrameworkCore.Tools` a la última 10.0.x.
4. En *Herramientas → Administrador de paquetes NuGet → Consola*:

   ```powershell
   Add-Migration Inicial
   Update-Database
   ```

5. En MySQL Workbench aparece la base `vitalia`, con las tablas y los datos de la semilla.

Para cambiar el esquema más adelante: modificar `Models/`, `AppDbContext` o `ConfiguracionModulos`, y después `Add-Migration <Nombre>` y `Update-Database`. `ApiBaseDeDatosTests` verifica el modelo en el CI antes de migrar.
