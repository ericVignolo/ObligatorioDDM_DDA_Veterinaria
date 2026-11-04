# MVP veterinario de la entrega completa

Fecha: 2026-10-20

Estado: borrador de visión integral construido a partir del contexto, los requisitos y las decisiones documentadas del proyecto. No es una consigna oficial ni sustituye la matriz canónica.

## 1. Propósito

Construir un sistema de gestión para una clínica veterinaria que organice en un mismo recorrido la información pública, las cuentas y animales, la agenda, las consultas e historias clínicas, la vacunación, el inventario, los cargos, los cobros, la internación, la asistencia veterinaria y la administración.

La solución debe admitir distintas especies y situaciones, incluidos animales de producción y de campo. Para la demostración se propone trabajar con un caballo y una oveja, además de datos ficticios para los demás procesos. Las reglas clínicas, preguntas de asistencia y tratamientos aplicables requieren validación profesional; el software no realiza diagnósticos autónomos.

## 2. Referencia académica y decisiones de alcance

El documento “Obligatorio Andres” describe un sistema agrícola SIGA. Se toma como referencia de análisis y presentación: problema actual, actores, requisitos, reglas, entidades, historias, criterios de aceptación, dudas y propuesta de primera versión. Sus reglas de potreros, cultivos, campañas, eventos agrícolas, tareas de campo y operación sin conexión no se trasladan al sistema veterinario.

La devolución docente que ya forma parte del contexto valoró la profundidad del proyecto y recomendó concretar el alcance, los estados, permisos, modelo de datos y casos de uso completos. Por eso este MVP resume la entrega completa por hitos H0–H5, manteniendo la matriz vigente: ubicar un requisito en un hito no lo elimina ni lo vuelve opcional.

## 3. Solución acordada

- **Blazor/C#** para la interfaz principal de computadora y paneles de gestión.
- **API ASP.NET Core en C#** como única autoridad de datos, autenticación, autorización y reglas de negocio.
- **React Native** para las experiencias móviles de Cliente y personal clínico, según el mapa de navegación por completar.
- **MySQL** como motor decidido para la futura persistencia. Clientes nunca accederán directamente a la base de datos.
- **Figma** para preparar el diseño móvil después de cerrar el comportamiento, los pseudocódigos y las decisiones pendientes.
- Hasta que la API exista, los recorridos interactivos de escritorio pueden usar datos ficticios claramente identificados; no prueban persistencia ni seguridad real.

## 4. Experiencia de punta a punta

El sistema debe permitir demostrar, de manera integrada y con permisos adecuados, los siguientes recorridos:

1. Una persona visitante consulta servicios, profesionales, ubicación, contacto, urgencias y políticas sin iniciar sesión.
2. Al iniciar una acción protegida, puede iniciar sesión o registrarse y luego retomar la intención. La disponibilidad, autorización y vigencia se revalidan; no se conserva un bloqueo de agenda mientras inicia sesión.
3. El Cliente verifica su cuenta, registra uno o varios animales y gestiona la responsabilidad vigente sobre ellos.
4. El Cliente consulta disponibilidad y reserva, reprograma o cancela según las reglas vigentes. La API impide colisiones incluso ante solicitudes simultáneas.
5. Recepción registra llegada y administra la cola de atención. El Veterinario autorizado abre la consulta y registra evaluación, diagnóstico, tratamiento, alergias, adjuntos e indicaciones aplicables.
6. El cierre clínico firma el registro y conserva el original. Correcciones posteriores se agregan como enmiendas trazables.
7. Al atender, el sistema valida y registra consumos de inventario; aplica lotes y vencimientos según las reglas; calcula cargos usando la tarifa histórica aplicable.
8. Recepción registra cobros y emite comprobantes internos. La consulta y el cargo conservan ciclos independientes. El correo de resumen se encola y puede reintentarse sin deshacer una consulta firmada.
9. El equipo registra vacunas vinculadas a producto/lote, verifica vencimiento e intervalo, calcula refuerzos y genera alertas sanitarias según configuración.
10. El personal ingresa una internación con box exclusivo, plan terapéutico, evolución y novedades diferenciadas entre internas y visibles para el Cliente; registra el alta y sus indicaciones.
11. El Cliente firma consentimientos informados requeridos y consulta novedades autorizadas, historia y documentos propios.
12. El Cliente solicita asistencia para uno o varios animales, incluido un grupo o especie no doméstica. El sistema clasifica de forma preliminar, prioriza, asigna o escala según elegibilidad, permite informar empeoramiento y muestra seguimiento/ubicación solo bajo las condiciones acordadas.
13. Administración configura servicios, horarios, recursos, tarifas, usuarios, roles, competencias, cobertura, parámetros y preferencias operativas; consulta auditoría e indicadores.
14. El sistema permite ejercer derechos sobre datos conforme a obligaciones de privacidad y conservación, y soporta respaldo, restauración y exportaciones administrativas controladas.

El recorrido no implica que todas las acciones se encuentren implementadas hoy. El estado se demostrará con evidencia por RF, RN y RNF.

## 5. Actores y responsabilidades

| Actor | Responsabilidad principal |
|---|---|
| Visitante | Consultar información pública e iniciar registro o acceso al seleccionar una acción protegida. |
| Cliente | Administrar animales bajo su responsabilidad, reservar, consultar registros visibles, firmar consentimientos y solicitar/seguir asistencia. |
| Recepción | Gestionar turnos, llegada, cola, cobros, comprobantes, coordinación y tareas de asistencia asignadas. |
| Veterinario | Atender pacientes dentro de su competencia y habilitación, registrar y firmar consultas, vacunas, internaciones y asistencia. |
| Administrador | Gestionar configuración, usuarios y roles, catálogo, servicios, tarifas, recursos, parámetros y auditoría; utiliza el segundo factor requerido. |
| API y dominio | Validar reglas, identidad, permisos, integridad, transacciones, idempotencia, prioridad y trazabilidad. |
| Sistema temporal | Ejecutar reevaluaciones y procesos programados con estados y auditoría. |
| Servicios externos | Apoyar mapas, correo u otros servicios aprobados sin asumir autoridad clínica ni recibir datos innecesarios. |

## 6. Servicios de ejemplo para la demostración

El catálogo deberá ser configurable por la clínica. Para los datos ficticios iniciales se proponen:

- Revisión o consulta clínica general de un caballo.
- Revisión clínica de una oveja, para demostrar especies y categorías distintas de perros y gatos.
- Vacunación, con registro del producto/lote y validaciones correspondientes.
- Desparasitación como atención/tratamiento separado de la vacunación, con el producto y las indicaciones definidos por un Veterinario.
- Rehabilitación o fisioterapia veterinaria equina, solo si la clínica ofrece ese servicio y cuenta con personal competente. Es una prestación existente en hospitales veterinarios equinos; se modelará como servicio configurable, no como indicación automática ni como promesa de disponibilidad.

La secuencia más simple para presentar podría ser una consulta general de un caballo y el registro de una vacuna o desparasitación según el plan profesional. La oveja puede cubrir el caso de especie/grupo y filtrado por competencia en el módulo de asistencia. Estos son ejemplos de demostración, no protocolos clínicos.

## 7. Hitos de entrega

| Hito | Resultado dentro de la entrega integral |
|---|---|
| H0 — Especificación | Cerrar registro, requisitos y casos, estados, permisos, dominio/datos, contratos, pseudocódigo, mapa funcional y decisiones pendientes. No habilita diseño final/código de producto hasta satisfacer las instrucciones del proyecto. |
| H1 — Identidad, pacientes y agenda | Área pública, registro/verificación, acceso y roles, gestión de Cliente/animales, servicios, profesionales, disponibilidad, reservas, reprogramación/cancelación, agenda, llegada y estados. |
| H2 — Consulta, historia y cobro | Historia clínica, consulta, adjuntos, firma/enmiendas, reglas de inventario aplicadas al consumo, cargos, cobro por Recepción, resumen por correo e indicadores iniciales. |
| H3 — Atención ampliada | Vacunación y alertas sanitarias, internación, consentimiento clínico, preferencias, derechos y procesos administrativos correspondientes. |
| H4 — Asistencia veterinaria | Solicitudes de todos los niveles, especies/grupos, cuestionarios aprobados, prioridad, empeoramiento, reevaluación, guardias, asignación, costos, privacidad de ubicación, mapa y seguimiento. |
| H5 — Integración y entrega | Verificación integral, pruebas de negocio/concurrencia/autorización, respaldo/restauración, exportación, auditoría, documentación UML/manuales, despliegue de demostración y evidencia reproducible. |

La asignación detallada de RF a hitos se conserva en `ALCANCE_ENTREGA.md`. Las extensiones posteriores siguen separadas y no se presentan como parte comprometida de la base.

La construcción se organizará operativamente en las secciones acumulativas A — escritorio, B — móvil y C — API e integración. H0–H5 describen la progresión funcional y los criterios de entrega; A/B/C describen en qué orden se preparan los clientes y se conectan al backend. El detalle numerado está en `PLAN_TAREAS_PROYECTO.md`.

## 8. Reglas críticas

- Una sola API aplica autenticación, autorización y reglas. La seguridad no depende de ocultar controles en los clientes.
- El acceso a datos clínicos exige autorización por rol, relación con el animal y propósito aplicable; se auditan las consultas relevantes.
- Un registro clínico firmado no se actualiza ni borra: la corrección es una enmienda firmada que conserva el original.
- Cerrar una consulta no significa cobrarla. Firma clínica, turno, inventario, cargo y pago tienen estados relacionados pero diferenciados.
- El cierre confirma de forma consistente la firma, consumos, saldo, cargos, auditoría y correo pendiente. Si falla una parte transaccional, no deja un cierre parcial.
- El stock no puede quedar negativo; se aplican vencimientos, lotes y política de consumo definida.
- Las reservas y consumos se vuelven a validar al confirmar y manejan concurrencia e idempotencia desde la API.
- Una vacunación vencida o antes del intervalo válido se rechaza. Los valores de intervalos requieren configuración profesional.
- Un procedimiento sujeto a consentimiento no comienza sin consentimiento informado válido y trazable.
- Internación respeta exclusividad de box, permiso de publicación de novedades y privacidad de notas internas.
- Las solicitudes de asistencia rojas activan el circuito inmediato de guardia; la deuda o una falla de pago no bloquean su creación. La clasificación es orientación, no diagnóstico, y solo Veterinario puede reducir prioridad con motivo registrado.
- No se inventa profesional, cobertura ni estimación de llegada. GPS requiere consentimiento; se admite ubicación manual cuando corresponda y el seguimiento termina al dejar de ser necesario.
- Datos de demo son ficticios, reproducibles y distinguibles de información real.

## 9. Pagos y Mercado Pago

La entrega base incluye que Recepción registre el cobro del cargo y el medio de pago, con comprobante interno. El portal de pago del Cliente y una pasarela online figuran actualmente como extensión en `ALCANCE_ENTREGA.md`.

Eric propone mostrar pagos con tarjeta mediante una integración como la API de Mercado Pago. Se registra aquí como **extensión propuesta y decisión pendiente**, no como integración confirmada ni compromiso de alcance. Si se aprueba incluirla, clasificar y actualizar antes la matriz, el alcance y los flujos relacionados. Para un prototipo de proveedor se usarían credenciales TEST, verificación del resultado por la API y ningún cobro real; la aplicación nunca solicitaría ni almacenaría número de tarjeta o CVV. Se deberá decidir si esta integración es necesaria para la entrega académica y qué producto de checkout se utilizaría.

## 10. Privacidad, integridad y trazabilidad

- Los datos personales, clínicos, documentos y coordenadas se recopilan para finalidades definidas y se muestran según autorización.
- Las aceptaciones obligatorias se registran por separado con versión, fecha, canal y respuesta; las opciones facultativas no se premarcan ni se confunden con un consentimiento clínico.
- Los registros importantes se inactivan o corrigen conservando evidencia; la auditoría identifica usuario, acción y momento sin copiar secretos o texto clínico innecesario a logs.
- Se definen conservación y eliminación/disociación para cada clase de dato. No se promete borrado incompatible con obligaciones de conservación.
- Los adjuntos clínicos requieren acceso temporal autorizado. La ubicación no se expone en logs ni notificaciones en pantalla bloqueada.
- La base y los registros transaccionales cuentan con respaldo y restauración verificables según los RNF aplicables.

## 11. Funciones que no se adoptan desde la referencia agrícola

La primera entrega veterinaria no incorpora potreros, campañas de cultivo, eventos de maleza/plaga, maquinaria agrícola, cosechas, sensores de campo ni registros móviles sin conexión por el solo hecho de figurar en SIGA. Se aplican los límites ya definidos en la matriz: integraciones entre clínicas, contabilidad/sueldos, homologación fiscal, equipos clínicos y ciertas funciones online o automatizaciones permanecen fuera o como extensiones según su clasificación documentada.

## 12. Datos ficticios y criterios de demostración

El contexto propone como conjunto reproducible mayor 30 clientes, 50 animales y 200 consultas históricas. Para demostrar cada hito se puede usar un subconjunto sintético y ampliar el volumen sin datos personales reales.

Criterios integrales de aceptación:

- El catálogo público puede consultarse sin cuenta y una acción protegida retoma su intención después del acceso/verificación.
- Los roles solo pueden ejecutar operaciones autorizadas desde la API y la prueba negativa de acceso a animales ajenos se rechaza.
- Cliente verificado registra animales y reserva; intentos concurrentes incompatibles producen un solo resultado confirmado.
- Recepción gestiona llegada/cola y cobra cargos sin alterar registros clínicos.
- Veterinario autorizado firma una consulta; los originales permanecen inmutables y las enmiendas quedan enlazadas.
- Error inducido durante el cierre revierte todos los efectos transaccionales; correo fallido después del cierre no deshace la consulta y queda reintentable.
- Vacuna vencida/intervalo inválido y consumo que excedería stock se rechazan.
- Internación impide duplicar ocupación del box y protege notas internas.
- Sin consentimiento válido no comienza el procedimiento que lo exige.
- Solicitud amarilla que empeora a roja activa guardia; la falta de respuesta a las 24 horas no reduce prioridad ni cierra la solicitud.
- Sin veterinario elegible se avisa a Recepción y no se muestra asignación ni llegada inventadas.
- GPS, ubicación manual, seguimiento y ausencia de datos se presentan bajo las reglas de consentimiento y privacidad.
- Se ejecutan escenarios de acceso indebido, idempotencia, concurrencia, restauración y otros casos de prueba trazados a RF/RN/RNF.
- Se puede reconstruir la demostración con datos sintéticos y evidencia, distinguiendo lo implementado, lo probado y las extensiones pendientes.

## 13. Clasificación de nuevas ideas expresadas

| Idea | Clasificación del proyecto | Tratamiento en este borrador |
|---|---|---|
| Mostrar caballo y oveja en datos/casos ficticios | Diseño/UX y datos de demostración; requerimiento funcional si se acuerda como caso de aceptación | Propuesta de demostración; el dominio ya contempla especies variadas y grupos. |
| Incluir revisión clínica, vacunación y desparasitación | Requerimiento funcional/catálogo; las indicaciones y validaciones clínicas son reglas de negocio profesionales | Servicios de ejemplo; vacuna y desparasitación se distinguen y siguen el plan del Veterinario. |
| Ofrecer fisioterapia/rehabilitación equina | Requerimiento funcional del catálogo y decisión de operación/capacidad profesional | Servicio configurable y condicionado; no se presupone que la clínica lo ofrezca. |
| Integrar Mercado Pago para tarjeta | Requerimiento funcional y decisión técnica/integración externa, con controles de seguridad/consentimiento | Extensión propuesta pendiente de decisión y actualización documental; no altera todavía el alcance base. |

## 14. Preguntas que todavía requieren decisión

1. ¿Se aprueba Mercado Pago TEST como extensión de la entrega o se mantiene el alcance actual de cobro interno por Recepción?
2. ¿Qué servicios ficticios y tarifas de ejemplo mostramos? No se tomarán las tarifas como reglas reales.
3. ¿La demostración usará primero un caballo y luego una oveja como segundo caso, o priorizamos uno solo por hito?
4. ¿La clínica ficticia ofrece rehabilitación equina o dejamos el servicio visible solo como ejemplo configurable?
5. ¿Qué reglas clínicas de vacunación, desparasitación y asistencia validará el profesional asesor? No se inventarán protocolos.
6. ¿Hay más devolución escrita del profesor que convenga incorporar además de la ya registrada en el contexto del proyecto?

## 15. Fuentes internas y externas

- Matriz canónica: `docs/contexto-base/MATRIZ_REQUISITOS.md`.
- Contexto y arquitectura acordados: `docs/contexto-base/CONTEXTO_BASE.md`.
- Asignación de alcance e hitos: `docs/analisis/ALCANCE_ENTREGA.md`.
- Reserva y consulta: `docs/analisis/FLUJO_RESERVA_CONSULTA.md`.
- Registro de Cliente: `docs/analisis/ESPECIFICACION_REGISTRO_CLIENTE.md`.
- Asistencia: `docs/analisis/ESPECIFICACION_ALERTAS_ASISTENCIA.md`.
- Rehabilitación equina como servicio veterinario especializado: University of Florida Veterinary Hospitals, University of Tennessee College of Veterinary Medicine y Virginia Tech Marion duPont Scott Equine Medical Center.

Este borrador debe contrastarse periódicamente con la matriz y las decisiones del equipo. Una nueva funcionalidad que altere arquitectura, reglas obligatorias o alcance debe clasificarse y documentarse antes de modificar el diseño o el código.
