# Sistema de diseño de VITALIA — Opción A (Eric) y Opción B (Claude)

Estado: propuesta para que **Diego elija** (2026-11-17). Clasificación: diseño/UX. Los dos conjuntos usan las mismas variables `--vt-*`, así que cambiar de opción es reemplazar un archivo. La advertencia sobre prototipos y la autoridad de la API están en la sección 3.1 de `CONTEXTO_BASE.md`.

Cómo verlo: abrir `previsualizacion.html` (lado a lado, con interruptor claro/oscuro, esqueletos y avisos).

| Decisión | Opción A — Eric | Opción B — Claude |
|---|---|---|
| Color principal | Verde salud `#1B7F53` | Verde azulado profundo `#0B7A75` |
| Acento | Naranja cálido `#B8530A` (en oscuro `#F5A25D`) | Coral `#C4452E` (en oscuro `#FF9A85`) |
| Fondo | Verde muy claro `#F4FAF6` | Arena cálida `#FBF8F3` |
| Tipografía | Nunito (suave, cálida) | Plus Jakarta Sans (más nítida, buena en tamaños chicos) |
| Botones | Redondeo suave, 10 px | Redondeo amplio, 14 px; tarjetas de 22 px |
| Iconos | Contorno fino (Lucide) | Duotono (Phosphor): contorno más relleno suave |
| Estilo general | Moderno con tarjetas | Moderno con tarjetas, más aire y fondo cálido |
| Modo claro y oscuro | Sí | Sí |
| Carga y respuestas | Esqueletos + avisos flotantes | Esqueletos + avisos flotantes |

## Por qué la Opción B
- El verde azulado se distingue de otras apps de mascotas (casi todas verde puro o naranja) y transmite «clínica» sin frialdad.
- El coral solo aparece en acciones y alertas, así que no compite con el color de marca.
- El fondo arena cansa menos la vista en jornadas largas de Recepción.
- Plus Jakarta Sans se lee mejor en tablas densas del panel; Nunito es más amable pero más ancha.
- Los iconos duotono dan jerarquía visual (el relleno suave guía el ojo) sin pesar.

## Lo que comparten (decisiones de Eric, 2026-11-17)
Modo claro y oscuro; esqueletos mientras carga y avisos flotantes que se van solos (los errores se quedan hasta cerrarlos y se anuncian con `role="alert"`); tarjetas con sombra suave; botones de al menos 44 px de alto; foco visible de 3 px; sin animación si la persona pidió reducir movimiento.

## Contraste (WCAG AA, texto normal ≥ 4,5:1)
Medido con la fórmula de luminancia relativa.

| Par | Relación |
|---|---|
| Blanco sobre verde A `#1B7F53` | 4,99 |
| Verde de éxito `#187048` sobre su fondo suave `#E3F4EA` | 5,33 |
| Blanco sobre naranja A `#B8530A` | 4,91 |
| Texto suave A `#5A6B62` sobre fondo | 5,34 |
| Blanco sobre verde azulado B `#0B7A75` | 5,18 |
| Blanco sobre coral B `#C4452E` | 4,96 |
| Texto suave B `#5C6B6A` sobre fondo | 5,26 |
| Verde claro `#4ADE9A` sobre fondo oscuro A | 9,79 |
| Turquesa `#5EC9C2` sobre fondo oscuro B | 8,90 |

Corrección del 2026-11-17: el verde de éxito original (`#1B7F53`) daba 4,37:1 sobre su fondo suave y se oscureció a `#187048`; en el botón de borde, el texto al pasar el mouse usa `primario-hover` (5,79:1 en A y 6,39:1 en B).

Nota: el verde `#2E9E6B` y el naranja `#F28C28` «puros» no pasan AA con texto blanco; por eso los tonos de botón están oscurecidos. El naranja puro queda solo como color de foco.

## Archivos (las imágenes están en `../opcion-a/exports` y `../opcion-b/exports`; ver `../README.md`)
- `tokens-opcion-a.css` y `tokens-opcion-b.css`: variables de color, forma, tipografía, espacio y movimiento (claro y oscuro).
- `componentes.css`: botones, tarjetas, insignias, campos, esqueletos y avisos; solo usa `--vt-*`.
- `previsualizacion.html`: demostración de ambas opciones.

## Cómo se usa después
- **Blazor:** se copia la opción elegida a `wwwroot/` como `tokens.css`, se enlaza en `App.razor` y se migran las pantallas pantalla por pantalla (una rama por sesión). Nunito/Plus Jakarta se alojan dentro del proyecto, sin depender de internet.
- **React Native:** los mismos valores pasan a un archivo `tema.ts`; los iconos son los mismos de la librería elegida.
- **OpenPencil:** las variables se cargan como variables de diseño para dibujar las pantallas.

## Pendiente
Elección de Diego; recién ahí se aplica a las pantallas (puerta H0 por recorrido, DT-ORG-03).
