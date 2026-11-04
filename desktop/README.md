# Interfaz para computadora

`Veterinaria.Blazor`: interfaz de Visitante, Cliente, Recepción, Veterinario y Administración. Se construye en la sección A (rama `desktop`) con datos de demostración y se conecta a la API en la sección C.

Reglas que aplican: [principios vigentes](../docs/contexto-base/CONTEXTO_BASE.md#31-principios-vigentes).

Proyecto: `Veterinaria.Blazor/Veterinaria.Blazor.slnx` (.NET 10). Se abre en Visual Studio 2026 y se ejecuta con F5.

Hecho: inicio, catálogo de servicios, profesionales, la clínica, urgencias y documentos legales (A-001 a A-005). Siguiente: registro e ingreso (A-006 en adelante). Avance en el [plan de tareas](../docs/analisis/PLAN_TAREAS_PROYECTO.md).

Recorrido principal: [catálogo → acceso → reserva](../docs/diagramas/INICIO_ESCRITORIO.md), con las decisiones cerradas de [registro](../docs/analisis/ESPECIFICACION_REGISTRO_CLIENTE.md) y [reserva/consulta](../docs/analisis/FLUJO_RESERVA_CONSULTA.md).
