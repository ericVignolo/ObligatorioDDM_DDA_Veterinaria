# Matriz de requisitos del sistema veterinario

Fuente canónica: `docs/fuentes/Analisis_Clinica_Veterinaria.docx`, versión 1.0, setiembre de 2026.

Complementos incorporados: `Implementacion_Politicas.txt` y decisiones funcionales sobre alertas y asistencia confirmadas el 2026-09-26. El detalle conductual del nuevo módulo se mantiene en `../analisis/ESPECIFICACION_ALERTAS_ASISTENCIA.md`.

Esta matriz conserva los requerimientos recuperados del documento original. Las decisiones técnicas posteriores sobre Blazor, ASP.NET Core, React Native y Figma prevalecen cuando el documento recomienda un stack diferente.

## Requerimientos funcionales del visitante

| ID | Requerimiento | Prioridad | Fase |
|---|---|---|---|
| RF-VIS-01 | Consultar catálogo de servicios con descripción y precio de referencia. | Must | 2 |
| RF-VIS-02 | Ver equipo profesional, especialidades y horarios de atención de cada veterinario. | Must | 2 |
| RF-VIS-03 | Consultar ubicación con mapa, teléfonos y datos de contacto de la clínica. | Must | 2 |
| RF-VIS-04 | Consultar disponibilidad general de la agenda sin poder reservar, como incentivo al registro. | Should | 3 |
| RF-VIS-05 | Acceder a contenido educativo: calendario de vacunación por especie, cuidados y señales de alarma. | Should | 2 |
| RF-VIS-06 | Ver información de urgencias y acceso directo al teléfono de guardia. | Must | 2 |
| RF-VIS-07 | Iniciar el registro como cliente desde cualquier punto del sitio. | Must | 2 |
| RF-VIS-08 | Consultar la ficha pública mínima de una mascota extraviada mediante su código QR. | Could | 4 |
| RF-VIS-09 | Enviar una consulta mediante un formulario de contacto con protección anti-bot. | Should | 2 |
| RF-VIS-10 | Consultar sin autenticarse la identificación y contacto del responsable, la Política de Privacidad, los Términos y Condiciones y, cuando corresponda, la Política de Cookies vigentes. | Must | 2 |

## Requerimientos funcionales del cliente

| ID | Requerimiento | Prioridad | Fase |
|---|---|---|---|
| RF-CLI-01 | Registrar y editar sus mascotas con datos identificatorios, foto y señas particulares. | Must | 2 |
| RF-CLI-02 | Agendar turno seleccionando tipo de cita, veterinario y un horario del listado de disponibles. | Must | 3 |
| RF-CLI-03 | Cancelar o reprogramar un turno dentro de la ventana de tiempo permitida. | Must | 3 |
| RF-CLI-04 | Consultar el historial completo de turnos con su estado. | Must | 3 |
| RF-CLI-05 | Consultar en modo lectura la historia clínica de cada mascota, ordenada cronológicamente. | Must | 3 |
| RF-CLI-06 | Descargar el carnet sanitario en PDF, con código QR que permite verificar su autenticidad. | Should | 3 |
| RF-CLI-07 | Recibir alertas de refuerzo de vacunas y desparasitaciones próximas a vencer. | Must | 3 |
| RF-CLI-08 | Visualizar y descargar los archivos adjuntos cargados por el veterinario. | Must | 3 |
| RF-CLI-09 | Seguir en tiempo real las novedades publicadas durante la internación de su mascota. | Must | 3 |
| RF-CLI-10 | Recibir por correo el resumen clínico al finalizar cada consulta u operación. | Must | 3 |
| RF-CLI-11 | Leer y firmar digitalmente el consentimiento informado antes de una cirugía o procedimiento con anestesia. | Must | 4 |
| RF-CLI-12 | Consultar su cuenta corriente, ver comprobantes y abonar en línea. | Should | 4 |
| RF-CLI-13 | Autorizar a un segundo responsable sobre una mascota o transferir la titularidad. | Should | 2 |
| RF-CLI-14 | Registrar el peso de la mascota en casa y ver la curva de evolución junto a los pesos medidos en clínica. | Could | 4 |
| RF-CLI-15 | Solicitar la renovación de un tratamiento crónico sin agendar consulta presencial. | Could | 4 |
| RF-CLI-16 | Mantener una conversación de seguimiento postoperatorio con la clínica, con envío de fotos. | Should | 4 |
| RF-CLI-17 | Responder la encuesta de satisfacción posterior a la atención. | Could | 4 |
| RF-CLI-18 | Configurar por qué canal desea recibir cada tipo de notificación. | Should | 3 |
| RF-CLI-19 | Solicitar la exportación o eliminación de sus datos personales. | Should | 4 |
| RF-CLI-20 | Consultar y modificar desde su perfil las preferencias opcionales de privacidad, comunicaciones o seguimiento que admitan revocación. | Should | 3 |
| RF-CLI-21 | Solicitar orientación o asistencia veterinaria a domicilio para uno o varios animales, indicando especie o categoría, cantidad afectada, síntomas, gravedad percibida y modalidad requerida. | Must | 3 |
| RF-CLI-22 | Compartir con consentimiento explícito su ubicación GPS durante una solicitud de asistencia o, alternativamente, ingresar dirección y referencias manuales. | Must | 3 |
| RF-CLI-23 | Consultar el estado de la solicitud y, después de la aceptación de un veterinario, recibir el tiempo estimado de llegada, notificaciones de avance y el recorrido habilitado en el mapa. | Must | 3 |
| RF-CLI-24 | Informar en cualquier momento que uno o varios animales empeoraron y responder la reevaluación solicitada cuando una alerta permanezca pendiente durante 24 horas. | Must | 3 |
| RF-CLI-25 | Conocer y aceptar el costo o el criterio de cálculo de la asistencia antes de confirmarla, salvo las excepciones definidas para no demorar una urgencia de nivel rojo. | Must | 3 |

## Requerimientos funcionales del veterinario

| ID | Requerimiento | Prioridad | Fase |
|---|---|---|---|
| RF-VET-01 | Consultar su agenda del día y de la semana, con el detalle de cada paciente antes de atenderlo. | Must | 3 |
| RF-VET-02 | Registrar la consulta completa: motivo, anamnesis, examen físico, signos vitales, peso, diagnóstico y tratamiento. | Must | 3 |
| RF-VET-03 | Adjuntar radiografías, ecografías, PDF de laboratorio externo y fotos de evolución a la ficha. | Must | 3 |
| RF-VET-04 | Registrar la aplicación de vacunas y antiparasitarios indicando lote, laboratorio y fecha de refuerzo. | Must | 3 |
| RF-VET-05 | Emitir receta digital firmada, con validación contra las alergias registradas del paciente. | Should | 4 |
| RF-VET-06 | Registrar el consumo de insumos durante la atención, con descuento automático del stock. | Must | 3 |
| RF-VET-07 | Ver la historia clínica completa con las alergias y contraindicaciones destacadas permanentemente. | Must | 3 |
| RF-VET-08 | Derivar el caso a un colega o solicitar interconsulta, dejando constancia en la ficha. | Could | 4 |
| RF-VET-09 | Ingresar una mascota a internación, asignarle box y definir el plan terapéutico. | Must | 3 |
| RF-VET-10 | Publicar novedades de internación diferenciando notas internas de actualizaciones visibles al dueño. | Must | 3 |
| RF-VET-11 | Registrar la evolución diaria y otorgar el alta con indicaciones para el domicilio. | Must | 3 |
| RF-VET-12 | Cerrar y firmar la consulta, acción que dispara el envío del resumen al cliente. | Must | 3 |
| RF-VET-13 | Agregar una enmienda a un registro firmado. El texto original nunca se modifica ni se borra. | Must | 4 |
| RF-VET-14 | Operar la aplicación móvil sin conexión durante visitas a domicilio y sincronizar al recuperar la red. | Could | 4 |
| RF-VET-15 | Consultar sus propios indicadores: consultas realizadas, patologías frecuentes y tasa de ausencias. | Could | 4 |
| RF-VET-16 | Recibir solicitudes de asistencia compatibles con las especies, cantidad de animales, competencias, equipamiento, disponibilidad o guardia y zona del profesional; aceptar o rechazar la asignación. | Must | 3 |
| RF-VET-17 | Confirmar o modificar con motivo registrado la clasificación de gravedad, actualizar los estados del traslado y compartir su ubicación únicamente durante la asistencia asignada. | Must | 3 |

## Requerimientos funcionales del recepcionista

| ID | Requerimiento | Prioridad | Fase |
|---|---|---|---|
| RF-REC-01 | Agendar, reprogramar y cancelar turnos en nombre de un cliente que llama o se presenta. | Must | 3 |
| RF-REC-02 | Registrar la llegada del paciente y administrar la sala de espera con tiempos estimados. | Should | 3 |
| RF-REC-03 | Dar de alta rápidamente un cliente y su mascota en el mostrador, con datos mínimos y completado posterior. | Must | 3 |
| RF-REC-04 | Gestionar la lista de espera y asignar automáticamente los cupos liberados por cancelación. | Should | 4 |
| RF-REC-05 | Registrar cobros, medios de pago y emitir el comprobante. | Must | 4 |
| RF-REC-06 | Marcar ausencias sin aviso y aplicar la política de la clínica. | Should | 4 |
| RF-REC-07 | Reenviar comunicaciones al cliente, como resumen, carnet o comprobante, bajo pedido. | Could | 4 |
| RF-REC-08 | Consultar y gestionar la cola de asistencias verde, amarilla y naranja, visualizar profesionales elegibles y asignarlos respetando prioridad, horario y capacidades requeridas. | Must | 3 |
| RF-REC-09 | Recibir escalaciones cuando no haya veterinario disponible, la ubicación esté fuera de cobertura o no pueda validarse, y contactar al solicitante para coordinar una alternativa. | Must | 3 |

## Requerimientos funcionales del administrador

| ID | Requerimiento | Prioridad | Fase |
|---|---|---|---|
| RF-ADM-01 | Dar de alta, editar, bloquear y desactivar usuarios, asignando roles y permisos. | Must | 2 |
| RF-ADM-02 | Administrar servicios, tipos de cita, duraciones y tarifas con fecha de vigencia. | Must | 3 |
| RF-ADM-03 | Administrar recursos físicos: boxes, quirófano, sala de estética y equipamiento. | Must | 3 |
| RF-ADM-04 | Configurar horarios laborales, licencias, ausencias y feriados por profesional. | Must | 3 |
| RF-ADM-05 | Gestionar el inventario: compras, ingreso por lote, ajustes y bajas por vencimiento o rotura. | Must | 3 |
| RF-ADM-06 | Consultar el tablero de alertas de stock mínimo y productos próximos a vencer. | Must | 4 |
| RF-ADM-07 | Consultar indicadores de gestión: ocupación de agenda, ingresos por servicio, ausencias y rotación de stock. | Should | 4 |
| RF-ADM-08 | Consultar la bitácora de auditoría filtrando por usuario, entidad, acción y fechas. | Must | 4 |
| RF-ADM-09 | Editar las plantillas de correo y notificación sin intervención de desarrollo. | Could | 4 |
| RF-ADM-10 | Parametrizar la ventana de cancelación, antelación máxima, umbrales de stock y días de aviso de refuerzo. | Should | 4 |
| RF-ADM-11 | Ejecutar y verificar respaldos, y exportar datos en formato abierto. | Should | 5 |
| RF-ADM-12 | Administrar sucursales y asignar personal y recursos a cada una. | Could | 5 |
| RF-ADM-13 | Configurar horarios y zonas de cobertura de asistencia, guardias, competencias por especie, equipamiento, tarifas y parámetros de reevaluación y escalamiento. | Must | 3 |

## Reglas de negocio de agenda y turnos

| ID | Regla | Tipo |
|---|---|---|
| RN-01 | Un veterinario no puede tener dos turnos cuyos intervalos se solapen, ni siquiera parcialmente. | Bloqueante |
| RN-02 | Un recurso físico no puede asignarse a dos turnos simultáneos. | Bloqueante |
| RN-03 | La duración del turno la determina el tipo de cita y no puede reducirse por debajo de su duración base. | Bloqueante |
| RN-04 | Solo puede agendarse dentro del horario laboral del profesional, descontando licencias, ausencias y feriados. | Bloqueante |
| RN-05 | Un turno debe agendarse con una antelación mínima parametrizable y no más allá del horizonte máximo configurado. | Bloqueante |
| RN-06 | El cliente puede cancelar sin penalidad hasta N horas antes; después requiere intervención de la clínica. | Bloqueante |
| RN-07 | Una misma mascota no puede tener dos turnos activos superpuestos. | Bloqueante |
| RN-08 | Un cliente con más de N ausencias sin aviso en 12 meses no puede autoagendar; debe hacerlo por mostrador. | Advertencia |
| RN-09 | Solo Recepcionista o Administrador puede crear un sobreturno fuera de la disponibilidad calculada. | Bloqueante |
| RN-10 | Un turno quirúrgico no pasa a En curso sin consentimiento informado firmado. | Bloqueante |
| RN-11 | La transición de estados sigue el flujo definido; no puede finalizarse un turno nunca confirmado. | Bloqueante |

## Reglas de historia clínica y acto médico

| ID | Regla | Tipo |
|---|---|---|
| RN-12 | Un registro clínico firmado no se modifica ni elimina. Toda corrección es una enmienda fechada y firmada que conserva el original. | Bloqueante |
| RN-13 | Solo un Veterinario con matrícula vigente puede firmar diagnósticos y emitir recetas. | Bloqueante |
| RN-14 | La historia clínica nunca se borra, incluso tras el fallecimiento del paciente o la baja del cliente, y se conserva durante el plazo legal. | Bloqueante |
| RN-15 | No puede aplicarse una vacuna de un lote vencido. | Bloqueante |
| RN-16 | Debe respetarse el intervalo mínimo entre dosis; adelantar una aplicación exige justificación escrita. | Advertencia |
| RN-17 | Recetar un principio activo registrado como alergia exige confirmación explícita y queda asentado. | Advertencia |
| RN-18 | Un cliente accede únicamente a las fichas de las mascotas de las que es responsable registrado. | Bloqueante |
| RN-19 | La fecha de refuerzo se calcula automáticamente según el producto aplicado y no se ingresa manualmente. | Bloqueante |
| RN-20 | Toda consulta debe tener al menos un diagnóstico, presuntivo o definitivo, para cerrarse. | Bloqueante |

## Reglas de internación

| ID | Regla | Tipo |
|---|---|---|
| RN-21 | Una mascota no puede tener dos internaciones abiertas simultáneamente. | Bloqueante |
| RN-22 | Un box no admite más de un paciente internado a la vez. | Bloqueante |
| RN-23 | Solo el personal publica novedades; el cliente solo puede leer las marcadas como visibles. | Bloqueante |
| RN-24 | Una novedad publicada no se edita ni borra; se rectifica con una nueva entrada. | Bloqueante |
| RN-25 | No puede darse el alta sin registrar evolución final e indicaciones domiciliarias. | Bloqueante |

## Reglas de inventario

| ID | Regla | Tipo |
|---|---|---|
| RN-26 | El stock de un artículo nunca puede quedar negativo. | Bloqueante |
| RN-27 | Se consume primero el lote con vencimiento más próximo, según FEFO. | Bloqueante |
| RN-28 | Cuando el stock cruza el mínimo configurado se genera una alerta al administrador en la misma transacción. | Bloqueante |
| RN-29 | Un lote vencido se inhabilita automáticamente para consumo y venta y genera una baja registrada. | Bloqueante |
| RN-30 | Todo movimiento tiene origen tipado y trazable: compra, consumo, venta, ajuste o baja. | Bloqueante |
| RN-31 | Un ajuste manual requiere motivo escrito y rol Administrador. | Bloqueante |

## Reglas de facturación y cuenta corriente

| ID | Regla | Tipo |
|---|---|---|
| RN-32 | No puede emitirse un comprobante si el tratamiento o consulta asociada no está finalizado. | Bloqueante |
| RN-33 | Cada cargo se calcula con la tarifa vigente en la fecha de la prestación. | Bloqueante |
| RN-34 | Un comprobante emitido no se modifica; se anula mediante una nota de crédito trazable. | Bloqueante |
| RN-35 | Una cirugía programada puede exigir una seña previa; sin ella el turno no se confirma. | Advertencia |
| RN-36 | Una deuda vencida superior al límite genera aviso al agendar, pero no bloquea una urgencia. | Advertencia |

## Reglas de cuentas datos y auditoría

| ID | Regla | Tipo |
|---|---|---|
| RN-37 | Un correo electrónico identifica una única cuenta de cliente. | Bloqueante |
| RN-38 | Para agendar o registrar mascotas se exige una cuenta verificada. El modo invitado no accede a datos personales. | Bloqueante |
| RN-39 | Un usuario con actos médicos asociados no se elimina físicamente; se desactiva. | Bloqueante |
| RN-40 | Toda lectura de una historia clínica por parte del personal queda registrada en auditoría. | Bloqueante |
| RN-41 | El número de microchip, si existe, debe ser único en el sistema. | Bloqueante |
| RN-42 | La transferencia de titularidad conserva la historia clínica y registra a ambos responsables con sus fechas. | Bloqueante |

## Reglas complementarias de consentimiento y publicación

| ID | Regla | Tipo |
|---|---|---|
| RN-43 | Cada aceptación se presenta y registra por finalidad, documento y versión, fecha, canal y respuesta; las opciones facultativas no aparecen premarcadas ni se agrupan con términos obligatorios o consentimientos clínicos. | Bloqueante |
| RN-44 | Las cookies, rastreadores o SDK no esenciales no se activan antes de la decisión requerida; rechazar debe ser tan accesible como aceptar y la preferencia debe poder modificarse. | Bloqueante |
| RN-45 | No se publican reseñas ficticias, sellos no obtenidos ni afirmaciones sobre servicios, resultados o profesionales que no tengan origen y respaldo verificables. | Bloqueante |

## Reglas de solicitudes de orientación y asistencia

| ID | Regla | Tipo |
|---|---|---|
| RN-46 | La clasificación preliminar combina la gravedad percibida por el cliente con un cuestionario de orientación adaptado a la especie y cantidad de animales; antes de la revisión profesional, el sistema puede elevar pero no reducir el nivel indicado por el cliente. Esta clasificación no constituye un diagnóstico. | Bloqueante |
| RN-47 | Los cuatro niveles se ordenan de mayor a menor prioridad: rojo, naranja, amarillo y verde. El color siempre se acompaña de nombre, descripción e indicación accesible. | Bloqueante |
| RN-48 | Las solicitudes verdes se gestionan dentro del horario de la clínica. Las amarillas y naranjas también se gestionan dentro del horario; si no se atienden antes del cierre, pasan pendientes al siguiente día de atención con las naranjas por encima de las amarillas y, dentro del mismo nivel, por antigüedad. | Bloqueante |
| RN-49 | Una solicitud roja tiene prioridad máxima y activa el circuito de guardia sin depender del horario habitual; nunca se difiere automáticamente al día siguiente. | Bloqueante |
| RN-50 | Toda solicitud que continúe pendiente al cumplirse 24 horas desde su creación debe pedir al cliente una reevaluación del estado. La falta de respuesta no reduce el nivel ni cierra la solicitud automáticamente. | Bloqueante |
| RN-51 | El cliente puede informar empeoramiento en cualquier momento. La API registra el evento, repite la evaluación de inmediato y eleva la prioridad cuando corresponda, incluso de amarillo o naranja a rojo. | Bloqueante |
| RN-52 | Solo un veterinario puede reducir una clasificación preliminar; debe registrar el motivo. Todo cambio de nivel conserva valor anterior, valor nuevo, autor, fecha y motivo o respuestas que lo originaron. | Bloqueante |
| RN-53 | El tiempo estimado de llegada se muestra únicamente después de que un veterinario acepta la asistencia; debe identificarse como estimación y actualizarse cuando cambien las condiciones disponibles. | Bloqueante |
| RN-54 | La elegibilidad para asignar una asistencia considera especie o categoría, cantidad de animales, competencia profesional, equipamiento, disponibilidad o guardia y zona de cobertura antes de utilizar la proximidad como criterio. | Bloqueante |
| RN-55 | Si no hay un veterinario elegible, la ubicación está fuera de cobertura o no puede validarse, la solicitud no se descarta silenciosamente: se escala a Recepción para contactar al cliente y coordinar guardia, traslado, punto de encuentro u otra alternativa disponible. | Bloqueante |
| RN-56 | Una visita a domicilio exige ubicación GPS consentida o una dirección manual validable. Si el cliente no proporciona ninguna ubicación, puede recibir orientación o coordinar el traslado a la clínica, pero no confirmar una visita sin destino. | Bloqueante |
| RN-57 | La ubicación del veterinario se comparte con el cliente únicamente desde el inicio del traslado de una asistencia aceptada hasta la llegada, cancelación o finalización; el estado también se comunica mediante notificaciones para no exigir el seguimiento permanente del mapa. | Bloqueante |
| RN-58 | La asistencia genera un cargo conforme a la tarifa o criterio vigente e informado. Una deuda previa o una falla de pago no bloquea la creación ni el escalamiento de una solicitud roja. | Bloqueante |
| RN-59 | Una solicitud puede involucrar un animal individual o un grupo de animales de cualquier especie. La asignación y el cuestionario no pueden presuponer que se trata únicamente de perros o gatos. | Bloqueante |

## Requerimientos no funcionales de seguridad

| ID | Requerimiento |
|---|---|
| RNF-SEG-01 | Almacenar contraseñas con una función de derivación lenta y salt individual; nunca texto plano ni hash simple. |
| RNF-SEG-02 | Cifrar toda comunicación con HTTPS y certificado válido, rechazando tráfico no cifrado. |
| RNF-SEG-03 | Autenticación por token de expiración corta y token de refresco revocable por sesión. |
| RNF-SEG-04 | Segundo factor obligatorio para Administrador y opcional para los demás roles. |
| RNF-SEG-05 | Verificar autorización en el servidor para cada petición. Ocultar botones no constituye control de acceso. |
| RNF-SEG-06 | Evitar inyección SQL mediante ORM o consultas parametrizadas; nunca concatenar consultas. |
| RNF-SEG-07 | Servir adjuntos clínicos con enlaces firmados de vigencia limitada, no URL públicas adivinables. |
| RNF-SEG-08 | Validar tipo, tamaño y contenido real de archivos; almacenarlos fuera de la raíz web. |
| RNF-SEG-09 | Limitar intentos de autenticación y aplicar bloqueo temporal progresivo por cuenta y origen. |
| RNF-SEG-10 | Mantener auditoría inmutable desde la aplicación con usuario, acción, entidad, valores, fecha y origen. |
| RNF-SEG-11 | No registrar datos sensibles en logs: contraseñas, tokens ni contenido clínico. |
| RNF-SEG-12 | Leer credenciales y cadenas de conexión desde variables de entorno o un gestor de secretos. |
| RNF-SEG-13 | Inventariar y revisar antes de su activación todo script, SDK o servicio externo, documentando proveedor, finalidad, datos transmitidos, permisos, mecanismo de desactivación e impacto en privacidad; nunca enviarle contenido clínico, credenciales ni tokens. |
| RNF-SEG-14 | Restringir y auditar el acceso a ubicaciones precisas de clientes y profesionales; no incluir coordenadas en logs, analítica ni notificaciones visibles desde una pantalla bloqueada. |

## Requerimientos no funcionales de concurrencia

| ID | Requerimiento |
|---|---|
| RNF-CON-01 | Garantizar en la base de datos la unicidad de una reserva, no solo en la aplicación. |
| RNF-CON-02 | Ante una colisión, mostrar al usuario perdedor un mensaje comprensible y los horarios actualizados. |
| RNF-CON-03 | Cerrar una consulta atómicamente: historia, stock, cargo y correo encolado en una transacción. |
| RNF-CON-04 | Enviar correo fuera de la transacción mediante una cola con reintentos. |
| RNF-CON-05 | Incorporar versión optimista en entidades editables concurrentemente. |
| RNF-CON-06 | Definir reintentos ante interbloqueos, con máximo y espera creciente. |
| RNF-CON-07 | Mantener transacciones breves y nunca esperar al usuario con bloqueos abiertos. |
| RNF-CON-08 | Usar read committed con versionado de filas por defecto y elevarlo solo cuando sea necesario. |
| RNF-CON-09 | Probar dos reservas simultáneas y verificar que exactamente una tenga éxito. |
| RNF-CON-10 | Hacer idempotentes la creación de solicitudes, la aceptación de asignaciones y los avisos de empeoramiento para que reintentos de red no dupliquen asistencias, despachos ni cambios de prioridad. |

## Requerimientos no funcionales de base de datos

| ID | Requerimiento |
|---|---|
| RNF-BD-01 | Normalizar el esquema hasta 3FN y justificar toda desnormalización. |
| RNF-BD-02 | Declarar integridad referencial con claves foráneas y comportamiento explícito ante borrado. |
| RNF-BD-03 | Declarar en la base restricciones de rango, unicidad y obligatoriedad. |
| RNF-BD-04 | Indexar claves foráneas y combinaciones frecuentes de agenda, historia clínica y stock. |
| RNF-BD-05 | Aplicar baja lógica a entidades con valor histórico. |
| RNF-BD-06 | Almacenar fechas y horas con zona horaria explícita. |
| RNF-BD-07 | Versionar el esquema mediante migraciones reproducibles desde cero. |
| RNF-BD-08 | Resolver escrituras críticas múltiples con transacciones explícitas o procedimientos almacenados. |
| RNF-BD-09 | Implementar auditoría mediante disparadores sobre tablas sensibles. |
| RNF-BD-10 | Mantener binarios fuera de la base, guardando ubicación, tamaño, tipo y huella digital. |
| RNF-BD-11 | Contar con datos de prueba realistas y reproducibles. |

## Requerimientos no funcionales de rendimiento y disponibilidad

| ID | Requerimiento |
|---|---|
| RNF-REN-01 | Los listados responden en menos de dos segundos con datos de prueba y 30 usuarios concurrentes. |
| RNF-REN-02 | La disponibilidad de agenda de un mes se calcula en menos de un segundo. |
| RNF-REN-03 | Los listados extensos se paginan en el servidor. |
| RNF-REN-04 | Las imágenes tienen previsualización reducida y tamaño completo solo bajo pedido. |
| RNF-DIS-01 | Respaldo completo diario y del registro de transacciones cada hora, con 30 días de retención mínima. |
| RNF-DIS-02 | Probar una restauración al menos una vez durante el proyecto. |
| RNF-DIS-03 | Objetivo de punto de recuperación de una hora y recuperación del servicio en cuatro horas. |
| RNF-DIS-04 | Si falla el tiempo real, las novedades de internación siguen disponibles al recargar. |
| RNF-DIS-05 | Si fallan el mapa, el cálculo de ruta o las notificaciones push, el estado de la asistencia, la dirección textual y las alternativas de llamada deben continuar disponibles. |

## Requerimientos no funcionales de usabilidad y mantenibilidad

| ID | Requerimiento |
|---|---|
| RNF-USA-01 | Agendar un turno requiere como máximo cuatro pasos desde el ingreso. |
| RNF-USA-02 | Los errores explican qué ocurrió y qué hacer, sin jerga técnica. |
| RNF-USA-03 | La interfaz es responsiva y utilizable desde 360 píxeles. |
| RNF-USA-04 | Contraste, área táctil y teclado cumplen WCAG 2.1 AA. |
| RNF-USA-05 | La interfaz usa español rioplatense, fechas y moneda locales. |
| RNF-USA-06 | Proporcionar alternativas textuales según el propósito de cada imagen: descripción para contenido informativo, nombre de acción para imágenes funcionales y alternativa vacía o exclusión del árbol de accesibilidad para imágenes decorativas. |
| RNF-USA-07 | Comunicar cada nivel de asistencia mediante texto, icono y prioridad, sin depender solo del color, y mantener siempre visible la acción para informar empeoramiento mientras la solicitud esté activa. |
| RNF-MAN-01 | Separación estricta de capas; ninguna regla reside en controladores o interfaces. |
| RNF-MAN-02 | Cobertura automatizada mínima de 60 % en servicios de dominio. |
| RNF-MAN-03 | Convenciones de nombres y estilo verificadas en integración continua. |
| RNF-MAN-04 | Los parámetros de negocio se configuran desde administración. |
| RNF-MAN-05 | La API tiene documentación navegable y contrato versionado. |

## Requerimientos no funcionales legales

| ID | Requerimiento |
|---|---|
| RNF-LEG-01 | Tratar datos personales conforme a la Ley 18.331 de Uruguay: consentimiento, finalidad y derechos de acceso, rectificación y supresión. |
| RNF-LEG-02 | Permitir exportar y eliminar datos personales conservando disociada la información clínica que deba preservarse. |
| RNF-LEG-03 | Mostrar privacidad y términos en modo invitado y registrar la versión y fecha aceptadas durante el registro. |
| RNF-LEG-04 | Conservar la historia clínica durante el plazo que determine la normativa veterinaria aplicable. |
| RNF-LEG-05 | Contemplar en el microchip el formato del registro nacional de identificación animal para una futura integración. |
| RNF-LEG-06 | Documentar para cada dato recolectado su finalidad, obligatoriedad, acceso, conservación y destino, limitando formularios y endpoints a los datos necesarios. |
| RNF-LEG-07 | Usar únicamente imágenes, iconos, fuentes y demás recursos propios, autorizados o con licencia compatible, conservando evidencia de procedencia y atribución. |
| RNF-LEG-08 | Realizar y documentar antes de producción una revisión de normativa uruguaya aplicable, bases de datos, derechos de los titulares, conservación clínica, terceros, incidentes y riesgos de las funciones efectivamente implementadas. |
| RNF-LEG-09 | Informar finalidad, destinatarios, duración y conservación del uso de ubicación de clientes y profesionales, obtener la decisión exigible antes de compartirla y cesar el seguimiento al finalizar su finalidad operativa. |

## Totales

- 74 requerimientos funcionales: 62 recuperados del documento original y 12 complementarios.
- 59 reglas de negocio: 42 recuperadas del documento original y 17 complementarias.
- 65 requerimientos no funcionales: 55 recuperados del documento original y 10 complementarios.
