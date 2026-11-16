# Sesión 2026-11-11 — Cerebro dividido por aplicación (Eric)

Rama: `eric/sesion-2026-11-11-cerebro` desde `desktop`.

## Qué se hizo

- `VeterinariaBrain/40 Código/` ahora tiene una carpeta por aplicación:
  - `Desktop/` — Blazor (`Páginas`, `Componentes`, `Servicios`, `Modelos` y `Carpeta desktop`).
  - `Api/` — API (`Carpeta api`; al haber código: `Modelos`, `Datos`, `Controladores`, `Servicios`, `Repositorios`, `DTOs`).
  - `Mobile/` — React Native (`Carpeta mobile`; al haber código: `Pantallas`, `Componentes`, `Servicios`, `Modelos`).
  - `Tests/` — `Carpeta tests`.
- `_scripts/actualizar_codigo.py` lee las tres aplicaciones. Si una no existe todavía, la saltea.
  - Las notas de la API llevan el sufijo «(API)» y las del móvil «(Móvil)», para que Obsidian no confunda, por ejemplo, el modelo `Prestacion` de Blazor con el de la API.
  - Si el mismo nombre existe en varias aplicaciones, cada nota tiene la sección «En las otras aplicaciones».
  - Las notas tienen la propiedad `app:` y la etiqueta de la aplicación (`desktop`, `api`, `mobile`), útiles para filtrar el grafo.
  - `Mapa Código` tiene una sección por aplicación.
- Las notas de Blazor quedaron iguales; solo se agregaron `app: desktop` y la etiqueta `desktop`.
- `Inicio.md` y `AGENTS.md` actualizados con la nueva ruta.

## Cómo probarlo

1. Abrir `VeterinariaBrain` en Obsidian: los enlaces siguen funcionando (Obsidian enlaza por nombre, no por carpeta).
2. Correr `python VeterinariaBrain/_scripts/actualizar_codigo.py` desde la raíz: debe decir «60 notas de código generadas» en `desktop` sin la API.
3. Se probó también sobre la rama de Diego (`diego/sesion-2026-11-10`) mezclada con esta: el merge no tiene conflictos y genera 70 notas (10 de la API: 9 modelos y `AppDbContext`).

## Qué falta

- [ ] Después de mezclar la rama de Diego, correr el script y quitar de `Carpeta api` la frase «Las notas generadas cubren solo Blazor».
- [ ] Al empezar `mobile/`, revisar que las carpetas usadas (`screens`, `components`, `services`) coincidan con las que reconoce el script.

## Más tarde: rama `desarrollo` (DT-ORG-09)

- Se mezcló el PR #20 (A-010, A-017 a A-020) después de traerle el cerebro dividido y regenerar sus notas.
- Decisión de Eric: `desktop` pasa a llamarse `desarrollo` y es la única rama de trabajo; no se crean `desktop-mobile` ni `desktop-mobile-api`. Versiones por aplicación con etiquetas. Registrado en `CONTEXTO_BASE.md` (DT-ORG-09), `AGENTS.md`, `README.md` y la nota `Ramas git`.
- El CI escucha `desarrollo` y, mientras dure el cambio de nombre, también `desktop`.
- Pendiente: renombrar la rama en GitHub (*Branches* → lápiz) y, cuando Diego mezcle el #21, borrar las ramas ya integradas.
