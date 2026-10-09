# Recorrido integrado — Reservar turno y realizar consulta

Versión: 0.1

Fecha: 2026-10-02

Estado: Especificación funcional propuesta para revisión; no autoriza todavía código de producto.

Fuentes: [matriz canónica](../contexto-base/MATRIZ_REQUISITOS.md), [alcance de entrega](ALCANCE_ENTREGA.md), [CU-CLI-02 existente](CASOS_DE_USO_VISITANTE_CLIENTE.md#cu-cli-02--agendar-turno) y [historias de atención](HISTORIAS_USUARIO_ATENCION.md).

## 1. Objetivo y frontera

Definir un primer recorrido individual y programado: un responsable reserva para un animal; Recepción registra llegada; el Veterinario abre, documenta y firma la consulta; el sistema incorpora el registro a la historia, confirma consumos, genera cargos y encola el resumen; Recepción puede continuar con el cobro.

La atención puede ser de cualquier especie configurada. El ejemplo no impone campos exclusivos de perros o gatos. Las solicitudes grupales, urgencias sin turno, internaciones y procedimientos quirúrgicos tienen variantes propias que se completarán en sus casos de uso. Una restricción de este recorrido programado no debe aplicarse automáticamente a una urgencia.

Los identificadores RF, RN y RNF remiten al texto exacto de la matriz. Las siguientes decisiones derivadas hacen concreto el borrador; no crean RN canónicas nuevas ni cambian sus tipos Bloqueante/Advertencia.

## 2. Decisiones derivadas y clasificación

| ID local | Clasificación | Propuesta y fundamento |
|---|---|---|
| RC-D01 | Pseudocódigo/comportamiento | La reserva ordinaria sin seña se crea Confirmada tras validación atómica. Pendiente se reserva para condiciones previas expresamente definidas. RN-11, RN-35. |
| RC-D02 | Pseudocódigo/comportamiento | Turno, consulta y cargo tienen ciclos independientes. Finalizar una consulta no significa cobrarla. RF-VET-12, RF-REC-05, RN-32. |
| RC-D03 | Requerimiento no funcional derivado: autorización | Veterinario habilitado y asignado realiza el acto clínico; Recepción gestiona admisión/cobro; Administrador configura y audita. Un rol administrativo por sí solo no habilita a firmar. RN-13, RN-40, RNF-SEG-05. |
| RC-D04 | Decisión técnica propuesta | Idempotencia persistida para reservar, iniciar, registrar consumo y cerrar; versión optimista en borradores, unicidad del vínculo turno–consulta y garantías de disponibilidad en BD. RT-08, RNF-CON-01, RNF-CON-05 y estrategia del análisis complementario. |
| RC-D05 | Decisión técnica propuesta | Reservar existencias por lote durante la consulta; consumo contable definitivo al cierre atómico. Disponibilidad descuenta reservas; un uso real no se libera por caducidad automática. RF-VET-06, RN-26 a RN-30, RNF-CON-03. Validar operativa con el equipo antes de implementar. |
| RC-D06 | Decisión técnica propuesta | Guardar el mensaje pendiente de correo en la misma BD/transacción del cierre; un procesador lo envía después con reintentos. RNF-CON-03, RNF-CON-04. |
| RC-D07 | Diseño/UX | Reserva ordinaria en cuatro pasos visibles: animal; tipo/profesional; fecha/hora; revisión/confirmación. Definir en P-RC-01 cómo se mide “desde el ingreso” cuando interviene registro. RNF-USA-01. |
| RC-D08 | Consentimiento/alerta | Advertencia de deuda, derivación a mostrador por ausencias y consentimiento clínico conservan el comportamiento y clasificación canónicos; no se inventan umbrales ni se agrupan aceptaciones. RN-08, RN-10, RN-36, RN-43. |
| RC-D09 | Pseudocódigo/comportamiento | Guardar un borrador no firma, publica al cliente, genera cargo ni envía resumen. Cerrar y firmar forman una única operación con efectos persistidos juntos. RN-12, RN-20, RNF-CON-03. |

## 3. CU-CLI-02 — Reservar turno: contrato de conexión

Se conserva el identificador existente. Esta sección precisa su salida y su conexión con CU-VET-01.

| Campo | Especificación |
|---|---|
| Actor principal | Cliente responsable. La variante de mostrador corresponde a Recepcionista, RF-REC-01. |
| Disparador | Solicita agendar atención para un animal. |
| Precondiciones del flujo autenticado | Cuenta válida y verificada; responsabilidad vigente; animal, servicio y profesional habilitados para la atención solicitada. |
| Entrada | Animal, tipo de cita, profesional, horario de inicio y clave de operación. Duración/fin/recursos se calculan en el servidor. |
| Éxito | Turno con identificador, animal, profesional, tipo, inicio, fin, recursos, estado Confirmado y versión; visible en ambas agendas autorizadas. |
| Garantía mínima | Ninguna reserva parcial ni incompatible persiste; la consulta clínica aún no existe. |
| Consumidor de la salida | Recepción registra llegada; el veterinario asignado inicia CU-VET-01. |

### Flujo principal

1. El cliente selecciona un animal propio, tipo de cita, profesional y horario disponible mediante los cuatro pasos definidos en RC-D07.
2. La API calcula la disponibilidad real con jornada, licencias, feriados, duración, recursos, antelación y horizonte configurados.
3. La interfaz muestra el resumen y la política de cancelación. El precio de referencia de la reserva no sustituye la tarifa vigente en la fecha de la prestación (RN-33).
4. El cliente confirma una vez; los reintentos conservan la misma clave de operación.
5. La API revalida identidad, responsabilidad, configuración, restricciones y disponibilidad en el momento de escribir.
6. La BD garantiza exclusión de intervalos incompatibles para profesional, animal y recursos. Un sobreturno autorizado no elimina RN-01, RN-02 ni RN-07.
7. Se persiste el turno Confirmado y su auditoría; se guarda el resultado idempotente.
8. El cliente recibe constancia y la agenda profesional incorpora el turno. No se crea cargo clínico ni historia clínica a partir de la sola reserva.

### Alternativas y excepciones

| Caso | Comportamiento observable |
|---|---|
| Visitante o sesión vencida | Acceso/registro, verificación cuando corresponda y reanudación de intención; disponibilidad se recalcula. No se conserva un bloqueo de agenda mientras el usuario se autentica. |
| Sin animales registrados | Continuar por alta de animal y retomar la reserva; medición de pasos pendiente en P-RC-01. |
| Animal ajeno, cuenta no verificada o profesional inhabilitado | Rechazar la operación con mensaje apropiado sin exponer datos ajenos. |
| Solapamiento concurrente | Una reserva gana; la otra recibe conflicto y horarios actualizados. Se conservan las selecciones compatibles. |
| Ausencias por encima del umbral | Aplicar RN-08: informar que debe agendar por mostrador. Se conserva su clasificación canónica de Advertencia; su redacción impide autoagendar. |
| Deuda por encima del umbral | Aviso conforme a RN-36; no inventar un bloqueo de urgencia. |
| Seña exigida para cirugía | Ruta Pendiente, confirmación de seña y liberación de capacidad pendientes de especificar en P-RC-02. No afirmar que la cirugía está confirmada antes de cumplir RN-35. |
| Reprogramación | Conservar identificador y trazabilidad del cambio; validar nuevo intervalo y sustituirlo atómicamente. Si falla, conservar el anterior. No reprogramar un turno En curso. |
| Respuesta perdida | Consultar/reintentar la misma operación; devolver el turno ya creado después de verificar autorización. |

**Trazabilidad:** RF-CLI-02, RF-CLI-03, RF-CLI-04, RF-REC-01; HU-CLI-02/HU-CLI-03; RN-01 a RN-09, RN-11, RN-18, RN-35, RN-36, RN-38; RNF-CON-01, RNF-CON-02, RNF-CON-05, RNF-CON-07, RNF-CON-09, RNF-REN-02, RNF-USA-01, RNF-SEG-05.

## 4. Estados y transiciones propuestos

### 4.1 Turno

`En curso` es el nombre conservado de RN-10; puede mostrarse como “En consulta” en la interfaz, pero no se modelan dos estados distintos por esa diferencia de texto.

| Origen | Acción y actor | Destino | Condición/efecto |
|---|---|---|---|
| Sin turno | Reservar, Cliente o Recepción | Confirmado | Reserva ordinaria aceptada y persistida sin solapamientos. |
| Sin turno | Solicitar cita condicionada | Pendiente | Solo variante con condición previa explícita; definición de ocupación/caducidad pendiente. |
| Pendiente | Cumplir condición y confirmar, actor autorizado | Confirmado | Revalidar reglas y condición (p. ej., seña). No iniciar atención desde Pendiente. |
| Confirmado | Registrar llegada, Recepción | En espera | Registrar hora real de llegada; aún no existe acto clínico firmado. |
| En espera | Iniciar atención, Veterinario asignado | En curso | Verificar consentimiento previo si el procedimiento lo requiere; crear una sola consulta Borrador de forma atómica con la transición. |
| En curso | Cerrar y firmar, Veterinario autorizado | Finalizado | Misma transacción de firma, historia, consumos, cargos y correo pendiente. |
| Confirmado/Pendiente | Cancelar, Cliente autorizado o Recepción | Cancelado | Cliente respeta RN-06; fuera de ventana interviene la clínica. Liberar capacidad que realmente estuviera ocupada. |
| En espera | Retiro antes de atención, Recepción | Cancelado | Motivo y trazabilidad; solo si no inició la consulta ni hay consumos reales. |
| Confirmado | Registrar ausencia, Recepción | Ausente | Hora y tolerancia configuradas cumplidas, sin llegada ni atención; no marcar ausencia automática por error de red. |
| Confirmado | Reprogramar, Cliente o Recepción | Confirmado | Intervalo sustituido atómicamente; reglas y política se revalidan. |

Finalizado, Cancelado y Ausente son terminales en este recorrido. Una reconsulta crea otro turno. No hay transición directa Pendiente→Finalizado ni Cancelado→En curso. Correcciones administrativas de estados terminales requieren otro procedimiento trazable por definir; no se habilita editar libremente la columna Estado.

### 4.2 Consulta y cargo

| Entidad | Transición | Consecuencia |
|---|---|---|
| Consulta | No existe → Borrador | Iniciar desde el turno En espera; vínculo único a ese turno y autor inicial. |
| Consulta | Borrador → Borrador | Guardar con versión esperada; conflictos de edición no sobrescriben silenciosamente. |
| Consulta | Borrador → Firmada | Cierre atómico; firma, fecha y contenido histórico inmutable. |
| Registro firmado | Agregar enmienda | Nuevo registro con autor, fecha y referencia al original; original permanece Firmado e intacto. |
| Cargo | No existe → Pendiente de cobro | Se genera al cierre de la prestación, una vez por origen. |
| Cargo | Pendiente → Parcialmente pagado/Pagado | Solo mediante cobro validado y trazable en el futuro CU de Recepción; no cambia el estado clínico. |

Una interrupción de la atención conserva el borrador y las reservas/consumos reales para resolución por el personal. No se convierte automáticamente en Cancelada ni se borra evidencia. Definir interrupciones definitivas en P-RC-04 antes de implementar.

## 5. Matriz de permisos del recorrido

Propuesta de mínimo acceso, aplicada en la API por rol y relación con cada registro. Varios roles de una misma persona se evalúan explícitamente; Administrador no hereda competencias veterinarias.

| Acción | Cliente | Veterinario | Recepcionista | Administrador |
|---|---|---|---|---|
| Crear/modificar turno | Animal propio; cuenta verificada y política | Consultar su agenda; gestión adicional por definir | En nombre del cliente; política y trazabilidad | Sobreturno según RN-09; otros permisos operativos por definir |
| Registrar llegada/ausencia | No | Consulta de estado | Sí | Solo si posee además permiso de Recepción |
| Leer historia clínica completa | Solo vista autorizada de su animal | Asignado al caso o acceso clínico expresamente autorizado; auditado | Datos administrativos mínimos; sin historia completa por defecto | Sin acceso clínico por el solo rol administrativo |
| Crear/editar borrador clínico | No | Asignado y habilitado; autor o delegado autorizado | No | No, salvo rol veterinario y condiciones clínicas |
| Firmar/cerrar/enmendar | No | Matrícula vigente y autorización sobre la consulta | No | No, salvo rol veterinario y condiciones clínicas |
| Registrar insumos de la atención | No | En su consulta autorizada | No | No como acto clínico; sí inventario según su rol |
| Ajustar inventario | No | No por el solo rol veterinario | No | Sí, con motivo escrito, RN-31 |
| Generar cargo al cierre | Sin acción manual | Dispara la operación; calcula el dominio | Consulta el resultado para cobrar | Configura tarifas, sin alterar el registro clínico |
| Registrar cobro/comprobante | Pago online fuera de base | No por el solo rol veterinario | Sí, sobre prestaciones finalizadas | Solo con permiso de cobro; no asumido por jerarquía |
| Leer en móvil el resultado firmado | Animal bajo responsabilidad vigente | Caso autorizado | Datos de gestión, no texto clínico | Según permiso específico |

La consulta de historia por personal autorizado registra lector, animal, momento, acción y origen sin copiar texto clínico a logs. Acceder al expediente de otro paciente o adivinar un identificador no evita RN-18/RNF-SEG-05. La reasignación de un caso y el alcance de lectura de colegas se resuelven en P-RC-05.

## 6. CU-VET-01 — Realizar consulta

| Campo | Especificación |
|---|---|
| Actor principal | Veterinario asignado, autenticado y habilitado. |
| Actores secundarios | Cliente, Recepcionista, servicio externo de correo. La API y la BD son componentes internos del sistema. |
| Objetivo | Dejar una atención clínica firmada e histórica, con sus consumos y cargos consistentes. |
| Disparador | Selecciona “Iniciar atención” en un turno cuyo paciente llegó. |
| Precondiciones | Para iniciar: turno En espera, animal y responsable identificados, profesional autorizado, configuración y tarifas vigentes. Para retomar: turno En curso y consulta Borrador autorizada; se recupera sin crear otra. |
| Postcondición de éxito | Consulta Firmada, turno Finalizado, registro visible en historia autorizada, consumos/movimientos confirmados, cargos pendientes de cobro y resumen encolado. |
| Garantía mínima | Fallo antes del commit conserva el estado previo: consulta Borrador, turno En curso, sin efectos parciales del cierre. Se conservan borrador y reservas previas; no se simula revertir un uso físico real. |
| RF primarios | RF-VET-01, RF-VET-02, RF-VET-03, RF-VET-06, RF-VET-07, RF-VET-12, RF-VET-13. |
| RF relacionados | RF-CLI-05, RF-CLI-08, RF-CLI-10, RF-REC-02, RF-REC-05, RF-ADM-02, RF-ADM-05, RF-ADM-06. |

### 6.1 Flujo principal

1. Recepción registra llegada y el turno pasa a En espera.
2. El Veterinario abre el turno; la API verifica asignación, permisos y vigencia profesional.
3. Al iniciar atención, la API verifica el consentimiento previo si el procedimiento lo requiere y crea o recupera la única consulta del turno. En la creación, turno En curso y consulta Borrador se guardan juntos. Si durante la atención se propone un procedimiento sujeto a consentimiento, se valida antes de autorizar ese procedimiento.
4. La API registra la lectura de la historia y presenta antecedentes, alergias y contraindicaciones destacadas.
5. El Veterinario registra motivo, anamnesis, examen físico, signos vitales, peso, diagnóstico y tratamiento. Los campos/mediciones aplicables dependen de especie y contexto; la obligatoriedad precisa se cierra en P-RC-06. Para firmar siempre se exige al menos un diagnóstico presuntivo o definitivo (RN-20).
6. Puede guardar avances con control de versión. El Cliente aún no recibe el borrador como atención finalizada.
7. Adjunta los estudios pertinentes. La API valida archivo y autorización; almacena binarios protegidos y conserva metadatos. La carga externa se completa antes del cierre, sin mantener una transacción abierta mientras sube el archivo.
8. Registra prestaciones e insumos. La API calcula cantidades, selecciona lotes válidos por FEFO y reserva existencias disponibles. El uso real queda identificado por lote, cantidad y momento. Las vacunas se completan en H3 con sus validaciones específicas.
9. El sistema prepara una revisión con contenido clínico, consumos y cargos calculados por tarifa vigente en cada prestación. El Veterinario confirma la firma; las advertencias clínicas aplicables se confirman antes del cierre.
10. La API revalida autorización, matrícula, versión, estado, diagnóstico, archivos listos, consumos, tarifas y consentimientos cuando el procedimiento los exige.
11. En una transacción breve firma el registro, confirma movimientos y saldos, genera alertas de stock mínimo si corresponde, crea cargos, finaliza el turno, registra auditoría y encola el resumen. Si falla cualquier parte, revierte todos esos cambios.
12. Tras el commit, la interfaz muestra “Consulta finalizada” y “Cobro pendiente” como información separada. El Cliente puede consultar la historia según sus permisos. El servicio de correo envía el resumen fuera de la transacción.
13. Recepción toma los cargos para el caso de uso de cobro. Un pago fallido no reabre ni elimina una atención firmada.

### 6.2 Alternativas, excepciones y recuperación

| ID | Situación | Resultado requerido |
|---|---|---|
| A-RC-01 | Dos dispositivos inician el mismo turno | Se obtiene una única consulta; el segundo recupera la autorizada o recibe conflicto. |
| A-RC-02 | Edición concurrente | Rechazar la versión vieja; comparar/recargar sin perder silenciosamente la versión ya guardada. |
| A-RC-03 | Falta diagnóstico | Informar el campo; permanecer Borrador/En curso. Ningún cargo o movimiento final del cierre. |
| A-RC-04 | Matrícula vencida o permiso revocado | Rechazar firma incluso si el botón estaba visible. Reasignación por circuito autorizado pendiente. |
| A-RC-05 | Stock insuficiente al reservar | No aceptar consumo ni saldo negativo; conservar la consulta y solicitar revisión del insumo/cantidad. |
| A-RC-06 | Lote vencido al registrar aplicación/uso | Impedir nueva utilización y ejecutar la política de baja trazable. No sustituir silenciosamente el lote realmente usado por otro. |
| A-RC-07 | Tarifa inexistente o configuración de facturación incompleta | Impedir cierre parcial; guardar borrador e informar a quien administra tarifas. No generar importe cero como sustituto. |
| A-RC-08 | Falla al crear cargo o encolar correo | Rollback del cierre entero; mantienen borrador y reservas previas. |
| A-RC-09 | Correo no disponible después del commit | Consulta permanece firmada, cargo válido y mensaje pendiente de reintento. Mostrar estado del envío sin pedir otra firma. |
| A-RC-10 | Red cortada después de confirmar | Reconsultar/reintentar con misma clave; no duplicar firma, consumos, cargos ni mensaje pendiente. |
| A-RC-11 | Corrección después de firma | Registrar enmienda con motivo, fecha y autor; preservar original. Efectos financieros se corrigen con su operación propia, nunca editando la firma. |
| A-RC-12 | Atención interrumpida con insumos usados | Conservar uso/reservas para conciliación; no liberar ni cancelar automáticamente como si no hubiera atención. |
| A-RC-13 | Procedimiento requiere consentimiento y falta | Bloquear la transición/acto afectado conforme a RN-10 y RF-CLI-11; no reemplazar por checkbox genérico. |
| A-RC-14 | Cliente intenta leer borrador o animal ajeno | Denegar lectura no autorizada; no exponer contenido clínico ni adjuntos. |

### 6.3 Datos y trazabilidad

Auditar inicio, guardados relevantes, firma, enmiendas, cambios de estado, consumos, cargos y lecturas del personal. Identificar actor, entidad, fecha/hora y origen. La auditoría clínica/contable protegida se distingue del log técnico: este último no incluye diagnósticos, credenciales ni tokens (RNF-SEG-10/RNF-SEG-11).

**Reglas principales:** RN-11 a RN-20, RN-26 a RN-34, RN-39, RN-40; RN-10/RN-43 si se realiza un procedimiento sujeto a consentimiento. RN-15/RN-16/RN-19 al incorporar vacunación y RN-17 cuando se emitan recetas.

**Calidad principal:** RNF-SEG-02, RNF-SEG-05, RNF-SEG-07, RNF-SEG-08, RNF-SEG-10, RNF-SEG-11; RNF-CON-03 a RNF-CON-08; RNF-BD-02, RNF-BD-03, RNF-BD-05 a RNF-BD-10; RNF-REN-03, RNF-REN-04; RNF-USA-02 a RNF-USA-06; RNF-MAN-01, RNF-MAN-02, RNF-MAN-05; RNF-LEG-02, RNF-LEG-04, RNF-LEG-06.

## 7. Modelo de dominio inicial

Modelo conceptual para discutir el futuro DER; MySQL fue elegido en DT-BD-01. Los tipos físicos, índices concretos, migraciones y estrategia de intervalos se decidirán antes de implementar agenda real.

| Concepto | Relaciones y datos esenciales | Restricción |
|---|---|---|
| Cliente–Responsabilidad–Animal | Cliente y Animal N:M con fechas de vigencia y permisos; especie/categoría configurable. | La autorización consulta la relación vigente, no un ID enviado por la pantalla. |
| Veterinario | Cuenta, matrícula/vigencia, agenda y habilitaciones. | Firma con habilitación vigente y autorización del caso. |
| Turno | Animal, profesional, tipo, inicio/fin, recursos, estado, versión y auditoría. | Intervalos no superpuestos y transición controlada. |
| Consulta | Animal, turno de origen, profesional, datos clínicos, versión y firma. Un turno programado tiene 0..1 consulta. | Animal acumula 0..N consultas; otras fuentes de atención requerirán modelar su origen, sin turno artificial obligatorio. |
| Historia clínica / Registro clínico | Historia reúne consultas firmadas, otras actuaciones y enmiendas del mismo animal. | No se reemplaza “la historia” con el texto de la última consulta; un origen firmado no se duplica al publicar. |
| Diagnóstico / Tratamiento | 0..N en borrador, diagnóstico presuntivo/definitivo y plan indicado. | Al menos un diagnóstico al firmar. |
| Enmienda | Referencia al original, texto, motivo, autor, fecha y firma. | Nuevo registro histórico, nunca actualización destructiva del original. |
| Adjunto | Consulta/animal, ubicación privada, tipo, tamaño, huella y estado validado. | Binario fuera de BD; acceso firmado temporal y autorización vigente. |
| Producto–Lote | Producto 1:N Lote; vencimiento y saldo por lote. | Unidad de consumo explícita; no cantidades negativas ni uso vencido. |
| Reserva de stock | Consulta, lote, cantidad y estado Reservada/UtilizadaPendienteCierre/Consolidada/Liberada. | Disponibilidad = saldo contable menos reservas activas; liberar solo lo no utilizado. |
| Movimiento de inventario | Producto/lote, cantidad, origen tipado, referencia a consulta o compra/ajuste/baja. | Una consolidación por consumo; ajuste solo Administrador con motivo. |
| Prestación y Tarifa | Servicio/insumo facturable, fecha de realización, cantidad y versión de tarifa aplicable. | Guardar valores históricos usados para calcular cada cargo. |
| Cargo y detalle | Consulta/origen, responsable económico identificado, líneas, cantidades e importes. | No duplicar origen facturable; no confundir deuda con estado clínico. |
| Pago / aplicación / Comprobante | Cobro vinculado a cargos; permite aplicaciones parciales; comprobante y nota de crédito. | CU de cobro posterior detallará secuencia; comprobante solo después de finalización. |
| Mensaje pendiente | Identificador de evento, destinatario autorizado, referencia a resumen/versionado, estado e intentos. | Creado con el cierre; datos protegidos y envío fuera de transacción. |

### Stock durante una consulta

Propuesta RC-D05: seleccionar el lote por FEFO entre lotes válidos con disponibilidad y reservarlo en una operación breve y atómica. Confirmar el uso registra el lote efectivamente usado. Al cerrar se descuenta ese saldo y se consolida la reserva en la misma transacción. No se selecciona otro lote al cerrar para aparentar que fue el utilizado.

El vencimiento se valida en el momento de autorizar/registrar el uso. Una aplicación registrada válidamente antes del vencimiento no se convierte en una nueva aplicación vencida por cerrar más tarde. Las reservas todavía no utilizadas sí deben revalidarse o rechazarse. Ajustes y bajas por vencimiento deben respetar los usos pendientes de consolidar y conciliarlos sin descontar dos veces; el detalle operativo es parte de P-RC-03.

### Cargos y tarifas

Se genera cargo por las prestaciones efectivamente realizadas: inicialmente consulta e insumos facturables; posteriormente procedimiento, internación o asistencia por su origen. La política indicará si un insumo está incluido en la tarifa de consulta para evitar doble cargo. La fecha relevante es la de prestación, no la de reserva ni la del reintento del cierre.

Mantener importe unitario, cantidad, moneda, fecha y versión de tarifa utilizados. El responsable económico se identifica para la prestación y se conserva históricamente; una posterior transferencia del animal no debe trasladar deuda silenciosamente. La política de selección del responsable, redondeo e inclusiones queda en P-RC-07.

## 8. Pseudocódigo de las operaciones críticas

Los nombres describen responsabilidades, no clases o endpoints definitivos. Todas las autorizaciones se verifican en servidor. Las primitivas de exclusión deben cubrir también la ausencia inicial de filas y los intervalos variables; una consulta previa de disponibilidad por sí sola es insuficiente.

### 8.1 Reservar

```text
reservarTurno(actor, entrada, clave):
    autenticar(actor)
    validarCuentaYResponsabilidad(actor, entrada.animal)
    validarFormato(entrada)
    iniciarTransaccionBreve()
    tomarClaveUnica(actor, "reservar", clave, huella(entrada))
    si existeResultadoCompatible:
        verificarAccesoActualAlResultado(actor)
        devolverResultadoPersistidoSinNuevaReserva()
    obtenerConfiguracionVigenteYDuracionDesdeServidor()
    validarAusenciasDeudaYCondicionesDeReserva()
    protegerIntervalosAnimalProfesionalRecursosEnBD()
    validarJornadaAntelacionHorizonteYNoSolapamiento()
    crearTurnoConfirmadoParaReservaOrdinaria()
    registrarAuditoriaYResultadoIdempotente()
    confirmarTransaccion()
    devolverConstancia()
    anteConflicto: revertir; devolverHorariosActualizados()
```

Una clave reutilizada con datos distintos se rechaza; no retorna el resultado de otra intención. No se llama a servicios externos ni se espera confirmación humana dentro de la transacción.

### 8.2 Iniciar y guardar

```text
iniciarConsulta(actor, turnoId, versionTurno, clave):
    autenticarYAutorizarVeterinario(actor, turnoId)
    enTransaccionBreve:
        tomarClaveUnicaYResolverReintento()
        cargarTurnoConControlDeConcurrencia()
        si yaTieneConsulta:
            devolverConsultaExistenteAutorizada()
        exigirEstado("En espera")
        validarVersionYAsignacion()
        validarConsentimientoPrevioSiElProcedimientoLoRequiere()
        crearConsultaBorradorConVinculoUnico(turnoId)
        cambiarTurno("En curso")
        auditarYGuardarResultado()

guardarBorrador(actor, consultaId, versionEsperada, datos):
    autenticarYAutorizar(actor, consultaId)
    exigirBorradorYVersionActual(versionEsperada)
    validarDatosAplicables(datos)
    guardarSinFirmarNiGenerarCargos()
    devolverNuevaVersion()
```

### 8.3 Cerrar y firmar

```text
cerrarConsulta(actor, consultaId, versionEsperada, clave, confirmaciones):
    autenticar(actor)
    autorizarAccionClinica(actor, consultaId)
    huellaSolicitud = calcularHuella(consultaId, versionEsperada, confirmaciones)
    iniciarTransaccionBreve()
    tomarClaveUnica(actor, "cerrar", clave, huellaSolicitud)
    si existeResultadoCompatible:
        verificarAccesoActualAlResultado(actor)
        devolverResultadoSinRepetirEfectos()
    cargarConsultaTurnoConsumosConControlDeConcurrencia()
    validarBorradorVersionYTurnoEnCurso()
    validarAsignacionYMatriculaVigente()
    exigirAlMenosUnDiagnostico()
    validarDatosClinicosAdjuntosListosYConsentimientosAplicables()
    validarAdvertenciasYConfirmacionesRegistradas()
    validarUsosLotesYReservasPropias()
    prestaciones = obtenerPrestacionesRealizadas()
    cargos = calcularConTarifasDeFechaPrestacion(prestaciones)
    exigirConfiguracionCompletaSinDobleFacturacion()
    para cada consumoRegistrado:
        consolidarUnaVezReservaYDescontarSaldoSinNegativo()
        crearMovimientoConOrigenConsulta()
        si cruzaMinimoConfigurado:
            crearAlertaPersistidaEnEstaTransaccion()
    firmarYPublicarRegistroHistoricoUnaSolaVez()
    crearCargosUnicosPorOrigen(cargos)
    cambiarTurno("Finalizado")
    crearMensajePendienteUnicoParaResumen()
    registrarAuditoriaYResultadoIdempotente()
    confirmarTransaccion()
    devolverConsultaFirmadaYCargosPendientes()
    anteCualquierFalloAntesDelCommit:
        revertirTransaccion()
        conservarBorradorYReservasPrevias()
        devolverErrorRecuperableSinDetallesTecnicos()

procesarMensajePendiente(mensaje):
    verificarDestinatarioYContenidoAutorizados()
    enviarFueraDeTransaccionClinica(conIdentificadorEstable)
    registrarResultadoOProgramarReintentoLimitado()
```

La persistencia garantiza un único mensaje lógico. La entrega externa de correo puede ser “al menos una vez”; cuando el proveedor no permita deduplicar, documentar el riesgo de repetición y no prometer entrega exactamente una vez. Un fallo externo nunca dispara un segundo cierre.

## 9. Ejemplo de demostración

Datos totalmente ficticios, expresados en UYU y elegidos solo para comprobar el recorrido; no son tarifas o pautas clínicas reales.

1. Ana tiene responsabilidad vigente sobre Luna y reserva una consulta ordinaria con un profesional configurado. Obtiene el turno T-001 Confirmado.
2. Recepción registra llegada. El Veterinario inicia atención: T-001 queda En curso y C-001 Borrador.
3. Registra un diagnóstico de demostración, tratamiento y una unidad del insumo ficticio P-01. Lote A vence antes que B y tiene saldo 2; se reserva y registra uso de 1 unidad de A por FEFO. Disponible A: 1; saldo contable A antes de cierre: 2.
4. La tarifa vigente de consulta es 1.200 y el insumo facturable no incluido cuesta 100. Cerrar produce C-001 Firmada, T-001 Finalizado, saldo A=1 y un cargo de 1.300 pendiente de cobro. Si mínimo=2, se genera la alerta al cruzarlo. Se encola un resumen.
5. Con correo caído, la consulta y el cargo permanecen válidos. Cuando el servicio vuelve, se reintenta ese resumen.
6. Recepción registra un cobro de 1.300 y su comprobante mediante el caso posterior. El estado clínico no cambia por cobrar.

Prueba de fallo: desde una copia independiente de los datos anteriores al cierre, provocar error al crear el cargo. El resultado debe conservar C-001 Borrador, T-001 En curso, saldo A=2, reserva utilizada de 1 y ningún nuevo movimiento, cargo, alerta o mensaje de ese cierre. Disponible A sigue siendo 1; no se libera un insumo realmente usado.

## 10. Casos de aceptación del recorrido

Especificación de pruebas futuras; estas pruebas aún no fueron ejecutadas sobre software.

| ID | Escenario verificable | Resultado | Referencia |
|---|---|---|---|
| CA-RC-01 | Dos reservas simultáneas incompatibles | Exactamente una confirmada, otra con conflicto; cubrir profesional, animal y recurso, incluidos intervalos variables. | CP-01, CP-02, CP-03; RN-01, RN-02, RN-07 |
| CA-RC-02 | Dos inicios del mismo turno | Una consulta enlazada y una transición a En curso. | RC-D04; RNF-CON-05 |
| CA-RC-03 | Cliente ajeno, Recepción o Administrador intentan firmar | Denegación sin modificar clínica, stock ni cargos. | RN-13, RN-18; RNF-SEG-05 |
| CA-RC-04 | Cierre sin diagnóstico | Borrador preservado, sin efectos del cierre. | RN-20 |
| CA-RC-05 | Dos consultas reservan la última unidad | Solo una reserva exitosa; disponibilidad/saldo nunca negativos. | CP-05; RN-26 |
| CA-RC-06 | Lotes válidos con distinto vencimiento | Reserva FEFO; lote vencido nunca se ofrece para nuevo uso. | CP-06, CP-12; RN-15, RN-27, RN-29 |
| CA-RC-07 | Falla después del descuento y antes del cargo | Rollback de todos los efectos del cierre, conservando reservas previas. | CP-08; RNF-CON-03 |
| CA-RC-08 | Cruce de stock mínimo | Alerta persistida en la misma transacción; tampoco persiste si el cierre revierte. | CP-04; RN-28 |
| CA-RC-09 | Repetir cierre por respuesta perdida | Un registro firmado, consumos/cargos/mensaje sin duplicados. | RC-D04; análisis de idempotencia |
| CA-RC-10 | Correo caído después de cierre | Firma y cargo válidos, mensaje reintentable. | CP-09; RNF-CON-04 |
| CA-RC-11 | Intento de editar firma y luego enmienda | Edición rechazada; enmienda vinculada, original intacto. | CP-11; RN-12 |
| CA-RC-12 | Tarifa cambia entre reserva y prestación | Cargo usa tarifa de prestación; reintento no la recalcula a la fecha actual. | RN-33 |
| CA-RC-13 | Comprobante con consulta abierta | Rechazado; cerrar no registra pago automáticamente. | CP-07; RN-32 |
| CA-RC-14 | Dos ediciones del borrador con igual versión inicial | Solo una actualiza; la otra recibe conflicto de versión. | RNF-CON-05 |
| CA-RC-15 | Personal lee historia y Cliente intenta adjunto ajeno/vencido | Lectura autorizada auditada; acceso ajeno/vencido denegado. | CP-10, CP-15; RN-40; RNF-SEG-07 |
| CA-RC-16 | Reprogramación falla al adquirir nuevo horario | Turno conserva horario original y no se duplica. | RF-CLI-03; RNF-CON-01 |
| CA-RC-17 | Cirugía sin consentimiento | No comienza la transición clínica afectada. | CP-13; RN-10 |
| CA-RC-18 | Clave idempotente reutilizada con otro contenido | Error de conflicto; no ejecutar ni devolver resultado de otra intención. | RC-D04 |
| CA-RC-19 | Atención interrumpida con insumo usado | Mantener reserva/uso para conciliación; no devolver automáticamente a disponible. | RC-D05; RN-26, RN-30 |

## 11. Decisiones acotadas pendientes

| ID | Clasificación | Pendiente y propuesta de trabajo |
|---|---|---|
| P-RC-01 | Diseño/UX | Precisar medición de RNF-USA-01: cuatro pasos de reserva; registro/alta previa requieren acordar cómo se contabilizan “desde el ingreso”. |
| P-RC-02 | Pseudocódigo/comportamiento | Definir estado Pendiente por seña: vigencia, ocupación de agenda, confirmación y vencimiento; cálculo de espera estimada, tolerancia de ausencia, límites de cancelación y sobreturnos sin violar otras RN. |
| P-RC-03 | Decisión técnica y pseudocódigo | Validar reservas/uso/consolidación de stock de RC-D05, unidades/fracciones, conciliación de vencimientos y gestión de reservas no usadas. Resolver antes de implementar consumo. |
| P-RC-04 | Pseudocódigo/comportamiento | Cierre de atención interrumpida, errores de admisión y corrección trazable de estados terminales. No inventar diagnóstico para poder cerrar. |
| P-RC-05 | Requerimiento no funcional: autorización | Alcance de lectura entre colegas, reasignación, reemplazo del autor y permisos de cobro adicionales. |
| P-RC-06 | Pseudocódigo y consentimiento | Campos clínicos obligatorios por especie, confirmaciones, mecanismo concreto de firma y consentimiento; no elegir validez jurídica ni proveedor por omisión. |
| P-RC-07 | Pseudocódigo/comportamiento | Responsable económico, tarifas incluidas/adicionales, unidad, redondeo, moneda y política de correcciones contables. |
| P-RC-08 | Decisión técnica | **Parcial:** MySQL confirmado por Eric (DT-BD-01). Diego debe definir versión/proveedor y estrategia de exclusión de intervalos variables, índices/aislamiento y reintentos. |

Las filas anteriores delimitan lo necesario para cerrar este recorrido; no eliminan los pendientes globales de registro y asistencia. Próximo documento funcional: CU de Recepción “Cobrar consulta”, con cobro, aplicación a cargos y comprobantes, seguido del ingreso/alta de internación.
