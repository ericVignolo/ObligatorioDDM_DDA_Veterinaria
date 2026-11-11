# Mensaje para Diego — 2026-11-09

> **Estado 2026-11-10 (Diego):** punto 2 resuelto — opción A, `MySql.EntityFrameworkCore` 10.0.7 + EF Core 10. Punto 3 en curso — primera versión de la base en `api/Veterinaria.Api` (falta correr `Add-Migration Inicial` / `Update-Database` en Visual Studio). Punto 4 resuelto — R1 y R2 no se aplican todavía. Punto 5 pendiente.

De parte de Eric. **Diego se encarga de la base de datos**: decidir el proveedor, crearla y lo que quedó pendiente de su parte.

1. **Fecha de entrega: jueves 4 de diciembre de 2026** (DT-ORG-07 en `CONTEXTO_BASE.md`). Quedan 25 días desde el 09/11.
2. **Proveedor MySQL (DT-BD-02) — decidir ya.** El soporte de EF Core 9 termina el 10/11/2026 y Pomelo solo funciona con EF Core 9. Opciones: **A)** `MySql.EntityFrameworkCore` (Oracle) + EF Core 10, el que usa el profesor (10.0.7, `UseMySQL`); **B)** `Microting.EntityFrameworkCore.MySql` + EF Core 10; **C)** Pomelo + EF Core 9, sin soporte. Recomendación: **A**. El `SELECT … FOR UPDATE` no cambia. Detalle en `DECISIONES_ERIC_2026-10-31.md`.
3. **Crear la base de datos.** Referencia: los modelos de `desktop/Veterinaria.Blazor/Models/` y las reglas de `Services/` (mapa en `desktop/README.md` y en `VeterinariaBrain/`). Conviene empezar la sección C en paralelo, en una rama `diego/sesion-AAAA-MM-DD` desde `desktop`, carpeta `api/`, y registrarlo en `CONTEXTO_BASE.md`. Estilo del profesor: Controller → Service → Repository → `AppDbContext`, DTOs y `HasData`.
4. **Plan B — aplicar el disparador** (`PLAN_B_ALCANCE.md`). El primer recorrido tardó 7 de 36 días (19 %) contando desde el 29/10, o 5 de 34 (15 %) desde el 31/10. Más del 15 % aplica R1 (Veterinario solo en Blazor) y R2 (asistencia sin mapa en vivo). Está en el límite: decidir y registrarlo en la sección 4.
5. **Ramas para borrar** (todas integradas en `desktop`): `diego/cierre-h0`, `diego/proyecto-blazor`, `eric/readme-estado`, `eric/a-006-registro`, `eric/a-008-sesion`, `eric/sesion-2026-11-05`, `eric/sesion-2026-11-05-2`, `eric/sesion-2026-11-05-3` y `feature/escritorio-api`.

Mientras tanto, Eric sigue con el panel del personal (A-010, A-017, A-018).
