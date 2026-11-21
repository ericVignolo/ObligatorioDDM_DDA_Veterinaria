---
tipo: controlador
app: api
fuente: "api/Veterinaria.Api/Controllers/InternacionesController.cs"
tags: [codigo, controlador, api]
generado: true
---
# InternacionesController (API)

> Fuente: `api/Veterinaria.Api/Controllers/InternacionesController.cs` · Nota generada por `_scripts/actualizar_codigo.py`; no editar a mano.

**Ruta:** `api/v1/internaciones`, `altas`, `{id:int}`, `boxes`, `{id:int}/boxes-libres`, `{id:int}/anulacion`, `{id:int}/ingreso`, `{id:int}/novedades`, `{id:int}/novedades/{novedadId:int}/rectificacion`, `{id:int}/alta`, `api/v1/mis-internaciones`, `{id:int}`, `api/v1/admin/boxes`, `{id:int}/servicio`

Internación para el personal (A-024, RF-VET-09 a RF-VET-11): Recepción admite o anula; el Veterinario ingresa

**Tareas:** A-024

**Requisitos y decisiones:** RF-ADM-03, RF-CLI-09, RF-VET-09, RF-VET-11, RN-18, RN-23, RN-24, RN-25

Parte de [[Mapa Código]] · [[Carpeta api]]

## Usa
- [[IInternacionService (API)]]
