# Decisiones que desbloquean el desarrollo

Fecha: 2026-10-08

Estado: preguntas en curso; ninguna respuesta se presume aprobada.

Este tablero es una guía de conversación para Eric y Codex, y una referencia para Diego. No reemplaza la [matriz canónica](../contexto-base/MATRIZ_REQUISITOS.md) ni los pendientes detallados de [reserva y consulta](FLUJO_RESERVA_CONSULTA.md). Una decisión pasa a «cerrada» solo cuando se registra respuesta, fecha y documentos impactados. «Documentado» no significa «implementado».

| Orden | Bloque | Preguntas de cierre | Estado y destino |
|---|---|---|---|
| 1 | Catálogo API | D-CAT-01 **cerrada: siempre «Desde»**; D-CAT-02 **parcial: visible sin reserva en línea**; D-CAT-03 convenciones comunes; D-CAT-04 **parcial: ocultar sin precio publicable** | Parcial. [Especificación del catálogo](ESPECIFICACION_CATALOGO_SERVICIOS_API.md). |
| 2 | Registro | D-REG-01 **parcial: siete campos obligatorios definidos**; D-REG-02 **parcial: código por correo**; D-REG-03 recuperación; D-REG-04 documentos/decisiones; D-REG-05 reanudación segura | Parcial. [Especificación de registro](ESPECIFICACION_REGISTRO_CLIENTE.md). |
| 3 | Reserva | P-RC-01 medición de pasos; P-RC-02 seña, expiración, espera, ausencia, cancelación y sobreturnos; P-RC-08 motor de BD y exclusión de solapamientos | Abierto. [Flujo](FLUJO_RESERVA_CONSULTA.md) y [recorrido API](RECORRIDO_API_RESERVA_CONSULTA.md). |
| 4 | Consulta clínica | P-RC-03 stock; P-RC-04 atención interrumpida/correcciones; P-RC-05 permisos; P-RC-06 campos/firma/consentimiento; P-RC-07 responsable económico, tarifa y redondeo | Abierto. [Flujo](FLUJO_RESERVA_CONSULTA.md) y [recorrido API](RECORRIDO_API_RESERVA_CONSULTA.md). |
| 5 | Después del primer recorrido | Cobro por Recepción, internación, alertas sanitarias y parámetros de asistencia | Abierto. No ampliar el primer endpoint a estos procesos. |

## Primera ronda de preguntas a Eric

1. **Respondida 2026-10-08:** mostrar siempre el precio como «Desde» (**D-CAT-01**). Falta decidir la fuente del importe y qué ocurre si no hay tarifa publicable (**D-CAT-04**).
2. **Respondida 2026-10-08:** nombre, apellido, correo, contraseña, teléfono, documento y dirección del hogar serán obligatorios; falta la ficha de finalidad, acceso y conservación de cada dato (**D-REG-01**).
3. **Respondida 2026-10-08:** verificar mediante código enviado al correo; faltan caducidad y límites de intentos/reenvíos (**D-REG-02**, RN-38).

## Regla práctica para Diego

Puede revisar la arquitectura, preparar el entorno y estimar el contrato propuesto del catálogo. No debe interpretar las preguntas abiertas como decisiones aprobadas ni marcar RF-VIS-01 como terminado por crear un endpoint. Conforme al [arranque del escritorio](ARRANQUE_ESCRITORIO.md) y [AGENTS.md](../../AGENTS.md), el inicio del código de producto sigue condicionado al cierre de H0, en particular registro, pseudocódigo restante y decisiones abiertas. Cuando se levante esa condición, el primer incremento verificable será **API pública de servicios → vista Blazor → pruebas CAT-01 a CAT-06**.
