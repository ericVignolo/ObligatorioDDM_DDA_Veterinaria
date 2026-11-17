# Especificación de accesibilidad y adaptación de escritorio (A-029)

Requisitos cubiertos: RNF-USA-03 a RNF-USA-07, RN-44 y WCAG 2.1 AA, en los límites de un prototipo de sección A.
Las advertencias comunes (autoridad de la API, límites de los prototipos) están en `CONTEXTO_BASE.md`, sección 3.1.

## Reglas aplicadas

| Id | Regla | Criterio WCAG | Dónde |
|---|---|---|---|
| AC-01 | Enlace «Saltar al contenido» como primer elemento enfocable; lleva el foco a `<main id="contenido-principal">`. | 2.4.1 | `MainLayout.razor`, `app.css` |
| AC-02 | Puntos de referencia: `<header>`, `<nav aria-label="Navegación principal">`, `<main>`. | 1.3.1, 2.4.1 | `MainLayout`, `NavMenu` |
| AC-03 | Botón del menú con nombre accesible. | 4.1.2 | `NavMenu.razor` |
| AC-04 | Foco visible en todo elemento interactivo (contorno de 3 px, blanco sobre la barra lateral). | 2.4.7 | `app.css` |
| AC-05 | Texto atenuado `.text-muted` con contraste ≥ 4,5:1 sobre fondo blanco. | 1.4.3 | `app.css` |
| AC-06 | Toda tabla tiene título (`caption` visible u oculto, o `aria-label`) y se desplaza dentro de `.table-responsive`. | 1.3.1, 1.4.10 | vistas con tablas |
| AC-07 | Todo campo tiene etiqueta (`label for`/`id`), o `aria-label`; toda `label for` apunta a un campo existente. | 1.3.1, 3.3.2 | vistas con formularios |
| AC-08 | Los mensajes de resultado de una acción se anuncian (`role="status"` o `role="alert"`). | 4.1.3 | vistas con `alert` dinámico |
| AC-09 | Enlaces que abren otra pestaña llevan `rel="noopener noreferrer"`. | seguridad | `Registro.razor` |
| AC-10 | Textos del sistema (reconexión, error inesperado, aviso de error) en español rioplatense; `lang="es-UY"`. | 3.1.1 | `ReconnectModal`, `app.css`, `App.razor` |
| AC-11 | Se respeta `prefers-reduced-motion`. | 2.3.3 | `app.css` |
| AC-12 | Por debajo de 641 px, botones y campos miden al menos 44 px; las casillas se agrandan; el contenido no desborda. | 2.5.5, 1.4.10 | `app.css` |
| AC-13 | Por encima de 1600 px el contenido se limita a 1400 px. | RNF-USA-06 | `app.css` |
| AC-14 | La impresión oculta el enlace de salto y la navegación. | — | `app.css` |

## Verificación

- Automática: `tests/Veterinaria.Tests/AccesibilidadTests.cs` lee los `.razor` y el CSS y falla si se pierde alguna de las reglas AC-01 a AC-12 (revisión estática, no ejecuta el navegador).
- Manual (Eric, con F5): recorrer con Tab sin mouse, probar el enlace de salto, ver el contorno de foco, reducir la ventana a 360 px, comprobar que las tablas se desplazan y que los textos de reconexión están en español.

## Limitaciones conocidas del prototipo

- No se hizo una auditoría con lector de pantalla (NVDA/VoiceOver) ni una medición de contraste de todos los colores; solo la de `.text-muted` y los colores de foco.
- `NavMenu` usa un `div` con `onclick` para cerrar el menú en pantallas angostas; funciona con mouse y toque, no es un control con teclado propio.
- Los avisos informativos estáticos no llevan `role`; solo los mensajes que aparecen tras una acción.
- Los colores, íconos y estados de carga se definen en la etapa de diseño (OpenPencil), que puede ajustar contrastes.
- Las pantallas móviles (React Native) tienen su propia verificación en la sección B.
