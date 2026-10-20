# Arranque del frente de ordenador

Fecha: 2026-10-09

Estado: plan ampliado el 2026-10-20. La sección A desarrolla escritorio; B agrega móvil; C integra ambos clientes con la API.

## Alcance de este arranque

La rama `desktop` parte del estado documentado en `feature/escritorio-api` y separa la primera etapa de Blazor para computadora. Primero se preparan y validan pantallas, navegación y recorridos interactivos con datos de demostración. La sección B agrega el cliente React Native con las mismas limitaciones de datos simulados. La sección C implementa e integra una sola API ASP.NET Core con ambos clientes; no se sustituye por acceso directo a la base desde Blazor o React Native.

La estructura física prevista es `api/`, `desktop/`, `mobile/`, `tests/` y `docs/`. Git confirma que `main` y `desktop` existen; `desktop-mobile` y `desktop-mobile-api` son los nombres propuestos para las dos ramas siguientes y aún no existen. Falta acordar su ascendencia/merge respecto de `feature/escritorio-api`. La secuencia objetivo es acumulativa: `main` → `desktop` → `desktop-mobile` → `desktop-mobile-api`. Primero se validan experiencias de escritorio con datos ficticios; después se prepara la experiencia móvil con contratos/adaptadores temporales; finalmente se implementa e integra la API común y MySQL. La API sigue siendo la autoridad única final. No se permite que Blazor o React Native accedan directamente a MySQL. Los recorridos A y B no se describen como persistencia, autenticación, autorización ni reglas críticas reales.

El inventario completo de trabajo, con tareas numeradas, dependencias y criterios de cierre, se mantiene en [PLAN_TAREAS_PROYECTO.md](PLAN_TAREAS_PROYECTO.md). Este plan por ramas organiza el orden de construcción; los hitos H0–H5 de [ALCANCE_ENTREGA.md](ALCANCE_ENTREGA.md) conservan la trazabilidad funcional y no se sustituyen.

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
3. Después de validar la experiencia móvil, preparar en la sección C la solución .NET/API y capas bajo `api/`, conectar MySQL según DT-BD-01 y definir la estrategia de intervalos antes de implementar agenda real. Conectar ambos clientes a contratos versionados y sustituir sus adaptadores temporales. Solo entonces se verifican los RF/RN/RNF que requieren persistencia, autorización o reglas de servidor.
4. Mantener por módulo la trazabilidad RF → caso de uso → endpoint → pantalla → prueba. Revisar el tiempo real invertido tras el primer incremento antes de comprometer fechas.

Para coordinar el primer incremento con Diego, consultar [contrato propuesto del catálogo](ESPECIFICACION_CATALOGO_SERVICIOS_API.md), [registro de Cliente](ESPECIFICACION_REGISTRO_CLIENTE.md), [recorrido API de reserva a consulta](RECORRIDO_API_RESERVA_CONSULTA.md) y [decisiones pendientes](DECISIONES_PARA_DESARROLLO.md). Son borradores trazables; no levantan por sí mismos la condición de cierre de H0 indicada en el punto 1.

Desde el 2026-10-09, Diego es responsable de cerrar las decisiones restantes y registrar el motivo y el impacto de cada elección. Eric y Codex revisan la documentación y la implementación; no se reabren tácitamente las decisiones ya confirmadas por Eric.

El primer [flujo y árbol binario de decisiones del escritorio](../diagramas/INICIO_ESCRITORIO.md) están en borrador Mermaid para revisar antes del diseño visual definitivo. Representan el comportamiento objetivo; durante `desktop` las operaciones que requieren API se ensayarán solo como simulación explícita.

La interfaz pública en ordenador debe conservar navegación sin inicio de sesión. En el prototipo se puede simular la solicitud de acceso y la reanudación de intención; la autenticación y autorización reales permanecen pendientes de la API/dominio. La interfaz de ordenador no consulta la base directamente.

En la etapa `desktop` sí se pueden comprobar navegación, validaciones de presentación, selección de opciones, cambios de estado visibles y mensajes de éxito/error usando datos de demostración reproducibles. Cada prueba debe distinguir «respuesta visual correcta» de «regla de negocio garantizada». No se guardan datos reales ni se simula que una reserva, firma clínica o cobro persista después de cerrar el prototipo.

## Qué sigue abierto

- Registro, alertas sanitarias, pseudocódigo restante y decisiones P-RC-01 a P-RC-08 del recorrido de reserva/consulta. Estos pendientes son puerta de salida H0, previa al diseño visual definitivo o código de producto.
- Parámetros del módulo de asistencia y mapas de navegación definitivos.
- Versión/proveedor de MySQL, exclusión de intervalos de agenda, solución .NET, pruebas ejecutables y fecha real de entrega.

La tabla de alcance mantiene los 47 RF Must en la base. No se rebajan por comenzar por ordenador; la rama móvil posterior completará los canales y funciones que le corresponden.
