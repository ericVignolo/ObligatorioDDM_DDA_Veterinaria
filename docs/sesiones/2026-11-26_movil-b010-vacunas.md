# Sesión 2026-11-26 (2) — Móvil: vacunas y alertas (B-010)

Rama: `eric/sesion-2026-11-26-movil-b010`, desde `eric/sesion-2026-11-26-movil-b009`.

## Qué se hizo
- **Carné sanitario** en la ficha del animal (`components/CarnetSanitario.tsx`, RF-CLI-07, A-021), solo lectura y solo de los animales del Cliente (RN-18): la última aplicación de cada producto con tipo, laboratorio, fecha, lote y refuerzo, y su estado con texto (no solo color): **Vigente**, **Refuerzo próximo** (5 días o menos, decisión del 31/10) o **Vencida**. Las alertas van arriba, con «Reservar turno» que abre la reserva con el animal y el servicio (vacunación o desparasitación) ya elegidos. El historial completo queda plegado. Sin datos: «Todavía no hay vacunas ni antiparasitarios registrados». Incluye el aviso de catálogo de demostración de Blazor.
- **Avisos** del Cliente (`app/(tabs)/avisos.tsx`): refuerzos vencidos o próximos de todos sus animales, lo más urgente primero, con «Ver carné» y «Reservar»; «Todo al día» si no hay. Los avisos del Veterinario quedan para B-017; el correo lo manda la API (A-028).
- **Inicio** del Cliente: tarjeta «Alertas sanitarias (n)» que lleva a Avisos.
- **Historia clínica:** la consulta muestra las vacunas aplicadas en ella, como en Blazor.
- **Cargar ejemplos** también carga el carné de ejemplo (caballo con refuerzo próximo y majada vencida), como en Blazor.
- Datos y reglas iguales a `VacunaService`: `data/productosSanitarios.ts`, `services/vacunaService.ts`, `types/Vacuna.ts`, componente `EstadoRefuerzo`. La fecha de refuerzo la calcula el producto (RN-19).

## Comprobado
- `tsc` sin errores y paquete web generado.
- Prueba automática en navegador, 17 de 17 sin errores en consola: alertas en Inicio y Avisos (orden por urgencia), reservar desde la alerta con animal y servicio elegidos, carné de Toby con los tres estados, lote y laboratorio, aviso de catálogo, vacuna en la historia, Mishi vigente, carné ajeno no visible, Pedro «Todo al día» y rodeo sin datos, modo oscuro, ejemplos de una cuenta nueva y avisos del Veterinario pendientes de B-017.
- Las pruebas anteriores siguen bien: B-009 (25), B-008 (28), animales (19), foto (8), Mi cuenta (8), registro (6) y recorrido general (56).
- **No** se probó en Expo Go.

## Siguiente
- B-011: internación del Cliente.
