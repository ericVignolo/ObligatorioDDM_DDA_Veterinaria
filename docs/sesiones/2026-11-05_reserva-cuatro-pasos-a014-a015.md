# Sesión 2026-11-05 (2) — Disponibilidad y reserva en cuatro pasos (A-014, A-015)

Participantes: Eric (con Claude). Rama: `eric/sesion-2026-11-05-2` desde `desktop` → un PR a `desktop`.

## Contexto al iniciar

- PR #12 (cerebro, recuperación de contraseña y animales) probado por Eric y mezclado; `main` al día (PR #13).
- Eric aprobó la propuesta de los cuatro pasos con estos ajustes.

## Decisiones (Eric, 2026-11-05)

- Duraciones: consulta clínica general **30 min**, vacunación **15 min**, desparasitación **15 min**.
- Quién atiende qué:

  | Profesional | Especies | Servicios |
  |---|---|---|
  | Dra. Ferreira | Perro, gato, ave, conejo, reptil, otra | Consulta, vacunación, desparasitación |
  | Dr. Silveira | Vaca, oveja, cabra, cerdo | Consulta, vacunación, desparasitación |
  | Dra. Acosta | Caballo | Consulta |

  Solo aparecen los profesionales que hacen **ese servicio** y atienden **esa especie**.
- Horarios de inicio **cada 15 minutos**, pero cada turno **bloquea toda su duración**: una consulta de 30 min a las 09:00 impide reservar a las 09:15.
- Se respetan la **jornada**, los **descansos**, las **ausencias** de cada profesional y los **días no laborables** de la clínica.
- **2 h** de anticipación mínima, **60 días** hacia adelante y cancelación sin costo hasta **24 h** antes (P-RC-02).

## Qué se hizo

**A-014 — Disponibilidad**
- `Models/FranjaHoraria.cs` y `Models/Ausencia.cs`. `Profesional` cambia la lista de textos `Horarios` por `Jornada`, `Descansos`, `Ausencias`, `Especies` y `PrestacionIds`.

  | Profesional | Jornada | Descanso | Ausencia de ejemplo |
  |---|---|---|---|
  | Ferreira | Lun a vie 9:00–14:00 | 11:30–12:00 | Dentro de 8 días (capacitación) |
  | Silveira | Lun a vie 14:00–19:00 y sáb 9:00–13:00 | 16:30–17:00 (lun a vie) | Dentro de 18 a 22 días (licencia) |
  | Acosta | Mar y jue 9:00–17:00 | 12:30–13:30 | — |

  Las ausencias de ejemplo son relativas a hoy para que siempre se vean en la demostración.
- `ProfesionalDetalle` muestra la jornada con descansos, las próximas ausencias y las especies que atiende.
- `Prestacion.DuracionMinutos` y `PrestacionService.ObtenerReservables()`.
- `Services/AgendaService.cs`: genera los horarios libres y descarta los que pisan un descanso, otro turno del profesional o del mismo animal, la anticipación mínima, el horizonte, feriados (25/12, 1/1 y 6/1) y ausencias. Trae dos turnos de otro cliente para mañana con la Dra. Ferreira (10:00 y 12:00), para que se vean horarios ocupados.

**A-015 — Reserva en cuatro pasos**
- `Pages/Reservar.razor` (`/reservar`) con el indicador `Components/PasosReserva.razor`:
  1. **Animal:** si hay uno solo viene elegido; si no hay ninguno, ofrece registrarlo y vuelve (`/animales/nuevo?volver=reservar`; no suma pasos, P-RC-01).
  2. **Servicio y profesional:** filtrados por servicio y especie; si hay uno solo, se elige solo.
  3. **Fecha y hora:** calendario limitado a 2 h–60 días; explica por qué un día no tiene horarios (feriado, ausencia, día que no atiende, sin cupo).
  4. **Revisión:** resumen, precio «desde», política de 24 h. Al confirmar **revalida** el horario; si se ocupó, vuelve al paso 3 con los horarios actualizados sin perder lo elegido.
- Botón de demostración «simular que otro cliente reservó este horario», para probar ese caso.
- `Models/Turno.cs` y `Pages/Turnos.razor` (`/turnos`): constancia del turno recién reservado y lista de próximos turnos.
- «Reservar turno» (catálogo, inicio, detalle) y el retorno después de ingresar o verificar ahora llevan a `/reservar?servicio=…`. Se quitó el aviso provisorio del detalle del servicio.
- Barra superior: «Mis turnos».

## Cómo probarlo

1. GitHub Desktop → *Fetch origin* → rama `eric/sesion-2026-11-05-2` → F5.
2. Crear cuenta y verificar; en Mis animales, «Cargar ejemplos» (caballo y majada) y registrar un perro.
3. Servicios → «Reservar turno» en Consulta:
   - Paso 2 con el caballo: solo la Dra. Acosta. Con «Vacunación» y el caballo: no hay profesional.
   - Paso 2 con el perro: Dra. Ferreira. Paso 3 en un día de semana: horarios de 9:00 a 13:30 sin 11:30–12:00. Mañana sin 10:00, 10:15 ni 12:00.
   - Reservar 9:00 y volver a reservar con el mismo profesional: 9:00 y 9:15 ya no aparecen.
   - Un sábado con la Dra. Ferreira: «no atiende los sábados». La fecha de su ausencia: «no atiende ese día (capacitación)».
4. En el paso 4, «simular que otro cliente reservó» y después «Confirmar»: vuelve al paso 3 con el aviso.
5. Mis turnos: constancia y lista.

> No compilado desde el entorno de Claude. Si Visual Studio marca un error, se corrige en esta rama.

## Pendientes

- [ ] Eric: probar la rama y mezclar el PR. Al mezclar, marcar A-014 y A-015 y pasar «🧪 En prueba» a ✅ en el README.
- [ ] A-016: cancelar (24 h) y reprogramar turnos.
- [ ] Recursos (consultorios) y agenda de Recepción y Veterinario: A-017.
- [ ] Al completar el recorrido catálogo → registro → reserva, medir el tiempo para el disparador del Plan B (falta fecha de entrega).
- [ ] Borrar `VeterinariaBrain_viejo`, las dos copias viejas del repositorio y las ramas ya integradas en GitHub.
- [ ] Diego: proveedor MySQL de DT-BD-02 (EF Core 9 sin soporte desde el 2026-11-10).
