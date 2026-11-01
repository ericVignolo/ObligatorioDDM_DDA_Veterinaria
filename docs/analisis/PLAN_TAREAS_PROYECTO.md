# Plan de tareas del proyecto veterinario

Fecha: 2026-10-20 (actualizado 2026-10-29)

Estado: plan de trabajo inicial. Las tareas están pendientes salvo que este documento se actualice con evidencia. Este plan organiza la construcción por ramas; no reemplaza la redacción canónica ni la prioridad de los RF, RN y RNF en `../contexto-base/MATRIZ_REQUISITOS.md`.

## 1. Objetivo y forma de trabajo

Completar la entrega del sistema veterinario mediante tres secciones acumulativas: A, aplicación de escritorio; B, aplicación móvil; C, implementación e integración de la API y la persistencia. El recorrido funcional completo incluye visitantes, Clientes, Recepción, Veterinarios y Administración, con los requisitos Must de la matriz y los Should asignados a la entrega base.

Los mockups, navegaciones y respuestas simuladas de A/B permiten validar experiencia y presentación. Solo al completar C se podrán acreditar para los RF correspondientes la persistencia, autenticación, autorización y reglas críticas ejecutadas por la API.

## 2. Ramas previstas

| Rama | Contenido acumulado | Criterio para avanzar |
|---|---|---|
| `main` (existe) | Base estable del repositorio y documentación aprobada. | Cambios integrados y revisados; no se considera una rama de trabajo de producto. |
| `desktop` (existe) | Aplicación de escritorio Blazor/C# de la sección A, inicialmente con datos de demostración. | Recorridos de escritorio aprobados en presentación y estados; sin afirmar persistencia o seguridad de servidor. |
| `desktop-mobile` (se crea desde `desktop`) | Sección A más aplicación React Native y diseño móvil Figma de la sección B. | Recorridos de ambos clientes demostrables con adaptadores temporales y contratos acordados. |
| `desktop-mobile-api` (se crea desde `desktop-mobile`) | Secciones A y B conectadas a la API ASP.NET Core y MySQL de la sección C. | API única, autorización y reglas aplicadas en servidor; pruebas, seguridad y evidencia de entrega completas. |

Flujo acordado (G-011, Diego 2026-10-29): cada rama de sección sale de la anterior cuando esta se aprueba, y al aprobarse se mezcla a `main` por PR. El trabajo diario va en ramas cortas `<nombre>/<tarea>` con PR contra la rama de la sección activa; cada PR lo revisa el otro integrante. `feature/escritorio-api` queda congelada: ya está contenida en `desktop`. Las ramas representan estados del producto, no arquitecturas distintas (ver DT-ORG-02 en `CONTEXTO_BASE.md`).

## 3. Estados de tareas

- `[ ]` Pendiente.
- `[~]` En curso, con responsable y evidencia anotados en la tarea o en el historial del equipo.
- `[x]` Terminada con evidencia verificable y revisión; el documento de planificación por sí solo no completa una tarea.
- `[!]` Bloqueada por decisión, dependencia o acceso que debe registrarse.

Cada tarea que implemente comportamiento debe enlazarse a sus RF/RN/RNF, caso de uso, flujo/pseudocódigo, pantalla, contrato y evidencia de prueba. Los parámetros clínicos se validan con profesionales; no se inventan para llenar el plan.

## 4. Tareas globales — H0 y coordinación

Desde DT-ORG-03, la puerta H0 se aplica **por recorrido**: un recorrido se programa cuando sus decisiones están cerradas. G-002, G-003 y G-011 ya permiten empezar el primer recorrido.

- [ ] **G-001 — Confirmar alcance de la entrega.** Repasar todos los RF de la matriz, su prioridad y ubicación H0–H5; registrar extensiones separadamente sin degradar los Must.
- [x] **G-002 — Cerrar especificación de registro.** Cerrada por Diego el 2026-10-29 (`ESPECIFICACION_REGISTRO_CLIENTE.md`). Resolver finalidad, acceso, conservación y destino de cada campo; formato de documento/dirección/teléfono; vigencia y reintentos del código; recuperación; documentos/versiones y tiempo de reanudación de intención.
- [x] **G-003 — Cerrar el recorrido Reservar turno → Realizar consulta.** Cerrada por Diego el 2026-10-29 (`FLUJO_RESERVA_CONSULTA.md`, sección 11). Resolver P-RC-01 a P-RC-08, incluyendo colisiones, identidad, permisos, estados, consumo, cierre e idempotencia conforme a `FLUJO_RESERVA_CONSULTA.md`.
- [ ] **G-004 — Completar casos de uso pendientes.** Detallar Cobrar consulta, Internar animal, alertas sanitarias, permisos de Recepción/Veterinario y los casos resumidos que se incorporen a la entrega.
- [ ] **G-005 — Resolver decisiones de asistencia.** Definir parámetros operativos pendientes: cuestionarios profesionales por especie/categoría, tiempos, guardia, cobertura, costos, cancelación, reevaluaciones posteriores y conservación de ubicación.
- [ ] **G-006 — Completar pseudocódigos.** Especificar comportamiento normal, alternativas, errores, permisos, idempotencia y reintentos para registro, agenda, consulta, cobro, vacunas, internación, consentimiento, derechos y asistencia.
- [ ] **G-007 — Completar mapa funcional de canales.** Validar qué acciones corresponden a Blazor, móvil o ambos; definir navegación pública, reanudación de acciones protegidas y experiencias Cliente/Veterinario. Recepción y Administración permanecen previstas en Blazor salvo decisión documentada.
- [ ] **G-008 — Revisar dominio y datos.** Revisar entidades/relaciones y DER MySQL como propuesta; validar historia, múltiples responsables, especies/grupos, turnos, consultas, vacunas, internación, inventario, cargos, pagos, consentimiento, alertas y auditoría. No crear migraciones hasta la sección C.
- [ ] **G-009 — Definir catálogo y datos de demostración.** Preparar datos sintéticos reproducibles, incluyendo casos de caballo y oveja, 30 Clientes, 50 animales y 200 consultas históricas para el conjunto completo previsto. Confirmar servicios, profesionales, horarios y tarifas ficticios.
- [ ] **G-010 — Trazar criterios de aceptación.** Asociar cada RF Must y RN/RNF aplicable a escenarios positivos, negativos, concurrentes y de recuperación. Mantener separados requisitos de prototipo y requisitos cuya conformidad depende de la API.
- [x] **G-011 — Acordar flujo de ramas.** Cerrada por Diego el 2026-10-29 (sección 2 y DT-ORG-02). Revisar ramas existentes, confirmar nombres propuestos, base de cada rama, reglas de revisión y cuándo integrar de nuevo a `main`.
- [~] **G-012 — Puerta de salida H0.** Cumplida para el primer recorrido (DT-ORG-03); pendiente para cobro, internación, alertas sanitarias y asistencia. Verificar que la especificación de registro esté completa, se haya definido el pseudocódigo restante y todas las decisiones abiertas estén documentadas con categoría, responsable, impacto y estado. Resolver antes del diseño final o código de producto cualquier pendiente que pueda cambiar comportamiento, reglas, permisos o alcance; registrar responsables y fecha real cuando exista.

## 5. Sección A — Aplicación de escritorio

**Rama de destino:** `desktop`. Interfaz Blazor/C#. Para recorridos que dependen de identidad, datos o reglas se usarán adaptadores temporales claramente identificados hasta C.

### A.1 Fundación y páginas públicas

- [ ] **A-001 — Preparar estructura de escritorio.** Crear el cascarón Blazor, navegación, estilos base y configuración local de demostración, conforme al H0 aprobado.
- [ ] **A-002 — Crear página de inicio.** Presentar identidad de la clínica, navegación pública, servicios destacados, contacto, ubicación y acceso visible a urgencias/políticas; probar tamaños de computadora acordados.
- [ ] **A-003 — Crear catálogo de servicios.** Mostrar servicios configurables con descripción y requisitos; incluir datos ficticios de revisión clínica, vacunación y desparasitación distinguidas, y rehabilitación equina solo como servicio condicional.
- [ ] **A-004 — Crear páginas públicas de profesionales y clínica.** Mostrar únicamente datos aprobados para publicación, horarios informativos, vías de contacto, ubicación, cobertura y políticas.
- [ ] **A-005 — Definir acceso a urgencias y políticas.** Presentar orientación clara, niveles informativos y enlaces públicos; no simular una garantía de atención ni sustituir un diagnóstico.

### A.2 Identidad y perfil

- [ ] **A-006 — Construir registro como Cliente.** Implementar el formulario visual desde la especificación aprobada, aceptaciones separadas y validaciones de presentación; el prototipo no crea credenciales reales.
- [ ] **A-007 — Construir verificación y recuperación demostrativas.** Mostrar estados y mensajes de código, expiración, reenvío y recuperación definidos en H0; indicar que son simulaciones mientras no exista API.
- [ ] **A-008 — Construir inicio y cierre de sesión demostrativos.** Crear rutas públicas/protegidas de presentación y estados de sesión simulados sin afirmar autorización real.
- [ ] **A-009 — Crear perfil del Cliente.** Mostrar y editar solo datos y preferencias definidos, con estados de guardado/error demostrativos y privacidad por campo.
- [ ] **A-010 — Crear perfiles de personal en el panel.** Preparar vistas de roles para Recepción, Veterinario y Administrador; separar permisos esperados de controles visuales simulados.
- [ ] **A-011 — Preparar recuperación de acción protegida.** Desde catálogo o agenda, guardar intención no sensible y retomarla después del acceso simulado; al retomarla volver a revisar disponibilidad en los datos de demostración.

### A.3 Animales y agenda

- [ ] **A-012 — Crear listado y ficha de animales.** Mostrar especie/categoría, identificadores permitidos, responsables, antecedentes y acciones; admitir animales de campo y datos de grupo donde corresponda.
- [ ] **A-013 — Crear alta y edición de animal.** Representar campos, validaciones y estados acordados; incluir ejemplos sintéticos de caballo y oveja.
- [ ] **A-014 — Crear disponibilidad orientativa.** Mostrar servicios, profesional, recursos, fecha y horarios ficticios; marcar la disponibilidad como demostrativa hasta que la API la confirme.
- [ ] **A-015 — Crear reserva en cuatro pasos.** Animal; tipo/profesional; fecha/hora; revisión y confirmación. Conservar errores de cupo y estados descritos en H0.
- [ ] **A-016 — Crear turnos, cancelación y reprogramación.** Listar próximos/históricos y presentar consecuencias/confirmación según políticas ya definidas; no conservar cupos reales durante la demostración.
- [ ] **A-017 — Crear agenda de Recepción y Veterinario.** Mostrar filtros, capacidad, estados y detalle de turno; permitir simular llegada/espera en el prototipo.
- [ ] **A-018 — Diseñar estados de agenda.** Unificar etiquetas, permisos esperados, errores, vacío, carga, éxito y colisión en pantallas y documentación.

### A.4 Atención clínica y módulos de operación

- [ ] **A-019 — Crear pantalla de consulta veterinaria.** Capturar anamnesis, evaluación, diagnóstico, tratamiento, alergias, adjuntos e indicaciones definidos; mantenerlo como borrador visual hasta API.
- [ ] **A-020 — Mostrar historia clínica y firma.** Diferenciar borrador, firmado y enmienda; impedir edición visual del original firmado en el modelo de interacción.
- [ ] **A-021 — Crear vistas de vacunación y alertas sanitarias.** Mostrar lote, fechas, intervalo, refuerzo y alertas según la especificación profesional; incluir estados de vacuna vencida y no aplicable.
- [ ] **A-022 — Crear vistas de inventario.** Productos, lotes, vencimientos, movimientos, stock mínimo, ajustes y consumo; dejar claro que no hay saldos persistentes en modo demo.
- [ ] **A-023 — Crear cargos, cobro y comprobante interno.** Separar cargo y pago; soportar medios acordados para el ejemplo, sin pasarela online mientras no se apruebe su cambio de alcance.
- [ ] **A-024 — Crear gestión de internación.** Ingreso, box, plan, evolución, novedades internas/visibles y alta; representar la exclusividad del box y la protección de notas.
- [ ] **A-025 — Crear consentimiento informado.** Mostrar versión, procedimiento, paciente, lectura/aceptación/firma según mecanismo aprobado; mantener separado de términos y privacidad.
- [ ] **A-026 — Crear panel de asistencia para Recepción/Veterinario.** Cola por prioridad, elegibilidad, ofertas/asignación, escalamiento y estados; los tiempos y las competencias usan parámetros aprobados.
- [ ] **A-027 — Crear vistas de configuración y administración.** Catálogo, horarios, recursos, tarifas, roles, parámetros, competencias, cobertura, auditoría e indicadores según RF.
- [ ] **A-028 — Crear comunicaciones y derechos.** Resumen/correo y su estado, alertas sanitarias/operativas, preferencias, exportación/eliminación y conservación con controles visibles.

### A.5 Cierre de escritorio

- [ ] **A-029 — Aplicar accesibilidad y adaptación de escritorio.** Teclado, foco, contraste, etiquetas, mensajes y puntos de corte aplicables; documentar limitaciones del prototipo.
- [ ] **A-030 — Integrar recorridos demostrativos de escritorio.** Preparar datos sintéticos y recorridos de Visitante, Cliente, Recepción, Veterinario y Administrador con resultados repetibles y marca de simulación.
- [ ] **A-031 — Revisar sección A y preparar integración.** Revisar requisitos asignados, estados, navegación, errores, requisitos de privacidad y evidencia; no marcar backend/seguridad como completos.

## 6. Sección B — Aplicación móvil

**Rama de destino:** `desktop-mobile`, acumulada desde la entrega aprobada de A. React Native con TypeScript y Expo según compatibilidad confirmada. Figma precede la implementación visual definitiva. Sin API, usar adaptadores y datos sintéticos; no incorporar modo offline si no se aprueba como alcance.

- [ ] **B-001 — Cerrar mapa móvil por rol.** Definir navegación Cliente y Veterinario, acceso público y reanudación de acciones; determinar qué flujos de Recepción siguen en escritorio.
- [ ] **B-002 — Completar flujos y wireframes en Figma.** Cubrir estados normales, protegidos, errores, alertas, carga/vacío y consentimiento antes de alta fidelidad.
- [ ] **B-003 — Construir sistema visual móvil.** Aplicar dirección acordada, tipografía, componentes, accesibilidad, estados y responsive táctil; revisar en Figma con el equipo.
- [ ] **B-004 — Preparar base React Native/Expo.** Navegación, ambientes, componentes compartidos, manejo de errores y configuración de adaptadores demostrativos.
- [ ] **B-005 — Implementar área pública y servicios móviles.** Inicio, catálogo, información de clínica/profesionales, políticas y contacto.
- [ ] **B-006 — Implementar registro, sesión y perfil móvil.** Formularios y estados de verificación/recuperación según H0; tokens y autorización reales quedan para C.
- [ ] **B-007 — Implementar animales del Cliente.** Listado, fichas, altas, edición, identificadores permitidos e historia autorizada en modo de presentación.
- [ ] **B-008 — Implementar reserva y turnos.** Consulta de horarios ficticios, flujo de cuatro pasos, próximos turnos, historial, cancelación/reprogramación y estados.
- [ ] **B-009 — Implementar historia clínica móvil.** Lectura autorizada simulada, adjuntos con enlaces de demostración no sensibles y representación de firma/enmienda.
- [ ] **B-010 — Implementar vacunas y alertas.** Carnet/fechas/recordatorios conforme a reglas aprobadas y estados sin datos, vigentes y vencidos.
- [ ] **B-011 — Implementar internación del Cliente.** Estado y novedades visibles; notas internas no se muestran.
- [ ] **B-012 — Implementar consentimiento y preferencias.** Documentos/versiones, decisiones obligatorias y opcionales, privacidad y accesibilidad.
- [ ] **B-013 — Implementar solicitud móvil de asistencia.** Animal/grupo/especie, modalidad, síntomas y gravedad percibida; cuestionarios solo tras aprobación profesional.
- [ ] **B-014 — Implementar seguimiento e informes de cambio.** Estado, empeoramiento, reevaluación, ubicación consentida, ausencia de ubicación/datos y mapa como experiencia demostrativa.
- [ ] **B-015 — Implementar agenda móvil del Veterinario.** Turnos/asistencias, detalle de paciente autorizado, aceptación/rechazo y estados de traslado según permisos.
- [ ] **B-016 — Implementar captura clínica móvil del Veterinario.** Registro/consulta, vacunación, internación o asistencia según el mapa; mostrar límites de borrador si no hay API.
- [ ] **B-017 — Implementar notificaciones y perfil del Veterinario.** Estados, preferencias permitidas, perfil y manejo de permisos del dispositivo.
- [ ] **B-018 — Adaptar accesibilidad, tamaños y privacidad.** Teclado/lector de pantalla, controles táctiles, sesión, exposición de información en notificaciones y datos locales.
- [ ] **B-019 — Integrar recorridos móviles demostrativos.** Cliente y Veterinario con fixtures repetibles; documentar cada respuesta simulada y conservar alternativa visual ante errores.
- [ ] **B-020 — Revisar sección B y preparar integración.** Confirmar paridad funcional requerida con escritorio, estados y contratos previstos; no declarar seguridad ni persistencia reales.

## 7. Sección C — API, persistencia e integración

**Rama de destino:** `desktop-mobile-api`, acumulada desde A y B aprobadas. Implementar una API ASP.NET Core compartida; configurar MySQL cuando estén confirmados versión, proveedor compatible y usuario exclusivo. Los contratos son explícitos y versionados; los clientes no consumen entidades de persistencia.

### C.1 Fundación, seguridad y datos

- [ ] **C-001 — Crear solución .NET y capas.** API, Domain, Application, Infrastructure, Contracts y Tests con dependencias controladas.
- [ ] **C-002 — Confirmar MySQL y acceso.** Verificar versión/proveedor EF Core compatible, usuario exclusivo de API, configuración por ambiente, secretos y alojamiento objetivo.
- [ ] **C-003 — Definir persistencia y migraciones.** DER aprobado, restricciones, índices, transacciones, migraciones revisables y datos sintéticos; no almacenar credenciales o datos sensibles en claro.
- [ ] **C-004 — Publicar contratos versionados.** Endpoints/DTO OpenAPI, manejo de errores, paginación, formatos, idempotencia y compatibilidad de clientes.
- [ ] **C-005 — Implementar autenticación y cuentas.** Registro, correo único, verificación, recuperación, renovación/cierre de sesión y mitigación de abuso sin revelar cuentas.
- [ ] **C-006 — Implementar autorización por rol/relación.** Visitante, Cliente, Recepción, Veterinario y Administrador; permisos sobre animales, consultas, turnos y operaciones.
- [ ] **C-007 — Implementar privacidad, consentimientos y auditoría de identidad.** Versionado de políticas, finalidad, evidencia, accesos a historia y auditoría sin secretos ni datos excesivos.
- [ ] **C-008 — Preparar cuentas y datos sintéticos.** Roles de demostración, catálogo, profesionales, horarios, animales de campo y relaciones autorizadas reproducibles.

### C.2 Catálogo, pacientes y agenda

- [ ] **C-009 — Implementar catálogo y datos maestros.** Especies/categorías, razas cuando aplican, servicios, tipos de atención, profesionales, recursos, horarios y políticas.
- [ ] **C-010 — Implementar Clientes, responsables y animales.** Validaciones de identidad/relación, microchip único, varios responsables, vigencias y soporte de animales/grupos conforme al dominio.
- [ ] **C-011 — Implementar disponibilidad y agenda.** Reglas de recursos/horarios, estrategia concurrente para intervalos, transacciones y respuestas comprensibles a colisiones.
- [ ] **C-012 — Implementar ciclo de turnos.** Crear, confirmar, cancelar, reprogramar, llegada, espera, ausencias, historial, permisos e idempotencia.

### C.3 Atención, inventario y cobros

- [ ] **C-013 — Implementar consulta e historia clínica.** Borrador, cierre/firmado, diagnóstico, tratamiento, alergias y validación de competencia/habilitación.
- [ ] **C-014 — Implementar enmiendas y auditoría clínica.** Original inmutable, nueva enmienda enlazada, autor/fecha/motivo y lectura autorizada.
- [ ] **C-015 — Implementar archivos clínicos protegidos.** Carga/validación, almacenamiento configurado, escaneo según política, permisos y enlaces temporales.
- [ ] **C-016 — Implementar productos, lotes y movimientos.** Compras, ajustes, bajas, FEFO, stock mínimo, no negatividad, vencimiento y trazabilidad.
- [ ] **C-017 — Implementar reservas y consumo de stock.** Validación/consolidación atómica, prevención de doble descuento y conciliación de reservas/vencimientos.
- [ ] **C-018 — Implementar vacunación y alertas sanitarias.** Producto/lote, vencimiento, intervalo/refuerzo, alertas y reglas profesionales parametrizadas.
- [ ] **C-019 — Implementar cargos y tarifas históricas.** Cargos por prestaciones e insumos, tarifas vigentes al momento correspondiente, estados y vínculo con atención.
- [ ] **C-020 — Implementar cobros de Recepción.** Medios acordados, estado del cobro, comprobante interno, reversiones trazables y separación entre cargo y pago.
- [ ] **C-021 — Resolver alcance de pasarela de pago.** Si Eric aprueba la extensión, actualizar matriz/alcance/flujo antes de programar; seleccionar checkout, credenciales TEST, verificación proveedor e idempotencia. Si no se aprueba, cerrar la tarea dejando solo el flujo interno.
- [ ] **C-022 — Cerrar atención en una transacción.** Firma, movimientos, cargos, turno, alertas, auditoría y mensaje pendiente con reversión coherente ante fallo.
- [ ] **C-023 — Implementar correo y reintentos.** Encolar junto al cierre y enviar posteriormente con idempotencia, estado visible y reintentos sin deshacer la atención.

### C.4 Internación, asistencia y administración

- [ ] **C-024 — Implementar consentimiento clínico.** Documento/versión, responsable, animal, procedimiento, firma/mecanismo aprobado, vigencia y bloqueo de procedimiento sin autorización.
- [ ] **C-025 — Implementar internación.** Box exclusivo, ingreso, plan, evolución, novedades internas/públicas, alta e indicaciones; tiempo real según RF aplicables.
- [ ] **C-026 — Implementar solicitud y clasificación de asistencia.** Idempotencia, animal/grupo, cuestionario aprobado, clasificación preliminar, prioridad y registro de eventos.
- [ ] **C-027 — Implementar asignación, empeoramiento y reevaluación.** Elegibilidad por especie/equipamiento/disponibilidad, guardia, cola, escalamiento, 24 horas y límites de automatización.
- [ ] **C-028 — Implementar ubicación, mapa y estimación.** Consentimientos, GPS/manual, acceso mínimo, estimación tras aceptación, seguimiento y cese; no inventar cobertura ni disponibilidad.
- [ ] **C-029 — Implementar costos y cargos de asistencia.** Informar tarifas/criterio y preservar creación de solicitud roja ante deuda o fallo de pago.
- [ ] **C-030 — Implementar configuración administrativa.** Roles, 2FA obligatorio para Administrador, horarios, servicios, recursos, tarifas, competencias, cobertura y parámetros.
- [ ] **C-031 — Implementar indicadores, derechos y exportación.** Métricas autorizadas, solicitudes de datos, exportación/eliminación según conservación y auditoría.

### C.5 Conectar clientes y cerrar entrega

- [ ] **C-032 — Conectar Blazor a contratos API.** Reemplazar fixtures/adaptadores; implementar carga, vacío, error, reintento, validación y sesión por rol.
- [ ] **C-033 — Conectar React Native a la misma API.** Cliente HTTP tipado, almacenamiento seguro, renovación/cierre de sesión, permisos del dispositivo y errores recuperables.
- [ ] **C-034 — Retirar simulaciones que se presenten como reales.** Identificar los datos de demo que sigan disponibles y asegurar que producción/despliegue de defensa no confunda mocks con servidor.
- [ ] **C-035 — Verificar reglas, autorización y concurrencia.** Ejecutar casos asociados a cada RN/RNF, incluyendo acceso indebido, duplicación, stock y colisiones de agenda.
- [ ] **C-036 — Verificar procesos de error y recuperación.** Correo caído, pérdida de respuesta, transacciones revertidas, datos inválidos, sesión vencida, proveedor externo no disponible y respaldo/restauración.
- [ ] **C-037 — Revisar seguridad, privacidad y calidad.** Secretos, logging, archivos, coordenadas, dependencia/licencias, accesibilidad, rendimiento, configuración y auditoría.
- [ ] **C-038 — Preparar despliegue y restauración.** Ambientes, variables/secretos, migración, respaldos, retención y prueba documentada de restauración.
- [ ] **C-039 — Actualizar documentación técnica y académica.** UML/DER, manuales, instalación, contratos, decisiones, límites, matriz de trazabilidad y catálogo de evidencia.
- [ ] **C-040 — Ejecutar demostración final reproducible.** Recorridos por rol, casos de caballo/oveja, errores controlados, evidencia y límites/extensiones declarados.
- [ ] **C-041 — Revisión final y entrega.** Confirmar RF incluidos, RN/RNF aplicables, defectos bloqueantes, licencias, rama integrada y aprobación del equipo antes de considerar el proyecto completo.

## 8. Dependencias principales

```text
G-001…G-012 (H0)
        ↓
A-001…A-031 (desktop con datos de demostración)
        ↓
B-001…B-020 (desktop + mobile, todavía demostrativo)
        ↓
C-001…C-041 (API única + MySQL + integración de ambos clientes)
        ↓
Revisión de entrega en H5 y merge acordado a main
```

Las tareas de documentación y preparación pueden avanzar en paralelo cuando no dependan de una decisión abierta. Cada recorrido se programa cuando cerró su propia puerta H0 (DT-ORG-03). No crear migraciones ni conectar a MySQL durante A/B. El diseño y las pruebas funcionales de prototipo no reemplazan pruebas de API, autorización, transacciones o persistencia en C.

## 9. Criterio de finalización del proyecto

El proyecto se considera completo cuando todas las tareas comprometidas de A, B y C tienen evidencia revisada; los RF/RN/RNF de la matriz están trazados y verificados según su naturaleza; ambas aplicaciones consumen la misma API; los datos y ejemplos son sintéticos; los respaldos y restauración se han verificado; y la entrega final distingue funciones implementadas, extensiones y limitaciones. La integración a `main` se realiza según el flujo Git acordado por el equipo.
