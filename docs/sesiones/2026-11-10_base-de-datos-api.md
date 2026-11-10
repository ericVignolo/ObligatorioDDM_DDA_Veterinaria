# Sesión 2026-11-10 — Base de datos y proyecto de la API (Diego)

Rama: `diego/sesion-2026-11-10` desde `desktop`.

## Decisiones (Diego)

| ID | Decisión |
|---|---|
| DT-BD-02 | Proveedor: `MySql.EntityFrameworkCore` 10.0.7 (Oracle) con **EF Core 10**, el mismo que usa el profesor. Pomelo + EF Core 9 queda descartado (sin soporte desde hoy). |
| DT-API-01 | La API es **un solo proyecto** `api/Veterinaria.Api` con carpetas (estilo del profesor). Reemplaza la propuesta de cinco proyectos. |
| DT-ORG-08 | La sección C (API + base) empieza **en paralelo** con lo que queda de A. |
| Plan B | **R1 y R2 no se aplican todavía**; se vuelve a medir en el próximo hito. |
| Alcance de la base | Primera versión = primer recorrido + base de identidad. Los demás módulos se agregan con su propia migración. |
| Creación | Con migraciones EF Core (`Add-Migration Inicial` / `Update-Database`). |

## Qué se hizo

- `api/Veterinaria.Api/`: `Veterinaria.Api.csproj` (.NET 10, `MySql.EntityFrameworkCore` 10.0.7, `Microsoft.EntityFrameworkCore.Tools`, OpenAPI), `Veterinaria.Api.slnx`, `Program.cs` con `UseMySQL`, `appsettings.json` sin contraseña, `appsettings.Development.example.json` y `launchSettings.json`.
- `Models/`: `Cuenta`, `Rol`/`CuentaRol`, `CodigoCuenta`, `DocumentoLegal`/`AceptacionLegal`, `Especie`/`Animal`/`Responsabilidad`, `Prestacion`, `Profesional` (con `ProfesionalEspecie`, `ProfesionalPrestacion`, `FranjaHoraria` y `Ausencia`), `Recurso`/`Turno`/`TurnoRecurso`, y `Clinica`, `Parametro`, `DiaNoLaborable` y `Auditoria`.
- `Data/AppDbContext.cs`: 22 tablas; índices únicos (correo, microchip, rol, especie, documento + versión, feriado); índices de solapamiento de DT-BD-02; `Version` de `Turno` como control de concurrencia; sin borrado en cascada; semilla con los mismos datos ficticios de Blazor.
- `.gitignore`: se agregó `api/Veterinaria.Api/appsettings.Development.json`.
- Documentación: DT-BD-02, DT-API-01 y DT-ORG-08 en `CONTEXTO_BASE.md`; `api/README.md`; Plan B §4; `AGENTS.md` (punto de continuación); estado en `MENSAJE_PARA_DIEGO.md`; nota `Carpeta api` del cerebro.

Diferencias con los modelos de Blazor:
- `Animal.ResponsableId` pasa a la tabla `Responsabilidades` (cliente ↔ animal con vigencia, FLUJO_RESERVA_CONSULTA §7).
- `Animal.Especie` (texto) pasa a ser `EspecieId`. `Profesional.Especies` y `PrestacionIds` pasan a ser tablas intermedias.
- Los códigos de verificación y recuperación pasan de `Cuenta` a la tabla `CodigosCuenta`, y las aceptaciones legales a `AceptacionesLegales`.
- `DateOnly`/`TimeOnly` se guardan como `DateTime` con columna `DATE` y como `TimeSpan`.
- Las ausencias de la semilla tienen fechas fijas (18/11 y 28/11–02/12), porque `HasData` no admite «hoy + N días».

## Cómo probarlo

> Claude no pudo compilar. La primera vez se prueba en Visual Studio.

1. GitHub Desktop → *Fetch origin* → rama `diego/sesion-2026-11-10`.
2. Copiar `api/Veterinaria.Api/appsettings.Development.example.json` como `appsettings.Development.json` y poner la contraseña de MySQL.
3. Abrir `api/Veterinaria.Api/Veterinaria.Api.slnx` → compilar.
4. Consola del Administrador de paquetes: `Add-Migration Inicial` y después `Update-Database`.
5. En MySQL Workbench: base `vitalia`, 22 tablas más `__EFMigrationsHistory`. Por ejemplo, `SELECT * FROM Prestaciones;` devuelve 7 filas y `SELECT * FROM FranjasHorarias;` 36.
6. Subir también la carpeta `Migrations/` que se genera, para que Eric cree la misma base.

## Qué falta

- [ ] Diego: correr la migración inicial, subir `Migrations/` y mezclar el PR.
- [ ] Primer endpoint con Controller → Service → Repository → DTOs: catálogo `GET /api/v1/prestaciones` (ESPECIFICACION_CATALOGO_SERVICIOS_API).
- [ ] Hash de contraseñas y cuentas del personal (sin semilla por ahora).
- [ ] Migraciones de los módulos siguientes: consulta e historia clínica, stock por lote, cargos y cobro, vacunas, internación, asistencia y notificaciones.
- [ ] Borrar en GitHub las ramas ya integradas (punto 5 del mensaje).
- [ ] Volver a medir el disparador del Plan B en el próximo hito.
