# Sistema de gestión veterinaria

Proyecto académico de dos integrantes. La interfaz principal para computadora se desarrollará con Blazor/C#; la aplicación móvil con React Native. Ambas consumirán una única API ASP.NET Core, responsable de la autenticación, autorización, datos y reglas de negocio.

## Carpetas

| Carpeta | Contenido |
|---|---|
| [`api/`](api/README.md) | API y capas compartidas de aplicación, dominio, infraestructura y contratos. |
| [`desktop/`](desktop/README.md) | Interfaz Blazor para computadora. |
| [`mobile/`](mobile/README.md) | Aplicación React Native y su integración posterior con la API. |
| [`tests/`](tests/README.md) | Pruebas del dominio, API, base de datos e integración. |
| [`docs/`](docs/contexto-base/CONTEXTO_BASE.md) | Contexto, requisitos, casos de uso, historias y decisiones. |

Las carpetas de código todavía no tienen proyectos ejecutables. Las reglas comunes a todo el proyecto están en los [principios vigentes](docs/contexto-base/CONTEXTO_BASE.md#31-principios-vigentes).

## Orden de trabajo

- **Sección A — escritorio** (rama `desktop`): Blazor con datos de demostración. Es la sección activa.
- **Sección B — móvil** (rama `desktop-mobile`): React Native y Figma.
- **Sección C — API e integración** (rama `desktop-mobile-api`): API ASP.NET Core con MySQL 8.4 y conexión de ambos clientes.

Cada sección sale de la anterior y se mezcla a `main` al aprobarse; el trabajo diario va en ramas cortas con PR. Las decisiones del primer recorrido están cerradas: ver el [tablero de decisiones](docs/analisis/DECISIONES_PARA_DESARROLLO.md). Si el tiempo no alcanza, se aplica el [plan B de alcance](docs/analisis/PLAN_B_ALCANCE.md).

La [matriz de requisitos](docs/contexto-base/MATRIZ_REQUISITOS.md) es la fuente canónica. La [tabla de alcance](docs/analisis/ALCANCE_ENTREGA.md) organiza la entrega por hitos y el [plan de tareas](docs/analisis/PLAN_TAREAS_PROYECTO.md) el orden de construcción.
