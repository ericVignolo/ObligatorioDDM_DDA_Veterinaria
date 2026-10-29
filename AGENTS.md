# Instrucciones persistentes del proyecto

Antes de planificar, diseñar o implementar, leer:

1. `docs/contexto-base/CONTEXTO_BASE.md`.
2. `docs/contexto-base/MATRIZ_REQUISITOS.md` cuando el trabajo afecte funciones, reglas, validaciones, seguridad, datos o pruebas.
3. `docs/contexto-base/ANALISIS_COMPLEMENTARIO.md` cuando el trabajo afecte alcance, fases, arquitectura, UML, riesgos o defensa.
4. `docs/analisis/ESPECIFICACION_ALERTAS_ASISTENCIA.md` cuando el trabajo afecte alertas de asistencia, gravedad, ubicación, asignación, seguimiento, costos o atención fuera de horario.

## Reglas no negociables vigentes

- Mantener una única API ASP.NET Core en C# como autoridad de datos, autenticación, autorización y reglas de negocio.
- Blazor y React Native consumen esa API; ningún cliente accede directamente a la base de datos.
- La interfaz principal para computadora se desarrolla con Blazor/C#.
- La aplicación móvil se desarrolla con React Native y su diseño se prepara en Figma.
- Las reglas críticas pertenecen al backend/dominio y, cuando corresponda, se refuerzan con transacciones o restricciones de base de datos.
- Clasificar cada requisito nuevo como: decisión técnica, requerimiento funcional, regla de negocio, requerimiento no funcional, diseño/UX, pseudocódigo o consentimiento/alerta.
- Aplicar la redacción y clasificación de RF, RN-01 a RN-59 y RNF documentada en `MATRIZ_REQUISITOS.md`. No sustituirla por resúmenes informales.
- La navegación pública sin autenticación está permitida; las acciones protegidas deben solicitar inicio de sesión o registro y luego retomar la intención original del usuario.
- Un recorrido se diseña en alta fidelidad o se programa cuando sus decisiones, estados y pseudocódigo están cerrados (puerta H0 por recorrido, DT-ORG-03 en `CONTEXTO_BASE.md`).
- Las advertencias comunes (autoridad de la API, límites de los prototipos, estados de un requisito) se escriben solo en la sección 3.1 de `CONTEXTO_BASE.md`; los demás documentos la enlazan.
- Cualquier cambio de arquitectura, tecnología o regla obligatoria debe actualizar primero la documentación de contexto.

## Punto de continuación

Actualizado: 2026-10-29.

- Equipo: Eric y Diego, con apoyo de asistentes de IA. Sin fecha de entrega real todavía.
- Decisiones del primer recorrido (catálogo → registro → reserva → consulta) cerradas por Diego: ver `docs/analisis/DECISIONES_PARA_DESARROLLO.md`.
- Base de datos: MySQL 8.4 LTS, EF Core 9 con Pomelo, agenda protegida con `SELECT … FOR UPDATE` (DT-BD-01 y DT-BD-02).
- Ramas: secciones A (`desktop`), B (`desktop-mobile`) y C (`desktop-mobile-api`), encadenadas, con merge a `main` al aprobar cada una; trabajo diario en ramas cortas con PR (DT-ORG-02).
- Plan B de alcance listo, sin recortes aplicados: `docs/analisis/PLAN_B_ALCANCE.md`.
- Siguiente paso: sección A, tareas A-001 a A-003 de `docs/analisis/PLAN_TAREAS_PROYECTO.md` (esqueleto Blazor y catálogo con datos de demostración).
- Siguen abiertos: cobro por Recepción, internación, alertas sanitarias y parámetros de asistencia; se cierran antes de sus propias pantallas.
