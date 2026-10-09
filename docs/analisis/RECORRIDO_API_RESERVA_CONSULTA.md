# Recorrido propuesto de API: reservar → realizar consulta

Fecha: 2026-10-08

Estado: mapa de contrato para cierre por Diego; no congelado ni implementado.

Responsable de cerrar P-RC-01 a P-RC-08: Diego, por decisión de Eric del 2026-10-09. Cada cierre debe quedar documentado antes de implementar la operación afectada.

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
- Dos intentos incompatibles de reserva no pueden tener éxito ambos. MySQL está elegido como motor futuro (DT-BD-01); la estrategia concreta de intervalos está pendiente en P-RC-08.
- El borrador puede guardarse; la historia visible al Cliente incorpora la consulta firmada. Tras firmar, las correcciones clínicas son enmiendas; el cargo tiene corrección contable separada.
- Un error al cerrar no puede dejar firma sin cargo o consumir stock parcialmente. Una caída posterior del correo no revierte el cierre; el mensaje permanece pendiente de reintento.
- Los detalles de `400/401/403/404/409` y del cuerpo de error siguen la convención común de API aún por fijar. No publicar diagnóstico ni contenido clínico en errores o logs.

## Próxima ronda de decisiones para cerrar este contrato

1. Reserva: P-RC-01 y P-RC-02 — pasos, seña, vencimiento, cancelación y estados excepcionales.
2. Consulta: P-RC-03 a P-RC-07 — stock, interrupción, permisos, firma y responsable económico.
3. Persistencia: P-RC-08 — motor relacional, exclusión de intervalos y reintentos.

**Estado de requisitos:** `RF-CLI-02` y los RF de consulta están especificados parcialmente en el flujo integrado, no implementados. El catálogo público se podrá verificar de forma separada; este recorrido requiere pruebas de concurrencia, autorización, transacción e idempotencia antes de darse por cumplido.
