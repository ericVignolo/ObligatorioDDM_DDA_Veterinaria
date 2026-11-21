---
tipo: controlador
app: api
fuente: "api/Veterinaria.Api/Controllers/TurnosController.cs"
tags: [codigo, controlador, api]
generado: true
---
# TurnosController (API)

> Fuente: `api/Veterinaria.Api/Controllers/TurnosController.cs` · Nota generada por `_scripts/actualizar_codigo.py`; no editar a mano.

**Ruta:** `api/v1/turnos`, `disponibilidad`, `{id:int}`, `{id:int}/cancelacion`, `{id:int}/horario`

Turnos del cliente (CU-CLI-02, A-014 a A-016). Solo con token de Cliente; cada cliente ve y cambia los suyos.

**Tareas:** A-014, A-016

**Requisitos y decisiones:** CU-CLI-02, RN-06, RN-08

Parte de [[Mapa Código]] · [[Carpeta api]]

## Usa
- [[ITurnoService (API)]]
- [[PaginaDto (API)]]
