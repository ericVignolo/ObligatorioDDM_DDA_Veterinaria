---
tipo: controlador
app: api
fuente: "api/Veterinaria.Api/Controllers/CobrosController.cs"
tags: [codigo, controlador, api]
generado: true
---
# CobrosController (API)

> Fuente: `api/Veterinaria.Api/Controllers/CobrosController.cs` · Nota generada por `_scripts/actualizar_codigo.py`; no editar a mano.

**Ruta:** `api/v1/cobros`, `api/v1/recepcion/cobros`, `cargos`, `cargos/{id:int}`, `pagos`, `pagos`, `pagos/{id:int}`, `resumen`, `deuda/{clienteId:int}`, `api/v1/admin/cobros`, `pagos/{id:int}/anulacion`, `notas`

Cargos del cliente (A-023, RN-18): lo que debe, con su estado. Se paga en Recepción (DT-ALC-02).

**Tareas:** A-023

**Requisitos y decisiones:** RF-REC-05, RN-18, RN-34, RN-36

Parte de [[Mapa Código]] · [[Carpeta api]]

## Usa
- [[ICobroService (API)]]
- [[PaginaDto (API)]]
