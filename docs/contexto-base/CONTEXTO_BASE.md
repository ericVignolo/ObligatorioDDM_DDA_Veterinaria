# Contexto base — Sistema de gestión veterinaria

Actualizado: 2026-10-20

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
- La especificación contiene 62 RF, 42 RN y 55 RNF recuperados del documento original, más 12 RF, 17 RN y 10 RNF complementarios incorporados; su texto canónico está en `MATRIZ_REQUISITOS.md`.
- Los requisitos adicionales de privacidad, consentimiento, accesibilidad, terceros, derechos de autor y publicación fueron incorporados desde `Implementacion_Politicas.txt`.
- El plan de implementación visual, las alternativas evaluadas y la decisión inicial de diseño móvil están documentados en `Diseño.md`.
- Se incorporó la primera especificación funcional de alertas y asistencia, con cuatro niveles, prioridad, reevaluación, GPS, asignación, seguimiento, costos y atención de animales de cualquier especie. Permanecen abiertos sus parámetros operativos y la especificación adicional de registro y alertas sanitarias.
- El 2026-10-02 se confirmó un equipo de dos integrantes, con apoyo de Codex. No hay fecha límite real establecida; las diez semanas del plan original son una referencia histórica, no un plazo comprometido. La dedicación efectiva se estimará por hito.
- El frente inmediato acordado es cerrar la tabla de alcance de la entrega y desarrollar “Realizar consulta” conectado con “Reservar turno”, incluyendo estados, permisos, historia clínica, stock y cargos. Después se completarán los restantes casos y el mapa móvil.
- Repositorio indicado por el usuario: https://github.com/ericVignolo/ObligatorioDDM_DDA_Veterinaria (coincide con el remoto `origin` local). Al compartirlo inicialmente como referencia no se había solicitado publicación.
- El 2026-10-09 Eric delegó en Diego las decisiones aún abiertas del proyecto y pidió subir la documentación al repositorio. Las decisiones ya confirmadas por Eric se conservan; Diego registrará las restantes con fecha, justificación e impacto antes de implementar el módulo afectado. Eric y Codex mantendrán y revisarán la documentación.
- El 2026-10-09 Eric pidió separar una rama `desktop` y trabajar primero en la experiencia de escritorio. La API queda para una etapa posterior de integración; la arquitectura final de una única API no cambia. Hasta entonces, cualquier recorrido interactivo de Blazor será un prototipo con datos de demostración, no una implementación productiva de persistencia, permisos o reglas críticas.
- **DT-ORG-02 — Secuencia de trabajo y ramas (2026-10-20):** Eric definió organizar la construcción en cuatro ramas acumulativas: `main` (base estable), `desktop` (aplicación de escritorio), `desktop-mobile` (escritorio más móvil) y `desktop-mobile-api` (ambos clientes integrados con la API). La secuencia será sección A — escritorio, sección B — aplicación móvil, sección C — integración e implementación de la API. La lectura de Git confirmó que `main` y `desktop` existen; `desktop-mobile` y `desktop-mobile-api` todavía no existen y sus nombres son propuestas. Falta acordar la ascendencia/merge de las ramas respecto de `feature/escritorio-api`. Esta secuencia no crea arquitecturas separadas: al final habrá una única API ASP.NET Core compartida por Blazor y React Native. Los clientes A/B usarán datos ficticios y adaptadores temporales; antes de C no se declararán persistencia real, autenticación/autorización garantizadas ni reglas críticas implementadas. La API se conecta a MySQL y reemplaza los adaptadores simulados durante C. El plan detallado está en `docs/analisis/PLAN_TAREAS_PROYECTO.md`.
- **DT-BD-01 — Decisión técnica (2026-10-09):** Eric confirmó **MySQL** como motor de la futura base de datos. Esta elección no adelanta la persistencia durante la etapa `desktop`: Blazor continuará sin acceso directo a MySQL y, cuando exista, se conectará a través de la API. Quedan pendientes la versión del motor, proveedor de acceso y la estrategia concreta para impedir solapamientos de agenda.

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

### Organización física del repositorio y ramas (actualizado 2026-10-09)

Se conserva la arquitectura de una única API y dos clientes. La solución .NET podrá estar en la raíz del repositorio y agrupar proyectos ubicados en carpetas distintas:

```text
ObligatorioVeterinaria/
├── api/          API ASP.NET Core, Application, Domain, Infrastructure y Contracts
├── desktop/      interfaz Blazor/C# para computadora
├── mobile/       aplicación React Native (cuando se inicie el frente móvil)
├── tests/        pruebas de la API, el dominio y la integración
└── docs/         análisis, decisiones, diseño y evidencia
```

`feature/escritorio-api` conserva la preparación y documentación anteriores. La sección A continúa en `desktop`; la sección B agrega React Native en `desktop-mobile`; la sección C integra API y persistencia en `desktop-mobile-api`, según DT-ORG-02 y `docs/analisis/PLAN_TAREAS_PROYECTO.md`. En A/B se permiten sustitutos de datos explícitamente temporales. No se introduce acceso directo a MySQL ni se acreditan por simulación autenticación, autorización, persistencia, transacciones o reglas críticas. Las ramas organizan el trabajo; no representan arquitecturas finales distintas.

La API se implementará al llegar a C y será compartida por Blazor y React Native. Un flujo simulado puede demostrar UX, pero no satisface por sí solo los RF/RN/RNF que requieren datos reales, seguridad o concurrencia. Antes de diseño visual definitivo o código de producto sigue vigente el cierre de especificaciones y decisiones indicado en AGENTS.md.

La creación de carpetas y rama es una decisión técnica de organización y no prueba todavía ningún RF. Antes de considerar un requisito implementado se exigirá código funcional y pruebas pertinentes. Siguen pendientes la especificación de registro, el pseudocódigo restante y las decisiones abiertas antes del diseño visual definitivo y del código de producto.

El documento original desarrolla estrategias específicas para SQL Server y las conversaciones usan Entity Framework Core como base del plan técnico. La elección posterior de **MySQL** por Eric reemplaza la alternativa de SQL Server; cualquier técnica específica del motor se debe revisar antes de aplicarla. La versión, el proveedor de acceso, índices/aislamiento y la exclusión de intervalos variables se formalizarán antes de implementar agenda real. Las reglas de integridad y concurrencia no dependen de la interfaz elegida.

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
- Ejemplos confirmados de acciones protegidas: gestionar/agendar para un animal, reservar servicios y crear o seguir una solicitud de asistencia.
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

En la documentación histórica, “mascota” identifica al paciente asociado a un responsable. El alcance vigente no se limita a animales domésticos: incluye animales de granja, producción, equinos, aves, reptiles y demás especies configuradas, así como solicitudes que involucren grupos de animales.

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
- Consultas firmadas e inmutables según RN-12, RN-13, RN-14 y RN-20; correcciones mediante enmiendas.

### Internación

- Consulta y gestión de internaciones según rol.

### Inventario

- Gestión de inventario.
- Stock no negativo, FEFO, vencimientos, mínimos y movimientos trazables según RN-26 a RN-31.

### Comunicaciones

- Notificaciones dentro del flujo principal.
- Notificaciones push como capacidad móvil prevista.

### Alertas y asistencia veterinaria

- Solicitud de visita al lugar u orientación para trasladar uno o varios animales a la clínica.
- Clasificación preliminar combinada en cuatro niveles: rojo, naranja, amarillo y verde.
- Prioridad `rojo → naranja → amarillo → verde`; al día siguiente, naranja precede a amarillo.
- Aviso de empeoramiento iniciado por el cliente en cualquier momento y reevaluación solicitada a las 24 horas si continúa pendiente.
- Gestión ordinaria por Recepción y circuito inmediato de profesionales elegibles o de guardia para nivel rojo.
- Ubicación GPS consentida o dirección manual, estados, notificaciones, mapa y tiempo estimado después de la aceptación profesional.
- Costo informado sin bloquear una solicitud roja por deuda previa o falla de pago.
- Especificación detallada y pseudocódigo en `../analisis/ESPECIFICACION_ALERTAS_ASISTENCIA.md`.

### Facturación y administración

- Cargos, tarifas históricas y comprobantes según RN-32 a RN-36. Se distingue el cargo generado al cerrar la atención del cobro posterior; falta completar el caso de uso de cobro.
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
│   ├── Asistencia
│   │   ├── Nueva solicitud
│   │   ├── Solicitudes activas
│   │   ├── Estado, notificaciones y mapa
│   │   └── Informar empeoramiento
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
    ├── Solicitudes de asistencia
    │   ├── Alertas rojas elegibles
    │   ├── Aceptar o rechazar
    │   └── Traslado y estados
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

- 74 requerimientos funcionales: 10 de Visitante, 25 de Cliente, 17 de Veterinario, 9 de Recepcionista y 13 de Administrador.
- 59 reglas de negocio clasificadas como bloqueantes o de advertencia.
- 65 requerimientos no funcionales de seguridad, concurrencia, base de datos, rendimiento, disponibilidad, usabilidad, mantenibilidad y legalidad.
- Los complementos abarcan privacidad, consentimiento, seguimiento, accesibilidad, licencias, revisión normativa y alertas de asistencia.

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

## 11. Plan de construcción por secciones A, B y C

La secuencia de ramas del 2026-10-20 sustituye el orden de implementación de los planes A/B anteriores, sin cambiar la arquitectura final:

1. **Sección A — escritorio:** primero Blazor con inicio, catálogo de servicios, información pública y, después, identidad/perfiles y los recorridos de Cliente, Recepción, Veterinario y Administración. Los adaptadores usan datos sintéticos; no prueban persistencia ni reglas de servidor.
2. **Sección B — móvil:** diseñar en Figma y construir React Native con experiencias Cliente/Veterinario sobre las decisiones y contratos funcionales documentados. Hasta C, cualquier respuesta de datos permanece demostrativa.
3. **Sección C — API e integración:** implementar la API ASP.NET Core, capas de dominio/aplicación/infraestructura/contratos, persistencia MySQL, autenticación/autorización, reglas críticas y conexión de ambos clientes a la misma API.

La secuencia no permite iniciar código de producto antes de cerrar la puerta H0. El inventario completo de tareas está en `../analisis/PLAN_TAREAS_PROYECTO.md`; H0–H5 sigue siendo la planificación funcional de `../analisis/ALCANCE_ENTREGA.md`. La decisión visual inicial móvil de `Diseño.md` se aplica después de validar mapa, flujos, estados, pseudocódigo y requisitos.

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

El orden de trabajo vigente se organiza por H0–H5 en `../analisis/ALCANCE_ENTREGA.md`; estos hitos no renumeran la columna Fase de la matriz original. El primer paquete funcional es `../analisis/FLUJO_RESERVA_CONSULTA.md`. Sus decisiones derivadas de estados, permisos, reservas de stock y cierre atómico son propuestas explícitas de comportamiento/técnica, sujetas a las preguntas acotadas del propio documento. No modifican la clasificación ni la redacción de los requisitos canónicos.

1. Completar la especificación de registro, alertas sanitarias y los parámetros pendientes del módulo de asistencia; privacidad y consentimientos ya fueron incorporados desde `Implementacion_Politicas.txt`.
2. Conservar DT-BD-01: MySQL está confirmado. Antes de C faltan versión/proveedor compatibles y estrategia concreta para impedir solapamientos de intervalos variables.
3. Construir y validar los mapas funcionales de Visitante, Cliente, Veterinario y Recepción para A/B, respetando los RF de la matriz.
4. Completar el pseudocódigo restante, estados, errores, consentimientos y reanudación de acciones protegidas antes del diseño visual definitivo; asistencia ya cuenta con una primera especificación.
5. Preparar trazabilidad entre RF, RN, RNF, pantallas, endpoints y casos de prueba.
6. Actualizar UML y justificación de patrones para Blazor/C# + API .NET + React Native + Figma.
7. Seguir el inventario numerado A/B/C de `../analisis/PLAN_TAREAS_PROYECTO.md`, que organiza las ramas y conserva los hitos funcionales H0–H5.

## 14.1 Artefactos funcionales derivados

Se incorporaron como borradores para validación los primeros artefactos funcionales de Visitante y Cliente:

- `docs/analisis/HISTORIAS_USUARIO_VISITANTE_CLIENTE.md`: backlog con 35 historias, criterios de aceptación y trazabilidad.
- `docs/analisis/CASOS_DE_USO_VISITANTE_CLIENTE.md`: catálogo de 25 casos de uso y especificación detallada de 11 recorridos esenciales.
- `docs/analisis/ESPECIFICACION_ALERTAS_ASISTENCIA.md`: clasificación, estados, flujos, pseudocódigo, privacidad, excepciones y pruebas del nuevo módulo.
- `docs/analisis/ALCANCE_ENTREGA.md`: tabla operativa de entrega base y extensiones, asignación de los 74 RF a hitos, límites y evidencia esperada.
- `docs/analisis/FLUJO_RESERVA_CONSULTA.md`: detalle conectado de CU-CLI-02 y CU-VET-01 (Realizar consulta), estados, permisos, datos, pseudocódigo y criterios de aceptación.
- `docs/analisis/HISTORIAS_USUARIO_ATENCION.md`: historias derivadas del recorrido de atención, sin agregar RF a la matriz.
- `docs/analisis/ESPECIFICACION_CATALOGO_SERVICIOS_API.md`: propuesta acotada del primer contrato público API → Blazor para RF-VIS-01, con decisiones D-CAT-01 a 03 y pruebas.
- `docs/analisis/DECISIONES_PARA_DESARROLLO.md`: orden de preguntas que desbloquean catálogo, registro, reserva y consulta para el trabajo con Diego.
- `docs/analisis/ESPECIFICACION_REGISTRO_CLIENTE.md`: borrador de flujo, pseudocódigo, preguntas y pruebas del registro con reanudación de intención protegida.
- `docs/analisis/RECORRIDO_API_RESERVA_CONSULTA.md`: mapa propuesto de operaciones API desde reservar turno hasta firmar consulta y dejar cargo/correo pendientes.
- `docs/diagramas/INICIO_ESCRITORIO.md`: primer flujo de catálogo/acceso/reserva y árbol binario de decisiones para el siguiente paso, con límites claros de la simulación sin API.

Estos documentos no sustituyen la matriz canónica. Permanecen en estado de borrador hasta resolver los pendientes de registro, parámetros abiertos, pseudocódigo restante, alcance y casos resumidos.

La tabla de alcance del 2026-10-02 fija una base operativa para planificar por hitos; no declara aprobados por el profesor los detalles nuevos ni terminada la Fase 1. Conserva todos los RF Must y las RN/RNF aplicables. Postergar una obligación vigente requiere una revisión explícita de contexto y matriz; la devolución docente aporta criterios, no elimina requisitos automáticamente.

## 15. Fuentes de esta recuperación

- Proyecto ChatGPT: `ObligatorioDDM_DDA` (`g-p-6aacb1aef7648191ac475284acde7878`).
- Documento fuente: `docs/fuentes/Analisis_Clinica_Veterinaria.docx`, versión 1.0, setiembre de 2026.
- Conversación `Plan de implementación` (`6aaea422-6950-83e9-a374-6fe1378cc2df`).
- Conversación `Plan De Implementación apps` (`6aaea922-3358-83e9-a374-6fe1378cc2df`).
- Conversación `Conectar carpeta al proyecto` (`6ab19df0-9a98-83e9-bfb9-045c5ae7a916`).

El historial se registra en `HISTORIAL_RECUPERADO.md`; la matriz canónica está en `MATRIZ_REQUISITOS.md` y el alcance, pruebas, UML y riesgos en `ANALISIS_COMPLEMENTARIO.md`.
