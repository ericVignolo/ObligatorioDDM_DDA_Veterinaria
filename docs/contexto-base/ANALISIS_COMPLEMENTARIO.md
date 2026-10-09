# Alcance plan y criterios de defensa

Fuente: `docs/fuentes/Analisis_Clinica_Veterinaria.docx`, versión 1.0, setiembre de 2026.

Actualizado: 2026-09-26 con el alcance confirmado de alertas y asistencia veterinaria.

## Supuestos de trabajo

- Duración estimada: 10 semanas.
- Equipo: 2 personas con dedicación parcial.
- Clínica de referencia: una sucursal, entre 2 y 4 veterinarios, recepción, internación, quirófano y estética.
- El modelo de datos debe admitir múltiples sucursales aunque la primera implementación use una.
- La solución incluye una interfaz responsiva para la clínica y una aplicación móvil para clientes y veterinarios.

## Alcance funcional

Dentro del alcance:

- Gestión de clientes, responsables, mascotas e historia clínica.
- Agenda con recursos físicos y duraciones variables.
- Internación con seguimiento visible para el responsable.
- Inventario por lote, vencimiento y alertas.
- Notificaciones por correo y push.
- Alertas de orientación o asistencia para uno o varios animales de cualquier especie, con cuatro niveles de prioridad, ubicación consentida, asignación profesional y seguimiento del traslado.

Fuera del alcance declarado:

- Contabilidad, liquidación de sueldos y libros fiscales.
- Facturación electrónica homologada ante DGI.
- Integración directa con equipos de laboratorio o imagenología.
- Comercio electrónico con logística de envíos.
- Historia clínica interoperable entre clínicas.

La facturación interna, la cuenta corriente y los pagos en línea sí aparecen como funcionalidad propuesta. Lo excluido es la homologación fiscal ante DGI.

## Actores

| Actor | Autenticación | Responsabilidad principal |
|---|---|---|
| Visitante | No requiere | Consulta información pública y evalúa los servicios. |
| Cliente | Correo y contraseña; 2FA opcional | Gestiona animales y turnos, consulta historia clínica, sigue internaciones y solicita orientación o asistencia. |
| Veterinario | Credencial institucional y matrícula | Registra actos médicos, recetas y diagnósticos; publica novedades y acepta asistencias compatibles con su competencia y guardia. |
| Recepcionista | Credencial institucional | Gestiona agenda, admisión, cobros, la cola verde/amarilla/naranja y las excepciones que requieren contacto. |
| Administrador | Credencial institucional y 2FA obligatorio | Configura usuarios, inventario, tarifas, cobertura, guardias, competencias y equipamiento; consulta indicadores. |
| Sistema temporal | No aplica | Ejecuta recordatorios, reevaluaciones de 24 horas, alertas y tareas programadas. |

Recepcionista debe permanecer separado de Administrador en permisos y diagramas.

## Plan original en cinco fases

1. **Semanas 1 y 2:** relevamiento, alcance, historias de usuario, MoSCoW, dominio, 3FN, DER, arquitectura, CI, wireframes y diccionario de datos.
2. **Semanas 3 y 4:** modo invitado, identidad, RBAC, mascotas, responsables, datos maestros, auditoría y baja lógica.
3. **Semanas 5 y 6:** agenda, historia clínica, internación, comunicaciones e inventario en un flujo completo.
4. **Semanas 7 y 8:** reglas bloqueantes, concurrencia, transacciones, idempotencia y pruebas adversas.
5. **Semanas 9 y 10:** UML, pruebas formales, manuales, despliegue de demostración y preparación de la defensa.

Este cronograma es la base académica. El orden operativo vigente se divide además en dos frentes: API + Blazor y React Native + Figma, según `CONTEXTO_BASE.md`.

## Ocho funcionalidades propuestas

1. **Consentimiento informado digital:** firma, sello temporal y bloqueo de cirugías o anestesia sin consentimiento.
2. **Receta digital verificable:** matrícula, identificador único, QR, relación con inventario y validaciones clínicas.
3. **Plan sanitario preventivo:** calendario automático por especie, edad y peso, recalculado con cada aplicación.
4. **Seguimiento postoperatorio:** mensajería temporal con fotos, archivada en la historia clínica.
5. **Facturación, cuenta corriente y pago en línea:** tarifas históricas, comprobantes, pagos parciales, señas y planes.
6. **Tablero administrativo:** ocupación, ausencias, ingresos, rentabilidad y rotación o pérdida de inventario.
7. **Modo sin conexión para Veterinario:** agenda y pacientes del día, registro local y sincronización con política de conflictos.
8. **QR y ficha pública de emergencia:** datos mínimos, alergias críticas, contacto protegido y modo extraviada.

Para un equipo de dos personas en diez semanas, el documento prioriza consentimiento, receta y facturación. Seguimiento postoperatorio, tablero, modo sin conexión y QR pueden quedar parcialmente implementados y declararse como trabajo futuro. La priorización definitiva debe respetar la matriz MoSCoW.

## Arquitectura recuperada

Componentes contemplados:

- Cliente Blazor responsivo.
- Cliente React Native.
- API REST como única autoridad de reglas y autorización.
- Base relacional como fuente de verdad.
- Almacenamiento de archivos separado de la base.
- Canal en tiempo real para internación.
- Procesador de tareas para recordatorios, alertas y correo.
- Servicio de tiempo real para estados y ubicación durante asistencias activas.
- Proveedor de mapas y rutas aislado tras una interfaz de infraestructura y limitado a los datos mínimos.

Capas:

| Capa | Responsabilidad | Exclusiones |
|---|---|---|
| Presentación | Vistas, pantallas y controladores de API | Sin reglas ni consultas a la base. |
| Aplicación | Casos de uso, transacciones y validación | Sin detalles de interfaz o motor. |
| Dominio | Entidades, estados e invariantes | Sin dependencias externas. |
| Infraestructura | Datos, correo, archivos y notificaciones | Sin decisiones de negocio. |

Patrones propuestos que siguen siendo compatibles con el stack vigente:

- Repository, con cautela para no duplicar innecesariamente el ORM.
- Unit of Work para cierres transaccionales.
- State para Turno e Internación.
- Strategy para duración y tarifa.
- Specification para disponibilidad.
- Observer o publicación/suscripción para internación y alertas.
- State para el ciclo de la solicitud de asistencia y Strategy para clasificación, selección profesional y cálculo tarifario.
- DTO para el contrato de API.
- Inyección de dependencias.

Las referencias del documento a MVC para web, MVVM como decisión móvil y a mantener el móvil en la misma familia tecnológica son históricas y quedan reemplazadas por Blazor/C# y React Native. Los patrones de presentación concretos deberán justificarse dentro de esas tecnologías.

## Estrategias de concurrencia

- Índice o restricción única para espacios de agenda de granularidad fija.
- Bloqueo pesimista para intervalos variables cuando sea necesario.
- Concurrencia optimista para fichas y formularios editables.
- Actualización condicional atómica para stock.
- Clave de idempotencia para confirmaciones, pagos y reintentos.

## UML requerido

Diagramas obligatorios:

- Casos de uso general y específicos por agenda, clínica, internación, inventario y facturación.
- Clases de dominio con multiplicidades y relación N:M Cliente–Mascota.
- Modelo entidad-relación físico con tipos, claves, índices y restricciones.
- Secuencias de agenda concurrente, cierre de consulta, internación en tiempo real y receta.
- Secuencias de creación, escalamiento por empeoramiento, asignación y seguimiento de asistencia.
- Máquinas de estado de Turno, Internación y Solicitud de asistencia.
- Actividades de atención completa y procedimiento quirúrgico.
- Componentes de clientes, API, base, archivos, tiempo real, tareas y correo.
- Despliegue con nodos, protocolos y puertos.
- Paquetes con dependencias unidireccionales entre capas.

Diagramas opcionales: clases de diseño, objetos y comunicación.

## Estrategia de pruebas

- Unitarias del dominio.
- Integración contra base real.
- Concurrencia para agenda y stock.
- Aceptación de recorridos completos.
- Seguridad sobre autorización, privilegios, inyección y archivos.

Casos de referencia recuperados:

| ID | Cobertura |
|---|---|
| CP-01 | Solapamiento de turnos de un veterinario. |
| CP-02 | Dos reservas concurrentes; exactamente una se confirma. |
| CP-03 | Recurso físico ocupado. |
| CP-04 | Consumo que cruza el mínimo y genera alerta. |
| CP-05 | Rechazo de stock insuficiente sin alterar existencias. |
| CP-06 | Consumo FEFO. |
| CP-07 | Bloqueo de comprobante con consulta abierta. |
| CP-08 | Reversión total si falla el cargo al cerrar una consulta. |
| CP-09 | Cierre exitoso con correo caído y reintento posterior. |
| CP-10 | Cliente intentando acceder a una mascota ajena. |
| CP-11 | Enmienda de un diagnóstico firmado sin modificar el original. |
| CP-12 | Rechazo de vacuna vencida. |
| CP-13 | Bloqueo de cirugía sin consentimiento. |
| CP-14 | Rechazo de una segunda internación activa. |
| CP-15 | Denegación de un enlace de radiografía vencido o anónimo. |
| CP-16 | Solicitud amarilla pendiente que empeora, pasa a roja y activa guardia inmediatamente. |
| CP-17 | Traspaso de cierre: todas las naranjas pendientes quedan antes que las amarillas del día siguiente. |
| CP-18 | Reevaluación a las 24 horas sin reducción ni cierre automático por falta de respuesta. |
| CP-19 | Solicitud roja fuera de horario con búsqueda de guardia y escalamiento a Recepción si no hay elegibles. |
| CP-20 | GPS rechazado con dirección manual válida y bloqueo de visita cuando no existe ningún destino. |
| CP-21 | Tiempo estimado y mapa ocultos hasta la aceptación profesional y seguimiento detenido al finalizar. |
| CP-22 | Reintentos de red sin duplicar solicitud, aceptación ni aviso de empeoramiento. |
| CP-23 | Solicitud grupal o de especie no doméstica que filtra veterinarios por competencia y equipamiento. |
| CP-24 | Deuda o falla de pago que no bloquea ni demora la creación de una solicitud roja. |

Cada regla de negocio debe contar con al menos una prueba asociada.

## Riesgos principales

| Riesgo | Impacto | Mitigación |
|---|---|---|
| Alcance excesivo para dos personas y diez semanas | Alto | Cerrar MoSCoW al final de la Fase 1. |
| El móvil consume más tiempo del previsto | Alto | Reducir alcance móvil antes de afectar la API o las funciones Must. |
| Cambios tardíos en el modelo | Medio | Migraciones y revisión formal al cerrar la Fase 1. |
| Documentación postergada | Alto | Entregar documentación en cada fase. |
| Datos de demostración insuficientes | Medio | Mantener un juego realista desde la Fase 1. |
| Servicios externos fallan durante la defensa | Medio | Modo demostración con integraciones simuladas y evidencia de respaldo. |
| La clínica promete cobertura roja sin una guardia realmente disponible | Alto | Configurar guardias, mostrar disponibilidad real, escalar a Recepción y no prometer asignación ni llegada antes de la aceptación. |
| Una clasificación automática se interpreta como diagnóstico | Alto | Presentarla como orientación preliminar, permitir solo elevación automática y exigir revisión profesional trazada para reducirla. |
| Exposición indebida de ubicaciones de clientes o profesionales | Alto | Consentimiento específico, minimización, autorización, auditoría, cese del seguimiento y conservación definida. |
| Cuestionarios diseñados solo para perros y gatos | Alto | Modelar especies y grupos, validar preguntas con profesionales y filtrar asignación por competencia y equipamiento. |
| Alcance técnico de mapas, tiempo real y guardias supera el cronograma | Alto | Proteger primero creación, prioridad, escalamiento y contacto; planificar mapa en tiempo real como incremento verificable sin degradar el flujo seguro. |

## Datos para la demostración

La propuesta utiliza como referencia 30 clientes, 50 mascotas y 200 consultas históricas. Estos datos deben ser reproducibles y mantenerse durante las pruebas y la defensa.

## Consideraciones legales pendientes de verificación

- La Ley 18.331 de Protección de Datos Personales de Uruguay se considera una referencia firme del documento.
- El plazo exacto de conservación de historias veterinarias debe verificarse con normativa vigente.
- El formato del registro nacional de identificación animal debe mantenerse parametrizable hasta verificar su especificación actual.
