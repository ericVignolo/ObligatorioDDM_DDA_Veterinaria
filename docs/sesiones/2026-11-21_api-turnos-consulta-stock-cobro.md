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

### Pendientes

- [ ] Diego: probar los módulos 1 a 4 con el `.http`.
- [ ] Al final: mezclar los PR #39 a #47 en orden y ordenar las ramas.
- [ ] Siguientes módulos: internación, asistencia, comunicaciones (envío real), administración (tarifas, parámetros, personal), 2FA.
