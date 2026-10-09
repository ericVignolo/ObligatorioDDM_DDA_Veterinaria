# Arranque del frente de ordenador

Fecha: 2026-10-09

Estado: rama `desktop` creada para priorizar el escritorio; API pospuesta para integración.

## Alcance de este arranque

La rama `desktop` parte del estado documentado en `feature/escritorio-api` y separa la primera etapa de Blazor para computadora. Primero se preparan y validan pantallas, navegación y recorridos interactivos con datos de demostración. La API ASP.NET Core queda para una etapa posterior; no se sustituye por acceso directo a la base desde Blazor. Una futura rama de integración incorporará la API compartida, y el móvil se apoyará en ella.

La estructura física prevista es `api/`, `desktop/`, `mobile/`, `tests/` y `docs/`. Mantener `mobile/` ahora evita reubicar el repositorio cuando comience el segundo cliente; no adelanta su implementación ni crea otra API.

## Qué se puede marcar como hecho

| Trabajo | Estado | Evidencia |
|---|---|---|
| Clasificar los 74 RF entre entrega base y extensiones | Documentado | [ALCANCE_ENTREGA.md](ALCANCE_ENTREGA.md). |
| Desarrollar el primer caso integrado de reserva y consulta | Borrador funcional | [FLUJO_RESERVA_CONSULTA.md](FLUJO_RESERVA_CONSULTA.md); faltan decisiones P-RC. |
| Separar físicamente API, ordenador, móvil y pruebas | Preparado | Carpetas y README de cada área. |
| Crear rama separada de escritorio | Preparado | `desktop`, derivada de `feature/escritorio-api`. |
| Implementar y probar RF, RN y RNF | Pendiente | No existe todavía una solución de producto ni resultados de pruebas. |

Un RF pasa a “implementado” cuando existe un recorrido funcional en el canal comprometido y la API aplica sus reglas. Pasa a “verificado” cuando tiene pruebas adecuadas y evidencia. Haberlo documentado o asignado a un hito no equivale a cumplirlo.

## Próximo avance con mayor valor

1. Completar la especificación de registro y los puntos P-RC que afectan identidad, agenda, permisos, base de datos, consumo y cierre. Cerrar también las decisiones abiertas necesarias de los módulos que se incorporen. Esto completa H0 antes de iniciar código de producto conforme a [AGENTS.md](../../AGENTS.md).
2. Una vez cerrado H0, preparar Blazor bajo `desktop/` y ensayar un recorrido pequeño de catálogo público con datos de demostración. Las interacciones de escritorio se pueden validar como prototipo, sin presentar permisos, reservas, stock o cobros simulados como funcionalidad productiva.
3. En la etapa posterior, preparar la solución .NET y API/capas bajo `api/`, seleccionar motor de BD y estrategia de intervalos antes de implementar agenda real, y conectar el catálogo y los demás recorridos a contratos versionados. Solo entonces verificar RF-VIS-01 y continuar con registro/roles/animal, reserva (H1), consulta y cobro (H2).
4. Mantener por módulo la trazabilidad RF → caso de uso → endpoint → pantalla → prueba. Revisar el tiempo real invertido tras el primer incremento antes de comprometer fechas.

Para coordinar el primer incremento con Diego, consultar [contrato propuesto del catálogo](ESPECIFICACION_CATALOGO_SERVICIOS_API.md), [registro de Cliente](ESPECIFICACION_REGISTRO_CLIENTE.md), [recorrido API de reserva a consulta](RECORRIDO_API_RESERVA_CONSULTA.md) y [decisiones pendientes](DECISIONES_PARA_DESARROLLO.md). Son borradores trazables; no levantan por sí mismos la condición de cierre de H0 indicada en el punto 1.

Desde el 2026-10-09, Diego es responsable de cerrar las decisiones restantes y registrar el motivo y el impacto de cada elección. Eric y Codex revisan la documentación y la implementación; no se reabren tácitamente las decisiones ya confirmadas por Eric.

La interfaz pública en ordenador debe conservar navegación sin inicio de sesión. En el prototipo se puede simular la solicitud de acceso y la reanudación de intención; la autenticación y autorización reales permanecen pendientes de la API/dominio. La interfaz de ordenador no consulta la base directamente.

## Qué sigue abierto

- Registro, alertas sanitarias, pseudocódigo restante y decisiones P-RC-01 a P-RC-08 del recorrido de reserva/consulta.
- Parámetros del módulo de asistencia y mapas de navegación definitivos.
- Motor de base de datos, solución .NET, pruebas ejecutables y fecha real de entrega.

La tabla de alcance mantiene los 47 RF Must en la base. No se rebajan por comenzar por ordenador; la rama móvil posterior completará los canales y funciones que le corresponden.
