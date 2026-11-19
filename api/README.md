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

Capas (estilo del profesor): `Controllers/` → `Services/` (`IXxxService`) → `Repositories/` (`IXxxRepository`) → `AppDbContext`; `DTOs/` con mapeo explícito en el Service.

## Endpoints

| Método y ruta | Acceso | Respuesta |
|---|---|---|
| `GET /api/v1/servicios?page=1&pageSize=20` | Público | `200` con `items`, `page`, `pageSize` y `totalItems`; `400` (Problem Details, `PAGINACION_INVALIDA`) si `page` < 1 o `pageSize` no está entre 1 y 50 |
| `GET /api/v1/servicios/{id}` | Público | `200` con el servicio; `404` (Problem Details, `SERVICIO_NO_ENCONTRADO`) si no existe o no es público |
| `GET /api/v1/profesionales?page=1&pageSize=20` | Público | `200` paginado: nombre, especialidad, descripción, especies y servicios públicos; `400` si la paginación es inválida |
| `GET /api/v1/profesionales/{id}` | Público | `200` con lo anterior más `horario` por día (lunes a domingo, `jornadas` y `descansos` en `HH:mm`); `404` (`PROFESIONAL_NO_ENCONTRADO`) si no existe o no está publicado y activo |
| `GET /api/v1/clinica` | Público | `200` con nombre, dirección, teléfonos, guardia, correo, horario y cobertura; `404` (`CLINICA_NO_CONFIGURADA`) si no hay datos |

Un servicio es público si está publicado y tiene precio (`ReglasCatalogo.EsPublica`); un profesional, si está publicado y activo (`ReglasProfesionales.EsPublico`). A los profesionales no se les expone la matrícula, la cuenta ni las ausencias. Los errores salen de `Controllers/Problemas.cs`. Pruebas: `ApiCatalogoTests.cs` y `ApiProfesionalesTests.cs` en `tests/Veterinaria.Tests/`. El contrato OpenAPI se ve en `/openapi/v1.json` al correr en Development.

## Tablas (51)

Migración `Inicial` (2026-11-10, 22 tablas) y `ModulosClinicos` (2026-11-19, 29 tablas). La configuración de la segunda está en `Data/ConfiguracionModulos.cs`.

| Grupo | Tablas |
|---|---|
| Identidad | `Cuentas` (cliente y personal), `Roles`, `CuentaRoles`, `CodigosCuenta`, `DocumentosLegales`, `AceptacionesLegales` |
| Animales | `Especies`, `Animales`, `Responsabilidades` (cliente ↔ animal con vigencia) |
| Catálogo y agenda | `Prestaciones`, `Profesionales`, `ProfesionalEspecies`, `ProfesionalPrestaciones`, `FranjasHorarias`, `Ausencias`, `Recursos`, `Turnos`, `TurnoRecursos` |
| Consulta e historia | `Consultas`, `Enmiendas`, `LecturasHistoria`, `DocumentosConsentimiento`, `Consentimientos` |
| Vacunas e inventario | `Articulos`, `Lotes`, `MovimientosStock`, `ReservasStock`, `AlertasStock`, `ProductosSanitarios`, `ProductoSanitarioEspecies`, `AplicacionesSanitarias` |
| Cobro | `TarifasPrestacion`, `Cargos`, `Pagos` (comprobantes), `NotasCredito`, `Numeradores` |
| Internación | `Boxes`, `Internaciones`, `NovedadesInternacion` |
| Asistencia | `SolicitudesAsistencia`, `OfertasAsistencia`, `EventosAsistencia` |
| Comunicaciones y derechos | `TiposComunicacion`, `PreferenciasComunicacion`, `Comunicaciones` (cola de envío), `SolicitudesDerecho`, `PoliticasConservacion` |
| Configuración | `Clinicas`, `Parametros`, `DiasNoLaborables`, `Auditorias` |

El personal no tiene tabla propia: es una `Cuenta` con rol Recepcionista, Veterinario o Administrador, y el Veterinario se enlaza con su `Profesional`. Los autores (quién firmó, cobró, admitió, etc.) son claves a `Cuentas`.

Semilla (mismos datos de demostración que Blazor): roles, documentos legales 1.0, 11 especies, 8 prestaciones (con castración y su consentimiento), 5 profesionales con horarios, 4 recursos, VITALIA, parámetros y feriados; **personal**: `recepcion@`, `ferreira@`, `silveira@`, `acosta@` y `admin@vitalia.uy`, todos con la contraseña `Vitalia2026!`; 4 boxes; 12 artículos con 20 lotes y su movimiento de compra; 7 productos sanitarios; tarifas iniciales; tipos de comunicación y política de conservación. No trae clientes, animales ni turnos: se crean desde la API.

Decisiones del esquema:
- No hay borrado en cascada: las bajas son lógicas (`Activa`/`Activo`) y la historia se conserva.
- Restricciones `CHECK`: el stock de un lote no queda negativo (RN-26) y un pago es mayor que cero. Índices únicos: un cargo por consulta (RN-32), número de comprobante y de nota de crédito, una nota de crédito por comprobante (RN-34), un turno con una sola consulta.
- Lo que MySQL no puede expresar con un índice (una internación abierta por animal, RN-21; un paciente por box, RN-22) lo controla la API dentro de la transacción.
- Los textos clínicos largos son `LONGTEXT`, sin `MaxLength`, para no pasar el límite de tamaño de fila de MySQL. Los límites de largo se validan en los DTO.
- `Consultas`, `Turnos`, `Internaciones` y `SolicitudesAsistencia` tienen `Version` como control de concurrencia.
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

Para cambiar el esquema más adelante: modificar `Models/`, `AppDbContext` o `ConfiguracionModulos`, y después `Add-Migration <Nombre>` y `Update-Database`. `ApiBaseDeDatosTests` verifica el modelo en el CI antes de migrar.
