# Casos de uso — Visitante y Cliente

Versión: 0.3

Fecha: 2026-10-02

Estado: Borrador funcional para validación

Fuentes: `../contexto-base/MATRIZ_REQUISITOS.md` y `HISTORIAS_USUARIO_VISITANTE_CLIENTE.md`

## 1. Propósito

Los casos de uso describen cómo interactúan los actores con el sistema, incluyendo precondiciones, resultado, flujo principal, alternativas y reglas aplicables. No sustituyen los requisitos canónicos ni el pseudocódigo de dominio.

## 2. Actores relevantes

| Actor | Descripción |
|---|---|
| Visitante | Persona no autenticada que consulta información pública o inicia el acceso. |
| Cliente | Persona autenticada y, para acciones protegidas, con cuenta verificada. |
| Sistema de identidad | Componente de la API que autentica, verifica cuentas y administra sesiones. |
| Sistema temporal | Proceso que genera recordatorios, alertas y tareas programadas. |
| Pasarela de pago | Servicio externo condicional para pagos en línea. |
| Personal de la clínica | Actor secundario en solicitudes, internaciones y seguimiento. |
| Recepcionista | Gestiona solicitudes no rojas y excepciones que requieren contacto o coordinación humana. |
| Veterinario | Revisa la gravedad, acepta asistencias compatibles y actualiza el traslado y la atención. |
| Proveedor de mapas | Calcula ruta y tiempo estimado con los datos mínimos autorizados. |

## 3. Catálogo general

| ID | Caso de uso | Actor principal | RF principales | Prioridad |
|---|---|---|---|---|
| CU-VIS-01 | Consultar información pública de la clínica | Visitante | RF-VIS-01, 02, 03, 05, 06, 10 | Must/Should |
| CU-VIS-02 | Consultar disponibilidad general | Visitante | RF-VIS-04 | Should |
| CU-VIS-03 | Iniciar registro y retomar acción | Visitante | RF-VIS-07 | Must |
| CU-VIS-04 | Consultar mascota extraviada por QR | Visitante | RF-VIS-08 | Could |
| CU-VIS-05 | Enviar consulta de contacto | Visitante | RF-VIS-09 | Should |
| CU-CLI-01 | Registrar o editar mascota | Cliente | RF-CLI-01 | Must |
| CU-CLI-02 | Agendar turno | Cliente | RF-CLI-02 | Must |
| CU-CLI-03 | Cancelar o reprogramar turno | Cliente | RF-CLI-03 | Must |
| CU-CLI-04 | Consultar turnos | Cliente | RF-CLI-04 | Must |
| CU-CLI-05 | Consultar historia clínica y adjuntos | Cliente | RF-CLI-05, 08 | Must |
| CU-CLI-06 | Descargar carnet sanitario | Cliente | RF-CLI-06 | Should |
| CU-CLI-07 | Recibir alerta sanitaria | Cliente | RF-CLI-07, 18 | Must |
| CU-CLI-08 | Seguir internación | Cliente | RF-CLI-09 | Must |
| CU-CLI-09 | Recibir resumen clínico | Cliente | RF-CLI-10 | Must |
| CU-CLI-10 | Firmar consentimiento informado | Cliente | RF-CLI-11 | Must |
| CU-CLI-11 | Consultar cuenta y abonar | Cliente | RF-CLI-12 | Should |
| CU-CLI-12 | Gestionar responsables | Cliente | RF-CLI-13 | Should |
| CU-CLI-13 | Registrar y consultar peso | Cliente | RF-CLI-14 | Could |
| CU-CLI-14 | Solicitar renovación de tratamiento | Cliente | RF-CLI-15 | Could |
| CU-CLI-15 | Realizar seguimiento postoperatorio | Cliente | RF-CLI-16 | Should |
| CU-CLI-16 | Responder encuesta | Cliente | RF-CLI-17 | Could |
| CU-CLI-17 | Gestionar notificaciones y privacidad | Cliente | RF-CLI-18, 20 | Should |
| CU-CLI-18 | Solicitar exportación o eliminación | Cliente | RF-CLI-19 | Should |
| CU-CLI-19 | Solicitar orientación o asistencia veterinaria | Cliente | RF-CLI-21, 22, 25 | Must |
| CU-CLI-20 | Seguir la asistencia e informar empeoramiento | Cliente | RF-CLI-23, 24 | Must |

## 4. Relaciones principales

- `CU-VIS-02` puede extender a `CU-VIS-03` cuando el visitante decide reservar.
- `CU-VIS-03` incluye registro o inicio de sesión y la reanudación de la intención original.
- `CU-CLI-02` incluye validar cuenta, responsabilidad sobre la mascota y disponibilidad.
- `CU-CLI-03` reutiliza la validación de disponibilidad al reprogramar.
- `CU-CLI-05`, `CU-CLI-06`, `CU-CLI-08` y `CU-CLI-10` incluyen verificar responsabilidad sobre la mascota.
- `CU-CLI-07` consulta las preferencias administradas en `CU-CLI-17`.
- `CU-CLI-11` usa una pasarela externa únicamente si se implementa el pago en línea.
- `CU-CLI-19` incluye clasificar preliminarmente, proporcionar ubicación y aceptar el costo o su criterio.
- `CU-CLI-20` extiende `CU-CLI-19` cuando existe una solicitud activa y puede elevar su prioridad en cualquier momento.

## 5. Especificaciones detalladas del recorrido esencial

### CU-VIS-01 — Consultar información pública de la clínica

| Campo | Especificación |
|---|---|
| Actor principal | Visitante |
| Objetivo | Conocer servicios, profesionales, ubicación, contacto, urgencias, contenido educativo e información legal. |
| Disparador | El visitante abre el área pública. |
| Precondiciones | Ninguna; no requiere autenticación. |
| Postcondición de éxito | La información pública seleccionada fue presentada. |
| Postcondición mínima | No se modifica información ni se crea una sesión autenticada. |

**Flujo principal**

1. El sistema presenta la navegación pública.
2. El visitante selecciona una sección.
3. El sistema obtiene el contenido público vigente desde la API o fuente configurada.
4. El sistema muestra la información con su vigencia cuando corresponda.
5. El visitante puede navegar a otra sección, contactar a la clínica o iniciar una acción protegida.

**Alternativas y excepciones**

- A1. No hay resultados: se muestra un estado vacío y una vía de contacto.
- A2. Falla un mapa o servicio externo: se conserva la dirección y el contacto textual.
- A3. El visitante selecciona una acción protegida: se continúa en `CU-VIS-03`.
- A4. Se consulta urgencias: los datos se muestran directamente, sin exigir registro.

**Reglas y calidad:** RN-45; RNF-USA-02 a 06; RNF-LEG-03, 06, 07.

### CU-VIS-03 — Iniciar registro y retomar acción

| Campo | Especificación |
|---|---|
| Actor principal | Visitante |
| Actores secundarios | Sistema de identidad |
| Objetivo | Obtener acceso a una función protegida sin perder la intención original. |
| Disparador | El visitante elige registrarse o intenta una acción protegida. |
| Precondiciones | La acción original puede identificarse y conservarse de forma segura. |
| Postcondición de éxito | Cuenta creada, aceptaciones registradas, verificación completada cuando corresponda e intención retomada. |
| Postcondición mínima | No se ejecuta la acción protegida ni se crea una cuenta incompleta como cuenta verificada. |

**Flujo principal**

1. El sistema identifica la acción protegida y conserva su contexto no sensible.
2. Explica por qué se requiere una cuenta y ofrece iniciar sesión o registrarse.
3. El visitante elige registrarse.
4. El sistema presenta los campos necesarios y acceso a las políticas vigentes.
5. Presenta por separado términos obligatorios y decisiones opcionales, sin premarcar estas últimas.
6. El visitante completa los datos y confirma las decisiones requeridas.
7. La API valida datos, unicidad del correo, versiones aceptadas y protección contra abuso.
8. El sistema crea la cuenta en el estado definido y solicita verificación cuando corresponda.
9. Tras completar la verificación, autentica al nuevo cliente.
10. El sistema restaura el contexto y retoma la acción original.

**Alternativas y excepciones**

- A1. El correo ya existe: se informa sin exponer datos de la cuenta y se ofrece iniciar sesión o recuperar acceso.
- A2. Falta una aceptación obligatoria: se explica su finalidad y no se completa el registro.
- A3. El usuario rechaza una opción facultativa: el registro continúa si esa opción no es necesaria.
- A4. La verificación vence o falla: se ofrece reenvío sujeto a límites.
- A5. La intención ya no es válida o el recurso cambió: se conserva lo posible y se informa cómo continuar.
- A6. El visitante elige iniciar sesión: tras autenticarse se retoma el paso 10.

**Reglas:** RN-37, RN-38, RN-43, RN-44.  
**Calidad:** RNF-SEG-01 a 05, RNF-SEG-09, RNF-LEG-01, 03, 06.  
**Pendiente:** campos definitivos, mecanismo y vigencia de verificación, recuperación, límites y mensajes.

### CU-CLI-01 — Registrar o editar mascota

| Campo | Especificación |
|---|---|
| Actor principal | Cliente |
| Objetivo | Crear o actualizar la ficha identificatoria de una mascota. |
| Disparador | El cliente selecciona “Registrar mascota” o “Editar”. |
| Precondiciones | Sesión válida; cuenta verificada; para editar, responsabilidad vigente sobre la mascota. |
| Postcondición de éxito | La ficha queda creada o actualizada y vinculada al responsable. |
| Postcondición mínima | No se persisten datos inválidos ni archivos rechazados. |

**Flujo principal**

1. El sistema verifica sesión, cuenta y autorización.
2. Presenta el formulario y distingue datos obligatorios y opcionales.
3. El cliente ingresa datos identificatorios, foto y señas particulares.
4. El sistema valida formatos, rangos, especie/raza, archivo y microchip.
5. El cliente confirma.
6. La API vuelve a validar autorización, unicidad e integridad.
7. El sistema guarda la ficha y confirma el resultado.

**Alternativas y excepciones**

- A1. Microchip duplicado: no se guarda y se informa que requiere revisión de la clínica sin revelar otra ficha.
- A2. Foto inválida: se rechaza solo el archivo y se explica formato o tamaño permitido.
- A3. Sesión vencida: se solicita reautenticación y se conserva temporalmente el trabajo no sensible.
- A4. El cliente perdió autorización durante la edición: se rechaza la operación y se recarga el estado vigente.

**Reglas:** RN-18, RN-38, RN-41, RN-42.  
**Calidad:** RNF-SEG-05, 07, 08; RNF-BD-02, 03; RNF-USA-02.

### CU-CLI-02 — Agendar turno

Detalle de conexión con la atención: [FLUJO_RESERVA_CONSULTA.md](FLUJO_RESERVA_CONSULTA.md). Allí se proponen los estados, permisos y criterios de transición a CU-VET-01 sin cambiar el identificador de este caso.

| Campo | Especificación |
|---|---|
| Actor principal | Cliente |
| Objetivo | Confirmar una reserva válida para una mascota. |
| Disparador | El cliente selecciona “Agendar turno”. |
| Precondiciones | Sesión válida, cuenta verificada y responsabilidad vigente sobre al menos una mascota. |
| Postcondición de éxito | Para la reserva ordinaria sin seña, se propone crear exactamente un turno Confirmado y mostrar constancia (RC-D01). La variante condicionada por seña se especificará por separado; aún no crea una consulta clínica. |
| Postcondición mínima | No queda una reserva parcial ni se producen solapamientos. |

**Flujo principal**

1. El cliente selecciona la mascota.
2. Selecciona tipo de cita y, opcionalmente según el flujo, profesional.
3. La API calcula disponibilidad considerando duración, jornadas, ausencias, recursos, antelación, horizonte y reservas existentes.
4. El cliente selecciona fecha y horario.
5. El sistema presenta el resumen, precio de referencia, requisitos y política de cancelación.
6. El cliente confirma.
7. La API revalida cuenta, autorización, deuda/ausencias, disponibilidad y reglas de agenda.
8. La base de datos garantiza que no exista una reserva incompatible concurrente.
9. El sistema registra el turno y devuelve la confirmación.
10. La interfaz actualiza próximos turnos.

**Alternativas y excepciones**

- A1. La mascota ya tiene un turno activo superpuesto: se rechaza y se indica el conflicto.
- A2. El horario fue ocupado concurrentemente: se rechaza la confirmación, se conservan selecciones compatibles y se muestran horarios actualizados.
- A3. El cliente superó el umbral de ausencias: se informa que debe contactar al mostrador.
- A4. Existe deuda vencida superior al límite: se muestra advertencia; una urgencia no se bloquea por esta causa.
- A5. La cita requiere seña: el estado y el flujo de pago se aplican según la política configurada.
- A6. La cuenta no está verificada: se deriva a verificación y luego se retoma la intención.

**Reglas:** RN-01 a RN-09, RN-35, RN-36, RN-38.  
**Calidad:** RNF-CON-01, 02, 05 a 09; RNF-REN-02; RNF-USA-01, 02.  
**Nota:** el recorrido visible tendrá un máximo de cuatro pasos; las validaciones internas no cuentan como pasos de usuario.

**Precisión pendiente:** RNF-USA-01 dice “desde el ingreso”. El tratamiento de registro o alta previa de un animal al medir esos pasos se cierra en P-RC-01 del recorrido integrado. El comportamiento posterior es llegada por Recepción, inicio por Veterinario y cierre clínico; el pago tiene un ciclo propio.

### CU-CLI-03 — Cancelar o reprogramar turno

| Campo | Especificación |
|---|---|
| Actor principal | Cliente |
| Objetivo | Cambiar o cancelar una reserva vigente dentro de la política permitida. |
| Precondiciones | Sesión válida; turno perteneciente a una mascota bajo responsabilidad; estado modificable. |
| Postcondición de éxito | Turno cancelado o reprogramado de forma consistente y trazable. |
| Postcondición mínima | El turno original conserva un estado válido si no se completa la operación. |

**Flujo principal de cancelación**

1. El cliente abre un próximo turno y elige cancelar.
2. El sistema valida estado y ventana de cancelación.
3. Muestra consecuencias y solicita confirmación.
4. El cliente confirma.
5. La API cambia el estado según el flujo permitido y libera disponibilidad.
6. El sistema confirma y actualiza la agenda.

**Flujo principal de reprogramación**

1. El cliente abre un próximo turno y elige reprogramar.
2. El sistema valida que el turno admita el cambio.
3. Calcula y presenta nuevos horarios válidos.
4. El cliente selecciona y confirma uno.
5. La API revalida el nuevo horario y ejecuta el cambio de forma atómica.
6. El sistema confirma y actualiza la agenda.

**Alternativas:** fuera de ventana; estado incompatible; horario tomado concurrentemente; sesión vencida; intervención requerida por la clínica.

**Reglas:** RN-01 a RN-07, RN-11.  
**Calidad:** RNF-CON-01, 02, 05; RNF-USA-02.

### CU-CLI-05 — Consultar historia clínica y adjuntos

| Campo | Especificación |
|---|---|
| Actor principal | Cliente |
| Objetivo | Consultar antecedentes clínicos y archivos visibles de una mascota autorizada. |
| Precondiciones | Sesión válida y responsabilidad vigente. |
| Postcondición de éxito | Se presenta la historia en modo lectura y se permite abrir adjuntos autorizados. |
| Postcondición mínima | No se expone información de otra mascota ni archivos mediante enlaces permanentes. |

**Flujo principal**

1. El cliente selecciona una mascota.
2. Elige Historia clínica.
3. La API verifica responsabilidad y devuelve registros visibles ordenados cronológicamente.
4. El sistema muestra atenciones firmadas y enmiendas relacionadas, sin controles de edición.
5. El cliente selecciona un adjunto visible.
6. La API emite un acceso temporal autorizado.
7. El cliente visualiza o descarga el archivo.

**Alternativas:** acceso revocado; archivo retirado o no disponible; enlace vencido; formato sin previsualización; historial vacío.

**Reglas:** RN-12, RN-14, RN-18.  
**Calidad:** RNF-SEG-05, 07, 08; RNF-REN-03, 04; RNF-LEG-04.

### CU-CLI-08 — Seguir internación

| Campo | Especificación |
|---|---|
| Actor principal | Cliente |
| Actor secundario | Personal de la clínica |
| Objetivo | Consultar novedades visibles de una internación activa o histórica autorizada. |
| Precondiciones | Sesión válida, responsabilidad vigente y una internación accesible. |
| Postcondición de éxito | Se muestran las novedades publicadas para el cliente en orden temporal. |
| Postcondición mínima | Las notas internas y datos de otros pacientes no se exponen. |

**Flujo principal**

1. El cliente abre el detalle de su mascota y selecciona Internación.
2. La API verifica autorización e internación.
3. El sistema muestra estado, fechas y novedades visibles.
4. Mientras la pantalla está activa, recibe nuevas publicaciones autorizadas.
5. Cada novedad se añade cronológicamente sin modificar las anteriores.

**Alternativas:** no existe internación; falla tiempo real y se ofrece recarga; una novedad se rectifica con otra entrada; la responsabilidad deja de estar vigente.

**Reglas:** RN-21, RN-23, RN-24, RN-25.  
**Calidad:** RNF-DIS-04, RNF-SEG-05.

### CU-CLI-10 — Firmar consentimiento informado

| Campo | Especificación |
|---|---|
| Actor principal | Cliente responsable |
| Objetivo | Autorizar un procedimiento clínico mediante un consentimiento específico y trazable. |
| Precondiciones | Sesión válida, responsabilidad vigente y procedimiento que requiere consentimiento. |
| Postcondición de éxito | Documento firmado y vinculado a persona, mascota, procedimiento y versión. |
| Postcondición mínima | El procedimiento permanece sin autorización válida. |

**Flujo principal**

1. El sistema informa que existe un consentimiento pendiente.
2. El cliente abre el documento y visualiza procedimiento, mascota, alcance y versión.
3. El sistema exige las acciones de lectura o confirmación que se definan.
4. El cliente acepta y firma mediante el mecanismo aprobado.
5. La API verifica identidad, responsabilidad, vigencia y versión.
6. Registra respuesta, fecha, canal y evidencia.
7. El sistema confirma y conserva el documento histórico.

**Alternativas:** rechazo; documento vencido o reemplazado; pérdida de responsabilidad; sesión vencida; firma incompleta. En ningún caso el turno quirúrgico pasa a En curso sin consentimiento válido.

**Reglas:** RN-10, RN-43.  
**Calidad:** RNF-SEG-05, 10; RNF-LEG-01, 03, 06.

### CU-CLI-17 — Gestionar notificaciones y privacidad

| Campo | Especificación |
|---|---|
| Actor principal | Cliente |
| Objetivo | Consultar y modificar decisiones opcionales vigentes. |
| Precondiciones | Sesión válida. |
| Postcondición de éxito | Preferencias futuras actualizadas y decisión trazada. |
| Postcondición mínima | Se conserva el estado anterior si la actualización falla. |

**Flujo principal**

1. El cliente abre Preferencias.
2. La API devuelve finalidades, canales, estado y versión aplicable.
3. El sistema diferencia decisiones opcionales, términos obligatorios y consentimientos clínicos históricos.
4. El cliente cambia una preferencia revocable.
5. El sistema explica el efecto futuro y solicita confirmación cuando corresponda.
6. La API registra finalidad, documento/versión, respuesta, fecha y canal.
7. El sistema presenta el estado confirmado por la API.

**Alternativas:** canal no disponible; preferencia no revocable; versión actualizada que requiere nueva decisión; error de actualización.

**Reglas:** RN-43, RN-44.  
**Calidad:** RNF-SEG-10, RNF-LEG-01, 03, 06.  
**Pendiente:** catálogo definitivo de eventos, canales y decisiones facultativas.

### CU-CLI-19 — Solicitar orientación o asistencia veterinaria

| Campo | Especificación |
|---|---|
| Actor principal | Cliente |
| Actores secundarios | Recepcionista, Veterinario, Sistema temporal y Proveedor de mapas |
| Objetivo | Solicitar visita u orientación para uno o varios animales y obtener una respuesta priorizada y trazable. |
| Disparador | El cliente selecciona “Solicitar asistencia”. |
| Precondiciones | Sesión válida y cuenta verificada; medio de contacto vigente. |
| Postcondición de éxito | Solicitud creada una sola vez, clasificada y ubicada en el circuito de asignación correspondiente. |
| Postcondición mínima | No se promete profesional ni tiempo de llegada inexistentes; los datos y decisiones ingresados quedan protegidos. |

**Flujo principal**

1. El cliente elige visita al lugar u orientación para trasladarse a la clínica.
2. Selecciona un animal, registra datos mínimos o indica un grupo y su cantidad.
3. Informa especie o categoría, síntomas, riesgos y gravedad percibida.
4. El sistema presenta el cuestionario aplicable y calcula una clasificación preliminar.
5. La clasificación conserva como mínimo la gravedad indicada por el cliente y puede elevarla según las respuestas.
6. El sistema explica el uso de la ubicación; el cliente comparte GPS o ingresa dirección y referencias.
7. Se muestra costo, estimación o criterio de cálculo y el cliente confirma.
8. La API valida y crea la solicitud de forma idempotente.
9. Si es roja, activa guardia y busca veterinarios elegibles; en otro nivel, la publica en el tablero de Recepción.
10. El sistema confirma el nivel, el estado real y las acciones disponibles, incluida “Informar empeoramiento”.

**Alternativas y excepciones**

- A1. El GPS es rechazado: se solicita dirección manual sin impedir la orientación.
- A2. No se proporciona ningún destino: no se confirma visita; se permite orientación o coordinación de traslado.
- A3. La ubicación está fuera de cobertura o no puede validarse: se escala a Recepción para contacto.
- A4. No existe veterinario elegible: se escala a Recepción sin inventar asignación ni estimación.
- A5. Existe deuda o falla el pago: si la solicitud es roja, se crea y escala igualmente.
- A6. La red reintenta la confirmación: la clave de idempotencia evita una segunda solicitud.
- A7. Se trata de varios animales: se conserva cantidad y contexto grupal y se exige competencia profesional compatible.

**Reglas:** RN-36, RN-46 a RN-49, RN-54 a RN-56, RN-58, RN-59.

**Calidad:** RNF-SEG-13, 14; RNF-CON-10; RNF-DIS-05; RNF-USA-02, 07; RNF-LEG-06, 09.

**Detalle complementario:** `ESPECIFICACION_ALERTAS_ASISTENCIA.md`.

### CU-CLI-20 — Seguir la asistencia e informar empeoramiento

| Campo | Especificación |
|---|---|
| Actor principal | Cliente |
| Actores secundarios | Recepcionista, Veterinario, Sistema temporal y Proveedor de mapas |
| Objetivo | Conocer el estado de la asistencia y elevar la prioridad cuando cambie el estado de los animales. |
| Disparador | El cliente abre una solicitud activa, recibe una actualización o selecciona “Informar empeoramiento”. |
| Precondiciones | Solicitud activa y autorización vigente sobre ella. |
| Postcondición de éxito | Se presenta el estado vigente o se registra y procesa el empeoramiento una sola vez. |
| Postcondición mínima | Una falla de mapa o notificación no oculta el estado ni reduce o cierra la solicitud. |

**Flujo principal de seguimiento**

1. La API devuelve nivel, estado e historial visible.
2. Antes de la aceptación, la interfaz informa que todavía no existe un profesional confirmado.
3. El veterinario acepta la asistencia.
4. El sistema muestra profesional y tiempo estimado de llegada.
5. Cuando comienza el traslado, habilita mapa y notificaciones de avance.
6. Al llegar, cancelar o finalizar, cesa el seguimiento de ubicación profesional.

**Flujo de empeoramiento**

1. El cliente selecciona “Informar empeoramiento” en cualquier momento de la solicitud activa.
2. Registra nuevos síntomas o respuestas para uno o varios animales.
3. La API guarda el evento de forma idempotente y reevalúa inmediatamente.
4. Si corresponde, eleva el nivel, reordena la cola y notifica a Recepción y profesionales.
5. Si alcanza rojo, activa el circuito de guardia sin esperar el horario de la clínica.
6. El cliente recibe confirmación del nuevo estado.

**Reevaluación temporal**

- Al cumplirse 24 horas desde la creación y continuar pendiente, el sistema pregunta si el estado se mantuvo o empeoró.
- La falta de respuesta no reduce el nivel ni cierra la solicitud.
- Una reducción solo puede efectuarla un veterinario y exige motivo.

**Alternativas:** mapa no disponible; notificación fallida; estimación modificada; reasignación; solicitud fuera de cobertura; coordinación telefónica; reintento de aviso de empeoramiento.

**Reglas:** RN-47 a RN-53, RN-55, RN-57.

**Calidad:** RNF-SEG-14; RNF-CON-10; RNF-DIS-05; RNF-USA-07; RNF-LEG-09.

**Detalle complementario:** `ESPECIFICACION_ALERTAS_ASISTENCIA.md`.

## 6. Fichas resumidas para completar en la siguiente iteración

Los siguientes casos ya poseen objetivo y trazabilidad en el catálogo, pero requieren detallar sus flujos después de resolver las decisiones indicadas:

| Caso | Decisión pendiente antes del detalle final |
|---|---|
| CU-VIS-02 | Nivel de precisión y filtros de la disponibilidad pública. |
| CU-VIS-04 | Datos públicos autorizables, activación y revocación del QR. |
| CU-VIS-05 | Campos, destino, CAPTCHA/protección y plazo de conservación. |
| CU-CLI-04 | Estados definitivos del turno y filtros del historial. |
| CU-CLI-06 | Contenido, firma/verificación y vigencia del carnet. |
| CU-CLI-07 | Catálogo de alertas, anticipación, repetición y canales. |
| CU-CLI-09 | Contenido del resumen, destinatarios y política de reintentos. |
| CU-CLI-11 | Proveedor de pagos, señas, estados e idempotencia. |
| CU-CLI-12 | Invitación/aceptación del segundo responsable y transferencia. |
| CU-CLI-13 | Rangos, unidades y corrección de mediciones domésticas. |
| CU-CLI-14 | Vigencia del tratamiento y circuito de aprobación/rechazo. |
| CU-CLI-15 | Duración del seguimiento, participantes y criterios de urgencia. |
| CU-CLI-16 | Momento, anonimato, duplicados y eventual publicación. |
| CU-CLI-18 | Verificación de identidad, plazos y estados de la solicitud. |
| CU-CLI-19 | Cuestionarios por especie, tiempos objetivo, estrategia de guardia, zonas, tarifas y política de cancelación. |
| CU-CLI-20 | Repetición posterior a la reevaluación de 24 horas y canales operativos obligatorios. |

## 7. Plantilla para los próximos casos de uso

```text
ID y nombre:
Actor principal:
Actores secundarios:
Objetivo:
Disparador:
Precondiciones:
Postcondición de éxito:
Garantía mínima:
Flujo principal:
Alternativas y excepciones:
Reglas de negocio:
Requisitos no funcionales:
Historias y RF relacionados:
Datos y auditoría:
Preguntas pendientes:
```

## 8. Próxima iteración

1. Revisar [ALCANCE_ENTREGA.md](ALCANCE_ENTREGA.md) y resolver las decisiones P-RC del recorrido [Reserva y consulta](FLUJO_RESERVA_CONSULTA.md), que ya incorpora CU-VET-01 — Realizar consulta.
2. Completar la especificación de registro y los casos de Recepción “Cobrar consulta” y del Veterinario “Internar animal”.
3. Resolver alertas sanitarias y parámetros abiertos de asistencia; completar los casos resumidos de Visitante y Cliente.
4. Derivar pantallas, contratos y pruebas a partir de los casos aprobados y completar el mapa móvil y Blazor.
5. Extender el catálogo e historias para los restantes procesos de Veterinario, Recepcionista y Administrador.
