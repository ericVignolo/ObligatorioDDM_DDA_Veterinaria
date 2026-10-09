# API compartida

Aquí se ubicarán los proyectos ASP.NET Core y las capas `Veterinaria.Api`, `Veterinaria.Application`, `Veterinaria.Domain`, `Veterinaria.Infrastructure` y `Veterinaria.Contracts`.

La API será la única autoridad para datos, autenticación, autorización y reglas de negocio. Blazor y React Native accederán a ella mediante su contrato versionado; ninguna interfaz consultará directamente la base de datos.

Carpeta preparada; todavía no hay proyectos de producto. Consultar [contexto técnico](../docs/contexto-base/CONTEXTO_BASE.md) y [alcance](../docs/analisis/ALCANCE_ENTREGA.md).

Primer contrato público propuesto: [catálogo de servicios](../docs/analisis/ESPECIFICACION_CATALOGO_SERVICIOS_API.md). Consultar sus decisiones abiertas antes de implementarlo.
