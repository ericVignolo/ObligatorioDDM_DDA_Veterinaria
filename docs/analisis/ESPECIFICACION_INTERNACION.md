# Especificación: Internar animal (CU-VET-02)

Estado: implementada en escritorio con datos de demostración (A-024, 2026-11-16), pendiente de la prueba con F5. La advertencia común de los prototipos está en `CONTEXTO_BASE.md` §3.1.

Requisitos: RF-VET-09, RF-VET-10, RF-VET-11, RF-CLI-09, RN-18, RN-21 a RN-25, RNF-DIS-04. Decisión de Eric del 31/10: Recepción carga lo administrativo; el Veterinario, lo clínico y el alta.

## Clasificación

| Tipo | Elemento |
|---|---|
| Requerimiento funcional | RF-VET-09 (ingreso con box y plan), RF-VET-10 (novedades internas/visibles), RF-VET-11 (evolución y alta), RF-CLI-09 (seguimiento del cliente) |
| Regla de negocio | RN-21 a RN-25 y las reglas I-xx de abajo |
| Requerimiento no funcional | RNF-DIS-04: sin tiempo real, las novedades se ven al actualizar |
| Diseño/UX | `/panel/internacion`, `/panel/internacion/{Id}`, sección «Internación» en `/animales/{Id}` |

## Estados

`Admitida` (Recepción cargó los datos) → `Internado` (un veterinario asignó box y plan) → `Alta`.
Una admisión sin ingreso clínico puede pasar a `Anulada`. Abiertas = `Admitida` o `Internado`.

## Permisos

| Rol | Puede |
|---|---|
| Recepción | Admitir (animal, motivo, teléfono, observaciones); anular una admisión `Admitida` con motivo; ver boxes y lo visible |
| Veterinario (cualquiera) | Ingreso clínico; publicar novedad o evolución, visible o interna; rectificar; dar el alta; ver todo, incluido el plan y las notas internas |
| Administrador | Ver boxes, internaciones y lo visible |
| Cliente | Ver las internaciones de **sus** animales (no las anuladas), las entradas visibles y, al alta, la evolución final y las indicaciones |

## Boxes de demostración

Box 1, Box 2 y Box 3 (pequeños animales) y Box equino (grandes animales: caballo y vaca). Al arrancar, Canela (yegua de Pedro Gómez, cliente 101, animal 4) está internada en el Box equino con 4 entradas (2 visibles y 2 internas).

## Reglas

| Id | Regla |
|---|---|
| I-01 | Solo Recepción admite y anula admisiones. |
| I-02 | Un animal no puede tener dos internaciones abiertas (RN-21). Un grupo de animales no se interna. |
| I-03 | Motivo (hasta 200) y teléfono (hasta 30) son obligatorios; observaciones hasta 500. |
| I-04 | Solo se anula una admisión `Admitida`, con motivo (hasta 200). |
| I-05 | Solo un Veterinario hace el ingreso clínico, sobre una internación `Admitida`, con box y plan (hasta 2000). |
| I-06 | Un box admite un solo internado (RN-22) y debe ser del tipo de la especie (grandes: caballo y vaca). |
| I-07 | Solo un Veterinario publica, solo en una internación `Internado`, texto obligatorio de hasta 1000; tipo «Novedad» o «Evolución» (RN-23). |
| I-08 | Una entrada no se edita ni se borra: se rectifica con una entrada nueva de tipo «Rectificación», con la misma visibilidad que la original (RN-24). |
| I-09 | El cliente y el personal no veterinario solo ven las entradas visibles (RN-23). |
| I-10 | El alta exige evolución final (hasta 1000) e indicaciones (hasta 2000); la da un Veterinario sobre una internación `Internado`; libera el box (RN-25). |
| I-11 | Un cliente solo ve las internaciones de sus animales (RN-18). |
| I-12 | Rectificar solo mientras la internación está `Internado`. Se puede rectificar una rectificación. El tipo y la visibilidad que pida el formulario se ignoran: queda «Rectificación» con la visibilidad de la original. |
| I-13 | Al alta, `BoxId` se conserva como historial; el box queda libre porque solo ocupa una internación `Internado`. |
| I-14 | Un texto con solo espacios cuenta como vacío. El teléfono no tiene formato fijo, solo largo. |
| I-15 | Recepción puede admitir cualquier animal registrado de cualquier cliente. El Administrador cuenta como personal no veterinario. |

## Fuera de esta versión

Tiempo real con la API (RF-CLI-09, en la sección C; la demostración usa «Actualizar novedades», R4/RNF-DIS-04), cargo de la internación, consentimiento informado (A-025), administración de boxes (RF-ADM-03, A-027) y aviso por correo de las novedades.

## Pruebas

`tests/Veterinaria.Tests/InternacionTests.cs` (reglas, caja blanca) y `tests/Veterinaria.Tests/CajaNegra/` (pruebas del agente probador, escritas solo desde esta especificación). Las reglas I-12 a I-15 salieron de las preguntas del probador.
