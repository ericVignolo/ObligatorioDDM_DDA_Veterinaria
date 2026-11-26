# Sesión 2026-11-26 (3) — Móvil: internación del Cliente (B-011)

Rama: `eric/sesion-2026-11-26-movil-b011`, desde `eric/sesion-2026-11-26-movil-b010`.

## Qué se hizo
- **Internación en la ficha del animal** (`components/SeguimientoInternacion.tsx`, RF-CLI-09, CU-CLI-08), arriba del carné, como en `AnimalDetalle.razor`. Reemplaza el texto «se agrega en B-011».
  - Estado con texto: **Esperando al veterinario** (admitida), **Internado** o **Alta**; fecha de ingreso y motivo.
  - Solo las **novedades visibles**, de la más reciente a la más antigua; las rectificaciones dicen «corrección» y la original se conserva (RN-24).
  - Al alta: evolución final, indicaciones para casa y quién la firmó (RN-25).
  - «Actualizar novedades» mientras no hay alta, con la hora de la última actualización (RNF-DIS-04).
  - Sin datos: «… no tiene internaciones registradas».
- **Privacidad:** `services/internacionService.ts` devuelve solo lo que ve el Cliente: sin notas internas, plan terapéutico ni observaciones (RN-23), sin admisiones anuladas y nada de animales ajenos (RN-18).
- **Datos de demostración** (`data/internacionesDemo.ts`): Canela (Pedro) es el caso de Blazor, en curso, con 2 novedades visibles y 2 internas. **Solo en la app** se agregó una internación de Mishi (Ana) ya dada de alta, con una rectificación, para poder ver ese estado.
- Tipos en `types/Internacion.ts`.

## Decisiones
- Las novedades nuevas no se simulan: en la app nadie publica todavía (la captura del Veterinario es B-016), así que «Actualizar» vuelve a leer los mismos datos.

## Cómo probarlo
1. `cd mobile/Veterinaria.Movil` y `npx expo start`; abrir con Expo Go.
2. Ana (botón de prueba) → Animales → **Mishi**: internación terminada, indicaciones y corrección. **Toby**: sin internaciones.
3. Cerrar sesión; tocar el botón de prueba de Ana, cambiar el correo a `pedro.demo@vitalia.uy` e ingresar → Animales → **Canela**: internada, 2 novedades y ninguna nota interna. Tocar «Actualizar novedades».

## Comprobado
- `tsc --noEmit` sin errores y paquete web generado.
- Prueba en Chrome sin errores de consola registrados: Canela (visibles sí, internas y plan no), «Actualizar novedades», Mishi (alta, indicaciones, corrección, modo claro), Toby sin internaciones y ficha de Canela no disponible con la sesión de Ana.
- **No** se probó en Expo Go. El estado «Esperando al veterinario» no tiene datos de demostración y no se probó en pantalla.
- No se pudieron leer ni actualizar los documentos del Project de Claude (`claude/*.md`): la sesión no tenía esa herramienta. Se usaron las copias del repositorio.

## Siguiente
- B-012: consentimiento y preferencias.
