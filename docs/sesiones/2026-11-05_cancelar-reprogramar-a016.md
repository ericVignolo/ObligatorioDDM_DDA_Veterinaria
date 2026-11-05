# Sesión 2026-11-05 (3) — Cancelar y reprogramar turnos (A-016) y servicio de baño

Participantes: Eric (con Claude). Rama: `eric/sesion-2026-11-05-3` desde `desktop` → un PR a `desktop`.

## Contexto al iniciar

- PR #14 (reserva en cuatro pasos) probado por Eric y mezclado; `main` al día (PR #15). **El primer recorrido catálogo → registro → reserva está completo.**
- Eric propuso un perfil del cliente con reservas, animales, datos personales, servicios usados («reservar de nuevo») y un botón de historia clínica por animal. Es la tarea **A-009**; se hará cuando llegue en el plan. La historia clínica real llega con A-019/A-020.

## Decisiones (Eric, 2026-11-05)

- Se publica el servicio **«Baño y estética»**: 60 min, solo **perros y gatos** por ahora (las especies se revisan en las fases finales). Lo atiende una profesional ficticia de estética, **Sofía Méndez** (no veterinaria).
- La cantidad de servicios disponibles se revisa más adelante.
- Alta y baja de servicios: la hace el **Administrador**, como dice RF-ADM-02 (no Recepción).
- A-016 se hace ahora, en el orden del plan.

## Qué se hizo

**Catálogo y equipo**
- `PrestacionService`: «Estética» (no publicado) pasa a **«Baño y estética»** publicado y reservable (60 min, desde $ 900). Para seguir probando que lo no publicado no aparece, se agrega «Guardería» sin publicar (`/servicios/7`).
- `ProfesionalService`: Sofía Méndez — lunes a viernes 10–18 (descanso 13–14) y sábados 9–13; perros y gatos; solo baño y estética.
- `Profesionales`: «Conocé al equipo» en lugar de «a los veterinarios».

**A-016 — Cancelar y reprogramar**
- `Turno` suma motivo y fecha de cancelación y el horario anterior al reprogramar.
- `AgendaService`: `PuedeGestionar` (Confirmado y más de 24 h, RN-06), `Cancelar` (libera el horario), `Reprogramar` (mismo número de turno; si el nuevo horario no está libre, el turno queda como estaba), `HistorialDe` y `ObtenerDe` (solo turnos propios).
- `Turnos` (`/turnos`): pestañas **Próximos** e **Historial**. Cada turno próximo tiene **Reprogramar** y **Cancelar** (con confirmación y motivo opcional). Con 24 h o menos, en lugar de los botones muestra «llamá a Recepción».
- `Reservar` (`/reservar?reprogramar={id}`): abre directo en el paso 3 con animal, servicio y profesional fijos; muestra el horario actual y confirma el cambio.

## Cómo probarlo

1. GitHub Desktop → *Fetch origin* → rama `eric/sesion-2026-11-05-3` → F5.
2. Servicios: aparece «Baño y estética». `/servicios/7` dice «no disponible». Profesionales: aparece Sofía Méndez con sus horarios.
3. Reservar «Baño y estética» con un perro: solo aparece Sofía Méndez, turnos de 60 min. Con el caballo: nadie lo atiende.
4. Reservar un turno para dentro de 3 días → Mis turnos → **Reprogramar** → elegir otra hora → «Turno reprogramado» con el mismo número. El horario viejo vuelve a estar libre.
5. **Cancelar** con un motivo → pasa al **Historial** como Cancelado con el motivo; el horario queda libre.
6. Reservar un turno para mañana temprano (menos de 24 h): en Mis turnos aparece «llamá a Recepción» en lugar de los botones.
7. Al reprogramar, en el paso 4 usar «simular que otro cliente reservó» → Confirmar: avisa y el turno sigue con su horario anterior.

> No compilado desde el entorno de Claude. Si Visual Studio marca un error, se corrige en esta rama.

## Pendientes

- [x] Eric probó la rama: funciona. PR #16 mezclado; A-016 marcada.
- [ ] A-009: «Mi cuenta» (datos personales, animales con botón de historia clínica, turnos, servicios usados con «Reservar de nuevo»).
- [ ] A-017: agenda de Recepción y Veterinario (llegada, ausente, cancelación fuera de las 24 h, recursos).
- [ ] Definir cuántos servicios habrá y para qué especies es «Baño y estética» (fases finales).
- [ ] Al completar el primer recorrido: medir el tiempo para el Plan B (falta fecha de entrega).
- [ ] Borrar `VeterinariaBrain_viejo`, las copias viejas del repositorio y las ramas ya integradas en GitHub.
- [ ] Diego: proveedor MySQL de DT-BD-02 (EF Core 9 sin soporte desde el 2026-11-10).
