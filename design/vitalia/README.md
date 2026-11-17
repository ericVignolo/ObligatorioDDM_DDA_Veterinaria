# VITALIA · Sistema de diseño (Opción A de Eric y Opción B de Claude)

Propuesta de diseño/UX para que **Diego elija** entre dos opciones. No implica código aplicado a las pantallas: eso espera la elección (puerta H0 por recorrido, DT-ORG-03). La advertencia sobre los límites de los prototipos está en la sección 3.1 de `docs/contexto-base/CONTEXTO_BASE.md`.

## Estructura (misma idea que `design/` del repositorio del equipo hermano)

```
design/vitalia/
├─ opcion-a/  exports/*.png  vars.json    ← Eric: verde salud + naranja, Nunito, redondeo suave, iconos de contorno
├─ opcion-b/  exports/*.png  vars.json    ← Claude: verde azulado + coral, fondo arena, Plus Jakarta Sans, iconos duotono
├─ web/       tokens-opcion-{a,b}.css, componentes.css, previsualizacion.html, DISENO_COMPARACION.md
├─ fuentes/   Nunito y Plus Jakarta Sans (SIL OFL) alojadas en el proyecto + fuentes.css
└─ scripts/   generar_exports.py          ← regenera todas las imágenes y los vars.json desde los tokens
```

## Imágenes (`opcion-*/exports/`, PNG a escala 2)

| Archivo | Contenido |
|---|---|
| `S1-colores` / `-oscuro` | Muestras con hex y contraste, y pares de texto verificados (WCAG 2.1) |
| `S2-tipografia-espaciado` | Escala tipográfica, espaciado de 4 px, radios y sombras |
| `S3-botones-campos` / `-oscuro` | Botones (por defecto, presionado, foco, deshabilitado, cargando) y campos (normal, foco, error, deshabilitado) |
| `S4-insignias-tarjetas-avisos` / `-oscuro` | Insignias de estado de turno, tarjetas de servicio y avisos |
| `S5-iconos` | Biblioteca de muestra (16 iconos) y tamaños |
| `S6-tiempos-carga-respuestas` / `-oscuro` | Qué mostrar según cuánto se espera, movimiento, esqueletos y avisos flotantes |
| `M01-reservar-movil` / `-oscuro` | Pantalla de ejemplo de la aplicación móvil (375 × 812) |

Los tiempos de `S6` son una propuesta de diseño, no una medición del sistema.

## Cómo regenerar

```bash
pip install playwright && playwright install chromium
python design/vitalia/scripts/generar_exports.py        # las dos opciones
python design/vitalia/scripts/generar_exports.py a      # solo la A
```

## Cómo usarlo en OpenPencil (Windows)

OpenPencil se maneja desde la terminal con el comando `op` (paquete aparte del programa: `winget install --id ZSeven-W.OpenPencil.CLI -e`). Con el programa abierto:

```bash
op vars:set @design/vitalia/opcion-a/vars.json --replace    # carga los colores como variables de diseño
```

Los nombres de variable coinciden con las variables CSS sin el prefijo (`primario`, `acento`, `fondo`, `texto`...; el tema oscuro lleva el sufijo `-oscuro`). Verificá los comandos exactos con `op --help` de tu versión instalada.

## Cómo se aplica después (cuando Diego elija)

1. Copiar `tokens-opcion-X.css`, `componentes.css` y `fuentes/` a `desktop/Veterinaria.Blazor/wwwroot/` y enlazarlos en `App.razor`.
2. Migrar las pantallas recorrido por recorrido, una rama por sesión, con su prueba.
3. En React Native, pasar los mismos valores a un `tema.ts` y usar la misma biblioteca de iconos.
