# Interfaz para computadora

`Veterinaria.Blazor`: interfaz de Visitante, Cliente, Recepción, Veterinario y Administración. Se construye en la sección A (rama `desktop`) con datos de demostración y se conecta a la API en la sección C.

Reglas que aplican: [principios vigentes](../docs/contexto-base/CONTEXTO_BASE.md#31-principios-vigentes).

Primer recorrido a construir: [catálogo → acceso → reserva](../docs/diagramas/INICIO_ESCRITORIO.md), con las decisiones ya cerradas de [registro](../docs/analisis/ESPECIFICACION_REGISTRO_CLIENTE.md) y [reserva/consulta](../docs/analisis/FLUJO_RESERVA_CONSULTA.md). Tareas: A-001 en adelante en el [plan de tareas](../docs/analisis/PLAN_TAREAS_PROYECTO.md).

## Cómo ejecutarlo

Requiere el SDK de .NET 10 (Visual Studio 2026, o Visual Studio 2022 con el SDK 10 instalado aparte).

1. Abrir `Veterinaria.sln` (en la raíz del repositorio).
2. Elegir `Veterinaria.Blazor` como proyecto de inicio y ejecutar (F5).

Desde la terminal: `dotnet run --project desktop/Veterinaria.Blazor`.

## Estructura

| Carpeta | Contenido |
|---|---|
| `Components/Layout` | Encabezado, navegación, aviso de demostración y pie. |
| `Components/Pages` | Inicio, Servicios, detalle de servicio, Urgencias, Acceso (en construcción), Error y No encontrada. |
| `Components/Shared` | Tarjeta de servicio, precio «Desde» y acción Reservar / Consultar o llamar. |
| `Datos` | `ICatalogoServicios`: lo que la pantalla necesita del catálogo. |
| `Datos/Demo` | Datos ficticios (sección A). En la sección C se reemplazan por un cliente HTTP de la API registrado en `Program.cs`. |

Los DTO compartidos con la API están en `api/Veterinaria.Contracts`.
