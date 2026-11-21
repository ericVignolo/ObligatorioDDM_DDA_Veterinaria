---
tipo: controlador
app: api
fuente: "api/Veterinaria.Api/Controllers/VeterinarioController.cs"
tags: [codigo, controlador, api]
generado: true
---
# VeterinarioController (API)

> Fuente: `api/Veterinaria.Api/Controllers/VeterinarioController.cs` · Nota generada por `_scripts/actualizar_codigo.py`; no editar a mano.

**Ruta:** `api/v1/veterinario`, `agenda`, `turnos/{turnoId:int}/consulta`, `consultas/abiertas`, `consultas/{id:int}`, `consultas/{id:int}`, `consultas/{id:int}/insumos`, `consultas/{id:int}/insumos/{articuloId:int}`, `consultas/{id:int}/vacunas`, `consultas/{id:int}/vacunas/{aplicacionId:int}`, `consultas/{id:int}/firma`, `consultas/{id:int}/cancelacion`, `consultas/{id:int}/enmiendas`, `animales/{animalId:int}/historia`

Agenda, consulta clínica e historia del veterinario (RF-VET-01 a 06, RF-VET-13, CU-VET-01).

**Requisitos y decisiones:** CU-VET-01, RF-VET-01, RF-VET-13, RN-12, RN-15, RN-16, RN-40

Parte de [[Mapa Código]] · [[Carpeta api]]

## Usa
- [[IConsultaService (API)]]
- [[ITurnoService (API)]]
