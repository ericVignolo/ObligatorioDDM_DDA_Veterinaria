# Instrucciones persistentes del proyecto

Antes de planificar, diseñar o implementar, leer:

1. `docs/contexto-base/CONTEXTO_BASE.md`.
2. `docs/contexto-base/MATRIZ_REQUISITOS.md` cuando el trabajo afecte funciones, reglas, validaciones, seguridad, datos o pruebas.
3. `docs/contexto-base/ANALISIS_COMPLEMENTARIO.md` cuando el trabajo afecte alcance, fases, arquitectura, UML, riesgos o defensa.

## Reglas no negociables vigentes

- Mantener una única API ASP.NET Core en C# como autoridad de datos, autenticación, autorización y reglas de negocio.
- Blazor y React Native consumen esa API; ningún cliente accede directamente a la base de datos.
- La interfaz principal para computadora se desarrolla con Blazor/C#.
- La aplicación móvil se desarrolla con React Native y su diseño se prepara en Figma.
- Las reglas críticas pertenecen al backend/dominio y, cuando corresponda, se refuerzan con transacciones o restricciones de base de datos.
- Clasificar cada requisito nuevo como: decisión técnica, requerimiento funcional, regla de negocio, requerimiento no funcional, diseño/UX, pseudocódigo o consentimiento/alerta.
- Aplicar la redacción y clasificación de RF, RN-01 a RN-42 y RNF documentada en `MATRIZ_REQUISITOS.md`. No sustituirla por resúmenes informales.
- La navegación pública sin autenticación está permitida; las acciones protegidas deben solicitar inicio de sesión o registro y luego retomar la intención original del usuario.
- Antes del diseño visual definitivo se deben incorporar los requisitos suministrados por el usuario y definir el comportamiento/pseudocódigo correspondiente.
- Cualquier cambio de arquitectura, tecnología o regla obligatoria debe actualizar primero la documentación de contexto.

## Punto de continuación

El documento de análisis original ya fue incorporado. El siguiente trabajo funcional es construir y validar el mapa de la aplicación móvil, comenzando por Visitante y Cliente y continuando por Veterinario. No comenzar todavía el diseño visual definitivo ni implementar código de producto sin incorporar los requisitos adicionales de pseudocódigo, privacidad, alertas y registro que entregue el usuario.
