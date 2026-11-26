# Sesión 2026-11-26 — Móvil: historia clínica del Cliente (B-009)

Rama: `eric/sesion-2026-11-26-movil-b009`, desde `eric/sesion-2026-11-25-movil-b008`. Eric probó B-008 en el celular y funciona; la seña de la castración queda pendiente, como corresponde (se paga en Recepción, RN-35).

## Qué se hizo
- **Historia clínica** (`app/animales/[id]/historia.tsx`, botón en la ficha): RF-CLI-05 y CU-CLI-05. Solo lectura, solo consultas **firmadas** de los animales del Cliente (RN-18, A-RC-14), de la más reciente a la más antigua. Cada registro (`components/RegistroClinico.tsx`, como `RegistroClinico.razor`) muestra alergias destacadas, signos vitales, diagnóstico con su tipo, tratamiento e indicaciones, las **enmiendas** debajo del original con fecha, autor y motivo (RN-12) y el pie de **firma**. Sin la huella del contenido (solo la ve el personal) y sin controles de edición. Historia vacía con su mensaje.
- **Adjuntos** (RF-CLI-08, `app/animales/[id]/adjunto/[adjuntoId].tsx`): Blazor no los tiene («llegan con la API»), así que en la app son **de demostración y no sensibles**, como pide B-009: una imagen claramente falsa (`assets/demo/estudio-demo.png`), un PDF sin vista previa y un archivo retirado que no se puede abrir. Se ven con un **acceso temporal simulado de 5 minutos**: al vencer se pide uno nuevo (CU-CLI-05, alternativas «enlace vencido», «archivo retirado» y «formato sin previsualización»). Un animal o adjunto ajeno muestra «no disponible».
- **Datos:** `data/consultasDemo.ts` con las mismas consultas firmadas de Blazor (Toby con su enmienda, Canela con alergia a la penicilina) más los adjuntos; `services/historiaService.ts`; `types/Consulta.ts`.
- La ficha del animal ahora tiene el botón **Historia clínica**; vacunas e internaciones quedan para B-010 y B-011.

## Comprobado
- `tsc` sin errores y paquete web generado.
- Prueba automática en navegador, 25 de 25 en claro y oscuro, sin errores en consola: consulta firmada, diagnóstico y signos, enmienda debajo del original, firma, sin huella ni edición, adjuntos (retirado no se abre), imagen con acceso temporal, acceso vencido y renovado, PDF sin vista previa, historia vacía, historia y adjunto ajenos, alergia destacada para Pedro.
- Se corrigió un error que encontró la revisión visual: la imagen del adjunto desbordaba la pantalla (en la web `Image` no toma las clases de tamaño ni `aspectRatio`; ahora va dentro de un contenedor con la proporción).
- Las pruebas anteriores siguen bien: B-008 (28), animales (19), foto (8), Mi cuenta (8), registro (6) y recorrido general (56).
- **No** se probó en Expo Go.

## Siguiente
- B-010: vacunas y alertas (carné sanitario).
