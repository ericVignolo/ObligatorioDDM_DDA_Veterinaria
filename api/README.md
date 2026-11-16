# API compartida

`Veterinaria.Api`: un solo proyecto ASP.NET Core (.NET 10) con el estilo del profesor (Controller → Service → Repository → `AppDbContext`). Usa MySQL 8.4 con EF Core 10 y el proveedor de Oracle `MySql.EntityFrameworkCore` (DT-BD-02 y DT-API-01 en `CONTEXTO_BASE.md`).

Las reglas que aplican están en los [principios vigentes](../docs/contexto-base/CONTEXTO_BASE.md#31-principios-vigentes). El primer contrato es el [catálogo de servicios](../docs/analisis/ESPECIFICACION_CATALOGO_SERVICIOS_API.md).

## Estructura

```text
api/Veterinaria.Api/
├── Data/AppDbContext.cs     tablas, índices y semilla (HasData)
├── Models/                  entidades de la base
├── Properties/launchSettings.json
├── Program.cs               conexión a MySQL (UseMySQL)
├── appsettings.json         cadena de conexión sin contraseña
└── appsettings.Development.example.json
```

`Controllers/`, `Services/`, `Repositories/` y `DTOs/` se crean cuando se haga el primer endpoint.

## Tablas de la primera versión

| Grupo | Tablas |
|---|---|
| Identidad | `Cuentas`, `Roles`, `CuentaRoles`, `CodigosCuenta`, `DocumentosLegales`, `AceptacionesLegales` |
| Animales | `Especies`, `Animales`, `Responsabilidades` (cliente ↔ animal con vigencia) |
| Catálogo y agenda | `Prestaciones`, `Profesionales`, `ProfesionalEspecies`, `ProfesionalPrestaciones`, `FranjasHorarias`, `Ausencias`, `Recursos`, `Turnos`, `TurnoRecursos` |
| Configuración | `Clinicas`, `Parametros`, `DiasNoLaborables`, `Auditorias` |

La semilla tiene los mismos datos ficticios que Blazor: roles, documentos legales 1.0, 11 especies, 7 prestaciones, 5 profesionales con jornadas, descansos y ausencias, 4 recursos, los datos de VITALIA, los parámetros por defecto y los feriados. No trae cuentas, porque la contraseña necesita un hash y las cuentas se crean desde la API.

Consulta, historia clínica, stock, cargos, internación, vacunas, asistencia y notificaciones se agregan después, cada una con su migración.

Decisiones del esquema:
- No hay borrado en cascada: las bajas son lógicas (`Activa`/`Activo`) y la historia se conserva.
- `Microchip` es único y puede quedar vacío (`NULL`) (RN-41).
- Los índices `(ProfesionalId, Inicio, Fin)`, `(AnimalId, Inicio, Fin)` y `(RecursoId, Inicio, Fin)` sirven para controlar los solapamientos con `SELECT … FOR UPDATE` (DT-BD-02).
- Las fechas sin hora se guardan como `DATETIME` en C# con columna `DATE`, y las horas como `TimeSpan` (`TIME`). En Blazor son `DateOnly` y `TimeOnly`, y la conversión se hace en el Service. Así no dependemos de cómo trate el proveedor de Oracle a `DateOnly`/`TimeOnly`.

## Crear la base en tu computadora

1. Tener MySQL 8.4 corriendo (MySQL Workbench o el servicio de Windows).
2. Copiar `appsettings.Development.example.json` como `appsettings.Development.json`, en la misma carpeta, y poner tu contraseña de MySQL. Ese archivo no se sube a GitHub.
3. Abrir `api/Veterinaria.Api/Veterinaria.Api.slnx` en Visual Studio y compilar. NuGet descarga `MySql.EntityFrameworkCore` 10.0.7 y `Microsoft.EntityFrameworkCore.Tools`. Si NuGet avisa de un conflicto de versiones, actualizá `Microsoft.EntityFrameworkCore.Tools` a la última 10.0.x.
4. En *Herramientas → Administrador de paquetes NuGet → Consola*:

   ```powershell
   Add-Migration Inicial
   Update-Database
   ```

5. En MySQL Workbench aparece la base `vitalia`, con las tablas y los datos de la semilla.

Para cambiar el esquema más adelante: modificar `Models/` o `AppDbContext`, y después `Add-Migration <Nombre>` y `Update-Database`.
