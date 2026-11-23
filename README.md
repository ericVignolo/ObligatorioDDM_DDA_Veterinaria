# VITALIA — Sistema de gestión veterinaria

Obligatorio de **DDM / DDA** del Instituto Tecnológico CTC Colonia (Uruguay), carrera Analista Programador.

Sistema integral para **VITALIA**, una clínica veterinaria **ficticia** que atiende animales de compañía y de campo (perros, gatos, caballos, ovinos y bovinos). El recorrido central es **agendar → atender → registrar → notificar**: turnos, consulta e historia clínica, stock por lote, cobro, vacunas, internación y asistencia a domicilio con niveles de urgencia.

**Equipo:** Diego y Eric, responsables con igual autoridad ([DT-ORG-05](docs/contexto-base/CONTEXTO_BASE.md#2-estado-actual)), con apoyo de asistentes de IA.

## Tecnologías

| Parte | Tecnología | Estado |
|---|---|---|
| Escritorio | **Blazor Web App** · **C#** · **.NET 10** · interactividad Server · Bootstrap | ✅ Sección A terminada (A-001 a A-031), con datos de demostración. Falta el diseño visual |
| API | **ASP.NET Core** (.NET 10) · EF Core 10 · `MySql.EntityFrameworkCore` 10.0.7 · JWT | ✅ Completa (22/11): 21 controladores y unos 150 endpoints. Los últimos cinco PR (#52 a #56) están pendientes de mezclar en `desarrollo` |
| Base de datos | **MySQL 8.4 LTS** | ✅ 52 tablas y semilla de demostración. 3 migraciones en `desarrollo`; la cuarta (`SegundoFactor`) viene en el PR #56 |
| Móvil | **React Native** · **Expo SDK 58** · TypeScript · Expo Router · NativeWind 4 | 🟡 Sección B en curso |

Reglas de arquitectura: una sola API; Blazor y React Native la consumen y **ningún cliente accede a la base de datos**. Detalle en los [principios vigentes](docs/contexto-base/CONTEXTO_BASE.md#31-principios-vigentes). Entrega: **domingo 13/12/2026**.

## Estado actual

| Sección | Qué incluye | Estado |
|---|---|---|
| **A — Escritorio** | Visitante, Cliente, Recepción, Veterinario y Administrador: catálogo, registro e ingreso, animales, turnos, consulta e historia clínica, vacunas y carnet, inventario FEFO, cobro presencial, internación, consentimiento, asistencia a domicilio, administración, comunicaciones y accesibilidad. Ver [`desktop/README.md`](desktop/README.md) | ✅ Funcional (falta el CSS de diseño) |
| **Base de datos + API** | Catálogo, profesionales, clínica, cuentas y sesiones, animales, turnos con bloqueo de agenda, consulta con firma atómica, carnet, inventario, consentimiento, cobro, administración, internación, comunicaciones y asistencia. Más de 970 métodos de prueba y CI. Ver [`api/README.md`](api/README.md) | ✅ Completa. En los PR #52 a #56 (pendientes de mezclar): derechos del titular, tareas automáticas, correo por SMTP, avisos en tiempo real con SignalR y verificación en dos pasos (TOTP) del Administrador. Falta cargar la cuenta de correo (`Correo` en `appsettings.Development.json`) |
| **B — Móvil** | Proyecto Expo con la paleta de VITALIA (claro y oscuro) y el primer recorrido del Visitante (inicio, servicios, detalle). Ver [`mobile/README.md`](mobile/README.md). Hechas B-004, B-005 y B-001 (mapa por rol, pestañas, componentes compartidos y área pública), en la rama `eric/sesion-2026-11-21-movil-b001`, pendiente de mezclar | 🟡 En curso. Sigue B-006: registro, sesión y perfil |
| **C — Integración** | Conectar Blazor y la app móvil a la API | ⏳ Pendiente |
| **Diseño** | Opciones A y B en [`design/vitalia/`](design/vitalia/README.md) | ⏳ Falta elegir una |

Avance completo en el [plan de tareas](docs/analisis/PLAN_TAREAS_PROYECTO.md) y en las [bitácoras de trabajo](docs/sesiones/).

## Cómo ejecutarlo

Clonar el repositorio y cambiar a la rama `desarrollo`.

### Escritorio (Blazor)

Requisitos: **Visual Studio 2026** o el SDK de **.NET 10**. Abrir `desktop/Veterinaria.Blazor/Veterinaria.Blazor.slnx` y ejecutar con **F5**, o desde la terminal:

```
cd desktop/Veterinaria.Blazor
dotnet run
```

### API y base de datos

1. Tener **MySQL 8.4** corriendo.
2. Copiar `api/Veterinaria.Api/appsettings.Development.example.json` como `appsettings.Development.json` y poner la contraseña de MySQL (ese archivo no se sube).
3. Abrir `api/Veterinaria.Api/Veterinaria.Api.slnx`; en la *Consola del Administrador de paquetes* correr `Update-Database`.
4. Ejecutar con **F5** (`http://localhost:5191`). Para probarla, usar `Veterinaria.Api.http`. Personal de demostración: `recepcion@`, `ferreira@`, `silveira@`, `acosta@` y `admin@vitalia.uy`, contraseña `Vitalia2026!`.

Detalle en [`api/README.md`](api/README.md#crear-la-base-en-tu-computadora). Pruebas: [`tests/README.md`](tests/README.md).

### Móvil (Expo)

Requisitos: **Node.js** (LTS) y **Expo Go** en el celular o un emulador de Android.

```
cd mobile/Veterinaria.Movil
npm install
npx expo start
```

Escanear el QR con Expo Go. Detalle en [`mobile/README.md`](mobile/README.md).

## Estructura

| Carpeta | Contenido |
|---|---|
| [`desktop/Veterinaria.Blazor/`](desktop/README.md) | Aplicación Blazor: `Models/`, `Services/`, `Components/` y `Components/Pages/`. |
| [`api/`](api/README.md) | API ASP.NET Core: `Controllers/`, `Services/`, `Repositories/`, `DTOs/`, `Models/`, `Data/` y migraciones. |
| [`mobile/`](mobile/README.md) | Aplicación React Native con Expo (`mobile/Veterinaria.Movil/`). |
| [`design/vitalia/`](design/vitalia/README.md) | Sistema de diseño: paletas, tipografía, componentes e iconos (opciones A y B). |
| [`tests/`](tests/README.md) | Pruebas del dominio, API, base de datos e integración. |
| [`docs/`](docs/contexto-base/CONTEXTO_BASE.md) | Contexto, requisitos, casos de uso, decisiones, diagramas y bitácoras. |
| [`VeterinariaBrain/`](VeterinariaBrain/00%20Mapas/Inicio.md) | Cerebro de Obsidian: notas enlazadas de documentos y código, con sus dependencias. |

## Documentación principal

- [Contexto base](docs/contexto-base/CONTEXTO_BASE.md): objetivo, arquitectura y decisiones técnicas.
- [Matriz de requisitos](docs/contexto-base/MATRIZ_REQUISITOS.md): fuente canónica de RF, RN y RNF.
- [Tablero de decisiones](docs/analisis/DECISIONES_PARA_DESARROLLO.md) y [decisiones del 31/10](docs/analisis/DECISIONES_ERIC_2026-10-31.md).
- [Alcance por hitos](docs/analisis/ALCANCE_ENTREGA.md) y [plan B de alcance](docs/analisis/PLAN_B_ALCANCE.md).
- [Bitácoras de trabajo](docs/sesiones/): qué se hizo y qué falta en cada sesión.

## Forma de trabajo

- **Ramas:** `main` (versiones estables) · `desarrollo` (trabajo diario: escritorio, API y móvil juntos, cada uno en su carpeta). Cada aplicación se versiona con etiquetas (`desktop-vX.Y`, `api-vX.Y`, `mobile-vX.Y`).
- Cada sesión de trabajo va en una rama `<nombre>/sesion-AAAA-MM-DD` desde `desarrollo`, con un PR al final. Quien trabajó la prueba y mezcla su PR. `main` se actualiza desde `desarrollo` en cada hito estable.
- Al cerrar cada sesión se escribe una bitácora en `docs/sesiones/` con lo hecho y lo pendiente.
- El código sigue el estilo del docente; las convenciones y reglas para asistentes de IA están en [`AGENTS.md`](AGENTS.md).

---

VITALIA, sus datos de contacto y sus profesionales son ficticios. Los textos legales tienen estructura de documento real, pero no fueron revisados por un abogado.
