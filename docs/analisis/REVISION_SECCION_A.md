# Revisión de la sección A (A-031)

Fecha: 2026-11-17. Alcance: escritorio Blazor con datos en memoria (A-001 a A-030). Las advertencias comunes están en `CONTEXTO_BASE.md`, sección 3.1: **esta revisión no declara completos el backend, la seguridad ni la persistencia**; eso pertenece a la API (sección C).

Método: revisor independiente (lectura del código contra la matriz, sin compilar), comprobaciones propias y CI (compila y corre 780+ pruebas). El revisor no alcanzó a leer todas las páginas (ver «Cobertura»).

## Hallazgos corregidos

| # | Hallazgo | Corrección |
|---|---|---|
| 1 | La bandeja de comunicaciones mostraba el texto del resumen clínico a Recepción y Administración. | El contenido del resumen clínico ya no se muestra en la bandeja; el reenvío sigue funcionando. |
| 2 | El motivo de una ausencia (puede ser una licencia médica) se veía en la ficha pública del profesional y en la reserva. | Se muestra solo «no está disponible ese día». |
| 3 | `NotFound` y `Error` estaban en inglés. | Traducidas al español rioplatense. |
| 4 | Textos de la guía de demostración y el README no coincidían con el comportamiento. | Corregidos. |
| 5 | Posible `@if` inválido en `FirmarConsentimiento.razor:105`. | Descartado: el CI compila. |

Quedan como pruebas permanentes (`RevisionSeccionATests`): enlaces internos literales a pantallas existentes, páginas de error en español, motivo de ausencia no expuesto y resumen clínico oculto en la bandeja.

## Decisiones (resueltas por Eric el 2026-11-17)

1. **Aviso de refuerzo por defecto (D-COM-03).** Hoy arranca por correo y la alerta sanitaria apagada. RN-43 (bloqueante) pide que lo facultativo no aparezca premarcado; Eric respondió «solo dejamos marcado por defecto lo que es necesario». **Resuelto:** cuenta como necesario y queda por correo por defecto.
2. **Reglas sin construir en la sección A y sin nota previa:** RN-08 (límite de ausencias para autoagendar), RN-09 (sobreturno solo Recepción/Administración), RN-16 (intervalo mínimo entre dosis con justificación), RN-17 (receta de un principio activo registrado como alergia), RN-35 (seña previa en cirugía) y RN-36 (deuda vencida y aviso al agendar). **Resuelto:** se construyeron (ver `ESPECIFICACION_REGLAS_PENDIENTES.md`): 3 ausencias, seña del 30 %, aviso de deuda desde $ 3.000, configurables.

## Cobertura y límites

- El revisor no leyó a fondo: páginas `Admin*`, `Cobros`, `Comprobante`, `Inventario`, `Internaciones`, `MisDerechos`, `Urgencias`, `LaClinica`, `Privacidad`, `Terminos`, ni `DerechosService`, `IndicadoresService` y `ProfesionalService`. No se afirma que estén libres de errores.
- La autorización por rol es visual; la real la hace la API. Los datos viven por pestaña.
- Sigue pendiente de Eric la prueba con F5 de A-029, A-030 y de esta tarea.
