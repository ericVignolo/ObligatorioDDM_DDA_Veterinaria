# Sesión 2026-11-28 (2) — Móvil: seguimiento de asistencia e informes de cambio (B-014)

Rama: `eric/sesion-2026-11-28-movil-b014`, desde `eric/sesion-2026-11-28-movil-b013`.

## Antes de la tarea: reparación del entorno
- `node_modules` de la app estaba incompleta (faltaban `expo`, `react`, `expo-router`, `nativewind` y otras), por eso VS Code marcaba errores en cada archivo y Expo no arrancaba. Se reinstaló con `npm ci`. Revisión: Blazor y API compilan, 1159 de 1159 pruebas pasan y Git no tiene archivos dañados.
- Expo Go nuevo solo abre SDK 58: se commiteó la subida a `expo` 58.1.0 y `expo-router` 58.0.33 (`d53dc92`, en la rama de B-013).
- `npx expo start` se ejecuta desde `mobile/Veterinaria.Movil`, no desde la raíz del repo.

## Qué se hizo
- **Ubicación consentida** (`components/UbicacionGps.tsx`, RF-CLI-22, RNF-LEG-09): en una visita, «Compartir mi ubicación actual» después de explicar para qué se usa, quién la ve y cuánto dura. Se pide una sola vez, sin seguimiento. Si se niega o falla, se escribe la dirección (sección 13); sin ninguna de las dos no hay visita (RN-56). Las coordenadas no se muestran y se descartan al terminar la asistencia. Dependencia nueva autorizada por Eric: `expo-location` ~58.0.23 (funciona en Expo Go) y texto del permiso en `app.json`.
- **Seguimiento** (`app/asistencia/[id].tsx`, RF-CLI-23): estado explicado, veterinario y llegada estimada solo después de aceptar (RN-53), **mapa de demostración** solo durante «En camino» (`components/MapaDemo.tsx`, RN-57), actualización cada 15 segundos y botón «Actualizar estado», historial sin los veterinarios ofrecidos ni los motivos de rechazo.
- **Informar empeoramiento** (`components/InformarEmpeoramiento.tsx`, RF-CLI-24, RN-51): nivel actual o más grave y qué cambió. Siempre queda el evento; si sube a rojo sin aceptar, se activa la guardia. El cliente no puede bajar el nivel (RN-52). Reintento con la misma clave no duplica.
- **Reevaluación de 24 horas** (RN-50): «Se mantiene igual» o «Empeoró», en el detalle, en **Avisos** y marcada en **Mis solicitudes**. Sin respuesta no se cierra ni baja.
- **Plazos** (`revisarPlazos` en `services/asistenciaService.ts`, como `RevisarPlazos` de Blazor): rojo ofrecido sin aceptar en 10 minutos → Recepción; 24 horas sin aceptar → reevaluación.
- **«Simular el siguiente paso»** (marcado como demostración): hace lo que harán Recepción y el veterinario (aceptar con 8 km cargados si faltaba la distancia, salir, llegar, atender, finalizar), para poder ver el seguimiento sin B-015.

## Decisiones tomadas en la implementación (confirmar con Diego)
- Coordenadas: se borran al finalizar. La especificación deja pendiente la conservación (sección 14).
- La reevaluación se pide una vez (la repetición es un parámetro pendiente, sección 11). Un empeoramiento también la da por respondida.
- No hay notificaciones push: la pantalla se actualiza sola. Llegan con B-017 y la API.

## Cómo probarlo
1. `cd mobile/Veterinaria.Movil` y `npx expo start -c`; abrir con Expo Go.
2. Ana → **Avisos**: «Solicitud de asistencia n.º 2» pide reevaluación → «Responder» → «Se mantiene igual».
3. «Informar empeoramiento»: enviar vacío (errores), elegir Rojo y escribir qué cambió → pasa a Rojo y «Ofrecida».
4. «Simular el siguiente paso» dos veces → «En camino» con mapa y llegada estimada. Seguir hasta «Finalizada»: ya no se puede informar empeoramiento.
5. **Pedir asistencia** → Visita → «Compartir mi ubicación actual»: aceptar el permiso (queda «Ubicación del celular agregada», la dirección pasa a opcional) o negarlo (aviso y dirección obligatoria).

## Comprobado
- `tsc --noEmit` sin errores y paquete generado para Android, iOS y web.
- En Chrome, sin errores de consola registrados: pasos 2 a 4, solo el nivel actual o más grave en el empeoramiento, historial sin datos internos, solicitud ajena no disponible y visita sin ubicación ni dirección rechazada.
- **No** se probó el GPS (permiso real) ni nada en Expo Go.
- No se actualizaron los documentos del Project de Claude (sin acceso desde la sesión).

## Siguiente
- B-015: agenda móvil del Veterinario (turnos, asistencias ofrecidas, aceptar o rechazar y estados del traslado).
