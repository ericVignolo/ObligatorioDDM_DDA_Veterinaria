# Arranque del frente de ordenador

Actualizado: 2026-10-29

Estado: el primer recorrido tiene sus decisiones cerradas; la sección A puede empezar en la rama `desktop`.

Límites de lo que se construye en A (datos de demostración, sin persistencia ni seguridad real): ver los [principios vigentes](../contexto-base/CONTEXTO_BASE.md#31-principios-vigentes). Ramas y secuencia A → B → C: DT-ORG-02 en el mismo documento. Inventario de tareas: [PLAN_TAREAS_PROYECTO.md](PLAN_TAREAS_PROYECTO.md).

## Estado del trabajo

| Trabajo | Estado | Evidencia |
|---|---|---|
| Clasificar los 74 RF entre entrega base y extensiones | Documentado | [ALCANCE_ENTREGA.md](ALCANCE_ENTREGA.md) y [plan B](PLAN_B_ALCANCE.md). |
| Decisiones del primer recorrido (catálogo, registro, reserva, consulta, MySQL, ramas) | Cerradas 2026-10-29 | [Tablero de decisiones](DECISIONES_PARA_DESARROLLO.md). |
| Separar físicamente API, ordenador, móvil y pruebas | Preparado | Carpetas y README de cada área. |
| Rama de escritorio | Preparada | `desktop`, que ya contiene `feature/escritorio-api`. |
| Implementar y probar RF, RN y RNF | Pendiente | Todavía no hay solución de producto ni pruebas. |

## Próximos pasos

1. **A-001 a A-003:** esqueleto Blazor en `desktop/`, inicio público y catálogo con datos de demostración. El adaptador de datos devuelve la misma forma que el [contrato del catálogo](ESPECIFICACION_CATALOGO_SERVICIOS_API.md), para cambiarlo por la API en C sin tocar las pantallas.
2. **A-006 a A-011:** registro, verificación, sesión y reanudación de la intención, simulados según las [decisiones de registro](ESPECIFICACION_REGISTRO_CLIENTE.md#decisiones-cerradas).
3. **A-012 a A-018:** animales y reserva en cuatro pasos según el [flujo de reserva y consulta](FLUJO_RESERVA_CONSULTA.md) y el [diagrama del primer recorrido](../diagramas/INICIO_ESCRITORIO.md).
4. Medir cuánto llevó el recorrido catálogo → registro → reserva y aplicar el disparador del [plan B](PLAN_B_ALCANCE.md#3-disparador).

## Qué sigue abierto

- Cobro por Recepción, internación, alertas sanitarias y parámetros de asistencia: se cierran antes de sus pantallas (DT-ORG-03).
- Mapas de navegación definitivos del móvil (sección B).
- Fecha real de entrega.
