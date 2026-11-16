# Sesión 2026-11-16 (2) — Internación (A-024), consentimiento (A-025) y agente probador (Eric)

Rama: `eric/sesion-2026-11-16-internacion-a024` desde `desarrollo`. PR contra `desarrollo`.

## Qué se hizo

- Eric probó A-023 con F5 (cobro, firma e insumos) y la aprobó: PR #28 mezclado.
- **Agente probador de caja negra** (`.claude/agents/probador.md`, pedido de Eric): escribe pruebas en `tests/Veterinaria.Tests/CajaNegra/` leyendo solo la especificación, los modelos y las firmas públicas. Regla agregada en `AGENTS.md`. Primer uso: internación.
- **A-024 — Internación:**
  - `InternacionService`: 4 boxes (3 de pequeños animales y uno equino), admisión de Recepción, anulación de una admisión sin ingreso, ingreso clínico del Veterinario con box libre y del tipo de la especie (RN-22) y plan, novedades y evoluciones visibles o internas (RN-23), rectificación sin borrar (RN-24), alta con evolución final e indicaciones que libera el box (RN-25), una internación abierta por animal (RN-21). Canela arranca internada para la demo.
  - Pantallas: `/panel/internacion` (boxes, en curso, admisión, altas) y `/panel/internacion/{Id}` (ingreso, seguimiento, rectificar, alta). El cliente ve en `/animales/{Id}` la internación de su animal, solo lo visible, con «Actualizar novedades» y las indicaciones al alta.
  - Especificación nueva: `docs/analisis/ESPECIFICACION_INTERNACION.md`.

- **A-025 — Consentimiento informado:**
  - Prestación nueva de demostración **Castración** ($ 4.500, 90 min, Dra. Ferreira) que pide consentimiento; turno de demo de Mishi hoy a las 12:00 (días hábiles).
  - `ConsentimientoService`: documento por prestación y versión; el responsable firma marcando la casilla y escribiendo su nombre completo (P-RC-06), o rechaza; se guarda versión, animal, procedimiento, fecha, canal y huella; nada se borra; una versión nueva obliga a volver a firmar.
  - **RN-10:** `ConsultaService.Iniciar` no pasa la cirugía a En curso sin consentimiento firmado.
  - Pantallas: `/consentimiento/{TurnoId}` (cliente), aviso en `/cuenta`, botón en `/turnos` y estado del consentimiento en las agendas de Recepción y del Veterinario.
  - Especificación nueva: `docs/analisis/ESPECIFICACION_CONSENTIMIENTO.md`.

## Decisiones

| Id | Decisión |
|---|---|
| D-INT-01 | Estados: Admitida → Internado → Alta; una admisión sin ingreso clínico se puede anular (Anulada). |
| D-INT-02 | Cualquier veterinario hace el ingreso, publica y da el alta (guardia), no solo el que ingresó. |
| D-INT-03 | Las notas internas y el plan los ven solo los veterinarios; Recepción y Administración ven lo mismo que el cliente. |
| D-INT-04 | Box del tipo de la especie: caballo y vaca van al box de grandes animales; los grupos no se internan. |
| D-INT-05 | La rectificación hereda la visibilidad de la entrada que corrige. |
| D-CON-01 | Mecanismo de P-RC-06: casilla sin marcar + nombre completo igual al de la cuenta (sin importar mayúsculas ni espacios). No es firma electrónica avanzada. |
| D-CON-02 | El consentimiento es por turno; la última respuesta manda. Rechazar no cancela el turno: lo deja bloqueado hasta que firme o se cancele. |
| D-CON-03 | Una versión nueva del documento invalida lo firmado con la anterior. La pantalla para publicar versiones llega con A-027. |
| D-INT-06 | Sin tiempo real en la demo: el cliente toca «Actualizar novedades» (RNF-DIS-04, R4 del Plan B solo para la sección A; en la sección C se mantiene RF-CLI-09). |

## Cómo probarlo

1. `recepcion@vitalia.uy` → Panel → **Internación**: Canela está en el Box equino. Admitir a Toby (de Ana) con motivo y teléfono. Probar admitir a Toby otra vez (error) y al «Rodeo La Esperanza» (no aparece: es un grupo).
2. Salir e ingresar como `ferreira@vitalia.uy` → Internación → abrir Toby → elegir Box 1 y plan → **Internar**. El Box equino no aparece en la lista (es de grandes animales).
3. Publicar una novedad **visible** y una evolución **interna**. Rectificar la visible.
4. Salir e ingresar como `ana.demo@vitalia.uy` / `Vitalia2026!` → Mis animales → Toby: se ve la internación con la novedad y su corrección, sin la nota interna.
5. Volver como veterinaria → **Dar el alta** sin indicaciones (error) y después completo. El Box 1 queda libre; Ana ve las indicaciones.
6. Recepción: admitir a Mishi y **anularla** con motivo antes del ingreso.

### Consentimiento (A-025)

7. `ana.demo@vitalia.uy` → **Mi cuenta**: aviso «Tenés 1 consentimiento para firmar» (castración de Mishi, si hoy es día hábil). Si no, reservar una **Castración** para un perro o gato con la Dra. Ferreira.
8. Abrir el consentimiento: probar firmar sin marcar la casilla y con otro nombre (errores). Tocar «No autorizo» y después firmar con «Ana Rodríguez» → queda el historial con las dos respuestas.
9. Recepción → Agenda del día: el turno muestra «Consentimiento: firmado» (o pendiente). Registrar la llegada.
10. `ferreira@vitalia.uy` → Mi agenda → Iniciar atención: con el consentimiento pendiente, error RN-10; firmado, entra a la consulta.

## Qué falta

- [ ] Eric: probar con F5 y mezclar el PR.
- [ ] Borrar en GitHub las ramas ya integradas (el proxy de Claude no deja borrar ramas; ver el mensaje de la sesión).
- [ ] Siguiente: A-026 panel de asistencia, A-027 configuración, A-028 comunicaciones, A-029 accesibilidad.
