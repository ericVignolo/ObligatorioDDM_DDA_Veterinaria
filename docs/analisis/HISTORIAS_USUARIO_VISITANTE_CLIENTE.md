# Historias de usuario — Visitante y Cliente

Versión: 0.2

Fecha: 2026-09-26

Estado: Borrador funcional para validación

Fuente canónica: `../contexto-base/MATRIZ_REQUISITOS.md`

## 1. Propósito y alcance

Este documento transforma los requerimientos funcionales de Visitante y Cliente en historias de usuario verificables. No reemplaza la redacción canónica de los RF, RN o RNF: la complementa con valor esperado y criterios de aceptación.

La primera versión comprende:

- 10 historias de Visitante (`HU-VIS`).
- 25 historias de Cliente (`HU-CLI`).
- criterios transversales para autenticación, privacidad, accesibilidad, seguridad y manejo de errores.

Los detalles pendientes sobre registro, alertas sanitarias y pseudocódigo se señalan expresamente. Las decisiones funcionales confirmadas para alertas de asistencia se desarrollan en `ESPECIFICACION_ALERTAS_ASISTENCIA.md`.

## 2. Convenciones

Formato de historia:

> Como **[actor]**, quiero **[capacidad]**, para **[beneficio]**.

Los criterios se expresan de forma resumida con `Dado / Cuando / Entonces`. La prioridad y la fase se heredan de la matriz canónica.

## 3. Épicas

| ID | Épica | Resultado esperado |
|---|---|---|
| EP-01 | Descubrimiento público | El visitante conoce la clínica, sus servicios, profesionales y vías de contacto sin autenticarse. |
| EP-02 | Acceso y conversión | El visitante puede registrarse o autenticarse y retomar una acción protegida. |
| EP-03 | Gestión de mascotas | El cliente administra mascotas y responsables con acceso autorizado. |
| EP-04 | Agenda | El cliente consulta, reserva, cancela y reprograma turnos. |
| EP-05 | Información clínica | El cliente consulta historia, carnet, archivos, alertas e internación. |
| EP-06 | Consentimientos y comunicaciones | El cliente gestiona consentimientos, avisos y canales de contacto. |
| EP-07 | Servicios complementarios | El cliente accede a pagos, seguimiento, tratamientos y encuestas según el alcance aprobado. |
| EP-08 | Derechos y privacidad | El cliente administra preferencias y solicitudes relativas a sus datos. |
| EP-09 | Alertas y asistencia | El cliente solicita orientación o visita, comunica cambios de gravedad y sigue la asignación y llegada del profesional. |

## 4. Historias de Visitante

### HU-VIS-01 — Consultar servicios

**Historia:** Como visitante, quiero consultar los servicios con su descripción y precio de referencia, para evaluar si la clínica puede atender mi necesidad.

**Criterios de aceptación:**

1. Dado que no inicié sesión, cuando ingreso al catálogo, entonces veo los servicios públicos disponibles.
2. Cuando selecciono un servicio, entonces veo su descripción y precio de referencia o una indicación clara si el precio requiere evaluación.
3. Si no hay servicios disponibles, entonces veo un estado vacío comprensible y una vía de contacto.

**Trazabilidad:** RF-VIS-01; RNF-USA-02, RNF-USA-03, RNF-USA-05. Prioridad: Must. Fase: 2.

### HU-VIS-02 — Consultar profesionales

**Historia:** Como visitante, quiero conocer a los profesionales, sus especialidades y horarios, para identificar quién puede atender a mi mascota.

**Criterios de aceptación:**

1. El listado muestra únicamente profesionales y datos habilitados para publicación.
2. El detalle muestra nombre, especialidad y horarios de atención vigentes.
3. Ninguna afirmación o acreditación se muestra sin respaldo verificable.

**Trazabilidad:** RF-VIS-02; RN-45. Prioridad: Must. Fase: 2.

### HU-VIS-03 — Consultar ubicación y contacto

**Historia:** Como visitante, quiero consultar ubicación, teléfonos y medios de contacto, para poder comunicarme o llegar a la clínica.

**Criterios de aceptación:**

1. La información de contacto puede consultarse sin autenticación.
2. El teléfono y la dirección se presentan en formatos accionables cuando el dispositivo lo permite.
3. Si el mapa externo no está disponible, la dirección textual permanece visible.

**Trazabilidad:** RF-VIS-03; RNF-DIS-04 por criterio de degradación equivalente. Prioridad: Must. Fase: 2.

### HU-VIS-04 — Consultar disponibilidad general

**Historia:** Como visitante, quiero consultar disponibilidad general sin reservar, para saber si existen opciones antes de registrarme.

**Criterios de aceptación:**

1. La consulta no expone nombres de pacientes ni otra información personal.
2. La disponibilidad mostrada es orientativa y no constituye una reserva.
3. Al intentar reservar, se solicita autenticación o registro y se conserva la selección realizada.

**Trazabilidad:** RF-VIS-04, RF-VIS-07; RN-38; RNF-REN-02. Prioridad: Should. Fase: 3.

### HU-VIS-05 — Consultar contenido educativo

**Historia:** Como visitante, quiero consultar información preventiva y señales de alarma, para cuidar mejor a mi mascota y reconocer cuándo pedir ayuda.

**Criterios de aceptación:**

1. El contenido puede filtrarse o identificarse por especie cuando corresponda.
2. Las señales de alarma diferencian claramente información educativa de una indicación médica personalizada.
3. El contenido urgente ofrece acceso visible a los datos de guardia.

**Trazabilidad:** RF-VIS-05, RF-VIS-06. Prioridad: Should. Fase: 2.

### HU-VIS-06 — Acceder a información de urgencias

**Historia:** Como visitante ante una urgencia, quiero ver inmediatamente el teléfono de guardia, la ubicación y las indicaciones, para contactar a la clínica sin demoras.

**Criterios de aceptación:**

1. La sección es pública y accesible directamente desde la navegación principal.
2. El teléfono de guardia puede accionarse desde un dispositivo compatible.
3. La interfaz no obliga a completar formularios ni iniciar sesión antes de mostrar los datos urgentes.

**Trazabilidad:** RF-VIS-06; RNF-USA-02, RNF-USA-04. Prioridad: Must. Fase: 2.

### HU-VIS-07 — Iniciar registro desde una acción protegida

**Historia:** Como visitante, quiero iniciar el registro desde cualquier acción protegida, para crear mi cuenta sin perder lo que estaba intentando hacer.

**Criterios de aceptación:**

1. Cuando intento una acción protegida, se explica por qué necesito una cuenta.
2. Puedo elegir iniciar sesión o registrarme.
3. Después de autenticarme y verificar la cuenta cuando corresponda, el sistema retoma la intención original con los datos no sensibles que puedan conservarse de forma segura.
4. Las aceptaciones obligatorias y opcionales se presentan separadas y sin opciones facultativas premarcadas.

**Trazabilidad:** RF-VIS-07; RN-37, RN-38, RN-43; RNF-LEG-03. Prioridad: Must. Fase: 2. **Pendiente:** completar campos, verificación y alertas con la especificación adicional de registro.

### HU-VIS-08 — Consultar mascota extraviada mediante QR

**Historia:** Como persona que encuentra una mascota, quiero consultar mediante su QR una ficha pública mínima, para colaborar con su regreso sin acceder a datos privados innecesarios.

**Criterios de aceptación:**

1. El QR permite consultar únicamente la información expresamente autorizada para publicación.
2. Un código inválido, revocado o inexistente no expone información privada.
3. La información clínica no pública y la historia de responsables nunca se muestran.

**Trazabilidad:** RF-VIS-08; RNF-LEG-01, RNF-LEG-06. Prioridad: Could. Fase: 4.

### HU-VIS-09 — Enviar consulta de contacto

**Historia:** Como visitante, quiero enviar una consulta a la clínica, para solicitar información sin necesidad de registrarme.

**Criterios de aceptación:**

1. El formulario solicita únicamente los datos necesarios e informa su finalidad.
2. La consulta válida se protege contra abuso automatizado y confirma su recepción sin prometer una respuesta inmediata.
3. Los errores identifican qué debe corregirse sin borrar los datos válidos ingresados.

**Trazabilidad:** RF-VIS-09; RNF-SEG-09, RNF-LEG-06, RNF-USA-02. Prioridad: Should. Fase: 2.

### HU-VIS-10 — Consultar información legal

**Historia:** Como visitante, quiero consultar la identidad del responsable y las políticas vigentes, para conocer quién trata mis datos y bajo qué condiciones.

**Criterios de aceptación:**

1. Privacidad, términos y, cuando corresponda, cookies son accesibles antes y después de iniciar sesión.
2. Cada documento muestra versión o fecha de vigencia.
3. Los formularios que recolectan datos ofrecen acceso a la información legal relevante.

**Trazabilidad:** RF-VIS-10; RN-43, RN-44; RNF-LEG-03. Prioridad: Must. Fase: 2.

## 5. Historias de Cliente

### HU-CLI-01 — Gestionar mascotas

**Historia:** Como cliente, quiero registrar y editar mis mascotas, para mantener actualizados sus datos identificatorios.

**Criterios de aceptación:**

1. Solo una cuenta autenticada y verificada puede registrar o editar mascotas.
2. El sistema valida los datos obligatorios, la foto y la unicidad del microchip cuando exista.
3. Un cliente solo puede editar mascotas sobre las que tenga responsabilidad vigente.

**Trazabilidad:** RF-CLI-01; RN-18, RN-38, RN-41; RNF-SEG-05, RNF-SEG-08. Prioridad: Must. Fase: 2.

### HU-CLI-02 — Agendar turno

**Historia:** Como cliente, quiero agendar un turno para una mascota eligiendo servicio, profesional y horario disponible, para obtener atención en una fecha conveniente.

**Criterios de aceptación:**

1. El recorrido requiere como máximo cuatro pasos desde su inicio.
2. Solo se ofrecen horarios que respetan profesional, recurso, duración, jornada, ausencias, antelación y horizonte configurados.
3. Antes de confirmar se muestra un resumen y la política de cancelación.
4. Si otro usuario ocupa el horario, no se crea una reserva duplicada; se conservan las selecciones compatibles y se muestran horarios actualizados.

**Trazabilidad:** RF-CLI-02; RN-01 a RN-05, RN-07, RN-08, RN-35, RN-36, RN-38; RNF-CON-01, RNF-CON-02, RNF-USA-01. Prioridad: Must. Fase: 3.

### HU-CLI-03 — Cancelar o reprogramar turno

**Historia:** Como cliente, quiero cancelar o reprogramar un turno dentro de la ventana permitida, para adaptar la atención a un cambio de disponibilidad.

**Criterios de aceptación:**

1. El sistema informa las consecuencias antes de confirmar la acción.
2. La cancelación autónoma se permite únicamente dentro de la ventana configurada.
3. La reprogramación vuelve a validar todas las reglas de disponibilidad y no libera el turno anterior hasta completar la operación según el comportamiento transaccional definido.

**Trazabilidad:** RF-CLI-03; RN-01 a RN-07, RN-11. Prioridad: Must. Fase: 3.

### HU-CLI-04 — Consultar turnos

**Historia:** Como cliente, quiero consultar próximos turnos e historial con sus estados, para conocer mis reservas y atenciones anteriores.

**Criterios de aceptación:**

1. Solo se muestran turnos vinculados a mascotas bajo mi responsabilidad.
2. Cada turno muestra su estado, fecha, hora, mascota, servicio y profesional cuando corresponda.
3. Los listados extensos se paginan y permiten distinguir próximos de históricos.

**Trazabilidad:** RF-CLI-04; RN-18; RNF-REN-03. Prioridad: Must. Fase: 3.

### HU-CLI-05 — Consultar historia clínica

**Historia:** Como cliente, quiero consultar en modo lectura la historia clínica de mi mascota en orden cronológico, para comprender sus antecedentes y atenciones.

**Criterios de aceptación:**

1. Solo responsables vigentes de la mascota pueden acceder.
2. Los registros firmados y sus enmiendas se muestran sin permitir su modificación.
3. La historia se presenta cronológicamente y conserva los registros exigidos aunque la cuenta o mascota se desactive.

**Trazabilidad:** RF-CLI-05; RN-12, RN-14, RN-18. Prioridad: Must. Fase: 3.

### HU-CLI-06 — Descargar carnet sanitario

**Historia:** Como cliente, quiero descargar el carnet sanitario en PDF con verificación por QR, para disponer de un documento portable y verificable.

**Criterios de aceptación:**

1. El PDF corresponde a una mascota bajo mi responsabilidad.
2. El QR permite verificar autenticidad sin exponer información adicional no autorizada.
3. Un documento inexistente o no disponible produce un mensaje comprensible.

**Trazabilidad:** RF-CLI-06; RN-18, RN-19; RNF-SEG-05. Prioridad: Should. Fase: 3.

### HU-CLI-07 — Recibir alertas sanitarias

**Historia:** Como cliente, quiero recibir alertas de vacunas y desparasitaciones próximas, para realizar los refuerzos a tiempo.

**Criterios de aceptación:**

1. La fecha de refuerzo se obtiene de la aplicación registrada y no se introduce libremente por el cliente.
2. La alerta identifica mascota, evento y fecha prevista sin incluir información clínica innecesaria en canales externos.
3. El envío respeta las preferencias aplicables y conserva el aviso dentro de la aplicación cuando corresponda.

**Trazabilidad:** RF-CLI-07; RN-19; RF-CLI-18. Prioridad: Must. Fase: 3. **Pendiente:** completar anticipación, repetición, canales y textos con la especificación adicional de alertas.

### HU-CLI-08 — Consultar archivos clínicos

**Historia:** Como cliente, quiero visualizar y descargar adjuntos habilitados por el veterinario, para acceder a estudios y documentos de mi mascota.

**Criterios de aceptación:**

1. Solo se muestran archivos de mascotas bajo mi responsabilidad y visibles para el cliente.
2. La descarga utiliza acceso temporal autorizado y no una URL pública permanente.
3. Se informa tipo, fecha y tamaño; una previsualización no sustituye el archivo original.

**Trazabilidad:** RF-CLI-08; RN-18; RNF-SEG-07, RNF-SEG-08, RNF-REN-04. Prioridad: Must. Fase: 3.

### HU-CLI-09 — Seguir una internación

**Historia:** Como cliente, quiero ver las novedades autorizadas de la internación de mi mascota, para conocer su evolución.

**Criterios de aceptación:**

1. Solo se muestra la internación de una mascota bajo mi responsabilidad.
2. Se ven únicamente novedades marcadas como visibles por el personal.
3. Las novedades publicadas no cambian silenciosamente; las rectificaciones aparecen como nuevas entradas.
4. Si falla la actualización en tiempo real, la información continúa disponible al recargar.

**Trazabilidad:** RF-CLI-09; RN-21, RN-23, RN-24; RNF-DIS-04. Prioridad: Must. Fase: 3.

### HU-CLI-10 — Recibir resumen clínico

**Historia:** Como cliente, quiero recibir por correo el resumen al finalizar una consulta u operación, para conservar las indicaciones dadas por la clínica.

**Criterios de aceptación:**

1. El resumen se genera únicamente después del cierre válido de la atención.
2. Una caída del correo no revierte el cierre clínico; el envío queda pendiente de reintento.
3. El contenido y los destinatarios respetan autorización y minimización de datos.

**Trazabilidad:** RF-CLI-10; RN-20; RNF-CON-03, RNF-CON-04. Prioridad: Must. Fase: 3.

### HU-CLI-11 — Firmar consentimiento informado

**Historia:** Como cliente responsable, quiero leer y firmar el consentimiento informado, para autorizar de manera consciente una cirugía o procedimiento con anestesia.

**Criterios de aceptación:**

1. El documento identifica procedimiento, versión y mascota antes de solicitar la firma.
2. La aceptación clínica se registra separada de términos, marketing y preferencias.
3. Sin consentimiento válido, un turno quirúrgico no puede pasar a En curso.
4. El documento firmado se conserva como evidencia histórica.

**Trazabilidad:** RF-CLI-11; RN-10, RN-43. Prioridad: Must. Fase: 4.

### HU-CLI-12 — Consultar cuenta y pagar

**Historia:** Como cliente, quiero consultar mi cuenta, comprobantes y opciones de pago, para conocer y regularizar mis obligaciones.

**Criterios de aceptación:**

1. Los cargos utilizan la tarifa vigente al momento de la prestación.
2. Los comprobantes emitidos no se modifican; una corrección queda trazada mediante anulación o nota de crédito.
3. Un pago reintentado no debe cobrarse dos veces.

**Trazabilidad:** RF-CLI-12; RN-32 a RN-36; requisitos de idempotencia del análisis complementario. Prioridad: Should. Fase: 4.

### HU-CLI-13 — Gestionar responsables

**Historia:** Como responsable de una mascota, quiero autorizar a otra persona o transferir la titularidad, para compartir o traspasar formalmente su gestión.

**Criterios de aceptación:**

1. Solo una persona con responsabilidad vigente puede iniciar la operación.
2. La transferencia conserva toda la historia clínica.
3. El sistema registra responsables anteriores y nuevos con sus fechas de vigencia.

**Trazabilidad:** RF-CLI-13; RN-18, RN-42. Prioridad: Should. Fase: 2.

### HU-CLI-14 — Registrar peso en casa

**Historia:** Como cliente, quiero registrar el peso de mi mascota en casa y compararlo con mediciones clínicas, para observar su evolución.

**Criterios de aceptación:**

1. Cada medición distingue origen, fecha y unidad.
2. El cliente solo registra mediciones para mascotas bajo su responsabilidad.
3. La gráfica diferencia las mediciones domésticas de las clínicas y permite una alternativa tabular accesible.

**Trazabilidad:** RF-CLI-14; RN-18; RNF-USA-04. Prioridad: Could. Fase: 4.

### HU-CLI-15 — Solicitar renovación de tratamiento

**Historia:** Como cliente de una mascota con tratamiento crónico, quiero solicitar una renovación sin agendar consulta, para evitar traslados cuando una evaluación presencial no sea necesaria.

**Criterios de aceptación:**

1. La solicitud se vincula a mascota, tratamiento y responsable autorizado.
2. La solicitud no equivale a una aprobación ni emite automáticamente una receta.
3. Solo un veterinario habilitado puede resolverla y, si corresponde, emitir la receta.

**Trazabilidad:** RF-CLI-15; RN-13, RN-17, RN-18. Prioridad: Could. Fase: 4.

### HU-CLI-16 — Seguimiento postoperatorio

**Historia:** Como cliente, quiero comunicarme con la clínica y enviar fotos durante el seguimiento postoperatorio, para informar la evolución de mi mascota.

**Criterios de aceptación:**

1. La conversación se vincula a la intervención y a la mascota correcta.
2. Los archivos se validan por tipo, tamaño y contenido y se almacenan de forma protegida.
3. Se informa que el canal no sustituye la atención de urgencia y se ofrece el contacto correspondiente.

**Trazabilidad:** RF-CLI-16; RN-18; RNF-SEG-07, RNF-SEG-08. Prioridad: Should. Fase: 4.

### HU-CLI-17 — Responder encuesta

**Historia:** Como cliente, quiero responder una encuesta posterior a la atención, para comunicar mi experiencia a la clínica.

**Criterios de aceptación:**

1. La encuesta se relaciona con una atención finalizada y evita respuestas duplicadas cuando así se defina.
2. Se informa la finalidad y el carácter opcional de la respuesta.
3. La opinión no se publica como reseña sin una autorización separada y respaldo verificable.

**Trazabilidad:** RF-CLI-17; RN-43, RN-45. Prioridad: Could. Fase: 4.

### HU-CLI-18 — Configurar canales de notificación

**Historia:** Como cliente, quiero elegir el canal para cada tipo de notificación, para recibir avisos de la manera que me resulte útil.

**Criterios de aceptación:**

1. El perfil muestra por separado cada finalidad y canal disponible.
2. Las opciones facultativas no aparecen premarcadas.
3. Un cambio afecta comunicaciones futuras y conserva trazabilidad de la decisión.

**Trazabilidad:** RF-CLI-18; RN-43; RNF-LEG-01. Prioridad: Should. Fase: 3. **Pendiente:** catálogo definitivo de eventos y canales.

### HU-CLI-19 — Solicitar exportación o eliminación

**Historia:** Como cliente, quiero solicitar la exportación o eliminación de mis datos personales, para ejercer mis derechos sobre ellos.

**Criterios de aceptación:**

1. El sistema verifica identidad antes de aceptar la solicitud.
2. La solicitud informa alcance, estado y eventuales datos que deban conservarse por obligación.
3. La eliminación no borra indebidamente la historia clínica; se conserva o disocia según corresponda y toda acción queda auditada.

**Trazabilidad:** RF-CLI-19; RN-14, RN-39; RNF-LEG-01, RNF-LEG-02, RNF-LEG-04. Prioridad: Should. Fase: 4.

### HU-CLI-20 — Gestionar preferencias de privacidad

**Historia:** Como cliente, quiero consultar y modificar preferencias opcionales revocables, para controlar futuros tratamientos de mis datos y comunicaciones.

**Criterios de aceptación:**

1. El estado mostrado coincide con el registrado por la API.
2. Revocar una preferencia detiene el tratamiento futuro asociado sin alterar operaciones legítimas anteriores.
3. Los consentimientos clínicos históricos no se presentan como preferencias revocables.

**Trazabilidad:** RF-CLI-20; RN-43, RN-44; RNF-LEG-01, RNF-LEG-03. Prioridad: Should. Fase: 3.

### HU-CLI-21 — Solicitar orientación o asistencia

**Historia:** Como cliente, quiero solicitar orientación o asistencia veterinaria para uno o varios animales, para recibir una respuesta priorizada según el estado y las condiciones reales del caso.

**Criterios de aceptación:**

1. Puedo solicitar una visita al lugar u orientación para trasladar al animal a la clínica.
2. La solicitud admite un animal registrado, datos mínimos de uno no registrado o un grupo de animales de cualquier especie.
3. Indico gravedad percibida, síntomas, cantidad afectada y riesgos relevantes; el cuestionario se adapta a la especie o categoría.
4. El sistema asigna un nivel preliminar rojo, naranja, amarillo o verde sin presentarlo como diagnóstico.
5. Una solicitud roja activa el circuito de guardia aunque la clínica esté fuera de su horario habitual.

**Trazabilidad:** RF-CLI-21; RN-46 a RN-49, RN-54, RN-59; RNF-USA-07. Prioridad: Must. Fase: 3.

### HU-CLI-22 — Proporcionar ubicación para la asistencia

**Historia:** Como cliente, quiero compartir mi GPS o ingresar una dirección manual, para que la clínica pueda localizar a los animales respetando mi privacidad.

**Criterios de aceptación:**

1. Antes de activar el GPS se informa finalidad, destinatarios y duración del uso y se registra la decisión aplicable.
2. Puedo rechazar el GPS e ingresar dirección y referencias manuales.
3. Si no proporciono ningún destino, no puedo confirmar una visita, pero puedo solicitar orientación o coordinar el traslado.
4. Si la ubicación no puede validarse o está fuera de cobertura, la solicitud se escala a Recepción y no se descarta silenciosamente.

**Trazabilidad:** RF-CLI-22; RN-55, RN-56; RNF-SEG-14, RNF-DIS-05, RNF-LEG-09. Prioridad: Must. Fase: 3.

### HU-CLI-23 — Seguir la asignación y llegada

**Historia:** Como cliente, quiero recibir estados y notificaciones y consultar el recorrido del veterinario, para poder atender a los animales sin mirar permanentemente el teléfono.

**Criterios de aceptación:**

1. Antes de la aceptación profesional veo el estado real de la solicitud, sin nombre ni tiempo de llegada inventados.
2. Después de la aceptación veo el veterinario asignado y un tiempo estimado identificado como tal.
3. Desde el inicio del traslado puedo consultar el mapa y recibo notificaciones de cambios relevantes.
4. El seguimiento de la ubicación profesional termina al llegar, cancelar o finalizar la asistencia.
5. Si falla el mapa o una notificación, el estado, la dirección textual y el contacto continúan disponibles.

**Trazabilidad:** RF-CLI-23; RN-53, RN-57; RNF-DIS-05, RNF-USA-02, RNF-LEG-09. Prioridad: Must. Fase: 3.

### HU-CLI-24 — Informar empeoramiento y reevaluar

**Historia:** Como cliente, quiero informar inmediatamente que uno o varios animales empeoraron y responder reevaluaciones de solicitudes demoradas, para que la prioridad refleje la situación actual.

**Criterios de aceptación:**

1. Mientras la solicitud está activa, la acción “Informar empeoramiento” permanece visible y accesible.
2. Al informar nuevos síntomas, la API reevalúa y puede elevar inmediatamente la prioridad, incluso de amarillo o naranja a rojo.
3. Cada cambio conserva nivel anterior, nivel nuevo, fecha y causa.
4. Si la solicitud sigue pendiente 24 horas después de su creación, recibo una pregunta de reevaluación.
5. No responder la reevaluación no reduce el nivel ni cierra la solicitud automáticamente.

**Trazabilidad:** RF-CLI-24; RN-50 a RN-52; RNF-CON-10, RNF-USA-07. Prioridad: Must. Fase: 3.

### HU-CLI-25 — Conocer el costo de la asistencia

**Historia:** Como cliente, quiero conocer el costo o cómo se calculará antes de confirmar, para decidir con información suficiente sin demorar una urgencia crítica.

**Criterios de aceptación:**

1. Se muestra la tarifa vigente, una estimación o las variables que impedirían determinar todavía un importe definitivo.
2. La aceptación queda vinculada a la solicitud y versión tarifaria aplicable.
3. Una deuda previa o una falla de pago no bloquea la creación ni el escalamiento de una solicitud roja.
4. El cargo definitivo conserva trazabilidad respecto de la asistencia prestada.

**Trazabilidad:** RF-CLI-25; RN-33, RN-36, RN-58; RNF-USA-02. Prioridad: Must. Fase: 3.

## 6. Criterios transversales de aceptación

Aplican a todas las historias afectadas:

1. La API valida autenticación, autorización y reglas; ocultar una acción en la interfaz no constituye seguridad.
2. Los mensajes explican qué ocurrió y qué puede hacer la persona, sin detalles técnicos.
3. La interfaz funciona desde 360 px, usa español rioplatense y cumple los criterios aplicables de WCAG 2.1 AA.
4. Los estados de carga, vacío, error recuperable, sin conexión, sesión expirada y acceso denegado se diseñan cuando correspondan.
5. Los datos clínicos, credenciales y tokens no se envían a analítica ni se escriben en logs.
6. Todo campo recolectado debe tener finalidad, obligatoriedad, acceso, conservación y destino documentados.

## 7. Definición de preparada (Definition of Ready)

Una historia está preparada para entrar a desarrollo cuando:

- tiene actor, valor y criterios de aceptación verificables;
- su RF, RN y RNF relacionados están identificados;
- no depende de una decisión funcional desconocida, o la decisión está marcada y aceptada;
- existe flujo o pseudocódigo para sus variantes relevantes;
- se conocen datos, permisos, mensajes y estados principales;
- puede estimarse y probarse de forma independiente.

## 8. Definición de terminada (Definition of Done)

Una historia está terminada cuando:

- la API y los clientes incluidos en su alcance están implementados e integrados;
- las reglas residen en backend/dominio y las garantías críticas necesarias existen en base de datos;
- cumple los criterios funcionales, de autorización, accesibilidad y privacidad;
- posee pruebas proporcionales al riesgo y trazabilidad actualizada;
- se actualizaron contrato de API, documentación y diagramas afectados;
- fue validada con datos de demostración reproducibles.

## 9. Pendientes para aprobar este backlog

1. Completar la especificación de registro: campos, verificación, recuperación, caducidades, reenvíos y mensajes.
2. Completar el catálogo de alertas sanitarias y los parámetros pendientes de las alertas de asistencia: destinatarios, tiempos objetivo, canales, reiteración, tarifas y cancelación.
3. Definir pseudocódigo de agenda, reanudación de intención, alta de animales, internación, consentimientos y derechos sobre datos; el pseudocódigo inicial de asistencia ya se encuentra en `ESPECIFICACION_ALERTAS_ASISTENCIA.md`.
4. Confirmar el alcance de las historias Should y Could para la entrega académica.
5. Validar estas historias con los interesados antes de convertirlas en compromiso de implementación.
