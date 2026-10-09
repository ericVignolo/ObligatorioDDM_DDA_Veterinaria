# API compartida

Aquí se ubicarán los proyectos ASP.NET Core y las capas `Veterinaria.Api`, `Veterinaria.Application`, `Veterinaria.Domain`, `Veterinaria.Infrastructure` y `Veterinaria.Contracts`.

La API será la única autoridad para datos, autenticación, autorización y reglas de negocio. Blazor y React Native accederán a ella mediante su contrato versionado; ninguna interfaz consultará directamente la base de datos.

Carpeta preparada para una etapa posterior; todavía no hay proyectos de producto. La rama `desktop` trabaja primero la experiencia Blazor con datos de demostración, sin crear una segunda fuente de verdad ni permitir acceso directo de Blazor a la BD. Consultar [contexto técnico](../docs/contexto-base/CONTEXTO_BASE.md) y [alcance](../docs/analisis/ALCANCE_ENTREGA.md).

Primer contrato público propuesto: [catálogo de servicios](../docs/analisis/ESPECIFICACION_CATALOGO_SERVICIOS_API.md). Consultar sus decisiones abiertas antes de implementarlo.
