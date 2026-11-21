# Sesión 2026-11-21 — API: turnos, consulta, vacunas y stock, cobro (Diego)

Objetivo de Diego: llevar la API del ~30 % al ~70 % sin errores. Plan aprobado: cuatro módulos, cada uno en su rama encadenada y su PR, con el CI en verde antes de probar.

## Al empezar

- La migración `Sesiones` ya estaba en GitHub (rama de cuentas). Se pasó a la rama de animales con un merge.

## Decisiones (Diego, 2026-11-21)

| Tema | Decisión |
|---|---|
| Consultorios | Se bloquean profesional y animal; los consultorios se agregan después, con su migración. |
| Seña de la castración (RN-35) | Se hace con el módulo de cobro; hasta entonces el turno queda Confirmado. |
| Doble envío | Se frena por el estado (turno ya tomado, consulta ya firmada), sin tabla de idempotencia. |
| Orden | Turnos → Consulta → Vacunas, stock y consentimiento → Cobro. |

## Módulo 1: Turnos

Rama `diego/sesion-2026-11-21-turnos`, que sale de la de animales. PR #44. No cambia la base.

- **Cliente:** disponibilidad (con el próximo día con horarios y el aviso de RN-08), reservar, mis turnos (próximos e historial), detalle, cancelar hasta 24 h antes y reprogramar.
- **Recepción y Administrador:** buscar clientes con sus animales, disponibilidad, agenda del día, turno por mostrador y sobreturno con motivo, auditado. **Solo Recepción:** llegada, ausencia y cancelación.
- **Veterinario:** su agenda del día.
- **Concurrencia:** transacción `READ COMMITTED` con `SELECT … FOR UPDATE` del profesional y después del animal, y 3 reintentos (50/100/200 ms). Si el cambio choca con la versión de otro, responde 409 en toda la API (`ManejadorConcurrencia`).
- **Archivos:**
  - `Services/CalculoAgenda.cs` (con `ReglasAgenda`), `ITurnoService.cs` y `TurnoService.cs`;
  - `Repositories/ITurnoRepository.cs` y `TurnoRepository.cs`;
  - `Controllers/TurnosController.cs`, `RecepcionController.cs`, `VeterinarioController.cs`, `UsuarioActual.cs` y `ManejadorConcurrencia.cs`;
  - `DTOs/TurnoDtos.cs`;
  - `Program.cs`;
  - pedidos en `Veterinaria.Api.http` (secciones 8 a 10).
- **Pruebas:**
  - `ApiTurnosTests`: 26 pruebas nuevas.
  - `ApiBaseDeDatosTests`: nueva «migraciones al día con el modelo».
  - CI en verde con 1071 pruebas.

### Cómo probar los turnos

F5 en la rama `diego/sesion-2026-11-21-turnos`. En `Veterinaria.Api.http`:
1. Sección 7: ingreso de Ana y alta de Luna.
2. Sección 8: disponibilidad de un día hábil (cambiar la fecha), reservar con un horario de la lista, mis turnos, reprogramar y cancelar.
3. Sección 9: ingreso de Recepción, buscar clientes, agenda del día y sobreturno. Poner en `clienteId` el id de Ana, que sale en `cuentas/yo`.
4. Sección 10: agenda de Ferreira.

### Pendientes de turnos

- [ ] Diego: probar con el `.http`.
- [ ] Bloquear consultorios (falta definir qué consultorio usa cada servicio).
- [ ] Seña RN-35 (módulo 4).

## Módulo 2: Consulta clínica

Rama `diego/sesion-2026-11-21-consulta`, que sale de la de turnos. PR #45. No cambia la base.

- **Veterinario:**
  - iniciar o retomar la consulta del turno En espera (con consentimiento para las cirugías, RN-10);
  - guardar el borrador con versión;
  - insumos y vacunas apartados por FEFO;
  - firma con la contraseña en una sola transacción: descuenta stock, deja movimientos y alerta de mínimo, genera el cargo, finaliza el turno y encola el resumen;
  - cancelar sin atención, enmiendas y consultas abiertas;
  - historia con la lectura registrada (y motivo si no hay caso activo).
- **Cliente:** historia de su animal.
- **Archivos:**
  - `Data/Transacciones.cs`: transacción con bloqueo y reintentos, ahora compartida con turnos;
  - `Services/ReglasStock.cs`, `ReglasClinicas.cs`, `IConsultaService.cs` y `ConsultaService.cs`;
  - `Repositories/IConsultaRepository.cs` y `ConsultaRepository.cs`;
  - `DTOs/ConsultaDtos.cs`;
  - endpoints en `VeterinarioController` y `AnimalesController`;
  - `Program.cs`;
  - pedidos en `Veterinaria.Api.http` (sección 11).
- **Pruebas:** `ApiConsultaTests`, 22 nuevas. CI en verde con 1093 pruebas.
- **Simplificaciones** (quedan anotadas):
  - los insumos no se cobran aparte, porque los artículos no tienen precio;
  - las cantidades de stock son enteras (P-RC-03 prevé decimales);
  - las reservas de borradores abandonados no se liberan solas a las 24 h (necesita una tarea programada).

### Cómo probar la consulta

En la rama `diego/sesion-2026-11-21-consulta`, F5. Hace falta un turno de Luna con Ferreira **para hoy**: reservalo desde Recepción con sobreturno si ya pasaron los horarios. Después, en `Veterinaria.Api.http`:
1. Registrar la llegada (sección 9).
2. Sección 11: iniciar, guardar, insumo, vacuna, firmar (con la última `version`), enmienda e historia.
3. En Workbench:
   - `SELECT * FROM Cargos;` → un cargo de 1200;
   - `SELECT * FROM MovimientosStock WHERE Tipo='Consumo';` → dos movimientos;
   - `SELECT * FROM Comunicaciones;` → el resumen Pendiente.

## Módulo 3: Carnet, inventario y consentimiento

Rama `diego/sesion-2026-11-21-salud`, que sale de la de consulta. PR #46. No cambia la base.

- **Cliente:** carnet de cada animal (Vigente, Refuerzo próximo o Vencida), alertas de refuerzo, consentimientos pendientes, firmar o rechazar el consentimiento de un turno.
- **Veterinario:** insumos disponibles y productos sanitarios de la especie con el lote que se usaría (FEFO).
- **Administrador:**
  - inventario: artículos, lotes, movimientos, compras, ajustes y bajas con motivo, baja de vencidos, alertas de mínimo;
  - avisos de refuerzo (se pueden disparar varias veces sin duplicar);
  - versiones del texto del consentimiento.
- **Cambio en la consulta:** la firma exige ahora que el consentimiento sea de la versión **vigente** del documento.
- **Archivos:**
  - `Services/CarnetService.cs`, `InventarioService.cs`, `ConsentimientoService.cs` y sus interfaces;
  - `Repositories/ISaludRepository.cs` y `SaludRepository.cs`; `ConsultaRepository.cs` (consentimiento vigente);
  - `Controllers/SaludController.cs`, `StockVeterinarioController.cs`, `InventarioController.cs` y `AdminSaludController.cs`;
  - `DTOs/SaludDtos.cs`; `Program.cs`; pedidos en `Veterinaria.Api.http` (sección 12).
- **Pruebas:** `ApiSaludTests`, 11 nuevas. CI en verde.
- **Simplificación:** los avisos de refuerzo los dispara el Administrador; la tarea diaria automática queda para después.

### Cómo probar el módulo 3

En la rama `diego/sesion-2026-11-21-salud`, F5. En `Veterinaria.Api.http`:
1. Ingreso de Ana (sección 1) y de admin con «ingresoPersonal».
2. Sección 12: carnet y alertas de Luna (aparece la vacuna firmada en la sección 11).
3. Reservar una castración (servicio 8) en la sección 8, ver los consentimientos pendientes y firmarlo con «Ana Pérez» (o el nombre de tu cuenta). Un nombre distinto da 400.
4. Inventario: artículos, lotes del 8, compra, baja, movimientos y alertas.

## Módulo 4: Cobro

Rama `diego/sesion-2026-11-21-cobro`, que sale de la de salud. PR #47. No cambia la base.

- **Cliente:** sus cargos con saldo y estado.
- **Recepción:** cargos con saldo, cobro con comprobante correlativo (`C-000001`), comprobantes del día, resumen de caja y aviso de deuda vencida (RN-36). Solo Recepción cobra.
- **Administrador:** anula un comprobante con nota de crédito (`NC-000001`), motivo y auditoría (RN-34).
- **Seña (RN-35):**
  - las cirugías se reservan *Pendiente de seña*;
  - el cargo de seña (30 %) se genera una vez;
  - pagarla confirma el turno y anularla lo vuelve atrás;
  - al firmar la consulta se descuenta lo pagado.
- **Archivos:**
  - `Services/ICobroService.cs` y `CobroService.cs`;
  - `Repositories/ICobroRepository.cs` y `CobroRepository.cs`;
  - `Controllers/CobrosController.cs`, con `CobrosController`, `CajaController` y `AdminCobrosController`;
  - `DTOs/CobroDtos.cs`;
  - `TurnoService.cs` (estado inicial), `ConsultaService.cs` y `ConsultaRepository.cs` (descuento de la seña);
  - `Program.cs`;
  - sección 13 del `.http`.
- **Pruebas:** `ApiCobroTests`, 8 nuevas, y 1 en `ApiConsultaTests`. CI en verde con 1113 pruebas.
- **Simplificaciones:**
  - la seña se genera al listar los cargos, sin tarea programada;
  - un turno sin seña no se cancela solo;
  - los parámetros de seña y deuda usan valores por defecto hasta que se carguen en `Parametros`.

### Cómo probar el cobro

En la rama `diego/sesion-2026-11-21-cobro`, F5. Hace falta haber firmado una consulta (sección 11), que deja un cargo.
1. Sección 13: cargos con saldo, cobrar 500, cobrar el resto y ver los comprobantes.
2. Con el admin: anular un comprobante y ver que el cargo vuelve a tener saldo.
3. Castración: reservarla como Ana (servicio 8) → queda *Pendiente de seña*. En los cargos aparece la seña: cobrarla confirma el turno.

## Prueba de Diego (2026-11-21, mediodía)

Diego probó en la rama de cobro: ingreso de Ana, alta de Luna, disponibilidad, reserva, el segundo intento en el mismo horario (rechazado), mis turnos, el ingreso de Recepción, la búsqueda de clientes y la agenda. Todo anduvo. Vio que la reserva nueva traía el cliente vacío: se corrigió en el módulo 5. La consulta queda para un día hábil, porque los sábados no atiende nadie que atienda perros. Diego decidió no probar todo y seguir en el orden Administración → Internación → Comunicaciones → Asistencia.

## Módulo 5: Administración

Rama `diego/sesion-2026-11-21-admin`, que sale de la de cobro. PR #48. No cambia la base.

- **Servicios y tarifas:**
  - crear un servicio, con su tarifa inicial;
  - editarlo, sin cambiar el precio;
  - programar tarifas desde hoy o una fecha futura (RN-33).
- **Personal:**
  - alta con contraseña inicial;
  - cambio de rol y bloqueo, que cierran las sesiones de la cuenta;
  - nadie se cambia ni se bloquea a sí mismo y siempre queda un Administrador activo;
  - un veterinario necesita una ficha de profesional libre.
- **Ausencias y feriados:** no se cargan sobre turnos activos.
- **Parámetros:**
  - cada uno tiene su rango;
  - los de la seña y la deuda se crean al cambiarlos, sin migración;
  - los que la API todavía no usa (registro y asistencia) se muestran, pero no se pueden cambiar.
- **Otros:** stock mínimo y auditoría paginada.
- **Archivos:**
  - `Services/IAdministracionService.cs` y `AdministracionService.cs`;
  - `Repositories/IAdministracionRepository.cs` y `AdministracionRepository.cs`;
  - `Controllers/AdministracionController.cs`;
  - `DTOs/AdminDtos.cs`;
  - `TurnoService.cs` (nombre del cliente en la respuesta) y `Program.cs`;
  - sección 14 del `.http`.
- **Pruebas:** `ApiAdministracionTests`, 8 nuevas. CI en verde.
- **Falta:** los boxes van con internación. La guardia del veterinario va con asistencia.

## Módulo 6: Internación

Rama `diego/sesion-2026-11-21-internacion`, que sale de la de administración. PR #49. No cambia la base.

- **Recepción:**
  - admite (RN-21: una internación abierta por animal; un grupo no se interna);
  - anula antes del ingreso.
- **Veterinario:**
  - ingreso clínico con un box libre, en servicio y del tipo de la especie (RN-22), más el plan;
  - novedades visibles o internas (RN-23) y rectificaciones que conservan la original (RN-24);
  - alta con evolución e indicaciones (RN-25).
- **Cliente:** ve las internaciones de sus animales, solo con lo visible.
- **Administrador:** agrega boxes y los saca de servicio, si no están ocupados.
- **Concurrencia:** la admisión bloquea la fila del animal y el ingreso la del box (`Transacciones` admite ahora `Boxes`).
- **Archivos:**
  - `Services/IInternacionService.cs` y `InternacionService.cs`;
  - `Repositories/IInternacionRepository.cs` y `InternacionRepository.cs`;
  - `Controllers/InternacionesController.cs`, con `InternacionesController`, `MisInternacionesController` y `BoxesController`;
  - `DTOs/InternacionDtos.cs`;
  - `Data/Transacciones.cs` y `Program.cs`;
  - sección 15 del `.http`.
- **Pruebas:** `ApiInternacionTests`, 7 nuevas. CI en verde.
- **Igual que Blazor:**
  - la internación no genera cargo, porque no tiene tarifa;
  - van a box de grandes animales solo caballos y vacas;
  - las novedades se ven al consultar: el tiempo real (RF-CLI-09) llega con comunicaciones.

## Módulo 7: Comunicaciones

Rama `diego/sesion-2026-11-21-comunicaciones`, que sale de la de internación. PR #50. No cambia la base.

- **Cliente:**
  - bandeja con no leídas y marcar un mensaje como leído;
  - preferencias por tipo (los tipos esenciales no se apagan).
- **Recepción y Administración:**
  - bandeja de salida con filtros;
  - cola con 3 intentos por correo y después *Fallido*;
  - reintento de un fallido y reenvío como copia nueva;
  - envío de un comprobante, una sola vez;
  - aviso de la clínica a todos los clientes.
- **Administrador y Veterinario:** alerta sanitaria por especie.
- **Envío:** `IEnvioComunicacion` con una versión de desarrollo que no manda nada. La cola también entrega lo que dejan pendiente el carnet y la consulta.
- **Archivos:**
  - `Services/IComunicacionService.cs` y `ComunicacionService.cs`;
  - `Services/EnvioCorreo.cs` (`IEnvioComunicacion`);
  - `Repositories/IComunicacionRepository.cs` y `ComunicacionRepository.cs`;
  - `Controllers/ComunicacionesController.cs`, con tres controladores;
  - `DTOs/ComunicacionDtos.cs`;
  - `Program.cs`;
  - sección 16 del `.http`.
- **Pruebas:** `ApiComunicacionesTests`, 8 nuevas. CI en verde.
- **Falta:**
  - el correo real (SMTP o un proveedor);
  - una tarea que procese la cola sola;
  - el aviso en tiempo real (RF-CLI-09).

## Módulo 8: Asistencia

Rama `diego/sesion-2026-11-21-asistencia`, que sale de la de comunicaciones. PR #51. No cambia la base.

- Igual a `AsistenciaService` de Blazor y a las decisiones D-ASI-01 a 11 de la especificación.
- **Cliente:**
  - crea una solicitud desde la app, con GPS consentido o dirección, y sin duplicar si repite la clave;
  - sigue la solicitud e informa empeoramiento, que solo sube el nivel.
- **Recepción:**
  - cola por nivel y antigüedad;
  - solicitudes por teléfono, elegibles, asignación, empeoramiento por teléfono y cierre (un rojo, solo tras escalar);
  - revisión de los plazos de 10 minutos y 24 horas.
- **Veterinario:** acepta (el primero se la queda) o rechaza, avanza el traslado, finaliza con cargo y cambia el nivel con motivo.
- **Administrador:** la guardia y los parámetros de asistencia, que ahora se editan desde `admin/parametros`.
- **Archivos:**
  - `Services/IAsistenciaService.cs` y `AsistenciaService.cs`;
  - `Repositories/IAsistenciaRepository.cs` y `AsistenciaRepository.cs`;
  - `Controllers/AsistenciaController.cs`, con cuatro controladores;
  - `DTOs/AsistenciaDtos.cs`;
  - `AdministracionService.cs` (catálogo de parámetros) y `Program.cs`;
  - sección 17 del `.http`.
- **Pruebas:** `ApiAsistenciaTests`, 14 nuevas. CI en verde con **1150 pruebas**, sin advertencias.
- **Falta:** el proveedor de mapas (distancia y ruta), la ubicación en vivo del veterinario, las notificaciones de cada estado, el cuestionario clínico y una tarea programada para los plazos.
- **Corrección de ramas:** el commit del módulo se hizo primero en la rama de comunicaciones, sin subirlo. Se movió a la rama nueva y la de comunicaciones quedó igual que en GitHub.

## Cierre de la sesión

Hoy salieron los módulos 1 a 8 (PR #44 a #51), todos con el CI en verde. La API cubre todos los recorridos de Blazor: catálogo, cuentas, animales, turnos, consulta, carnet, inventario, consentimiento, cobro, administración, internación, comunicaciones y asistencia.

### Pendientes

- [ ] Al final: mezclar los PR #39 a #51 en orden (cada uno sale del anterior) y ordenar las ramas.
- [ ] Diego: probar con F5 la consulta (un día hábil, con Ferreira) y los módulos 3 a 8 con el `.http`.
- [ ] Falta para cerrar la API:
  - verificación en dos pasos (2FA);
  - correo real (SMTP o un proveedor);
  - tareas programadas (cola de correo, avisos de refuerzo, plazos de asistencia, vencidos de stock);
  - tiempo real (internación y asistencia);
  - derechos del titular (DerechosService de Blazor);
  - borrado de cuentas sin verificar.
- [ ] Después: conectar Blazor a la API (sección C, `XxxApiService`) y React Native.
- [ ] Definir con Eric si la internación cobra (tarifa por día) y qué especies van a box de grandes animales.
- [ ] Definir con Eric si la internación cobra (tarifa por día) y qué especies van a box de grandes animales.
