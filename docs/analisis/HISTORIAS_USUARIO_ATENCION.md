# Historias de usuario — Recorrido de atención

Versión: 0.1

Fecha: 2026-10-02

Estado: Borrador derivado de requisitos existentes.

Complementa [las historias de Visitante y Cliente](HISTORIAS_USUARIO_VISITANTE_CLIENTE.md) y el caso [CU-VET-01 — Realizar consulta](FLUJO_RESERVA_CONSULTA.md). Los identificadores de historia no implican correspondencia numérica automática con los RF: se declara cada vínculo. Se conservan los tipos y prioridades de la matriz.

## 1. HU-VET-01 — Preparar la atención

Como Veterinario, quiero consultar mi agenda y los antecedentes del animal asignado para iniciar la atención con su información relevante.

- Dado mi usuario, cuando abro la agenda, veo mis turnos del día/semana y su estado.
- Dado un caso autorizado, cuando consulto su historia, veo antecedentes, alergias y contraindicaciones destacadas y se registra mi lectura en auditoría.
- Dado un turno En espera, cuando inicio atención, se crea una sola consulta Borrador y el turno pasa a En curso.
- Dado un reintento o apertura concurrente, se recupera la consulta autorizada existente sin duplicarla.

Trazabilidad: RF-VET-01, RF-VET-07; RN-11, RN-40; RNF-SEG-05, RNF-CON-05. Must; fase canónica 3; hitos H1/H2 según dependencia. CU-VET-01, CA-RC-02, CA-RC-15. Permisos y transiciones: RC-D02/RC-D03, sujetos a P-RC-05.

## 2. HU-VET-02 — Documentar la consulta

Como Veterinario, quiero registrar los hallazgos, diagnóstico y tratamiento en un borrador recuperable para documentar la atención antes de firmarla.

- Dada una consulta autorizada, puedo registrar motivo, anamnesis, examen físico, signos vitales, peso, diagnóstico y tratamiento según los campos aplicables a la especie.
- Cuando guardo avances, el sistema conserva el borrador sin generar cargos, publicar una atención finalizada ni enviar el resumen.
- Si otra sesión modificó el borrador, mi versión antigua produce un conflicto comprensible y no sobrescribe la información guardada.
- Si intento cerrar sin diagnóstico presuntivo o definitivo, la operación se rechaza y mis datos guardados permanecen.

Trazabilidad: RF-VET-02; RN-20; RNF-CON-05, RNF-USA-02. Must; fase canónica 3; H2. CU-VET-01, CA-RC-04, CA-RC-14. Pendiente de obligatoriedad clínica por especie: P-RC-06.

## 3. HU-VET-03 — Adjuntar estudios

Como Veterinario, quiero incorporar estudios y fotos de evolución a la consulta para conservar la evidencia clínica vinculada al animal.

- Cuando cargo un archivo, la API valida autorización, tipo, tamaño y contenido real; un rechazo explica cómo corregirlo.
- Cuando el archivo queda listo, se conserva su referencia, fecha, tamaño, tipo y huella en la ficha; el binario permanece protegido fuera de la base.
- Un cliente responsable puede abrir los adjuntos autorizados mediante acceso temporal; no obtiene archivos de otro animal ni usa un enlace vencido.
- Una carga incompleta se señala antes del cierre. No se presenta como adjunto disponible ni mantiene abierta una transacción clínica mientras se transfiere.

Trazabilidad: RF-VET-03, RF-CLI-08; RN-18; RNF-SEG-07, RNF-SEG-08, RNF-BD-10, RNF-CON-07. Must; fase canónica 3; H2. CU-VET-01, CA-RC-15.

## 4. HU-VET-04 — Registrar insumos utilizados

Como Veterinario, quiero registrar los insumos y lotes utilizados para que la historia, las existencias y los cargos reflejen la misma atención.

- Dadas existencias válidas, al seleccionar un consumo se prioriza FEFO y se reserva únicamente cantidad disponible según RC-D05.
- Si dos consultas intentan reservar la última unidad, solo una operación tiene éxito.
- Al confirmar uso se conserva el lote efectivamente utilizado; no se sustituye por otro al cerrar.
- Al cerrar válidamente se consolida una vez el consumo y, si cruza el mínimo, se genera la alerta en la misma transacción.
- Si el cierre falla, no quedan movimientos/cargos parciales y las reservas previas permanecen; un uso real no se libera automáticamente.

Trazabilidad: RF-VET-06, RF-ADM-05, RF-ADM-06; RN-26 a RN-31; RNF-CON-03. Must; fase canónica 3 para consumo y 4 para tablero; H2 operativo. CU-VET-01, CA-RC-05 a CA-RC-08, CA-RC-19. RC-D05 es propuesta técnica pendiente de P-RC-03.

## 5. HU-VET-05 — Firmar y cerrar la consulta

Como Veterinario, quiero revisar y firmar la atención para dejar un registro definitivo y habilitar su comunicación y cobro.

- Dadas asignación y matrícula vigentes, diagnóstico y datos válidos, al confirmar se firman historia y consumos, se generan cargos, se finaliza el turno y se encola el resumen de manera atómica.
- Si falla una escritura, todo el cierre revierte y la consulta permanece en borrador con sus datos previos.
- Si la respuesta se pierde y reintento la operación, no se duplican firma, consumos, cargos ni mensaje lógico.
- Si falla el servicio externo de correo tras el cierre, la consulta queda firmada y el envío se reintenta por separado.
- El resultado informa que la atención finalizó y que sus cargos están pendientes de cobro; la firma no registra un pago.

Trazabilidad: RF-VET-12, RF-CLI-10; RN-11, RN-12, RN-13, RN-20, RN-32, RN-33; RNF-CON-03, RNF-CON-04. Must; fase canónica 3; H2. CU-VET-01, CA-RC-03, CA-RC-04, CA-RC-07 a CA-RC-10, CA-RC-12, CA-RC-13.

## 6. HU-VET-06 — Rectificar mediante enmienda

Como Veterinario autorizado, quiero agregar una enmienda fechada y firmada para corregir un registro conservando su contenido original y trazabilidad.

- Un intento de editar o borrar el contenido firmado es rechazado.
- Al registrar una enmienda válida se crea otra entrada con autor, fecha, motivo y referencia al original.
- La historia permite identificar qué se rectificó y consultar el original intacto.
- La enmienda no recalcula ni sobrescribe cargos, pagos o movimientos pasados; una corrección de esos efectos usa su operación trazable correspondiente.

Trazabilidad: RF-VET-13; RN-12, RN-13, RN-14, RN-30, RN-34. Must; fase canónica 4; H2 operativo por dependencia de la firma. CU-VET-01, CA-RC-11. Autoría/delegación: P-RC-05.

## 7. HU-REC-01 — Registrar llegada y espera

Como Recepcionista, quiero registrar la llegada del animal y consultar la sala de espera para coordinar el inicio de su atención.

- Dado un turno Confirmado válido, registro llegada y hora real; el turno pasa a En espera.
- La sala muestra orden y tiempo estimado con el criterio que se defina, distinguiendo estimación de compromiso de atención.
- La operación no inicia por sí sola el acto veterinario ni permite editar la historia clínica.
- Una llegada o atención ya registrada impide marcar simultáneamente ese turno como Ausente sin un procedimiento de corrección trazable.

Trazabilidad: RF-REC-02; RN-11; RNF-SEG-05. Should seleccionado para la base; fase canónica 3; H1. CU-VET-01 como precondición de llegada. Pendientes: cálculo del tiempo de espera, tolerancia de ausencia y correcciones en P-RC-02/P-RC-04.

## 8. Preparación y terminación

Aplican las definiciones de preparada/terminada del documento de historias de Visitante y Cliente. Las historias con decisiones P-RC abiertas todavía no están listas para implementación. Estas siete historias complementan las 35 existentes y no aumentan los 74 RF canónicos.

La evidencia incluirá los casos CA-RC enlazados, permisos de ambos clientes y demostración del recorrido compartiendo una sola API. El cobro y el ingreso/alta de internación tendrán sus propias historias cuando se detalle cada caso.
