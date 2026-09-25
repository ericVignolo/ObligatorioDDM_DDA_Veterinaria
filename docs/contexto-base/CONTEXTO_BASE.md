# Contexto base — Sistema de gestión veterinaria

Actualizado: 2026-09-22

Este documento consolida el contexto recuperado del proyecto de ChatGPT `ObligatorioDDM_DDA`. Es la referencia operativa para continuar el trabajo en la carpeta local `ObligatorioVeterinaria`.

## 1. Objetivo del proyecto

Construir un sistema de gestión veterinaria con dos clientes que comparten una única API y las mismas reglas de negocio:

- Una interfaz principal para computadora desarrollada con Blazor/C#.
- Una aplicación móvil desarrollada con React Native.
- Una API ASP.NET Core en C# como autoridad del sistema.
- Diseño previo de la experiencia móvil en Figma.

El flujo general mencionado en los planes es:

`agendar → atender → registrar → notificar`

## 2. Estado actual

- La carpeta local estaba vacía al iniciar esta recuperación.
- No se ha creado todavía la solución .NET, la aplicación React Native ni el archivo de Figma.
- No se debe comenzar todavía el diseño visual definitivo.
- El documento original `docs/fuentes/Analisis_Clinica_Veterinaria.docx` ya fue revisado e incorporado al contexto.
- La especificación contiene 62 RF, 42 RN y 55 RNF recuperados del documento original, más 2 RF, 3 RN y 5 RNF complementarios incorporados; su texto canónico está en `MATRIZ_REQUISITOS.md`.
- Los requisitos adicionales de privacidad, consentimiento, accesibilidad, terceros, derechos de autor y publicación fueron incorporados desde `Implementacion_Politicas.txt`.
- El plan de implementación visual, las alternativas evaluadas y la decisión inicial de diseño móvil están documentados en `Diseño.md`.
- Continúan pendientes los requisitos adicionales de pseudocódigo, registro y alertas que entregue el usuario.
- El próximo frente acordado es funcional: incorporar los requisitos adicionales y construir el mapa de navegación, empezando por Visitante y Cliente y después Veterinario.

## 3. Reglas técnicas obligatorias

| ID | Regla |
|---|---|
| RT-01 | Existirá una única API que centralizará acceso a datos, autenticación, autorización y reglas de negocio. |
| RT-02 | La interfaz principal para computadora será desarrollada completamente con Blazor/C#. |
| RT-03 | Blazor no accederá directamente a la base de datos; consumirá la API. |
| RT-04 | La aplicación móvil será desarrollada con React Native. |
| RT-05 | React Native consumirá la misma API utilizada por Blazor. |
| RT-06 | El diseño de la aplicación móvil se realizará previamente en Figma. |
| RT-07 | Las interfaces deberán ser responsivas y contemplar, como mínimo, móvil y computadora según corresponda. |
| RT-08 | Las reglas críticas se implementarán en backend/API y, cuando corresponda, con restricciones o transacciones de base de datos; nunca dependerán únicamente de Blazor o React Native. |
| RT-09 | El contrato de la API deberá estar documentado y versionado para que ambos clientes evolucionen sin duplicar lógica. |
| RT-10 | Los cambios de arquitectura, tecnologías o reglas obligatorias deberán reflejarse en el plan y la documentación antes de considerarse oficiales. |

Principios derivados:

- Ocultar una función en la interfaz no constituye seguridad; la autorización se verifica en el servidor.
- Ambos clientes pueden avanzar en paralelo cuando los endpoints y DTO de un módulo estén estables.
- Figma es una herramienta de diseño, no una pieza de ejecución del sistema.
- Las tecnologías acordadas no deben mezclarse con las reglas de negocio: cada decisión debe clasificarse correctamente.

## 4. Arquitectura objetivo

```text
Blazor/C# ───────┐
                 ├── REST/HTTPS ── API ASP.NET Core ── Dominio/Servicios ── Datos ── BD
React Native ────┘
      ▲
      └── diseño en Figma
```

Estructura .NET propuesta:

```text
Veterinaria.sln
├── Veterinaria.Api
├── Veterinaria.Blazor
├── Veterinaria.Application
├── Veterinaria.Domain
├── Veterinaria.Infrastructure
├── Veterinaria.Contracts
└── Veterinaria.Tests
```

`Veterinaria.Contracts` concentrará los DTO y contratos REST compartidos conceptualmente por ambos clientes. React Native no dependerá del código C#.

El documento original desarrolla estrategias específicas para SQL Server y las conversaciones usan Entity Framework Core como base del plan técnico. La selección debe formalizarse al cerrar las fundaciones, pero las reglas de integridad y concurrencia no dependen de la interfaz elegida.

## 5. Roles identificados

- Cliente/responsable.
- Veterinario.
- Recepcionista.
- Administrador.
- Visitante no autenticado.

Cliente y Veterinario son los roles móviles principales mencionados. Recepcionista y Administrador aparecen en el plan de identidad, seguridad y Blazor.

## 6. Regla de acceso público y autenticación

- Un visitante puede navegar, conocer la aplicación y explorar los servicios sin iniciar sesión.
- Las acciones que operan sobre información personal o reservan servicios son protegidas.
- Ejemplos confirmados de acciones protegidas: gestionar/agendar para una mascota y reservar/agendar servicios.
- Si un visitante intenta una acción protegida, el sistema debe llevarlo a iniciar sesión o registrarse.
- Tras autenticarse, el flujo debe permitirle continuar la acción que estaba realizando, evitando perder su intención.
- El registro deberá contemplar los consentimientos y alertas que el usuario suministrará, incluyendo privacidad y aceptaciones obligatorias.
- Las aceptaciones se separarán por finalidad y versión; las preferencias opcionales no estarán premarcadas y podrán revocarse cuando corresponda.
- Las políticas legales serán públicas y versionadas. La API conservará la autoridad y trazabilidad de las decisiones del usuario.

## 7. Módulos y funcionalidades identificados

### Base e identidad

- Registro, inicio de sesión, recuperación de cuenta, sesión persistente y cierre de sesión.
- Usuarios, roles, permisos, autorización de endpoints, auditoría y baja lógica.
- Clientes/responsables, veterinarios y datos maestros.

### Datos maestros

- Especies y razas.
- Servicios y tipos de cita.
- Recursos físicos.
- Otras tablas maestras que surjan del documento funcional.

### Gestión de mascotas

- Registro y listado de mascotas.
- Detalle de mascota.
- Historia clínica.
- Carnet sanitario y vacunas.
- Adjuntos/archivos.
- Estado o información de internación.

### Agenda

- Próximos turnos e historial.
- Agendar, confirmar y cancelar turnos según las reglas de negocio.
- Agenda diaria/semanal del veterinario.
- Disponibilidad de veterinarios y recursos.
- Respuesta comprensible ante colisiones, seguida de una actualización de horarios; no exponer un error técnico al usuario.

### Atención clínica

- Consulta de pacientes asignados.
- Registro de atención y consultas.
- Historia clínica.
- Vacunación.
- Consultas firmadas/inmutables, mencionadas como regla crítica pendiente de recuperar con su redacción exacta.

### Internación

- Consulta y gestión de internaciones según rol.

### Inventario

- Gestión de inventario.
- Stock no negativo y FEFO fueron mencionados como reglas críticas; falta recuperar su definición canónica.

### Comunicaciones

- Notificaciones dentro del flujo principal.
- Notificaciones push como capacidad móvil prevista.

### Facturación y administración

- Facturación con restricciones de negocio aún pendientes de recuperar en detalle.
- Administración e indicadores en Blazor.

### Capacidades móviles adicionales

- Cámara/fotos y QR como capacidades previstas.
- Funcionamiento offline figura como `Could`; no es obligatorio ni parte del camino crítico inicial.

## 8. Mapa funcional móvil provisional

```text
APP MÓVIL
│
├── Área pública
│   ├── Inicio
│   ├── Servicios
│   └── Acceso
│       ├── Iniciar sesión
│       ├── Registrarse
│       └── Recuperar contraseña
│
├── Cliente
│   ├── Inicio
│   ├── Mis mascotas
│   │   ├── Detalle
│   │   ├── Historia clínica
│   │   ├── Carnet sanitario
│   │   └── Internación
│   ├── Turnos
│   │   ├── Próximos
│   │   ├── Historial
│   │   └── Agendar turno
│   ├── Notificaciones
│   └── Perfil
│
└── Veterinario
    ├── Inicio
    ├── Mi agenda
    ├── Paciente
    ├── Historia clínica
    ├── Registrar atención
    ├── Internaciones
    └── Perfil
```

Este mapa es provisional. Debe validarse contra los nuevos requisitos antes de convertirse en diseño.

Flujo de agenda propuesto:

`Cliente → Mis mascotas → Mascota → Agendar turno → Tipo de consulta → Veterinario → Horarios disponibles → Confirmación`

Comportamiento propuesto al confirmar un turno:

1. Validar que exista un horario seleccionado.
2. Solicitar confirmación de la reserva.
3. Enviar la solicitud de creación a la API.
4. Si se acepta, mostrar confirmación y actualizar próximos turnos.
5. Si el horario dejó de estar disponible, informar claramente y actualizar la disponibilidad.
6. Si ocurre otro error, mostrar un mensaje adecuado sin detalles técnicos.

## 9. Especificación funcional y reglas recuperadas

La especificación canónica se encuentra en `MATRIZ_REQUISITOS.md` e incluye:

- 62 requerimientos funcionales: 9 de Visitante, 19 de Cliente, 15 de Veterinario, 7 de Recepcionista y 12 de Administrador.
- 42 reglas de negocio completas y clasificadas como bloqueantes o de advertencia.
- 55 requerimientos no funcionales de seguridad, concurrencia, base de datos, rendimiento, disponibilidad, usabilidad, mantenibilidad y legalidad.
- 2 RF, 3 RN y 5 RNF complementarios de privacidad, consentimiento, seguimiento, accesibilidad, licencias y revisión normativa.

Al implementar o diseñar un flujo, se deben conservar sus identificadores y rastrear cada pantalla, endpoint, validación y prueba a los RF, RN y RNF afectados.

## 10. Requerimientos no funcionales mencionados

- Interfaz utilizable desde 360 px de ancho.
- Objetivo de accesibilidad WCAG 2.1 AA.
- API documentada y versionada con Swagger/OpenAPI.
- Manejo global de errores y logging.
- Configuración por ambientes.
- Concurrencia, transacciones e idempotencia.
- Auditoría.
- Paginación y manejo de archivos.
- Pruebas unitarias, de integración API/BD, autorización, concurrencia, aceptación y end-to-end.

Esta lista es solo un resumen. La redacción verificable completa está en `MATRIZ_REQUISITOS.md`.

## 11. Plan A — API + Blazor

1. **Fundación:** solución .NET, capas, entidades, base de datos, Entity Framework Core, DTO/contratos, migraciones, ambientes, Swagger/OpenAPI, errores, logging y pruebas.
2. **Identidad y seguridad:** usuarios, roles, permisos, autenticación, estrategia de tokens si corresponde, autorización, auditoría y baja lógica.
3. **Núcleo funcional:** clientes/responsables, mascotas, veterinarios, especies/razas, servicios, tipos de cita, recursos y tablas maestras. Comenzar primeras pantallas Blazor en paralelo.
4. **Módulos:** agenda/disponibilidad → historia clínica → internación → inventario → comunicaciones → facturación.
5. **Reglas críticas y robustez:** RN-01 a RN-42, concurrencia, transacciones, idempotencia y garantías en base de datos.
6. **Blazor completo:** paneles por rol, agenda, fichas clínicas, internación, inventario, administración, facturación e indicadores.
7. **Integración y defensa:** pruebas, seguridad, datos de demostración, documentación y UML actualizado.

## 12. Plan B — React Native + Figma

1. **Requisitos y comportamiento:** incorporar documentos del usuario; clasificar RF, RN, RNF, UX, pseudocódigo y consentimientos. La última aclaración establece que el pseudocódigo debe preceder al diseño visual definitivo.
2. **Definición funcional:** mapa de navegación y user flows, primero Cliente y luego Veterinario.
3. **Wireframes y Figma:** estructura en escala de grises, navegación, formularios, jerarquía y estados; después design system y UI de alta fidelidad.
4. **Prototipo navegable:** validar recorridos, alertas, estados y acciones protegidas.
5. **Fundación React Native:** navegación, tema, componentes, estado, cliente HTTP, almacenamiento seguro de token, sesión, ambientes y errores.
6. **Integración:** autenticación/usuarios → mascotas → agenda → funciones clínicas → internación → notificaciones.
7. **Adaptación y pruebas:** tamaños de pantalla, carga/error/sin conexión, expiración de sesión, permisos, concurrencia y flujos completos.

Organización React Native propuesta:

```text
src/
├── api/
├── components/
├── features/
│   ├── auth/
│   ├── pets/
│   ├── appointments/
│   ├── clinicalHistory/
│   └── hospitalization/
├── navigation/
├── screens/
├── services/
├── hooks/
├── store/
├── theme/
└── utils/
```

La decisión visual inicial se encuentra en `Diseño.md`: dirección clínica cálida, verde petróleo y coral, tipografía Inter, navegación inferior funcional y navegación contextual dentro de cada mascota. La alta fidelidad permanece condicionada al cierre de pseudocódigo, registro y alertas.

## 13. Método para incorporar nuevos requisitos

Cada elemento nuevo debe registrarse sin mezclar categorías:

- Decisión técnica (DT/RT).
- Requerimiento funcional (RF).
- Regla de negocio (RN).
- Requerimiento no funcional (RNF).
- Requisito de diseño/UX.
- Requisito de pseudocódigo/comportamiento.
- Consentimiento, política, confirmación o alerta.

Si una regla cambia, registrar qué reemplaza, la fecha y el impacto sobre API, Blazor, React Native, Figma, pruebas y documentación.

## 14. Pendientes inmediatos

1. Incorporar los documentos adicionales de pseudocódigo, registro y alertas; privacidad y consentimientos ya fueron incorporados desde `Implementacion_Politicas.txt`.
2. Confirmar formalmente el motor de base de datos y la estrategia concreta para impedir solapamientos de intervalos variables.
3. Construir y validar el mapa funcional de Visitante y Cliente, respetando los RF-VIS y RF-CLI.
4. Construir y validar el mapa funcional del Veterinario, respetando los RF-VET.
5. Definir pseudocódigo, estados, errores, consentimientos y reanudación de acciones protegidas antes del diseño visual definitivo.
6. Preparar trazabilidad entre RF, RN, RNF, pantallas, endpoints y casos de prueba.
7. Actualizar UML y justificación de patrones para que coincidan con Blazor/C# + API .NET + React Native + Figma.

## 14.1 Artefactos funcionales derivados

Se incorporaron como borradores para validación los primeros artefactos funcionales de Visitante y Cliente:

- `docs/analisis/HISTORIAS_USUARIO_VISITANTE_CLIENTE.md`: backlog con 30 historias, criterios de aceptación y trazabilidad.
- `docs/analisis/CASOS_DE_USO_VISITANTE_CLIENTE.md`: catálogo de 23 casos de uso y especificación detallada de 9 recorridos esenciales.

Estos documentos no sustituyen la matriz canónica. Permanecen en estado de borrador hasta resolver los pendientes de registro, alertas y pseudocódigo, validar el alcance y completar los casos resumidos.

## 15. Fuentes de esta recuperación

- Proyecto ChatGPT: `ObligatorioDDM_DDA` (`g-p-6aacb1aef7648191ac475284acde7878`).
- Documento fuente: `docs/fuentes/Analisis_Clinica_Veterinaria.docx`, versión 1.0, setiembre de 2026.
- Conversación `Plan de implementación` (`6aaea422-6950-83e9-a374-6fe1378cc2df`).
- Conversación `Plan De Implementación apps` (`6aaea922-3358-83e9-a374-6fe1378cc2df`).
- Conversación `Conectar carpeta al proyecto` (`6ab19df0-9a98-83e9-bfb9-045c5ae7a916`).

El historial se registra en `HISTORIAL_RECUPERADO.md`; la matriz canónica está en `MATRIZ_REQUISITOS.md` y el alcance, pruebas, UML y riesgos en `ANALISIS_COMPLEMENTARIO.md`.
