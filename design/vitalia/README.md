# VITALIA · Sistema de diseño (Opción A de Eric y Opción B de Claude)

Propuesta de diseño/UX para que **Diego elija** entre dos opciones. No implica código aplicado a las pantallas: eso espera la elección (puerta H0 por recorrido, DT-ORG-03). La advertencia sobre los límites de los prototipos está en la sección 3.1 de `docs/contexto-base/CONTEXTO_BASE.md`.

## Estructura (misma idea que `design/` del repositorio del equipo hermano)

```
design/vitalia/
├─ opcion-a/  exports/*.png  vars.json  vitalia-a.op  openpencil/    ← Eric: verde salud + naranja, Nunito, redondeo suave, iconos de contorno
├─ opcion-b/  exports/*.png  vars.json  vitalia-b.op  openpencil/    ← Claude: verde azulado + coral, fondo arena, Plus Jakarta Sans, iconos duotono
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

Hay un archivo de diseño por opción: `opcion-a/vitalia-a.op` y `opcion-b/vitalia-b.op`. Cada uno tiene las 46 variables de color (claro y oscuro como variables separadas, sufijo `-oscuro`) y la previsualización (`web/previsualizacion.html`) importada. Las imágenes exportadas desde OpenPencil están en `opcion-*/openpencil/` (`preview-a.png`, `preview-b.png`). Las de `exports/` son las generadas con Playwright y son las de referencia.

### Conectar `op` con el programa (probado el 2026-11-17)

1. Instalar el programa y el CLI: `winget install --id ZSeven-W.OpenPencil -e` y `winget install --id ZSeven-W.OpenPencil.CLI -e`.
2. `op` necesita saber dónde está el programa. En cada ventana de PowerShell nueva (o una sola vez con `setx`, que vale desde la ventana siguiente):

```powershell
$env:OPENPENCIL_DESKTOP_BIN = "C:\Program Files\OpenPencil\OpenPencil-desktop.exe"
setx OPENPENCIL_DESKTOP_BIN "C:\Program Files\OpenPencil\OpenPencil-desktop.exe"
```

3. Abrir, cargar y guardar (desde la raíz del repositorio):

```powershell
op start --file design/vitalia/opcion-a/vitalia-a.op   # abre el editor y deja el servidor en 127.0.0.1:3100
op status                                              # debe decir running:true
op vars:set "@design/vitalia/opcion-a/vars.json"       # carga las variables
op import:html "design/vitalia/web/previsualizacion.html"
op save "design/vitalia/opcion-a/vitalia-a.op"
op export-frames --output-dir "design/vitalia/opcion-a/openpencil" --format png
op stop                                                # antes de pasar a la otra opción
```

Para la B, lo mismo cambiando `opcion-a` por `opcion-b`. Las dos opciones usan los mismos nombres de variable, por eso van en archivos separados.

### Cosas que conviene saber

- Cada comando va en su propia línea: dos pegados en una (`op varsTest-Path ...`) no se ejecutan.
- Las rutas con `--file` o `@archivo` son relativas a la carpeta donde está la terminal; abrir PowerShell en `C:\WINDOWS\System32` rompe las rutas relativas.
- El botón «进入专业画布» (lienzo profesional) del programa pide iniciar sesión con una cuenta Zseven. No hace falta para trabajar con `op`.
- `op import:html` omite la hoja `fuentes.css` (Nunito y Plus Jakarta Sans no se asignan solas), importa los iconos SVG como imágenes de relleno y aproxima grillas y filas con salto de línea. Es una referencia visual, no la fuente de verdad: los valores son los de `web/tokens-opcion-*.css`.
- El eje de temas claro/oscuro quedó sin crear (`op themes`); las variantes oscuras son variables sueltas. Pendiente hasta que Diego elija la opción.
- `op design:lint` sobre la previsualización importada avisó de radios mezclados (artefacto de la importación) y de dos campos de texto sin etiqueta, que se corrigen en `previsualizacion.html` y en las pantallas al aplicar el diseño.

## Cómo se aplica después (cuando Diego elija)

1. Copiar `tokens-opcion-X.css`, `componentes.css` y `fuentes/` a `desktop/Veterinaria.Blazor/wwwroot/` y enlazarlos en `App.razor`.
2. Migrar las pantallas recorrido por recorrido, una rama por sesión, con su prueba.
3. En React Native, pasar los mismos valores a un `tema.ts` y usar la misma biblioteca de iconos.
