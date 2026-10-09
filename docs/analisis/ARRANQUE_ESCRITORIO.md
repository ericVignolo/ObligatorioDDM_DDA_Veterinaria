# Arranque del frente de ordenador

Fecha: 2026-10-08

Estado: preparación del repositorio y próximos criterios de avance.

## Alcance de este arranque

La rama `feature/escritorio-api` reúne el desarrollo de Blazor para computadora y la API que será común a ambos clientes. La futura rama móvil se creará desde esta rama una vez estable, o desde `main` cuando ese trabajo ya esté fusionado. Así el móvil aprovecha contratos y reglas existentes.

La estructura física prevista es `api/`, `desktop/`, `mobile/`, `tests/` y `docs/`. Mantener `mobile/` ahora evita reubicar el repositorio cuando comience el segundo cliente; no adelanta su implementación ni crea otra API.

## Qué se puede marcar como hecho

| Trabajo | Estado | Evidencia |
|---|---|---|
| Clasificar los 74 RF entre entrega base y extensiones | Documentado | [ALCANCE_ENTREGA.md](ALCANCE_ENTREGA.md). |
| Desarrollar el primer caso integrado de reserva y consulta | Borrador funcional | [FLUJO_RESERVA_CONSULTA.md](FLUJO_RESERVA_CONSULTA.md); faltan decisiones P-RC. |
| Separar físicamente API, ordenador, móvil y pruebas | Preparado | Carpetas y README de cada área. |
| Crear rama de trabajo de ordenador con API compartida | Preparado | `feature/escritorio-api`. |
| Implementar y probar RF, RN y RNF | Pendiente | No existe todavía una solución de producto ni resultados de pruebas. |

Un RF pasa a “implementado” cuando existe un recorrido funcional en el canal comprometido y la API aplica sus reglas. Pasa a “verificado” cuando tiene pruebas adecuadas y evidencia. Haberlo documentado o asignado a un hito no equivale a cumplirlo.

## Próximo avance con mayor valor

1. Completar la especificación de registro y los puntos P-RC que afectan identidad, agenda, permisos, base de datos, consumo y cierre. Cerrar también las decisiones abiertas necesarias de los módulos que se incorporen. Esto completa H0 antes de iniciar código de producto conforme a [AGENTS.md](../../AGENTS.md).
2. Preparar la solución .NET en la raíz, con API/capas bajo `api/`, Blazor bajo `desktop/` y pruebas bajo `tests/`. Seleccionar formalmente el motor de base de datos y la estrategia de exclusión de intervalos antes de implementar agenda.
3. Construir un primer recorrido completo y pequeño: catálogo público de servicios en API y Blazor (RF-VIS-01), con datos y contrato versionado. Después, registro/roles/animal y la reserva (H1), para continuar con la consulta y cobro (H2).
4. Mantener por módulo la trazabilidad RF → caso de uso → endpoint → pantalla → prueba. Revisar el tiempo real invertido tras el primer incremento antes de comprometer fechas.

Para coordinar el primer incremento con Diego, consultar [contrato propuesto del catálogo](ESPECIFICACION_CATALOGO_SERVICIOS_API.md), [registro de Cliente](ESPECIFICACION_REGISTRO_CLIENTE.md), [recorrido API de reserva a consulta](RECORRIDO_API_RESERVA_CONSULTA.md) y [decisiones pendientes](DECISIONES_PARA_DESARROLLO.md). Son borradores trazables; no levantan por sí mismos la condición de cierre de H0 indicada en el punto 1.

Desde el 2026-10-09, Diego es responsable de cerrar las decisiones restantes y registrar el motivo y el impacto de cada elección. Eric y Codex revisan la documentación y la implementación; no se reabren tácitamente las decisiones ya confirmadas por Eric.

La interfaz pública en ordenador debe conservar navegación sin inicio de sesión. Las acciones protegidas solicitan acceso y retoman la intención original. Las reglas permanecen en la API/dominio. La interfaz de ordenador no consulta la base directamente.

## Qué sigue abierto

- Registro, alertas sanitarias, pseudocódigo restante y decisiones P-RC-01 a P-RC-08 del recorrido de reserva/consulta.
- Parámetros del módulo de asistencia y mapas de navegación definitivos.
- Motor de base de datos, solución .NET, pruebas ejecutables y fecha real de entrega.

La tabla de alcance mantiene los 47 RF Must en la base. No se rebajan por comenzar por ordenador; la rama móvil posterior completará los canales y funciones que le corresponden.
