# Especificación: Firmar consentimiento informado (CU-CLI-10)

Estado: implementada en escritorio con datos de demostración (A-025, 2026-11-16). Probada por Eric con F5 el 2026-11-16. La advertencia común de los prototipos está en `CONTEXTO_BASE.md` §3.1.

Requisitos: RF-CLI-11 (Must), RN-10, RN-18, RN-43, CU-CLI-10, HU-CLI-11, A-RC-13, CA-RC-17. Mecanismo de firma ya decidido en P-RC-06 (`FLUJO_RESERVA_CONSULTA.md`): el responsable marca la aceptación y escribe su nombre completo; se guarda versión, procedimiento, animal, fecha, canal y hash. **No** es una firma electrónica avanzada.

## Clasificación

| Tipo | Elemento |
|---|---|
| Consentimiento/alerta | Consentimiento clínico por procedimiento y versión, separado de Términos y Privacidad (RN-43) |
| Requerimiento funcional | RF-CLI-11 |
| Regla de negocio | RN-10 y las reglas K-xx de abajo |
| Diseño/UX | `/consentimiento/{TurnoId}`, aviso en `/cuenta`, botón en `/turnos`, estado en las agendas del personal |

## Qué procedimientos lo piden

Las prestaciones con `RequiereConsentimiento = true`. En la demostración: **Castración** (prestación 8, $ 4.500, 90 min, la atiende la Dra. Ferreira, perros y gatos). Hay un turno de demostración de Mishi (gata de Ana Rodríguez, cliente 100) hoy a las 12:00, si es día hábil.

Documento vigente al arrancar: «Consentimiento informado para castración con anestesia general», versión `1.0`.

## Estados del consentimiento de un turno

`No requiere` · `Pendiente` (sin respuesta) · `Firmado` (la última respuesta es una aceptación de la versión vigente) · `Rechazado` (la última respuesta es un rechazo) · `Versión nueva` (se aceptó una versión que ya no está vigente).

## Reglas

| Id | Regla |
|---|---|
| K-01 | Solo el cliente responsable del turno responde; para otro cliente el turno «no existe» (RN-18). |
| K-02 | Solo se responde con el turno `Confirmado` o `En espera`. |
| K-03 | Para firmar: casilla marcada (nunca viene marcada) y nombre completo igual al de la cuenta («Nombre Apellido»), sin importar mayúsculas ni espacios de más. |
| K-04 | La firma guarda turno, cliente, animal, prestación, documento y versión, respuesta, nombre escrito, canal «En línea», fecha y una huella SHA-256 (64 caracteres hexadecimales). |
| K-05 | Ninguna respuesta se modifica ni se borra; firmar después de rechazar agrega un registro nuevo. No se firma dos veces lo ya firmado ni se rechaza dos veces seguidas. |
| K-06 | **RN-10:** una consulta de un turno que pide consentimiento no se inicia (no pasa a `En curso`) si el consentimiento no está `Firmado`. Con el consentimiento firmado se inicia normalmente. |
| K-07 | Si el Administrador publica una versión nueva del documento, lo firmado con la anterior deja de valer y hay que volver a firmar. Solo el Administrador publica; la versión tiene que ser distinta de la vigente. |
| K-08 | `PendientesDe(cliente)` lista los turnos próximos del cliente que piden consentimiento y no lo tienen `Firmado`. |
| K-09 | Un turno de una prestación que no lo pide no admite firma ni rechazo. |
| K-10 | El responsable puede **retirar** un consentimiento firmado rechazándolo mientras el turno no empezó (el texto lo permite «avisando antes del procedimiento»); queda como un registro nuevo. |
| K-11 | Si la última respuesta es un rechazo, el estado es «Rechazado» aunque después se publique una versión nueva. |
| K-12 | El nombre se compara contra «Nombre Apellido» de la cuenta, ignorando mayúsculas y espacios de más (también entre palabras). Las tildes cuentan: «Perez» no es «Pérez». Se guarda tal como lo escribió (sin espacios al principio y al final). |
| K-13 | Al publicar una versión: versión y texto obligatorios (sin contar espacios) y distinta de la vigente. |
| K-14 | `PendientesDe` incluye turnos próximos `Confirmado` o `En espera`; no los cancelados ni los ya terminados. Para responder solo cuenta el estado del turno, no la hora. |

## Fuera de esta versión

Firma en el mostrador con canal «Mostrador» (hoy se usa la sesión del cliente, como dice P-RC-06), pantalla de administración de documentos (A-027), seña previa (RN-35) y PDF del documento firmado.

## Pruebas

`tests/Veterinaria.Tests/ConsentimientoTests.cs` (caja blanca) y `tests/Veterinaria.Tests/CajaNegra/ConsentimientoCajaNegraTests.cs` (agente probador). Las reglas K-10 a K-14 salieron de las preguntas del probador.
