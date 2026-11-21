---
tipo: controlador
app: api
fuente: "api/Veterinaria.Api/Controllers/ComunicacionesController.cs"
tags: [codigo, controlador, api]
generado: true
---
# ComunicacionesController (API)

> Fuente: `api/Veterinaria.Api/Controllers/ComunicacionesController.cs` · Nota generada por `_scripts/actualizar_codigo.py`; no editar a mano.

**Ruta:** `api/v1/comunicaciones`, `{id:long}/leida`, `preferencias`, `preferencias/{tipoId:int}`, `api/v1/recepcion/comunicaciones`, `cola`, `{id:long}/reintento`, `{id:long}/reenvio`, `comprobantes/{pagoId:int}`, `avisos`, `api/v1/difusion/alerta-sanitaria`

Bandeja y preferencias del cliente (RF-CLI-18, RF-CLI-20). Cada cliente ve solo sus mensajes (RN-18).

**Requisitos y decisiones:** RF-CLI-18, RF-CLI-20, RF-REC-07, RN-18, RNF-CON-04

Parte de [[Mapa Código]] · [[Carpeta api]]

## Usa
- [[IComunicacionService (API)]]
- [[PaginaDto (API)]]
- [[Rol (API)]]
