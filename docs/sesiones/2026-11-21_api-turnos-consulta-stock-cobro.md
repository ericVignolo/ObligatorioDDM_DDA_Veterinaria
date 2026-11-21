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
