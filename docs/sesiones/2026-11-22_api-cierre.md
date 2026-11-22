# Sesión 2026-11-22 — Cierre de la API (Diego, con Claude)

## Antes de empezar

Eric mezcló los PR #39 a #51 en `desarrollo` y en `main` (21/11). Esta sesión sale de `desarrollo`.

Plan aprobado por Diego, con cada parte en su rama encadenada y su PR:
1. derechos del titular;
2. tareas automáticas;
3. correo real;
4. tiempo real;
5. verificación en dos pasos.

Decisiones de Diego:
- **dos pasos con app autenticadora** (TOTP);
- **correo real** listo para configurar, pero sin datos cargados: mientras tanto sigue el envío de prueba.

## Parte 1: Derechos del titular

Rama `diego/sesion-2026-11-22-derechos`. PR #52. No cambia la base.

- **Cliente:**
  - ve las políticas de conservación;
  - descarga sus datos en JSON, y el pedido queda registrado;
  - ve qué falta resolver para eliminar la cuenta;
  - pide la eliminación (con confirmación) y la puede cancelar.
- **Administrador:**
  - aprobar **anonimiza** la cuenta: datos personales neutros, se borran comunicaciones, preferencias y códigos, y se cierran las sesiones. Animales, historia clínica, cargos y consentimientos se conservan;
  - rechazar pide motivo;
  - antes de aprobar se vuelven a revisar los pendientes.
- **Archivos:**
  - `Services/IDerechosService.cs` y `DerechosService.cs`;
  - `Repositories/IDerechosRepository.cs` y `DerechosRepository.cs`;
  - `Controllers/DerechosController.cs`;
  - `DTOs/DerechosDtos.cs`;
  - `Program.cs`;
  - sección 18 del `.http`.
- **Pruebas:** `ApiDerechosTests`, 5 nuevas. CI en verde.

## Parte 2: Tareas automáticas

Rama `diego/sesion-2026-11-22-tareas`, que sale de la de derechos. PR #53. No cambia la base.

- `ServicioTareas`, un `BackgroundService` de .NET (no se instala nada):
  - **cada minuto:** la cola de comunicaciones y los plazos de asistencia;
  - **una vez por día**, desde las 6: avisos de refuerzo, lotes vencidos y cuentas sin verificar de más de 7 días.
- Se configura en `appsettings.json` → `TareasProgramadas`.
- **Archivos:**
  - `Services/TareasProgramadas.cs` (`OpcionesTareas`, `PlanTareas`, `ServicioTareas`);
  - `CuentaService` y `CuentaRepository` (borrado de cuentas sin verificar);
  - `Program.cs` y `appsettings.json`.
- **Pruebas:** `ApiTareasTests`, 3 nuevas, y 1 en `ApiCuentasTests`. CI en verde.
- **Para Diego:** si la base está apagada cuando levantás la API, en la consola va a aparecer cada minuto un aviso de que la tarea falló. Es normal: se reintenta sola.

## Parte 3: Correo real

Rama `diego/sesion-2026-11-22-correo`, que sale de la de tareas. PR #54. No cambia la base.

- `CorreoSmtp` (`System.Net.Mail`, viene con .NET) manda los códigos y la cola de Comunicaciones.
- Se activa solo si `appsettings` tiene `Correo:Servidor` y `Correo:Remitente`. Si no, sigue el envío de prueba.
- **Para Diego:** cuando tengan una cuenta para mandar, completen la sección `Correo` en `appsettings.Development.json`. Los pasos para Gmail están en `api/README.md`. La contraseña no va en GitHub.
- **Archivos:** `Services/EnvioCorreo.cs` (`OpcionesCorreo`, `CorreoSmtp`), `Program.cs`, `appsettings.json` y `appsettings.Development.example.json`.
- **Pruebas:** `ApiCorreoTests`, 3 nuevas. CI en verde.
