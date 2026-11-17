# Especificación de las reglas pendientes de la sección A

Decisiones de Eric del 2026-11-17, tras la revisión de la sección A (`REVISION_SECCION_A.md`). Las advertencias comunes están en `CONTEXTO_BASE.md`, sección 3.1: la autoridad real es la API.

## RN-08 — Ausencias sin aviso

| Id | Regla |
|---|---|
| RP-01 | Se cuentan los turnos del cliente en estado «Ausente» con inicio dentro de los últimos 12 meses (`AgendaService.AusenciasRecientes`). |
| RP-02 | Desde 2 ausencias (`AvisoAusencias`) el cliente ve un aviso al reservar. Desde `LimiteAusencias` (3 por defecto) `Reservar` del cliente falla con un mensaje que lo deriva a Recepción y no crea turno. `MensajeAusencias` devuelve "" si tiene menos de 2. |
| RP-03 | Recepción o el Administrador pueden dar el turno igual por mostrador (`ReservarPorMostrador`). No hay recargos por ausencias. |

## RN-09 — Turno por mostrador y sobreturno

| Id | Regla |
|---|---|
| RP-04 | `ReservarPorMostrador(cliente, animal, servicio, profesional, inicio, sobreturno, motivo, persona, out turno)`: solo Recepción o Administrador; el animal tiene que ser del cliente; el servicio tiene que tener duración. |
| RP-05 | Sin sobreturno, el horario tiene que estar entre los libres (`HorariosLibres`). |
| RP-06 | Con sobreturno: motivo de al menos 5 caracteres; no en un horario ya pasado; el inicio cae dentro de una franja de la jornada del profesional ese día, sin feriado ni ausencia; puede superponerse con otro turno del profesional, pero no con otro turno activo del mismo animal. |
| RP-07 | El turno queda con `EsSobreturno`, `MotivoSobreturno` y `CreadoPor`; el sobreturno se registra en la auditoría con entidad «Agenda» y acción «Sobreturno», sin el motivo. |

## RN-16 — Intervalo mínimo entre dosis

| Id | Regla |
|---|---|
| RP-08 | Cada producto sanitario tiene `IntervaloMinimoDias` (demostración: 330 para vacunas anuales, 150 para la clostridial, 75 para el antiparasitario interno, 25 para el externo). |
| RP-09 | Si el animal tiene una aplicación anterior del mismo producto y hoy es antes de esa fecha + `IntervaloMinimoDias`, `Registrar` exige `Justificacion` de al menos 10 caracteres; sin ella falla con un mensaje que menciona RN-16 y no aparta stock. |
| RP-10 | Con justificación se registra y queda en `JustificacionAdelanto`. Sin adelanto, `JustificacionAdelanto` queda vacía aunque se haya escrito algo. |

## RN-17 — Alergias en el tratamiento

| Id | Regla |
|---|---|
| RP-11 | Alergias del animal: las de sus consultas firmadas más las de la consulta en curso, separadas por coma, punto y coma, barra, salto de línea o « y »; se descartan las de menos de 3 letras y «ninguna», «no», «sin alergias», etc. (`ConsultaService.AlergiasDe`). |
| RP-12 | Hay choque si una alergia aparece como palabra completa en el tratamiento o las indicaciones, sin distinguir mayúsculas ni tildes (`AlergiasEnTratamiento`). |
| RP-13 | Con choque, `Firmar` falla (mensaje con RN-17, la consulta sigue en borrador) salvo que `ConfirmaAlergia` sea verdadero; al firmar, `Consulta.AlergiaConfirmada` guarda las alergias indicadas. Guardar el borrador no se bloquea. |

## RN-35 — Seña de cirugía

| Id | Regla |
|---|---|
| RP-14 | Un turno de una prestación que pide consentimiento (cirugía) se crea en estado «Pendiente de seña» (`AgendaService.PendienteSena`), por el cliente o por mostrador. Ocupa el horario, se puede cancelar y admite firmar el consentimiento, pero no registrar la llegada. |
| RP-15 | `CobroService.GenerarSena(turno)` crea, una sola vez por turno, un cargo `EsSena` por `PorcentajeSena` % (30 por defecto) del precio vigente, redondeado. Devuelve null para un turno que no está pendiente de seña y no tiene seña. |
| RP-16 | Cuando la seña queda totalmente pagada, el turno pasa a «Confirmado». Un pago parcial no confirma. Si se anula un pago y la seña vuelve a tener saldo, el turno confirmado vuelve a «Pendiente de seña». |
| RP-17 | Al firmar la consulta, lo pagado de seña se descuenta del cargo final. |

## RN-36 — Deuda vencida

| Id | Regla |
|---|---|
| RP-18 | Deuda vencida: suma de saldos de cargos del cliente que no son seña y tienen más de 30 días (`DiasParaVencer`). |
| RP-19 | Si supera `LimiteDeuda` ($ 3.000 por defecto), `AvisoDeuda` devuelve un aviso que dice que se puede agendar igual; si no, "". Nunca bloquea una reserva ni una urgencia. |

## Parámetros

| Id | Regla |
|---|---|
| RP-20 | `AdministracionService.CambiarParametrosAgenda(limiteAusencias, porcentajeSena, limiteDeuda, admin)`: solo el Administrador; ausencias 2 a 10, seña 10 a 100 %, deuda 0 a 1.000.000. Fuera de rango no cambia nada. |

## Límites

- Una seña pagada de un turno que después se cancela no se devuelve automáticamente: lo resuelve Recepción con una nota de crédito.
- Una seña nunca pagada no vence sola; Recepción cancela el turno.
- La demostración no trae cargos viejos: el aviso de deuda se ve al cambiar el límite a $ 0 con un cargo pendiente de más de 30 días.

## Aclaraciones del probador

- RP-17: «se descuenta» = el `Monto` del cargo final baja en lo pagado de seña y su descripción lo indica.
- Bordes: 12 meses y 30 días se miden contra `DateTime.Now`. Motivos y justificaciones se recortan antes de contar caracteres. `CreadoPor` guarda el nombre de la persona; los turnos por mostrador sin sobreturno no se auditan.
- RN-16 cuenta solo aplicaciones anteriores registradas en el carnet (consultas firmadas o registros previos), no las de la consulta en curso. Toby tiene el producto 6 a los 86 días, por encima del mínimo de 75.
- Seña: el redondeo es hacia arriba en 0,5; una segunda llamada a `GenerarSena` devuelve la misma seña; el precio es el vigente en la fecha del turno. No se puede cobrar más que el saldo. La anulación devuelve a «Pendiente de seña» solo un turno «Confirmado».
- El cambio de parámetros se audita como «Reglas de agenda actualizadas».
