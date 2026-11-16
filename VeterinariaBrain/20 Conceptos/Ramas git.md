---
tipo: concepto
tags: [concepto]
---
# Ramas git

`main` (versiones estables) ← `desarrollo` (escritorio, API y móvil juntos, cada uno en su carpeta). Antes se llamaba `desktop` y había ramas encadenadas por sección; las reemplazó DT-ORG-09 (2026-11-11). Cada aplicación se versiona con etiquetas (`desktop-vX.Y`, `api-vX.Y`, `mobile-vX.Y`).

- **Una rama por sesión** `<nombre>/sesion-AAAA-MM-DD` desde `desarrollo`, con un solo PR al final (DT-ORG-06).
- Quien trabajó prueba con F5 y mezcla su propio PR (DT-ORG-05).
- `main` se actualiza desde `desarrollo` en cada hito estable.

Parte de [[Mapa Planificación]].

## Aparece en
- [[Arranque escritorio]]
- [[Plan de tareas]]
- [[README del repositorio]]
- [[Sesión 2026-11-03 responsables]]
- [[Sesión 2026-11-04 inicio de sesión]]
