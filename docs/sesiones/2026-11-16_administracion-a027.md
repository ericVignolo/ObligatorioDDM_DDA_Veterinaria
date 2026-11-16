# Sesión 2026-11-16 (4) — Configuración y administración, parte 1 (A-027) (Eric)

Rama: `eric/sesion-2026-11-16-administracion-a027` desde `desarrollo`. PR contra `desarrollo`.

## Qué se hizo

- Eric probó A-026 y la aprobó: PR #30 mezclado.
- **A-027, parte 1** (`AdministracionService` + `AuditoriaService`; especificación `docs/analisis/ESPECIFICACION_ADMINISTRACION.md`):
  - **Servicios y tarifas** (`/panel/admin/servicios`): alta, edición, publicar/despublicar, reserva en línea con duración, y **precio con fecha de vigencia**. El cargo de una consulta usa el precio vigente en la fecha del turno (RN-33).
  - **Usuarios y roles** (`/panel/admin/personal`): alta (veterinario con ficha de profesional libre), cambio de rol, bloqueo. Una cuenta bloqueada no ingresa; siempre queda un Administrador; la guardia no se bloquea sin reemplazo.
  - **Horarios** (`/panel/admin/horarios`): jornadas (consulta), licencias y feriados, sin pisar turnos activos.
  - **Parámetros** (`/panel/admin/parametros`): guardia y tarifas de asistencia, boxes (alta y fuera de servicio), días de próximo a vencer y de aviso de refuerzo, stock mínimo y versiones del consentimiento. Las reglas de la agenda se muestran fijas.
  - Toda acción del Administrador queda en la bitácora (la pantalla de auditoría llega en la parte 2).
- `PrestacionService` guarda el historial de precios; `CobroService` y `ConsentimientoService` buscan el servicio aunque esté despublicado.

## Decisiones

| Id | Decisión |
|---|---|
| D-ADM-01 | Las acciones de configuración pasan por `AdministracionService`, que valida el permiso y escribe la bitácora en un solo lugar. |
| D-ADM-02 | El precio no se edita: se programa una tarifa desde hoy o una fecha futura. |
| D-ADM-03 | Bloquear no borra; nunca se queda la clínica sin Administrador activo. |
| D-ADM-04 | Ausencias y feriados no se cargan sobre turnos activos: primero se reprograman o cancelan. |
| D-ADM-05 | Las franjas de la jornada y las reglas de la agenda (24 h, 2 h, 60 días, 15 min) no se editan en la demostración porque afectan turnos ya reservados. |

## Cómo probarlo

1. `admin@vitalia.uy` → Panel → **Servicios y tarifas**: crear «Ecografía» ($ 1.800, 30 min, reserva en línea). Probar reserva en línea con 0 minutos (error). Despublicar «Baño y estética» y ver que sale de `/servicios`.
2. «Precio» de la Consulta clínica general: $ 1.400 desde hoy. Atender y firmar una consulta: el cargo en Cobros es de $ 1.400. Probar una fecha pasada (error).
3. **Usuarios y roles**: crear a Sofía Méndez como Veterinaria con su ficha. Bloquear a Laura (Recepción) y probar ingresar con `recepcion@vitalia.uy` (no entra); desbloquear. Intentar bloquear a la Dra. Ferreira (es la guardia: error).
4. **Horarios**: agregar una licencia en un día con turnos (error) y en uno libre (bien). Agregar y quitar un feriado.
5. **Parámetros**: cambiar la guardia, la tarifa de asistencia y ver el costo nuevo en Asistencia; sacar de servicio el Box 1 (y probar el equino, ocupado: error); publicar la versión 1.1 del consentimiento.

## Qué falta

- [ ] Eric: probar con F5 y mezclar el PR.
- [ ] A-027 parte 2: pantalla de auditoría (RF-ADM-08) que junte la bitácora con lecturas de historia, movimientos de stock, cobros y asistencias, e indicadores (RF-ADM-07).
- [ ] A-028 comunicaciones y derechos, A-029 accesibilidad.
