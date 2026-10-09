# Interfaz para computadora

Aquí se ubicará `Veterinaria.Blazor`, la interfaz principal de ordenador. Consumirá la API común para información pública y operaciones autorizadas de Recepción, Veterinario y Administración.

Una acción visible en Blazor no concede permisos por sí misma: la API valida cada operación. Los primeros recorridos se trazan desde [Reserva y consulta](../docs/analisis/FLUJO_RESERVA_CONSULTA.md).

La rama `desktop` prioriza esta interfaz. Una vez cerradas las especificaciones necesarias, sus recorridos podrán prototiparse con datos de demostración y una capa de acceso reemplazable. Eso no equivale a persistencia, seguridad ni reglas de negocio reales: la integración posterior debe consumir la única API compartida. Todavía no hay proyecto ejecutable ni diseño visual definitivo.
