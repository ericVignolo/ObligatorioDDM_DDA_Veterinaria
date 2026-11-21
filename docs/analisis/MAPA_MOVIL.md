# Mapa de la aplicación móvil por rol (B-001)

Fecha: 2026-11-21 (Eric). Base: [plan de tareas](PLAN_TAREAS_PROYECTO.md) B-001, [diseño](../contexto-base/Diseño.md) y lo construido en escritorio (sección A).

## Alcance

- La app móvil es para **Visitante, Cliente y Veterinario**.
- **Recepción y Administrador se quedan en escritorio**: agenda de mostrador, cobro, inventario, internación administrativa, personal, parámetros, auditoría y bandeja de salida. Si una de esas cuentas ingresa en el celular, ve un aviso que la manda a usar la versión de escritorio.
- Como en la sección A, todo funciona con **datos de demostración** hasta la sección C. Ninguna pantalla promete persistencia ni seguridad reales.

## Navegación

Pestañas abajo; cambian según quién ingresó. Las pantallas de detalle se abren encima de las pestañas, con botón para volver.

| Rol | Pestañas | Pantallas de detalle |
|---|---|---|
| Visitante | Inicio · Servicios · Urgencias · Ingresar | Detalle de servicio, Profesionales, Detalle de profesional, La clínica, Documento legal |
| Cliente | Inicio · Mis animales · Turnos · Avisos · Perfil | Las del Visitante, más ficha y alta de animal, reserva en 4 pasos, detalle de turno, historia, carnet, internación, asistencia, consentimiento |
| Veterinario | Agenda · Asistencias · Avisos · Perfil | Detalle de turno, paciente autorizado, consulta (borrador y firma), asistencia ofrecida/aceptada |

Reglas:
- **Urgencias siempre a mano**: pestaña para el Visitante y tarjeta destacada en el inicio del Cliente. Sin formularios ni sesión (HU-VIS-06).
- **Acción protegida → ingresar → volver**: si un Visitante toca «Reservar turno», va a Ingresar con el servicio elegido y, al entrar, retoma la reserva (A-011, RF-CLI-01).
- Cerrar sesión vuelve al inicio del Visitante.
- Tema claro, oscuro o del sistema, desde Perfil.

## Tareas que cubre cada pantalla

| Tarea | Pantallas |
|---|---|
| B-004 | Pestañas por rol, sesión simulada, componentes compartidos (botón, campo, insignia, aviso flotante, esqueleto, vacío) |
| B-005 | Inicio público, servicios, profesionales, la clínica, urgencias, privacidad y términos |
| B-006 | Ingresar, crear cuenta, verificar código, recuperar contraseña, perfil |
| B-007 | Mis animales (lista, ficha, alta, edición) |
| B-008 | Reserva en 4 pasos, turnos próximos e historial, cancelar y reprogramar |
| B-009, B-010 | Historia del animal, carnet y alertas sanitarias |
| B-011 | Internación del Cliente (solo lo visible) |
| B-012 | Consentimiento informado y preferencias de comunicación |
| B-013, B-014 | Pedir asistencia (ubicación consentida) y seguimiento |
| B-015 a B-017 | Agenda, asistencias, consulta y avisos del Veterinario |

## Fuera del celular

La política de cookies aplica al sitio web; la app enlaza solo Privacidad y Términos. El mapa de la clínica espera la elección de proveedor (Google Maps u OpenStreetMap); mientras tanto, la dirección se muestra en texto (HU-VIS-03).
