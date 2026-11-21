---
tipo: controlador
app: api
fuente: "api/Veterinaria.Api/Controllers/SaludController.cs"
tags: [codigo, controlador, api]
generado: true
---
# SaludController (API)

> Fuente: `api/Veterinaria.Api/Controllers/SaludController.cs` · Nota generada por `_scripts/actualizar_codigo.py`; no editar a mano.

**Ruta:** `api/v1`, `animales/{id:int}/carnet`, `alertas-sanitarias`, `turnos/consentimientos-pendientes`, `turnos/{id:int}/consentimiento`, `turnos/{id:int}/consentimiento`

Carnet sanitario, alertas de refuerzo y consentimiento informado del cliente (A-021, A-025, RF-CLI-06, RF-CLI-11).

**Tareas:** A-021, A-025

**Requisitos y decisiones:** RF-CLI-06, RF-CLI-11, RN-18

Parte de [[Mapa Código]] · [[Carpeta api]]

## Usa
- [[ICarnetService (API)]]
- [[IConsentimientoService (API)]]
