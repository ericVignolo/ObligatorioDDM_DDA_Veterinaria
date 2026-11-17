# Sesión 2026-11-17 — Reglas pendientes de la sección A

Rama: `eric/sesion-2026-11-17-reglas-pendientes`. Antes se mezclaron A-030 (PR #35) y A-031 (PR #36).

## Decisiones de Eric
- D-COM-03: el aviso de refuerzo de vacunas llega por correo por defecto (es necesario); se puede apagar.
- Construir RN-08, 09, 16, 17, 35 y 36 con: 3 ausencias en 12 meses (aviso desde 2), seña del 30 %, aviso por deuda vencida desde $ 3.000; los tres configurables por el Administrador.

## Qué se hizo
- RN-08: aviso y bloqueo de la reserva en línea por ausencias; Recepción puede dar el turno.
- RN-09: «Dar un turno en el mostrador» en la agenda de Recepción, con sobreturno, motivo y registro en la bitácora.
- RN-16: intervalo mínimo por producto; adelantar exige justificación, que queda en la aplicación.
- RN-17: si el tratamiento incluye una alergia registrada, firmar pide confirmación explícita y queda asentado.
- RN-35: la cirugía queda «Pendiente de seña» hasta cobrar el 30 %; la seña se descuenta del cargo final.
- RN-36: aviso al agendar si la deuda vencida supera el límite; nunca bloquea.
- Parámetros en `/panel/admin/parametros`. Especificación: `docs/analisis/ESPECIFICACION_REGLAS_PENDIENTES.md`.

## Cómo probarlo (F5)
1. Como Ana, reservá una castración: queda «Pendiente de seña» y el paso 4 avisa la seña.
2. Como Recepción, en Cobros aparece la seña; cobrala completa: el turno pasa a Confirmado.
3. En la agenda de Recepción, dá un turno por mostrador y un sobreturno en un horario ocupado (con motivo).
4. Como Ferreira, en una consulta de Toby registrá la vacuna polivalente: pide justificación (se aplicó hace 60 días).
5. Escribí «Penicilina» en alergias y en tratamiento y firmá: pide confirmar.
6. Como Administrador, cambiá los parámetros.

## Qué falta
- Diseño visual (sistema de diseño en el repo y luego OpenPencil), móvil e integración con la API.
