---
tipo: controlador
app: api
fuente: "api/Veterinaria.Api/Controllers/AdministracionController.cs"
tags: [codigo, controlador, api]
generado: true
---
# AdministracionController (API)

> Fuente: `api/Veterinaria.Api/Controllers/AdministracionController.cs` · Nota generada por `_scripts/actualizar_codigo.py`; no editar a mano.

**Ruta:** `api/v1/admin`, `servicios`, `servicios`, `servicios/{id:int}`, `servicios/{id:int}/tarifas`, `tarifas`, `personal`, `personal`, `personal/{id:int}/rol`, `personal/{id:int}/bloqueo`, `ausencias`, `ausencias`, `ausencias/{id:int}`, `feriados`, `feriados`, `feriados/{fecha}`, `parametros`, `parametros/{clave}`, `inventario/articulos/{id:int}/minimo`, `auditoria`

Configuración del Administrador (RF-ADM-01 a RF-ADM-04): servicios y tarifas, personal y roles, ausencias,

**Requisitos y decisiones:** RF-ADM-01, RF-ADM-04, RN-33

Parte de [[Mapa Código]] · [[Carpeta api]]

## Usa
- [[IAdministracionService (API)]]
- [[PaginaDto (API)]]
- [[Turno (API)]]
