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

## Parte 4: Tiempo real

Rama `diego/sesion-2026-11-22-tiempo-real`, que sale de la de correo. PR #55. No cambia la base.

- `AvisosHub` (SignalR) en `/hubs/avisos`. Cada conexión entra a los grupos de su cuenta y de sus roles.
- **Eventos:**
  - internación: `internacion.cambio` al cliente, solo lo visible, y `internacion.tablero` al personal;
  - asistencia: `asistencia.cambio` al cliente, `asistencia.cola` a Recepción y Administración, y `asistencia.oferta` a los veterinarios.
- **Privacidad:** solo ids, nivel y estado; nunca ubicación ni texto clínico.
- **Archivos:**
  - `Services/Notificador.cs` (`INotificador`, `NotificadorNulo`, `AvisosHub`, `NotificadorSignalR`);
  - `InternacionService.cs` y `AsistenciaService.cs`, con el notificador opcional en el constructor;
  - `Program.cs` (SignalR, el hub y el token por `?access_token=`).
- **Pruebas:** una en `ApiInternacionTests` y otra en `ApiAsistenciaTests`, con `NotificadorDePrueba`. CI en verde.

## Parte 5: Verificación en dos pasos

Rama `diego/sesion-2026-11-22-dos-pasos`, que sale de la de tiempo real. PR #56. **Cambia la base:** migración `SegundoFactor`.

- **Mecanismo:** TOTP (RFC 6238) con lo que trae .NET. El secreto se guarda cifrado y un código no se puede usar dos veces.
- **Cuenta propia:** estado, configuración (clave y enlace para el QR), activación y desactivación (el Administrador no puede desactivarla).
- **Ingreso:** con dos pasos activos se pide el código; un código malo cuenta como intento fallido.
- **Administrador:** sin los dos pasos activos solo usa su cuenta. Se puede apagar en desarrollo con `Seguridad:ExigirSegundoFactorAdministrador = false`.
- **Migración:** la escribió Claude a mano (`20261122190000_SegundoFactor` y su `Designer`, más el snapshot). El CI comprobó con `HasPendingModelChanges` que coincide con el modelo.
- **Archivos:**
  - `Services/SegundoFactor.cs` (`Totp`, `ProteccionSecretos`, `OpcionesSeguridad`, `SegundoFactorService`, el filtro);
  - `Controllers/SegundoFactorController.cs`;
  - `SesionService.cs`, `TokenService.cs` y `DTOs/CuentaDtos.cs`;
  - `Models/Cuenta.cs` y `Migrations/`;
  - `Program.cs` y `appsettings.json`;
  - sección 19 del `.http`;
  - `CONTEXTO_BASE.md` (DT-SEG-02).
- **Pruebas:** `ApiSegundoFactorTests`, 4 nuevas, y 1 en `ApiCuentasTests`. CI en verde con **1177 pruebas**.

## Cómo probar todo

1. En GitHub Desktop: **Fetch origin** y elegí `diego/sesion-2026-11-22-dos-pasos`, que tiene todo lo de hoy.
2. En la Consola del Administrador de paquetes: `Update-Database` (aplica `SegundoFactor`).
3. Si querés probar con el admin sin teléfono: en `appsettings.Development.json` agregá `"Seguridad": { "ExigirSegundoFactorAdministrador": false }`.
4. F5 y `Veterinaria.Api.http`, secciones 11 a 19.

## Pendientes

- [ ] Diego: `Update-Database` y probar con F5.
- [ ] Mezclar los PR #52 a #56 en orden (cada uno sale del anterior).
- [ ] Cuando haya una cuenta de correo: completar la sección `Correo` en `appsettings.Development.json`.
- [ ] Siguiente etapa: conectar Blazor a la API (sección C, `XxxApiService`) y React Native, con el hub de avisos (`@microsoft/signalr`).
- [ ] Para Eric: si la internación cobra por día y qué especies van al box de grandes animales.
