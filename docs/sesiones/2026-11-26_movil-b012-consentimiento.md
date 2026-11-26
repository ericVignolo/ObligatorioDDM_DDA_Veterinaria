# Sesión 2026-11-26 (4) — Móvil: consentimiento y preferencias (B-012)

Rama: `eric/sesion-2026-11-26-movil-b012`, desde `eric/sesion-2026-11-26-movil-b011`.

## Qué se hizo
- **Consentimiento informado** (`app/consentimiento/[turnoId].tsx`, RF-CLI-11, CU-CLI-10, reglas K-01 a K-14): procedimiento, paciente, turno, versión y texto; firma con casilla sin marcar y nombre completo igual al de la cuenta, «No autorizo», historial de respuestas y huella SHA-256. Un firmado se puede **retirar** con confirmación mientras el turno no empezó (K-10).
- **Turnos:** estado del consentimiento con texto y botón «Leer y firmar» o «Ver el consentimiento firmado».
- **Avisos:** los consentimientos sin firmar aparecen arriba de las alertas sanitarias; acceso a las preferencias.
- **Preferencias y privacidad** (`app/preferencias.tsx`, desde Perfil; RF-CLI-18, RF-CLI-20, CU-CLI-17): canal por tipo de comunicación (los opcionales se pueden apagar; la alerta sanitaria arranca apagada, RN-43), fecha de cada cambio, aceptaciones obligatorias con versión y consentimientos clínicos respondidos, separados entre sí.
- **Privacidad:** se informa que la app no usa analítica ni rastreadores (RN-44).
- Reglas en `services/consentimientoService.ts` y `services/comunicacionesService.ts`, iguales a Blazor. `utils/sha256.ts` calcula la huella sin agregar dependencias.

## Fuera de esta tarea
- Bandeja de mensajes («Mis comunicaciones» de Blazor).
- Exportar y eliminar datos (RF-CLI-19): la app indica que se hace en el sitio web.
- Consentimiento de ubicación (B-014).

## Observación para el escritorio
`FirmarConsentimiento.razor` no ofrece retirar un consentimiento ya firmado, aunque el servicio lo permite y K-10 lo describe. En la app sí está. No se tocó Blazor.

## Cómo probarlo
1. `cd mobile/Veterinaria.Movil` y `npx expo start`; abrir con Expo Go.
2. Ana → **Avisos**: consentimiento pendiente de la castración de Mishi → «Leer y responder».
3. Tocar «Firmar» sin completar (errores); escribir «Ana Rodriguez» sin tilde (no coincide); con tilde, firma.
4. «Retirar consentimiento» → queda rechazado, con historial; se puede volver a firmar.
5. **Turnos:** estado y botón en cada castración. **Perfil → Preferencias y privacidad:** cambiar el aviso de refuerzo; los mensajes del servicio no tienen «No recibir».

La castración de demostración existe hoy y mañana solo si la Dra. Ferreira atiende esos días; si no aparece, reservar una castración para Mishi.

## Comprobado
- `tsc --noEmit` sin errores y paquete web generado.
- La huella coincide con la de Node en 7 casos (vacío, tildes, emoji y bordes de bloque).
- Prueba en Chrome sin errores de consola registrados: pasos 2 a 5, y Pedro sin consentimientos ni datos de Ana (turno ajeno no disponible).
- **No** se probó en Expo Go. Tampoco el estado «Versión nueva» (la app no publica versiones) ni las aceptaciones de una cuenta recién registrada.
- No se actualizaron los documentos del Project de Claude (sin acceso desde la sesión).

## Siguiente
- B-013: solicitud móvil de asistencia.
