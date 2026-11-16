---
tipo: carpeta
fuente: "desktop/README.md"
tags: [carpeta]
---
# Carpeta desktop

> Fuente: `desktop/README.md`

Proyecto **Veterinaria.Blazor** (.NET 10, interactividad Server) con datos de demostración. Hoy tiene páginas públicas, registro, verificación por código, inicio de sesión y retomar la reserva. Mapa completo: [[Mapa Código]].

Organización: `Models/` (datos), `Services/` (lógica, uno por tema, `AddScoped`), `Components/` (reutilizables) y `Components/Pages/` (una página por ruta). En la sección C cada servicio se reemplaza por uno que llama a la API.

Parte de [[Mapa Arquitectura]].

## Documentos relacionados
- [[Flujo reserva y consulta]]
- [[Arranque escritorio]]
- [[Especificación registro de cliente]]
- [[Especificación catálogo de servicios]]
- [[README del repositorio]]

## Conceptos
- [[Blazor]]
- [[API ASP.NET Core]]
- [[VITALIA]]
