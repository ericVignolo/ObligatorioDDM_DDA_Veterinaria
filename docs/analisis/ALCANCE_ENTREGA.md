# Alcance de la entrega y planificación por hitos

Versión: 1.0 operativa

Fecha: 2026-10-02

Estado: Base de planificación; especificaciones de cada módulo pendientes de validación donde se indica.

## 1. Base del acuerdo

El usuario confirmó dos integrantes, con apoyo de Codex, y un plazo real todavía no establecido. La devolución docente considera suficiente la profundidad del sistema y recomienda concretar alcance, estados, permisos, modelo de datos y casos de uso completos.

Esta tabla asigna una ubicación a los 74 RF actuales. La prioridad y la redacción canónica siguen en [MATRIZ_REQUISITOS.md](../contexto-base/MATRIZ_REQUISITOS.md). “Base” identifica el objetivo de entrega del plan vigente; no certifica viabilidad temporal antes de estimar esfuerzo y disponibilidad. “Extensión” identifica funcionalidad no comprometida en la base, conservada como trabajo posterior. Ningún RF Must se convierte tácitamente en opcional.

Resultado de la asignación: 55 RF en la base (47 Must y 8 Should seleccionados) y 19 RF como extensiones. Es un alcance todavía amplio; los hitos hacen visible su costo y permiten revisar compromisos con evidencia, no equivalen a una estimación de esfuerzo.

Las RN y RNF aplicables acompañan cada módulo desde su construcción. Seguridad, privacidad, auditoría, integridad y pruebas no se difieren por el hecho de tener un hito final de verificación.

## 2. Tabla de alcance funcional

| Módulo | Entrega base verificable | Extensiones | Clientes principales |
|---|---|---|---|
| Área pública e identidad | Servicios, profesionales, ubicación/contacto, urgencias, políticas, registro/verificación, sesión, recuperación, roles y segundo factor del Administrador. Retomar acciones protegidas. | Disponibilidad pública orientativa, contenido educativo y formulario de contacto. | Blazor y React Native; administración en Blazor. |
| Responsables y animales | Alta y edición, foto, microchip único y responsabilidad vigente; alta desde recepción. Especies configurables. Modelo que admite varios responsables. | Transferencia e invitación de segundo responsable desde la interfaz; QR de extravío; peso doméstico. | Cliente móvil y Recepción en Blazor. |
| Agenda | Disponibilidad, reserva, reprogramación, cancelación, agenda profesional, llegada/espera, ausencias, estados e historial; colisiones controladas. | Lista de espera con asignación automática. | Cliente/Veterinario móvil y personal en Blazor. |
| Consulta e historia | Registro clínico, diagnósticos, tratamiento, alergias, adjuntos, firma/cierre y enmiendas; lectura autorizada y auditoría. | Interconsultas, renovación remota de tratamiento, indicadores personales y trabajo offline. | Veterinario en Blazor y móvil; lectura del Cliente móvil. |
| Vacunación | Aplicación vinculada al lote, validación de vencimiento/intervalo, cálculo de refuerzo y alertas sanitarias. | Carnet PDF verificable por QR. | Veterinario; consulta/avisos al Cliente. |
| Inventario | Productos, lotes, vencimiento, compras, ajustes, bajas, consumo FEFO, stock no negativo, alertas y trazabilidad. | Automatizaciones adicionales a las exigidas por las RN vigentes. | Administrador en Blazor; consumo desde atención. |
| Cobros y cargos | Cargo por prestación finalizada, tarifa histórica, cobro registrado por Recepción, medio de pago, comprobante interno y nota de crédito trazable. | Portal de cuenta corriente y pago online del Cliente; pasarela de pago. | Recepción en Blazor; configuración administrativa. |
| Internación | Ingreso, box exclusivo, plan terapéutico, evolución, novedades internas/públicas en tiempo real y alta con indicaciones. | Capacidades adicionales no enumeradas en los RF de base. | Veterinario y Recepción según permisos; seguimiento del Cliente. |
| Consentimientos | Aceptaciones por finalidad/versión y consentimiento informado firmado para procedimientos que lo requieren; mecanismo de firma por especificar. | Receta digital verificable. | Cliente y personal autorizado. |
| Comunicaciones y derechos | Resumen por correo con reintentos; alertas sanitarias/operativas; preferencias, exportación/eliminación con conservación exigible. | Chat postoperatorio, encuestas, edición de plantillas y reenvío manual dedicado. | Cliente móvil y administración en Blazor. |
| Asistencia | Solicitud, clasificación, empeoramiento, reevaluación, cobertura/guardia, asignación y aceptación, ubicación consentida, estados, mapa/estimación y cargos; desarrollar después del recorrido clínico. | Mejoras adicionales de optimización del despacho; no se difieren las funciones Must existentes de mapa/seguimiento. | Cliente/Veterinario móvil, tablero de Recepción y configuración en Blazor. |
| Administración e indicadores | Usuarios, servicios, tarifas, recursos, horarios, parámetros, auditoría e indicadores de ocupación, ingresos por servicio, ausencias y rotación de stock. | Gestión operativa de múltiples sucursales; indicadores adicionales. | Administrador en Blazor. |
| Calidad y entrega | Pruebas de reglas, concurrencia y autorización; respaldos/restauración, exportación administrativa, UML, manuales y demostración reproducible. | Integraciones y automatizaciones fuera del alcance declarado. | API, ambos clientes e infraestructura. |

El alcance por canal evita duplicar paneles administrativos en móvil. El contrato de la API y la autorización son comunes. La distribución definitiva de pantallas se valida en el mapa de navegación.

## 3. Hitos y criterio de salida

Los identificadores H0–H5 son hitos operativos. La columna “Fase” de la matriz sigue refiriéndose al plan original; no se renumera ni representa semanas restantes.

| Hito | Resultado | Criterio de salida |
|---|---|---|
| H0 — Especificación | Alcance asignado, registro, casos principales, estados, permisos, modelo de dominio/datos y contratos necesarios. | Pendientes que afectan el primer recorrido resueltos; mapa/wireframes funcionales revisados. Completar los restantes pendientes documentados antes del diseño visual definitivo o código de producto conforme a AGENTS.md. |
| H1 — Reservar | Identidad, datos maestros, cliente/animal, configuración profesional, reserva y gestión del turno. | Un cliente autenticado reserva; dos reservas simultáneas incompatibles producen exactamente un éxito; Recepción registra llegada; permisos y pruebas pasan. |
| H2 — Atender y cobrar | Consulta, adjuntos, historia, stock, cargos, correo, enmiendas, cobro interno e indicadores iniciales. | Recorrido de reserva a consulta cerrada y cobro demostrado; fallo inducido revierte el cierre completo; correo caído no lo revierte. |
| H3 — Completar atención | Vacunación, alertas, internación, consentimiento clínico, preferencias y solicitudes sobre datos. | Vacuna inválida rechazada; box exclusivo; novedad interna protegida; alta válida; procedimiento sujeto a consentimiento bloqueado sin firma. |
| H4 — Asistencia | Módulo completo según los RF Must y su especificación, incluida ubicación/mapa/estimación. | Escalamiento, falta de elegibles, empeoramiento, reevaluación, privacidad de ubicación e idempotencia probados; parámetros operativos cerrados. |
| H5 — Entrega integrada | Respaldo y restauración, métricas finales, trazabilidad, UML/manuales, despliegue de demostración y defensa. | Evidencia por RF incluido y RN/RNF aplicable, defectos bloqueantes resueltos, demostración reproducible y extensiones identificadas. |

H2 termina un primer recorrido demostrable, pero no equivale a haber terminado toda la entrega base. La asistencia se reserva para H4 para no impedir que ese recorrido se valide antes. Cuando exista fecha límite, revisar la carga de H3/H4 de forma explícita y documentar cualquier cambio de obligaciones.

## 4. Asignación exhaustiva de RF

Cada RF aparece una sola vez en esta tabla. “Base” mantiene la prioridad de origen incluso cuando se selecciona un Should. “Extensión” no elimina el requisito de la matriz. Los rangos de hitos indican un desarrollo incremental cuya conformidad completa corresponde al último hito indicado.

| RF | Prioridad canónica | Destino | Hito |
|---|---|---|---|
| RF-VIS-01 | Must | Base | H1 |
| RF-VIS-02 | Must | Base | H1 |
| RF-VIS-03 | Must | Base | H1 |
| RF-VIS-04 | Should | Extensión | Posterior |
| RF-VIS-05 | Should | Extensión | Posterior |
| RF-VIS-06 | Must | Base | H1 |
| RF-VIS-07 | Must | Base | H1 |
| RF-VIS-08 | Could | Extensión | Posterior |
| RF-VIS-09 | Should | Extensión | Posterior |
| RF-VIS-10 | Must | Base | H1 |
| RF-CLI-01 | Must | Base | H1 |
| RF-CLI-02 | Must | Base | H1 |
| RF-CLI-03 | Must | Base | H1 |
| RF-CLI-04 | Must | Base | H1 |
| RF-CLI-05 | Must | Base | H2 |
| RF-CLI-06 | Should | Extensión | Posterior |
| RF-CLI-07 | Must | Base | H3 |
| RF-CLI-08 | Must | Base | H2 |
| RF-CLI-09 | Must | Base | H3 |
| RF-CLI-10 | Must | Base | H2 |
| RF-CLI-11 | Must | Base | H3 |
| RF-CLI-12 | Should | Extensión | Posterior |
| RF-CLI-13 | Should | Extensión | Posterior |
| RF-CLI-14 | Could | Extensión | Posterior |
| RF-CLI-15 | Could | Extensión | Posterior |
| RF-CLI-16 | Should | Extensión | Posterior |
| RF-CLI-17 | Could | Extensión | Posterior |
| RF-CLI-18 | Should | Base | H3 |
| RF-CLI-19 | Should | Base | H3 |
| RF-CLI-20 | Should | Base | H1 |
| RF-CLI-21 | Must | Base | H4 |
| RF-CLI-22 | Must | Base | H4 |
| RF-CLI-23 | Must | Base | H4 |
| RF-CLI-24 | Must | Base | H4 |
| RF-CLI-25 | Must | Base | H4 |
| RF-VET-01 | Must | Base | H2 |
| RF-VET-02 | Must | Base | H2 |
| RF-VET-03 | Must | Base | H2 |
| RF-VET-04 | Must | Base | H3 |
| RF-VET-05 | Should | Extensión | Posterior |
| RF-VET-06 | Must | Base | H2 |
| RF-VET-07 | Must | Base | H2 |
| RF-VET-08 | Could | Extensión | Posterior |
| RF-VET-09 | Must | Base | H3 |
| RF-VET-10 | Must | Base | H3 |
| RF-VET-11 | Must | Base | H3 |
| RF-VET-12 | Must | Base | H2 |
| RF-VET-13 | Must | Base | H2 |
| RF-VET-14 | Could | Extensión | Posterior |
| RF-VET-15 | Could | Extensión | Posterior |
| RF-VET-16 | Must | Base | H4 |
| RF-VET-17 | Must | Base | H4 |
| RF-REC-01 | Must | Base | H1 |
| RF-REC-02 | Should | Base | H1 |
| RF-REC-03 | Must | Base | H1 |
| RF-REC-04 | Should | Extensión | Posterior |
| RF-REC-05 | Must | Base | H2 |
| RF-REC-06 | Should | Base | H1 |
| RF-REC-07 | Could | Extensión | Posterior |
| RF-REC-08 | Must | Base | H4 |
| RF-REC-09 | Must | Base | H4 |
| RF-ADM-01 | Must | Base | H1 |
| RF-ADM-02 | Must | Base | H1–H2 |
| RF-ADM-03 | Must | Base | H1–H3 |
| RF-ADM-04 | Must | Base | H1 |
| RF-ADM-05 | Must | Base | H2 |
| RF-ADM-06 | Must | Base | H2 |
| RF-ADM-07 | Should | Base | H2–H5 |
| RF-ADM-08 | Must | Base | H1–H2 |
| RF-ADM-09 | Could | Extensión | Posterior |
| RF-ADM-10 | Should | Base | H1–H3 |
| RF-ADM-11 | Should | Base | H5 |
| RF-ADM-12 | Could | Extensión | Posterior |
| RF-ADM-13 | Must | Base | H4 |

## 5. Límites y dependencias que no se pueden omitir

- RF-CLI-12 se deja como extensión completa: el cobro de Recepción (RF-REC-05) sí entra en H2. Compartir cargos internos no implica que el portal del Cliente ni el pago online estén terminados.
- RNF-SEG-04 conserva 2FA obligatorio para Administrador. La sugerencia docente de “2FA como extensión” no se adopta globalmente.
- RF-CLI-11 y RN-10 conservan el consentimiento clínico firmado. Su mecanismo concreto se definirá antes de habilitar procedimientos afectados. Firma de consulta y consentimiento del responsable son actos distintos.
- RF-CLI-09 conserva tiempo real en internación. RF-CLI-23 y RF-VET-17 conservan el seguimiento de asistencia comprometido. Reducirlos requeriría una decisión de alcance y actualización de la matriz.
- RF-CLI-13 puede esperar, pero el modelo Cliente–Animal admite N:M y vigencia. La historia no se reconstruirá ni perderá si luego se habilita una transferencia.
- RF-VET-05 puede esperar, pero la firma clínica, la matrícula y las enmiendas de RF-VET-12/RF-VET-13 permanecen en H2.
- Los umbrales de agenda, stock y refuerzo requieren configuración válida desde el módulo que los usa; la pantalla administrativa completa puede terminar en H3. No se usarán valores inventados como reglas definitivas.
- Antes de entregar funcionalidades de H1/H2 al público se deben cumplir sus RNF de privacidad y derechos, aunque la demostración interna se construya antes de H3/H5.
- Permanecen fuera del alcance contabilidad/sueldos, homologación fiscal DGI, conexión directa a equipos clínicos, logística de comercio electrónico e interoperabilidad entre clínicas.

## 6. Trabajo de los dos integrantes

Propuesta organizativa sin asignar nombres: para cada recorrido, un integrante lidera dominio/API/datos y el otro interfaces/integración. Ambos revisan casos de uso, pruebas y decisiones, y pueden alternar el liderazgo. La entrega se evalúa por recorrido integrado, no por cantidad de capas terminadas por separado.

Codex apoya documentación, implementación autorizada, revisión y pruebas. Las estimaciones deben basarse en la disponibilidad real de los dos integrantes y en el trabajo observado del primer hito.

## 7. Criterio de cierre del alcance

La clasificación de los 74 RF queda completa en esta versión. Quedan abiertas la duración, la asignación nominal, los parámetros clínicos/operativos y la aprobación de las decisiones de diseño de cada flujo. Al completar H1 se revisan esfuerzo real, defectos y carga restante; cuando el profesor confirme fecha o rúbrica se ajustan los hitos con trazabilidad.

Próximo paquete de detalle: [Reserva y consulta](FLUJO_RESERVA_CONSULTA.md), acompañado por [historias de atención](HISTORIAS_USUARIO_ATENCION.md). Después se completarán “Internar animal”, “Cobrar consulta” y los casos del personal para asistencia.
