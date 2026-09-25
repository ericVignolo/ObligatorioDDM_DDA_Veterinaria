# Historial recuperado de `ObligatorioDDM_DDA`

Fecha de recuperación: 2026-09-21

## Conversaciones encontradas

### 1. Plan de implementación

ID: `6aaea422-6950-83e9-a374-6fe1378cc2df`

Hitos:

- Se formalizó la separación entre el plan API + Blazor y el plan React Native + Figma.
- Se fijó una API única compartida, Blazor/C# para computadora, React Native para móvil, Figma para diseño y una interfaz responsiva.
- Se acordó clasificar por separado las decisiones técnicas, RF, RN, RNF, UX, pseudocódigo y consentimientos.
- Se propuso comenzar por el mapa funcional móvil del Cliente y continuar por Veterinario, antes de diseñar una pantalla de login de alta fidelidad.
- Se definió navegación pública sin autenticación y protección de acciones como agendar una mascota o un servicio.
- Se aclaró que los documentos futuros del usuario serán fuente de requisitos.
- La aclaración más reciente indicó que el comportamiento/pseudocódigo debe establecerse antes del diseño visual definitivo.

Prompts clave del usuario:

> La API debe ser consumida por la app de escritorio y la app móvil; escritorio en Blazor/C#, móvil en React Native, diseño en Figma y responsive.

> Lo próximo a formalizar deben ser requerimientos funcionales y reglas de negocio, sin tocar las decisiones técnicas ya establecidas.

> El usuario podrá navegar sin iniciar sesión, pero para agendar una mascota o servicios deberá iniciar sesión o registrarse.

> Se entregará una carpeta con requisitos de diseño, pseudocódigo, privacidad, aceptaciones, alertas y el camino de registro.

### 2. Plan De Implementación apps

ID: `6aaea922-3358-83e9-a374-6fe1378cc2df`

Hitos:

- Se enumeraron RT-01 a RT-10, ahora preservadas en `CONTEXTO_BASE.md`.
- Se detalló un plan de seis etapas para API + Blazor.
- Se detalló un plan de seis etapas para React Native + Figma.
- Se señaló que ambos clientes pueden avanzar en paralelo sobre contratos estables.
- Se detectó que el documento anterior contiene referencias desactualizadas a aplicación web, MVC/MVVM y una familia tecnológica común para móvil.

### 3. Conectar carpeta al proyecto

ID: `6ab19df0-9a98-83e9-bfb9-045c5ae7a916`

Hitos:

- Se aclaró que el nombre del proyecto ChatGPT y el de la carpeta local no necesitan coincidir.
- Proyecto de ChatGPT: `ObligatorioDDM_DDA`.
- Carpeta local elegida: `ObligatorioVeterinaria`.
- El proyecto de ChatGPT contiene el contexto conversacional y un documento de análisis; la carpeta local contendrá el código y la documentación persistente.

## Estado de los archivos

Las conversaciones no contienen adjuntos directos. Hacen referencia a un documento agregado a nivel del proyecto de ChatGPT, pero ese archivo no estuvo disponible mediante el historial recuperado. El navegador solicitó autenticación y no se automatizó el inicio de sesión.

No se atribuyó ningún nombre, formato ni contenido exacto al documento para evitar inventar información.

## Criterio de autoridad

- Las instrucciones explícitas más recientes del usuario prevalecen sobre propuestas anteriores.
- Las RT-01 a RT-10 se consideran vigentes.
- El orden interno de Figma/pseudocódigo tuvo propuestas distintas; la aclaración más reciente deja el pseudocódigo/comportamiento antes del diseño visual definitivo.
- Las RN-01 a RN-42 solo serán canónicas cuando el documento fuente esté disponible.
