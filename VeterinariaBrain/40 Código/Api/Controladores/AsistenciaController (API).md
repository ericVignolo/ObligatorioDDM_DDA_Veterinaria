---
tipo: controlador
app: api
fuente: "api/Veterinaria.Api/Controllers/AsistenciaController.cs"
tags: [codigo, controlador, api]
generado: true
---
# AsistenciaController (API)

> Fuente: `api/Veterinaria.Api/Controllers/AsistenciaController.cs` · Nota generada por `_scripts/actualizar_codigo.py`; no editar a mano.

**Ruta:** `api/v1/asistencias`, `{id:int}`, `{id:int}/empeoramiento`, `api/v1/recepcion/asistencias`, `cerradas`, `{id:int}`, `{id:int}/elegibles`, `{id:int}/asignacion`, `{id:int}/empeoramiento`, `{id:int}/cierre`, `plazos`, `api/v1/veterinario/asistencias`, `{id:int}`, `{id:int}/aceptacion`, `{id:int}/rechazo`, `{id:int}/avance`, `{id:int}/finalizacion`, `{id:int}/nivel`, `api/v1/admin/guardia`

Asistencia del cliente desde la app (RF-CLI-21 a 25, B-013, B-014): crear, seguir e informar empeoramiento.

**Tareas:** B-013, B-014

**Requisitos y decisiones:** RF-ADM-13, RF-CLI-21, RF-REC-08, RF-VET-16, RN-51, RN-52, RN-53, RN-58

Parte de [[Mapa Código]] · [[Carpeta api]]

## Usa
- [[Asistencia (API)]]
- [[IAsistenciaService (API)]]
