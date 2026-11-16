---
tipo: carpeta
fuente: "api/README.md"
tags: [carpeta]
---
# Carpeta api

> Fuente: `api/README.md`

Un solo proyecto `Veterinaria.Api` (DT-API-01, 2026-11-10), con el estilo del profesor. Es la única autoridad de datos y reglas. Primera versión de la base: `Data/AppDbContext.cs` (tablas, índices y semilla `HasData`) y entidades en `Models/`. Usa MySQL 8.4 + EF Core 10 + `MySql.EntityFrameworkCore` (DT-BD-02). Todavía no tiene endpoints. Las notas generadas cubren solo Blazor: antes de tocar `api/`, revisar `AppDbContext`, que configura todas las tablas.

Parte de [[Mapa Arquitectura]].

## Documentos relacionados
- [[Contexto base]]
- [[Alcance de entrega]]
- [[Especificación catálogo de servicios]]

## Conceptos
- [[API ASP.NET Core]]
- [[MySQL]]
