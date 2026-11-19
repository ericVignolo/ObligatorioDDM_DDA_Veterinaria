# Sesión 2026-11-19 — Base de datos completa (Diego)

Rama: `diego/sesion-2026-11-19`, creada desde `diego/sesion-2026-11-16` (el catálogo, PR #39, todavía sin mezclar). PR #40 contra `desarrollo`.

## Decisiones (Diego)

| Tema | Decisión |
|---|---|
| Orden | Primero se termina la base; después, los endpoints módulo por módulo. |
| Rama | Rama nueva a partir de la del catálogo, para no perder nada. Las ramas se mezclan y ordenan al final. |
| Migración | Todo lo que faltaba va en una sola migración nueva, `ModulosClinicos`. |

## Qué se hizo

- **Tablas nuevas (29, en total 51):**
  - consulta e historia: `Consultas`, `Enmiendas`, `LecturasHistoria`, `DocumentosConsentimiento`, `Consentimientos`;
  - inventario y vacunas: `Articulos`, `Lotes`, `MovimientosStock`, `ReservasStock`, `AlertasStock`, `ProductosSanitarios`, `ProductoSanitarioEspecies`, `AplicacionesSanitarias`;
  - cobro: `TarifasPrestacion`, `Cargos`, `Pagos`, `NotasCredito`, `Numeradores`;
  - internación: `Boxes`, `Internaciones`, `NovedadesInternacion`;
  - asistencia: `SolicitudesAsistencia`, `OfertasAsistencia`, `EventosAsistencia`;
  - comunicaciones y derechos: `TiposComunicacion`, `PreferenciasComunicacion`, `Comunicaciones`, `SolicitudesDerecho`, `PoliticasConservacion`.
- **Columnas nuevas:**
  - `Cuentas`: eliminación por derecho de supresión y correo nuevo pendiente.
  - `Turnos`: quién canceló, hora de llegada, sobreturno y quién lo dio.
  - `Profesionales`: vencimiento de la matrícula.
  - `Prestaciones`: si requiere consentimiento.
  - `Auditorias`: rol.
- **Restricciones:** `CHECK` de stock no negativo (RN-26) y de pago positivo; índices únicos de cargo por consulta (RN-32), número de comprobante y de nota de crédito, y una nota de crédito por comprobante (RN-34).
- **Semilla nueva:**
  - personal de demostración (5 cuentas con su rol, contraseña `Vitalia2026!`), enlazado con sus profesionales y con matrícula vigente hasta 2027;
  - prestación 8, «Castración», con su consentimiento 1.0;
  - 4 boxes;
  - 12 artículos, 20 lotes con fechas fijas y su movimiento de compra;
  - 7 productos sanitarios con sus especies;
  - tarifas iniciales;
  - 5 tipos de comunicación y la política de conservación.
- Archivos: `Models/Consulta.cs`, `Inventario.cs`, `Cobro.cs`, `Internacion.cs`, `Asistencia.cs` y `Comunicaciones.cs` (nuevos); `Cuenta.cs`, `Turno.cs`, `Profesional.cs`, `Prestacion.cs`, `Configuracion.cs` y `CodigoCuenta.cs` (cambios); `Data/ConfiguracionModulos.cs` (nuevo); `Data/AppDbContext.cs`.
- Prueba nueva: `tests/Veterinaria.Tests/ApiBaseDeDatosTests.cs`. Arma el modelo con el proveedor de MySQL y genera el script `CREATE` sin conectarse. CI en verde: 996 pruebas.
- Documentación: `api/README.md`, `tests/README.md`, DT-BD-02 en `CONTEXTO_BASE.md` y esta bitácora.

Diferencias con los modelos de Blazor:
- `Personal` no es una tabla: es una `Cuenta` con rol. Los autores (`AutorId`, `CobradoPorId`, `AdmitidoPorId`…) apuntan a `Cuentas`.
- `SolicitudAsistencia.OfrecidaA` (lista) pasa a la tabla `OfertasAsistencia`.
- `ProductoSanitario.Especies` (lista de texto) pasa a `ProductoSanitarioEspecies`.
- Los códigos de verificación de `Cuenta` siguen en `CodigosCuenta`, ahora también con el tipo `CambioCorreo`.
- Los `0` que en Blazor significan «sin valor» (por ejemplo `TurnoId = 0`) son `NULL` en la base.

## Cómo probarlo

1. GitHub Desktop → *Fetch origin* → rama `diego/sesion-2026-11-19`.
2. Si tenés un clon nuevo: copiar `appsettings.Development.example.json` como `appsettings.Development.json` y poner la contraseña.
3. Abrir `api/Veterinaria.Api/Veterinaria.Api.slnx` y compilar.
4. Consola del Administrador de paquetes:

   ```powershell
   Add-Migration ModulosClinicos
   Update-Database
   ```

5. En MySQL Workbench, refrescar la base `vitalia`: 51 tablas más `__EFMigrationsHistory`. Por ejemplo:
   - `SELECT COUNT(*) FROM Lotes;` → 20
   - `SELECT Correo FROM Cuentas;` → los 5 del personal
   - `SELECT Nombre, CuentaId, Matricula FROM Profesionales;` → Ferreira, Silveira y Acosta con cuenta
6. F5 y `https://localhost:7191/api/v1/servicios` → ahora son **7** servicios, porque se sumó «Castración».
7. Subir la carpeta `Migrations/` con la migración nueva.

## Qué falta

- [ ] Diego: correr `Add-Migration ModulosClinicos` y `Update-Database`, y subir `Migrations/`.
- [ ] Endpoints, en el orden del recorrido: profesionales → registro e inicio de sesión → animales → turnos → consulta → vacunas → stock → cobro → internación → asistencia → comunicaciones.
- [ ] Al final: mezclar los PR #39 y #40 y ordenar las ramas.

---

## Segunda parte: profesionales y datos de la clínica

Rama: `diego/sesion-2026-11-19-profesionales`, creada desde `diego/sesion-2026-11-19`. PR #41.

- Diego (2026-11-19): migración `ModulosClinicos` aplicada y subida (29 tablas nuevas, snapshot actualizado). CI en verde.
- Decisión de Diego: el horario del profesional se devuelve **como lista por día**.
- Endpoints nuevos:
  - `GET /api/v1/profesionales`, paginado;
  - `GET /api/v1/profesionales/{id}`, con el horario por día;
  - `GET /api/v1/clinica`.

  Son públicos. Se muestran solo los profesionales publicados y activos, y de cada uno solo sus servicios públicos. No se exponen la matrícula, la cuenta ni las ausencias.
- Archivos:
  - `Controllers/ProfesionalesController.cs`, `ClinicaController.cs` y `Problemas.cs` (los errores compartidos salen de `ServiciosController`);
  - `Services/IProfesionalService.cs`, `ProfesionalService.cs`, `IClinicaService.cs` y `ClinicaService.cs`, más `ReglasProfesionales` en `ReglasCatalogo.cs`;
  - `Repositories/IProfesionalRepository.cs`, `ProfesionalRepository.cs`, `IClinicaRepository.cs` y `ClinicaRepository.cs`;
  - `DTOs/ProfesionalReadDto.cs` y `ClinicaReadDto.cs`;
  - `Program.cs`.
- Pruebas: `ApiProfesionalesTests`, 10 nuevas. CI en verde con 1006 pruebas.

### Cómo probarlo

F5 en la API (rama `diego/sesion-2026-11-19-profesionales`). No hace falta migrar.
- `https://localhost:7191/api/v1/profesionales` → 4 profesionales (Ferreira, Silveira, Acosta y Méndez). Pereira no aparece porque no está publicado.
- `https://localhost:7191/api/v1/profesionales/1` → Ferreira, con `horario` de lunes a viernes de 09:00 a 14:00 y descanso de 11:30 a 12:00, y con la castración entre sus servicios.
- `https://localhost:7191/api/v1/profesionales/4` → 404.
- `https://localhost:7191/api/v1/clinica` → los datos de VITALIA.

### Qué falta

- [ ] Diego: probar con F5.
- [ ] Siguiente endpoint: registro, verificación e inicio de sesión (hash de contraseña, JWT y permisos por rol).
- [ ] Al final: mezclar los PR #39, #40 y #41 y ordenar las ramas.
