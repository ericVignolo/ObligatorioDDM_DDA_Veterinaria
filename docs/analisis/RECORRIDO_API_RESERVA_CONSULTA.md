# Recorrido propuesto de API: reservar → realizar consulta

Fecha: 2026-10-08

Estado: mapa de contrato con las decisiones P-RC-01 a P-RC-08 cerradas (2026-10-29); no implementado.

Las decisiones están en la [sección 11 del flujo](FLUJO_RESERVA_CONSULTA.md#11-decisiones-cerradas) y DT-BD-02. Formato de errores y paginación: D-CAT-03.

Este documento **no sustituye** [FLUJO_RESERVA_CONSULTA.md](FLUJO_RESERVA_CONSULTA.md), que contiene estados, permisos, pseudocódigo, excepciones y criterios de aceptación. Las rutas siguientes son decisiones técnicas **propuestas** para hacer visible qué consume cada interfaz. Su forma final depende de P-RC-01 a P-RC-08, especialmente agenda, permisos, stock, firma, tarifas y base de datos.

## Secuencia de negocio

| Paso | Actor | Operación API propuesta | Resultado y regla esencial |
|---|---|---|---|
| 1. Consultar servicio | Visitante/Cliente | `GET /api/v1/servicios/{id}` | Información pública; ver [catálogo](ESPECIFICACION_CATALOGO_SERVICIOS_API.md). |
| 2. Iniciar reserva | Cliente verificado | Consultar disponibilidad; ruta por fijar | La API calcula horarios, duración y recursos. La pantalla no determina capacidad por sí misma. |
| 3. Confirmar reserva | Cliente verificado o Recepción autorizada | `POST /api/v1/turnos` | Revalida identidad/responsabilidad y solapamientos; crea un turno Confirmado o el estado Pendiente que corresponda. Usa clave de operación para reintentos. No crea cargo clínico. |
| 4. Registrar llegada | Recepción | `POST /api/v1/turnos/{id}/llegada` | Confirmado → En espera; guarda llegada real y auditoría. |
| 5. Iniciar consulta | Veterinario asignado | `POST /api/v1/turnos/{id}/consultas` | En espera → En curso y crea **una** consulta Borrador en la misma operación. Si ya existe, recupera la autorizada o informa conflicto. |
| 6. Consultar/guardar borrador | Veterinario autorizado | `GET /api/v1/consultas/{id}` y actualización de borrador por definir | Lee historia autorizada, registra avances con versión esperada, adjuntos y consumos con operaciones protegidas. Un Cliente no lee el borrador. |
| 7. Revisar y firmar | Veterinario autorizado | `POST /api/v1/consultas/{id}/firma` | Con clave de operación y versión esperada; valida diagnóstico, permiso, matrícula, consentimiento, stock y tarifa. Una transacción firma, consolida consumos, genera cargos, finaliza el turno, audita y encola resumen. |
| 8. Consultar resultado | Cliente responsable o personal autorizado | Lectura de historia/cargos según permisos; rutas por fijar | El Cliente ve el registro firmado de su animal; Recepción ve cargos para cobrar, no historia clínica completa. Correo se envía después del commit. |

## Límites que Diego no debe asumir

- `GET` públicos del catálogo no autorizan reserva. El login/registro retoma la intención, pero la disponibilidad se recalcula antes de confirmar.
- La API, no Blazor ni React Native, hace las transiciones y aplica autorización por rol y relación con el animal/turno. Los identificadores enviados por el cliente no prueban titularidad.
- Dos intentos incompatibles de reserva no pueden tener éxito ambos: la API bloquea profesional, animal y recursos con `SELECT … FOR UPDATE` antes de validar e insertar (DT-BD-02).
- El borrador puede guardarse; la historia visible al Cliente incorpora la consulta firmada. Tras firmar, las correcciones clínicas son enmiendas; el cargo tiene corrección contable separada.
- Un error al cerrar no puede dejar firma sin cargo o consumir stock parcialmente. Una caída posterior del correo no revierte el cierre; el mensaje permanece pendiente de reintento.
- Los errores `400/401/403/404/409` usan Problem Details según D-CAT-03. No publicar diagnóstico ni contenido clínico en errores o logs.

**Estado de requisitos:** `RF-CLI-02` y los RF de consulta están especificados con sus decisiones cerradas, no implementados. Las rutas marcadas «por fijar» se definen al escribir el contrato OpenAPI en la sección C. El recorrido requiere pruebas de concurrencia, autorización, transacción e idempotencia antes de darse por cumplido.
