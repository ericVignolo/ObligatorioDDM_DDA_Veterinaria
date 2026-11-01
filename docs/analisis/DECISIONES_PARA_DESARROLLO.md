# Decisiones que desbloquean el desarrollo

Actualizado: 2026-10-29

Eric delegó en Diego el 2026-10-09 las decisiones abiertas. El 2026-10-29 Diego cerró las del primer recorrido (catálogo → registro → reserva → consulta). Cada decisión cerrada está escrita, con su impacto, en el documento del módulo; este tablero solo indica dónde. «Cerrada» significa decidida y documentada, no implementada.

| Orden | Bloque | Decisiones | Estado | Dónde está el detalle |
|---|---|---|---|---|
| 1 | Catálogo API | D-CAT-01 a D-CAT-04 | Cerradas (D-CAT-01 Eric 2026-10-08; resto Diego 2026-10-29) | [Catálogo](ESPECIFICACION_CATALOGO_SERVICIOS_API.md#decisiones) |
| 2 | Registro | D-REG-01 a D-REG-05 | Cerradas (campos y verificación por correo: Eric 2026-10-08; resto Diego 2026-10-29) | [Registro](ESPECIFICACION_REGISTRO_CLIENTE.md#decisiones-cerradas) |
| 3 | Reserva | P-RC-01, P-RC-02 | Cerradas (Diego 2026-10-29) | [Flujo, sección 11](FLUJO_RESERVA_CONSULTA.md#11-decisiones-cerradas) |
| 4 | Consulta clínica | P-RC-03 a P-RC-07 | Cerradas (Diego 2026-10-29) | [Flujo, sección 11](FLUJO_RESERVA_CONSULTA.md#11-decisiones-cerradas) |
| 5 | Persistencia | DT-BD-01 (MySQL, Eric 2026-10-09), DT-BD-02 y P-RC-08 | Cerradas (Diego 2026-10-29) | [Contexto base, DT-BD-02](../contexto-base/CONTEXTO_BASE.md#2-estado-actual) |
| 6 | Organización | DT-ORG-02 y G-011 (flujo de ramas) | Cerradas (secciones: Eric 2026-10-20; ramas: Diego 2026-10-29) | [Plan de tareas, sección 2](PLAN_TAREAS_PROYECTO.md#2-ramas-previstas) |
| 7 | Alcance | Plan B de recortes | Vigente como contingencia, sin recortes aplicados | [Plan B](PLAN_B_ALCANCE.md) |
| 8 | Después del primer recorrido | Cobro por Recepción, internación, alertas sanitarias, parámetros de asistencia | Abiertas | Se deciden al abordar cada módulo. |

## Qué habilita este cierre

Con los bloques 1 a 6 cerrados, el primer recorrido ya no tiene decisiones pendientes. Queda habilitado empezar la sección A en `desktop`: esqueleto Blazor y catálogo público con datos de demostración (A-001 a A-003), seguido del registro y la reserva demostrativos (A-006 a A-015).

El bloque 8 no frena la sección A: cada módulo cierra sus decisiones antes de su propia pantalla, no antes de empezar a programar.

## Regla para nuevas decisiones

Quien decide registra la opción elegida, la fecha, el impacto en API, Blazor, móvil y pruebas, y actualiza el documento del módulo. Las decisiones de Eric no se reabren sin hablarlo con él. Una decisión que cambia arquitectura, tecnología o una regla obligatoria se escribe primero en `CONTEXTO_BASE.md`.
