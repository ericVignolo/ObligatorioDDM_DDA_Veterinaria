# Especificación: Cobrar consulta (CU-REC-01)

Estado: implementada en escritorio con datos de demostración (A-023, 2026-11-16). Probada por Eric con F5 el 2026-11-16. La advertencia común de los prototipos está en `CONTEXTO_BASE.md` §3.1.

Decisiones: cobro **solo presencial** y sin pasarela (DT-ALC-02, Eric, 2026-11-16); el inventario no se vende (DT-ALC-01). Requisitos: RF-REC-05 (Must), RF-CLI-12 reducido a consultar, RN-32 a RN-34, RN-18, RNF-CON-03.

## Clasificación

| Tipo | Elemento |
|---|---|
| Decisión técnica | DT-ALC-02: sin pago en línea; el cargo se genera en la misma transacción que la firma (RNF-CON-03) |
| Requerimiento funcional | RF-REC-05, RF-CLI-12 (solo consultar) |
| Regla de negocio | RN-32, RN-33, RN-34 y las reglas de abajo |
| Diseño/UX | `/panel/cobros`, `/panel/comprobantes/{Id}`, sección «Mis cargos y comprobantes» en `/cuenta` |

## Actores y permisos

| Rol | Puede |
|---|---|
| Recepción | Ver cargos, **cobrar** (total o parcial), ver e imprimir comprobantes |
| Administrador | Ver cargos y comprobantes y **anular** un comprobante con nota de crédito |
| Veterinario | No cobra (su firma genera el cargo) |
| Cliente | Ver **sus** cargos, saldo y números de comprobante (RN-18); no paga en línea |

## Flujo

1. El veterinario firma la consulta. En el mismo paso se crea **un** cargo por la prestación del turno, con el precio vigente (RN-33). Un reintento no lo duplica. Una consulta en borrador o cancelada sin atención no genera cargo (RN-32).
2. El cliente se acerca a Recepción. Recepción elige el cargo, indica monto, medio y, en transferencias, el número de operación.
3. El sistema registra el cobro y emite un comprobante interno numerado (`C-000001`). El cargo pasa a **Pago parcial** o **Pagado** según el saldo.
4. Si hubo un error, el Administrador anula el comprobante con motivo: se emite una **nota de crédito** (`NC-000001`). El comprobante no se modifica ni se borra (RN-34) y el cargo recupera ese saldo.

## Estados del cargo

`Pendiente` (nada cobrado) → `Pago parcial` → `Pagado`. El estado se calcula con los comprobantes **no anulados**; anular uno puede volver el cargo a `Pago parcial` o `Pendiente`.

## Reglas

| Id | Regla |
|---|---|
| C-01 | Solo una consulta firmada genera cargo; hay uno por consulta (RN-32, idempotente). |
| C-02 | El monto del cargo queda fijado al firmar y no cambia si luego cambia la tarifa (RN-33). |
| C-03 | Solo Recepción registra cobros. |
| C-04 | El monto es un entero de pesos entre $ 1 y $ 1.000.000 y no supera el saldo. Un cargo pagado no se cobra de nuevo. |
| C-05 | Medios: efectivo, débito, crédito o transferencia. La transferencia exige número de operación. |
| C-06 | Los comprobantes se numeran en forma correlativa y nunca cambian (RN-34). |
| C-07 | Solo el Administrador anula, con motivo (hasta 200 caracteres); un comprobante se anula una sola vez. |
| C-08 | Un cliente ve solo sus cargos y comprobantes (RN-18). |

## Fuera de esta versión

Seña previa en cirugías (RN-35), aviso por deuda vencida al agendar (RN-36), cargo de asistencia (RN-58), cobro de internación, tarifas con fecha de vigencia editables (RF-ADM-02, A-027), reenvío del comprobante por correo (RF-REC-07) y comprobante fiscal. En la sección C la API guarda cargos, comprobantes y notas, y numera con una secuencia de la base.

## Pruebas

`tests/Veterinaria.Tests/CobroTests.cs` cubre C-01 a C-08. Pasos manuales en la bitácora `docs/sesiones/2026-11-16_cobro-a023.md`.
