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

Las carpetas de código delimitan responsabilidades. Aún no contienen proyectos ejecutables. La solución .NET se agregará en la raíz cuando se cierre la especificación pendiente para iniciar el código de producto.

## Orden de trabajo

El trabajo inicial de ordenador se realiza en `feature/escritorio-api`. Incluye Blazor y la API común; la aplicación móvil reutilizará esa API en una rama posterior. El [arranque del frente de ordenador](docs/analisis/ARRANQUE_ESCRITORIO.md) indica qué preparación está terminada y qué falta antes de considerar cumplidos los requisitos funcionales.

La [matriz de requisitos](docs/contexto-base/MATRIZ_REQUISITOS.md) es la fuente canónica. La [tabla de alcance](docs/analisis/ALCANCE_ENTREGA.md) organiza la entrega por hitos sin modificar la prioridad original.
