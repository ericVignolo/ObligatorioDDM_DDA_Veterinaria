---
tipo: mapa
tags: [mapa]
---
# Inicio — Segundo cerebro del Obligatorio Veterinaria

Sistema de gestión para **[[VITALIA]]**, clínica veterinaria ficticia: una API ASP.NET Core única, Blazor para computadora y React Native para móvil. Fuente canónica de requisitos: [[Matriz de requisitos]].

## Estado
- Fase de construcción, **sección A (escritorio)**: A-001 a A-008 y A-011 a A-015 hechas: el primer recorrido (catálogo → registro → reserva) está completo. A-016 (cancelar y reprogramar) hecha. Panel del personal (A-010, A-017, A-018), consulta e historia clínica (A-019, A-020) en prueba. **Entrega: 4 de diciembre de 2026.** Detalle en [[Plan de tareas]].
- Código actual: [[Mapa Código]], con una sección por aplicación (Desktop, Api y Mobile) y las dependencias de cada archivo.
- Pendiente de [[Diego]]: proveedor MySQL (ver [[MySQL]]), crear la base de datos y decidir R1/R2 del [[Plan B de alcance]].

## Mapas
- [[Arquitectura.canvas|Canvas de arquitectura]]
- [[Mapa Código]]
- [[Mapa Requisitos y análisis]]
- [[Mapa Planificación]]
- [[Mapa Arquitectura]]
- [[Mapa Diseño]]
- [[Mapa Historial]]
- [[Mapa Equipo y roles]]

## Últimas sesiones
- [[Sesión 2026-11-09 panel del personal]]
- [[Sesión 2026-11-05 cancelar y reprogramar]]
- [[Sesión 2026-11-05 reserva]]
- [[Sesión 2026-11-05 cerebro y animales]]
- [[Sesión 2026-11-04 inicio de sesión]]
- [[Sesión 2026-11-03 registro]]
- [[Sesión 2026-11-03 responsables]]

## Mantenimiento
- Las notas de `40 Código` y el [[Mapa Código]] se regeneran con `python VeterinariaBrain/_scripts/actualizar_codigo.py`. Se hace al cerrar cada sesión.
- `40 Código` tiene una carpeta por aplicación: `Desktop/` (Blazor), `Api/` (notas con sufijo «(API)») y `Mobile/` (notas con sufijo «(Móvil)»). Si el mismo nombre existe en varias aplicaciones, cada nota enlaza a las otras.
- Las demás notas se escriben a mano.

## Grafo
- Dorado: mapas · Azul: documentos · Verde: conceptos · Rojo: personas · Violeta: código
