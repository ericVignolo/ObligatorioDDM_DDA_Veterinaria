# Decisiones que desbloquean el desarrollo

Fecha: 2026-10-09

Estado: decisiones abiertas asignadas a Diego; ninguna respuesta pendiente se presume aprobada.

Eric delegó el 2026-10-09 en Diego las **decisiones aún abiertas**. Eric y Codex mantendrán la documentación y podrán revisar la implementación. Las decisiones ya tomadas por Eric se conservan. Este tablero no reemplaza la [matriz canónica](../contexto-base/MATRIZ_REQUISITOS.md) ni los pendientes detallados de [reserva y consulta](FLUJO_RESERVA_CONSULTA.md). Una decisión pasa a «cerrada» solo cuando Diego registra la opción elegida, justificación, fecha, impacto en API/Blazor/móvil/pruebas y documentos actualizados. «Documentado» no significa «implementado».

| Orden | Bloque | Preguntas de cierre | Estado, responsable y destino |
|---|---|---|---|
| 1 | Catálogo API | D-CAT-01 **cerrada: siempre «Desde»**; D-CAT-02 **parcial: visible sin reserva en línea**; D-CAT-03 convenciones comunes; D-CAT-04 **parcial: ocultar sin precio publicable** | Diego cierra lo restante. [Especificación del catálogo](ESPECIFICACION_CATALOGO_SERVICIOS_API.md). |
| 2 | Registro | D-REG-01 **parcial: siete campos obligatorios definidos**; D-REG-02 **parcial: código por correo**; D-REG-03 recuperación; D-REG-04 documentos/decisiones; D-REG-05 reanudación segura | Diego cierra lo restante. [Especificación de registro](ESPECIFICACION_REGISTRO_CLIENTE.md). |
| 3 | Reserva | P-RC-01 medición de pasos; P-RC-02 seña, expiración, espera, ausencia, cancelación y sobreturnos; P-RC-08 **parcial: MySQL elegido**, pendiente versión/proveedor y exclusión de solapamientos | Diego cierra lo restante. [Flujo](FLUJO_RESERVA_CONSULTA.md) y [recorrido API](RECORRIDO_API_RESERVA_CONSULTA.md). |
| 4 | Consulta clínica | P-RC-03 stock; P-RC-04 atención interrumpida/correcciones; P-RC-05 permisos; P-RC-06 campos/firma/consentimiento; P-RC-07 responsable económico, tarifa y redondeo | Diego. [Flujo](FLUJO_RESERVA_CONSULTA.md) y [recorrido API](RECORRIDO_API_RESERVA_CONSULTA.md). |
| 5 | Después del primer recorrido | Cobro por Recepción, internación, alertas sanitarias y parámetros de asistencia | Diego cuando se aborde cada módulo. No ampliar el primer endpoint a estos procesos. |

## Decisiones ya confirmadas por Eric

1. **Respondida 2026-10-08:** mostrar siempre el precio como «Desde» (**D-CAT-01**) y ocultar servicios sin importe publicable (**D-CAT-04 parcial**). Diego define la fuente del importe.
2. **Respondida 2026-10-08:** nombre, apellido, correo, contraseña, teléfono, documento y dirección del hogar serán obligatorios; falta la ficha de finalidad, acceso y conservación de cada dato (**D-REG-01**).
3. **Respondida 2026-10-08:** verificar mediante código enviado al correo; faltan caducidad y límites de intentos/reenvíos (**D-REG-02**, RN-38).

También quedó confirmado que un servicio puede mostrarse sin reserva en línea, con «Consultar/llamar» (**D-CAT-02 parcial**). Diego define los criterios restantes de publicación.

El 2026-10-09 Eric eligió MySQL como motor de la futura base de datos (**DT-BD-01**, P-RC-08 parcial); Diego define los detalles restantes de persistencia y concurrencia.

## Regla práctica para Diego

Diego puede tomar y documentar las decisiones pendientes, revisar la arquitectura y preparar el entorno. No debe interpretar una pregunta abierta como respuesta aprobada ni marcar RF-VIS-01 como terminado por crear un endpoint. Debe conservar las decisiones confirmadas, la redacción canónica de RF/RN/RNF y la arquitectura obligatoria; cualquier cambio a estas reglas se documenta primero en contexto. Conforme al [arranque del escritorio](ARRANQUE_ESCRITORIO.md) y [AGENTS.md](../../AGENTS.md), el inicio del código de producto sigue condicionado al cierre de H0, en particular registro, pseudocódigo restante y decisiones abiertas. Cuando se levante esa condición, el primer incremento verificable será **API pública de servicios → vista Blazor → pruebas CAT-01 a CAT-06**.
