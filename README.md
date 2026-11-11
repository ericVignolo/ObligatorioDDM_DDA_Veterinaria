# VITALIA — Sistema de gestión veterinaria

Obligatorio de **DDM / DDA** del Instituto Tecnológico CTC Colonia (Uruguay), carrera Analista Programador.

Sistema integral para **VITALIA**, una clínica veterinaria **ficticia** que atiende animales de compañía y de campo (perros, gatos, caballos, ovinos y bovinos). El recorrido central es **agendar → atender → registrar → notificar**: turnos, consulta e historia clínica, stock por lote, cobro, vacunas, internación y asistencia a domicilio con niveles de urgencia.

**Equipo:** Diego y Eric, responsables con igual autoridad ([DT-ORG-05](docs/contexto-base/CONTEXTO_BASE.md#2-estado-actual)), con apoyo de asistentes de IA.

## Tecnologías

| Parte | Tecnología | Estado |
|---|---|---|
| Escritorio | **Blazor Web App** · **C#** · **.NET 10** · interactividad Server · Bootstrap | En construcción (sección A) |
| Móvil | **React Native** · diseño en Figma | Sección B |
| API | **ASP.NET Core** (C#) · EF Core · única autoridad de datos y reglas | Sección C |
| Base de datos | **MySQL 8.4 LTS** | Sección C (proveedor de EF Core en revisión, DT-BD-02) |

Reglas de arquitectura: una sola API; Blazor y React Native la consumen y **ningún cliente accede a la base de datos**. Detalle en los [principios vigentes](docs/contexto-base/CONTEXTO_BASE.md#31-principios-vigentes).

## Estado actual

**Sección A — escritorio**, con datos de demostración (todavía sin API ni base de datos).

| Tarea | Qué incluye | Estado |
|---|---|---|
| A-001 | Proyecto Blazor, navegación y cultura `es-UY` | ✅ |
| A-002 | Página de inicio de VITALIA | ✅ |
| A-003 | Catálogo de servicios con detalle | ✅ |
| A-004 | Profesionales con horarios y página de la clínica | ✅ |
| A-005 | Urgencias y documentos legales (Privacidad, Términos, Cookies) | ✅ |
| A-006 | Registro de cliente con validaciones y aceptaciones separadas | ✅ |
| A-007 | Verificación del correo por código y recuperación de contraseña | ✅ |
| A-008 | Inicio y cierre de sesión | ✅ |
| A-011 | Retomar la reserva después de ingresar | ✅ |
| A-012, A-013 | Mis animales: listado, ficha, alta y edición (incluye grupos de campo) | ✅ |
| A-014, A-015 | Disponibilidad y reserva en cuatro pasos, con Mis turnos | ✅ |
| A-016 | Cancelar y reprogramar turnos (24 h), historial de turnos | ✅ |
| A-010, A-017, A-018 | Panel del personal por rol, agenda de Recepción y del Veterinario, estados unificados | ✅ |
| A-009 | Perfil del Cliente («Mi cuenta»): datos con quién ve cada uno, animales con historia, turnos y «Reservar de nuevo», cambio de correo y de contraseña con verificación | ✅ |
| A-019, A-020 | Consulta veterinaria (borrador, firma con contraseña), enmiendas e historia clínica para veterinario y cliente | ✅ |
| A-021 | Vacunas y antiparasitarios: se registran dentro de la consulta, lote que no vence (RN-15), refuerzo calculado (RN-19), carnet con estados y alertas sanitarias | 🧪 En prueba |
| A-022 en adelante | Inventario y demás módulos | Pendiente |

El mapa de páginas, servicios y modelos (con diagrama) está en [`desktop/README.md`](desktop/README.md).

Avance completo en el [plan de tareas](docs/analisis/PLAN_TAREAS_PROYECTO.md) y en la [última bitácora de trabajo](docs/sesiones/).

## Cómo ejecutarlo

Requisitos: **Visual Studio 2026** (o el SDK de **.NET 10**).

1. Clonar el repositorio y cambiar a la rama `desarrollo`.
2. Abrir `desktop/Veterinaria.Blazor/Veterinaria.Blazor.slnx`.
3. Ejecutar con **F5** y aceptar el certificado HTTPS de desarrollo si lo pide.

Desde la terminal:

```
cd desktop/Veterinaria.Blazor
dotnet run
```

## Estructura

| Carpeta | Contenido |
|---|---|
| [`desktop/Veterinaria.Blazor/`](desktop/README.md) | Aplicación Blazor: `Models/`, `Services/`, `Components/` y `Components/Pages/`. |
| [`api/`](api/README.md) | API ASP.NET Core (sección C). |
| [`mobile/`](mobile/README.md) | Aplicación React Native (sección B). |
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
