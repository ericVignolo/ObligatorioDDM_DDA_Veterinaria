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
- Antes del diseño visual definitivo se deben incorporar los requisitos suministrados por el usuario y definir el comportamiento/pseudocódigo correspondiente.
- Cualquier cambio de arquitectura, tecnología o regla obligatoria debe actualizar primero la documentación de contexto.

## Punto de continuación

El documento de análisis original, las políticas de privacidad y la primera especificación de alertas y asistencia ya fueron incorporados. El 2026-10-02 se confirmó un equipo de dos integrantes con apoyo de Codex y sin fecha límite real establecida. El 2026-10-08 se prepararon las carpetas `api/`, `desktop/`, `mobile/` y `tests/` y la rama `feature/escritorio-api` para trabajar primero en Blazor con la API compartida. La tabla de alcance está en `docs/analisis/ALCANCE_ENTREGA.md` y el arranque operativo en `docs/analisis/ARRANQUE_ESCRITORIO.md`. El frente funcional inmediato sigue siendo cerrar las decisiones de “Reservar turno” conectado con “Realizar consulta” en `docs/analisis/FLUJO_RESERVA_CONSULTA.md`, completar registro, cobro, internación y parámetros de asistencia, y construir y validar el mapa móvil de Visitante, Cliente, Asistencia y Veterinario, contemplando Recepción en Blazor. No comenzar todavía el diseño visual definitivo ni implementar código de producto hasta completar la especificación de registro, el pseudocódigo restante y las decisiones abiertas documentadas.
