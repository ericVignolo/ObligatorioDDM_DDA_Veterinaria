# Especificación: Comunicaciones y derechos del titular (A-028)

Estado: implementada en escritorio con datos de demostración el 2026-11-16, pendiente de la prueba con F5. La advertencia común de los prototipos está en `CONTEXTO_BASE.md` §3.1.

Requisitos: RF-CLI-07, RF-CLI-10, RF-CLI-18, RF-CLI-19, RF-CLI-20, RF-REC-07, RN-43, RNF-CON-03, RNF-CON-04, RNF-CON-10, RNF-LEG-01, RNF-LEG-02, RNF-LEG-06, RNF-LEG-09.

## Decisiones de este módulo

| Id | Decisión |
|---|---|
| D-COM-01 | En la sección A **no se envía ningún correo real**. Cada mensaje queda en una bandeja de salida en memoria con estado `Pendiente`, `Enviado` o `Fallido`. El envío real es de la API (SMTP o proveedor) y necesita las tablas de cola, preferencias y solicitudes (ver «Para la API»). |
| D-COM-02 | Tipos: **Resumen clínico**, **Comprobante de cobro** y **Aviso de la clínica** son *esenciales* (el cliente elige correo o solo la app, nunca «No recibir»). **Aviso de refuerzo** y **Alerta sanitaria** son *opcionales*. |
| D-COM-03 | Por defecto el aviso de refuerzo llega por **correo** (es un servicio sobre sus propios animales, RF-CLI-07 es Must) y la alerta sanitaria **no se recibe** hasta que el cliente la activa (RN-43: nada opcional premarcado). A confirmar por Eric. |
| D-COM-04 | Los mensajes de internación (RF-CLI-09) y el carnet en PDF (RF-CLI-06) **no** generan correo en esta tarea: las novedades se ven en la app y el PDF con QR no está construido. Se agregan como tipos cuando existan. |
| D-DER-01 | Exportar se atiende en el acto y entrega un JSON con los datos del titular, sus animales, turnos, cargos, consentimientos, comunicaciones y preferencias. **No** incluye el texto de las consultas. |
| D-DER-02 | Eliminar **anonimiza** la cuenta (datos personales por valores neutros, ingreso bloqueado) y conserva animales, historia clínica, cargos y consentimientos sin identificar al titular (RNF-LEG-02). Lo resuelve el Administrador. |
| D-DER-03 | Los plazos de conservación clínica y contable **no se inventan**: figuran «a confirmar» hasta la revisión de normativa previa a producción (RNF-LEG-04, RNF-LEG-08). |

## Comunicaciones (`ComunicacionesService`)

| Id | Regla |
|---|---|
| CM-01 | `Encolar` genera un mensaje solo si la cuenta existe, está verificada y no fue eliminada, el tipo existe, y el cliente no eligió «No recibir» ese tipo. Si no, devuelve `null`. |
| CM-02 | Canal «Correo»: el mensaje guarda el correo de destino al encolar y se intenta enviar en el acto. Canal «Solo en la app»: queda `Enviado` en la cuenta, sin correo ni destino. |
| CM-03 | Un intento sale bien salvo que esté activa la falla simulada. Cada falla suma un intento; con menos de 3 el estado sigue `Pendiente`, al tercero pasa a `Fallido` (RNF-CON-04). |
| CM-04 | `ProcesarCola` hace un intento más sobre cada mensaje `Pendiente` por correo y devuelve cuántos intentó. No toca los `Fallido` ni los enviados. |
| CM-05 | `Reintentar` (Recepción o Administrador activos) solo sobre un mensaje `Fallido`: reinicia los intentos y prueba de nuevo. Queda en auditoría. |
| CM-06 | `Reenviar` (Recepción o Administrador activos, RF-REC-07) crea un mensaje **nuevo** por correo con el mismo contenido, con `ReenvioDeId` del original y el nombre de quien lo pidió. Va por correo aunque el cliente prefiera solo la app (lo pidió). El original no cambia. Queda en auditoría. No se reenvía a una cuenta eliminada o sin verificar. |
| CM-07 | Si un mensaje lleva **clave** y ya existe uno del mismo tipo y cuenta con esa clave (que no sea reenvío), no se genera otro (RNF-CON-10). |
| CM-08 | Al firmar una consulta se genera el **resumen clínico** del responsable del turno (RF-CLI-10), con clave `consulta-<id>`, motivo, diagnóstico, tratamiento, indicaciones y peso. Si el cliente no lo recibe, la firma igual se completa y el mensaje lo avisa. |
| CM-09 | `EnviarComprobante` (Recepción o Administrador) genera el comprobante de un pago con clave `pago-<id>`. Una segunda vez falla: se reenvía desde la bandeja. |
| CM-10 | `EnviarAvisoOperativo` (Recepción o Administrador): título 1–120 y mensaje 1–1000 caracteres; llega a todos los clientes verificados, por el canal que cada uno eligió para ese tipo. |
| CM-11 | `EnviarAlertaSanitaria` (Administrador o Veterinario activos): mismas longitudes; opcionalmente solo clientes con animales de una especie. La reciben únicamente quienes la activaron. El resultado cuenta generados y omitidos por preferencia. |
| CM-12 | `AvisosRefuerzoService.Generar` crea un aviso por cada refuerzo próximo o vencido (el de `VacunaService.AlertasDe`) de cada cuenta verificada, con clave animal + producto + fecha de refuerzo. Llamarlo de nuevo no duplica. Devuelve cuántos generó. |
| CM-13 | `CambiarPreferencia`: el tipo debe existir, el canal ser uno de `Correo`, `Solo en la app` o `No recibir`, la cuenta existir y no estar eliminada; un tipo esencial no admite `No recibir`. Sin elección, rige el canal por defecto del catálogo. |
| CM-14 | El cliente ve solo sus mensajes (`DeCuenta`, más nuevo primero) y solo marca como leídos los suyos (RN-18). |
| CM-15 | La bandeja del personal filtra por estado, tipo y texto (asunto, destino o nombre del cliente). |
| CM-16 | Emitir un aviso o una alerta, reenviar, reintentar y enviar un comprobante dejan registro en la bitácora, entidad «Comunicaciones», sin copiar el cuerpo del mensaje. |

## Derechos del titular (`DerechosService`)

| Id | Regla |
|---|---|
| DR-01 | `Exportar` devuelve un JSON legible con los datos del propio cliente. Nunca incluye el derivado de la contraseña ni datos de otro cliente. |
| DR-02 | `SolicitarExportacion` deja un pedido `Completada` de tipo Exportación. Sin cuenta o con cuenta eliminada falla. |
| DR-03 | `SolicitarEliminacion` exige confirmar de forma explícita y no admite un segundo pedido mientras haya uno `Recibida`. |
| DR-04 | No se pide ni se aprueba la eliminación si el cliente tiene un cargo con saldo, un turno próximo o un animal internado. El mensaje dice cuál. |
| DR-05 | El titular puede **cancelar** su pedido mientras esté `Recibida`; solo el suyo. Después puede pedir de nuevo. |
| DR-06 | Solo el Administrador activo resuelve. **Rechazar** exige motivo y deja la cuenta intacta. **Aprobar** vuelve a controlar DR-04, anonimiza la cuenta (nombre, contacto, documento, domicilio y credenciales vacíos o neutros; ingreso imposible), borra sus mensajes y preferencias, y marca el pedido `Completada`. |
| DR-07 | Tras eliminar, el animal sigue existiendo con su historia y su `ResponsableId`, que ahora apunta a una cuenta anonimizada. Los cargos y consentimientos se conservan. |
| DR-08 | Aprobar o rechazar deja un registro de auditoría, entidad «Datos personales», sin datos personales en el detalle. |
| DR-09 | `Politicas` documenta por categoría finalidad, acceso, conservación y qué pasa al eliminar (RNF-LEG-06). Historia clínica y cobros dicen «a confirmar» (D-DER-03). |

## Aclaraciones surgidas de las pruebas de caja negra (2026-11-16)

| Id | Aclaración |
|---|---|
| AC-01 | **Turno próximo (DR-04):** el que ocupa la agenda (Confirmado, En espera o En curso) y todavía no terminó. Un turno Cancelado, Ausente o Finalizado no es obstáculo, ni el de otro cliente. |
| AC-02 | **Canal por defecto de los esenciales:** Correo. |
| AC-03 | Un mensaje enviado a la primera tiene 1 intento y `UltimoError` vacío. `Reintentar` devuelve éxito solo si el envío sale; si vuelve a fallar, devuelve error y el mensaje queda `Pendiente` con 1 intento. |
| AC-04 | `NoLeidas` cuenta todos los mensajes del cliente sin leer, por correo o en la app. |
| AC-05 | Orden de la bandeja: del `Id` más alto al más bajo (el más nuevo primero). |
| AC-06 | Los filtros de texto no distinguen mayúsculas de minúsculas (las tildes sí cuentan); estado y tipo se comparan exactos. |
| AC-07 | Título y mensaje se recortan (espacios al inicio y al final) antes de medir el largo; uno de solo espacios es vacío y falla. El motivo de rechazo de solo espacios también falla. |
| AC-08 | Un aviso o una alerta sin destinatarios (ningún cliente con esa especie, nadie verificado) sale bien con 0 generados. |
| AC-09 | Valores neutros al anonimizar: nombre «Titular», apellido «eliminado», correo `eliminada-<id>@eliminada.invalid`, el resto vacío, verificada en falso. |
| AC-10 | Se puede pedir la exportación todas las veces que se quiera. `Resolver` solo vale para pedidos de eliminación: sobre una exportación devuelve error. |
| AC-11 | `Bitacora.Buscar` devuelve una lista vacía si quien consulta no es un Administrador activo. |
| AC-12 | `Exportar` y los pedidos no controlan si la cuenta está verificada; sí que no esté eliminada (los pedidos). |

## Pantallas

| Ruta | Quién | Qué |
|---|---|---|
| `/panel/comunicaciones` | Recepción, Administrador | Bandeja de salida con filtros, ver contenido, reenviar, reintentar fallidos, procesar cola, generar avisos de refuerzo, simular falla, aviso de la clínica; alerta sanitaria solo Administrador. |
| `/panel/admin/derechos` | Administrador | Pedidos con aprobar o rechazar y política de conservación. |
| Mi cuenta → «Mis comunicaciones» | Cliente | Canal por tipo y bandeja propia con «Leer». |
| Mi cuenta → «Mis datos personales» | Cliente | Descargar mis datos, pedir eliminación, mis pedidos, qué se guarda. |
| Comprobante → «Enviar al cliente» | Recepción, Administrador | RF-REC-07. |

## Para la API (sección C)

- Tablas: `Comunicacion` (cola con estado, intentos, próximo intento, clave única por cuenta, tipo y clave), `PreferenciaComunicacion`, `SolicitudDerecho`.
- El envío es un proceso aparte con reintentos y espera creciente; se encola dentro de la transacción que cierra la consulta y se envía fuera de ella (RNF-CON-03, RNF-CON-04).
- Proveedor de correo y credenciales: **decisión pendiente** antes de integrar.
- Los avisos de refuerzo los genera un proceso diario; la exportación puede ser un archivo firmado con vencimiento.
- La anonimización es una transacción con auditoría que respeta las claves foráneas de animales, cargos y consentimientos.

## Límites de la demostración

Sin correo real, sin cola persistente, sin proceso diario automático, sin cálculo de plazos legales, sin control de asistencias a domicilio abiertas en DR-04 (llega con la API) y sin plantillas editables (RF-ADM-09, Could).
