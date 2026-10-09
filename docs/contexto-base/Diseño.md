# Plan de implementación de diseño en Figma

Versión: 1.1

Fecha: 2026-09-26

Estado: Decisión de diseño aprobada para planificación y wireframes

Alcance inicial: aplicación móvil React Native para Visitante y Cliente

## 1. Propósito

Este documento define la dirección visual, la arquitectura de información, la navegación, el sistema de diseño y el proceso de implementación en Figma para la aplicación móvil del sistema veterinario.

El diseño debe transmitir confianza clínica, cercanía con las mascotas, claridad y profesionalismo sin adoptar una apariencia fría o excesivamente infantil.

La implementación se divide en dos niveles:

1. Arquitectura, flujos y wireframes, que pueden comenzar con los requisitos actuales.
2. Diseño visual de alta fidelidad, que comenzará después de cerrar los requisitos pendientes de pseudocódigo, registro y los parámetros abiertos de alertas.

Este documento complementa `CONTEXTO_BASE.md`, `MATRIZ_REQUISITOS.md` e `Implementacion_Politicas.txt`. No sustituye sus requisitos canónicos.

## 2. Principios rectores

- Diseñar primero para Visitante y Cliente; continuar después con Veterinario.
- Permitir navegación pública sin autenticación.
- Solicitar autenticación únicamente al intentar una acción protegida.
- Conservar y retomar la intención original después del inicio de sesión o registro.
- Mantener las funciones principales accesibles desde pantallas de 360 px.
- Utilizar español rioplatense, mensajes claros y acciones explícitas.
- Evitar que una regla de negocio o autorización dependa únicamente de la interfaz.
- Diseñar estados de carga, vacío, error, concurrencia, permisos y sesión expirada.
- Incorporar accesibilidad, privacidad y trazabilidad desde los wireframes.
- No usar color como único medio para comunicar un estado.
- Representar animales domésticos, de granja, equinos, aves, reptiles y otros pacientes sin diseñar la experiencia exclusivamente alrededor de perros y gatos.
- Mantener visible la acción para informar empeoramiento durante toda solicitud de asistencia activa.

## 3. Opciones de dirección visual

### 3.1 Opción A — Clínica cálida

Opción recomendada y seleccionada como dirección inicial.

| Uso | Color | Código |
|---|---|---|
| Primario | Verde petróleo | `#0F766E` |
| Primario oscuro | Verde profundo | `#115E59` |
| Primario suave | Verde claro | `#CCFBF1` |
| Acento | Coral cálido | `#EA6A5A` |
| Fondo principal | Gris muy claro | `#F8FAFC` |
| Superficie | Blanco | `#FFFFFF` |
| Texto principal | Azul grisáceo oscuro | `#172033` |
| Texto secundario | Gris | `#526070` |
| Bordes | Gris claro | `#D8E0E8` |

Esta opción combina una imagen clínica confiable con una presencia cálida orientada a dueños de mascotas.

### 3.2 Opción B — Profesional y tecnológica

| Uso | Color | Código |
|---|---|---|
| Primario | Azul | `#2563EB` |
| Primario oscuro | Azul profundo | `#1E40AF` |
| Primario suave | Azul claro | `#DBEAFE` |
| Secundario | Verde salvia | `#4D7C68` |
| Acento | Ámbar | `#D97706` |
| Fondo | Gris frío | `#F8FAFC` |
| Texto | Azul noche | `#172554` |

Es una opción clara y profesional, especialmente adecuada para agenda y gestión, aunque menos cálida para el cliente.

### 3.3 Opción C — Cercana y diferenciada

| Uso | Color | Código |
|---|---|---|
| Primario | Violeta | `#6D28D9` |
| Primario oscuro | Violeta profundo | `#4C1D95` |
| Primario suave | Lavanda | `#EDE9FE` |
| Secundario | Verde petróleo | `#0F766E` |
| Acento | Dorado suave | `#B45309` |
| Fondo | Marfil claro | `#FFFCF7` |
| Texto | Gris oscuro | `#292524` |

Es más expresiva y memorable, pero exige mayor control para conservar la percepción clínica.

### 3.4 Colores semánticos

Estos colores son independientes de la paleta de marca:

| Estado | Color |
|---|---|
| Éxito | `#15803D` |
| Advertencia | `#B45309` |
| Error | `#B42318` |
| Información | `#1D4ED8` |
| Deshabilitado | `#94A3B8` |

Los estados deben acompañarse con texto, icono o ambos. Los contrastes definitivos se verificarán según WCAG 2.1 AA antes de aprobar los componentes.

### 3.5 Niveles de alertas de asistencia

La clasificación operativa utiliza cuatro niveles independientes de la paleta de marca:

| Prioridad | Color inicial | Comunicación obligatoria |
|---|---|---|
| Rojo | `#B42318` | “Rojo — atención crítica”, icono y acción inmediata. |
| Naranja | `#C2410C` | “Naranja — urgencia alta”, icono y posición prioritaria. |
| Amarillo | `#A16207` | “Amarillo — atención prioritaria”, icono y expectativa de espera. |
| Verde | `#15803D` | “Verde — situación estable”, icono y siguiente paso. |

Estos valores son provisionales hasta validar contraste en todos los componentes. El orden y el significado nunca dependen únicamente del color.

## 4. Opciones tipográficas

### 4.1 Inter

Opción recomendada y seleccionada.

- Una única familia para títulos, texto, formularios y controles.
- Buena legibilidad en pantallas pequeñas.
- Amplia variedad de pesos.
- Fácil correspondencia entre Figma y React Native.
- Reduce la complejidad del sistema tipográfico.

Escala inicial:

| Estilo | Tamaño recomendado |
|---|---:|
| Título destacado | 32 px |
| Título de pantalla | 24 px |
| Título de sección | 20 px |
| Subtítulo | 18 px |
| Cuerpo | 16 px |
| Texto secundario | 14 px |
| Etiqueta | 12–14 px |
| Botón | 16 px, semibold |

### 4.2 Nunito Sans e Inter

- Nunito Sans para encabezados.
- Inter para texto, botones y formularios.
- Genera una identidad más amable, pero aumenta la complejidad y el riesgo de inconsistencias.

### 4.3 Atkinson Hyperlegible

- Prioriza la diferenciación entre caracteres.
- Es adecuada si la accesibilidad tipográfica se convierte en el principal criterio visual.
- Tiene una personalidad menos neutra que Inter.

### 4.4 Tipografía del sistema

- San Francisco en iOS.
- Roboto en Android.
- Mejora la integración nativa, pero introduce diferencias visuales entre plataformas.

La tipografía elegida deberá incorporarse con una licencia compatible y, cuando sea posible, distribuirse localmente con la aplicación para no depender de una carga remota.

## 5. Opciones de navegación

### 5.1 Opción A — Navegación inferior funcional

Opción global recomendada y seleccionada.

Visitante:

- Inicio.
- Servicios.
- Profesionales.
- Urgencias.
- Acceder.

Cliente:

- Inicio.
- Asistencia, como acción prioritaria desde Inicio y acceso a solicitudes activas.
- Mascotas.
- Turnos.
- Notificaciones.
- Perfil.

Ventajas:

- Las funciones principales permanecen visibles.
- Utiliza un patrón móvil reconocido.
- Facilita el cambio entre el área pública y el área autenticada.
- Proporciona acceso directo a mascotas y turnos.

### 5.2 Opción B — Navegación centrada en tareas

- Inicio.
- Actividad.
- Nueva acción.
- Mensajes.
- Perfil.

Da gran visibilidad a la creación de turnos y mascotas, pero utiliza categorías menos explícitas y requiere más aprendizaje.

### 5.3 Opción C — Navegación centrada en la mascota

Después de seleccionar una mascota:

- Resumen.
- Salud.
- Turnos.
- Archivos.
- Internación.

Es excelente para organizar la información clínica, pero no debe reemplazar la navegación global porque dificulta las acciones generales y el uso con varias mascotas.

### 5.4 Decisión combinada

Se utilizará la Opción A como navegación global y la Opción C como navegación contextual dentro del detalle de una mascota.

## 6. Arquitectura de información del Visitante

```text
Área pública
├── Inicio
│   ├── Servicios destacados
│   ├── Disponibilidad general
│   ├── Equipo profesional
│   └── Acceso rápido a urgencias
├── Servicios
│   ├── Catálogo
│   └── Detalle del servicio
├── Profesionales
│   ├── Listado
│   └── Perfil, especialidad y horarios
├── Urgencias
│   ├── Teléfono de guardia
│   ├── Ubicación
│   └── Indicaciones
├── Contacto
├── Iniciar sesión
├── Registro
├── Recuperar contraseña
└── Información legal
    ├── Privacidad
    ├── Términos
    └── Cookies, cuando corresponda
```

El visitante puede explorar servicios, profesionales y disponibilidad general sin autenticarse.

Cuando intenta una acción protegida:

```text
Acción protegida
→ explicación breve
→ iniciar sesión o registrarse
→ verificar cuenta cuando corresponda
→ retomar automáticamente la acción original
```

El prototipo de Figma debe demostrar la conservación y reanudación de esta intención.

## 7. Arquitectura de información del Cliente

```text
Cliente
├── Inicio
│   ├── Próximo turno
│   ├── Alertas importantes
│   ├── Mascotas
│   └── Acciones rápidas
│       └── Solicitar asistencia
├── Asistencia
│   ├── Nueva solicitud
│   │   ├── Visita al lugar u orientación para traslado
│   │   ├── Animal individual o grupo
│   │   ├── Síntomas y gravedad percibida
│   │   ├── Preguntas adaptadas
│   │   ├── GPS consentido o dirección manual
│   │   └── Costo y confirmación
│   ├── Solicitudes activas
│   └── Detalle de solicitud
│       ├── Nivel, estado e historial
│       ├── Informar empeoramiento
│       ├── Profesional y tiempo estimado después de aceptar
│       ├── Mapa durante el traslado
│       └── Contacto con Recepción
├── Mascotas
│   ├── Listado
│   ├── Registrar mascota
│   └── Detalle
│       ├── Resumen
│       ├── Historia clínica
│       ├── Carnet sanitario
│       ├── Archivos
│       ├── Vacunas
│       └── Internación
├── Turnos
│   ├── Próximos
│   ├── Historial
│   ├── Agendar
│   ├── Reprogramar
│   └── Cancelar
├── Notificaciones
└── Perfil
    ├── Datos personales
    ├── Responsables autorizados
    ├── Preferencias de notificación
    ├── Privacidad y permisos
    ├── Exportar o eliminar datos
    ├── Documentos legales
    └── Cerrar sesión
```

## 8. Flujo de agenda

El flujo debe respetar el objetivo de completar una reserva en no más de cuatro pasos desde su inicio:

1. Seleccionar mascota.
2. Seleccionar servicio y, opcionalmente, profesional.
3. Seleccionar fecha y horario.
4. Revisar y confirmar.

La revisión final mostrará:

- mascota;
- servicio;
- profesional;
- fecha y hora;
- precio de referencia, cuando corresponda;
- requisitos previos;
- política de cancelación.

Si el horario deja de estar disponible por concurrencia, la interfaz conservará los datos ya seleccionados y presentará horarios actualizados sin mostrar detalles técnicos.

## 9. Entregas funcionales de diseño

### 9.1 Visitante y acceso

- Inicio público.
- Catálogo y detalle de servicios.
- Profesionales.
- Urgencias.
- Contacto.
- Inicio de sesión.
- Registro.
- Verificación de cuenta.
- Recuperación de contraseña.
- Privacidad y términos.
- Interrupción y reanudación de una acción protegida.

### 9.2 Cliente esencial

- Inicio del cliente.
- Listado, alta, edición y detalle de mascotas.
- Próximos turnos e historial.
- Flujo completo de agenda.
- Cancelación y reprogramación.
- Solicitud de asistencia: modalidad, animales, gravedad, ubicación y costo.
- Estado de asistencia, reevaluación, empeoramiento, notificaciones y mapa.
- Notificaciones.
- Perfil y preferencias.

### 9.3 Cliente clínico

- Historia clínica en modo lectura.
- Carnet sanitario.
- Vacunas y próximos refuerzos.
- Archivos adjuntos.
- Seguimiento de internación.
- Consentimiento informado.
- Solicitudes relacionadas con datos personales.

### 9.4 Veterinario

Se diseñará después de validar Visitante y Cliente:

- agenda diaria y semanal;
- alertas rojas elegibles y solicitudes asignadas;
- aceptación o rechazo, confirmación de gravedad y motivo de reclasificación;
- estados de preparación, traslado, llegada y atención;
- paciente e historia clínica;
- registro de atención;
- vacunación y adjuntos;
- internación;
- firma y cierre;
- enmiendas.

## 10. Organización del archivo Figma

```text
00 — Portada y decisiones
01 — Arquitectura y flujos
02 — Fundamentos
03 — Componentes
04 — Visitante y autenticación
05 — Cliente
06 — Veterinario
07 — Estados y casos especiales
08 — Prototipos
09 — Accesibilidad y validación
10 — Entrega a desarrollo
99 — Archivo
```

## 11. Sistema de diseño

### 11.1 Variables

- Colores de marca.
- Colores semánticos.
- Texto y superficies.
- Espaciado.
- Bordes.
- Radios.
- Sombras.
- Tipografía.
- Tamaños de iconos.

Escala de espaciado:

`4, 8, 12, 16, 24, 32, 40 y 48 px`

Radios sugeridos:

- Campos y botones: 8 px.
- Tarjetas: 12 px.
- Modales: 16 px.
- Avatares e imágenes de mascotas: circular o 16 px.

El tema claro será el alcance inicial. Las variables se prepararán para admitir un modo oscuro futuro sin incluirlo en la primera entrega.

### 11.2 Componentes

- Botones primario, secundario, de texto y de peligro.
- Campos de texto, búsqueda, selección y fecha.
- Checkbox, radio y switch.
- Barra de navegación inferior.
- Encabezados.
- Tarjetas de mascota, turno, profesional y servicio.
- Alertas y banners.
- Diálogos y hojas inferiores.
- Indicadores de estado.
- Calendario y selector de horario.
- Carga, esqueleto y progreso.
- Mensajes de estado vacío.
- Confirmaciones.
- Consentimientos.
- Visor de archivos.

Variantes mínimas:

- normal;
- presionado;
- enfocado;
- seleccionado;
- deshabilitado;
- cargando;
- error.

## 12. Estados obligatorios

Cada pantalla relevante debe contemplar:

- cargando;
- sin información;
- error recuperable;
- sin conexión;
- sesión expirada;
- permiso denegado;
- acceso no autorizado;
- contenido no disponible;
- turno ocupado por otro usuario;
- confirmación exitosa;
- acción irreversible o sensible;
- cuenta no verificada.

## 13. Accesibilidad

- Área táctil mínima aproximada de 44 × 44 px.
- Texto general de al menos 16 px.
- Contraste conforme a WCAG 2.1 AA.
- Foco visible y orden de navegación lógico.
- Botones con nombres claros y descriptivos.
- Etiquetas, instrucciones y errores asociados a los campos.
- Iconos acompañados por texto cuando su significado no sea evidente.
- Navegación completa mediante teclado cuando corresponda.
- Pruebas manuales de los recorridos principales con lector de pantalla.
- Respeto por la preferencia de movimiento reducido.

Tratamiento de imágenes:

- Las informativas tendrán una alternativa que comunique su contenido.
- Las funcionales comunicarán la acción que ejecutan.
- Las decorativas se excluirán del árbol de accesibilidad.
- Las fotografías de mascotas complementarán el nombre y la especie; no serán el único medio de identificación.

## 14. Privacidad y cumplimiento en el diseño

- Consentimientos separados por finalidad.
- Opciones facultativas sin marcar previamente.
- Acceso permanente a privacidad y términos.
- Políticas identificadas por versión o vigencia.
- Preferencias opcionales modificables desde el perfil.
- Información clínica excluida de analítica y material demostrativo público.
- Datos ficticios claramente identificados en los prototipos.
- Recursos visuales con procedencia y licencia documentadas.
- Inventario previo de mapas, CAPTCHA, analítica, pagos, notificaciones y otros terceros.

## 15. Iconografía e imágenes

- Utilizar una sola familia de iconos lineales.
- Mantener grosor, tamaño y estilo consistentes.
- Evitar mezclar bibliotecas.
- Utilizar fotografías propias, autorizadas o con licencia compatible.
- No incorporar imágenes clínicas reales o sensibles al prototipo.
- No publicar reseñas, sellos o afirmaciones sin respaldo verificable.

## 16. Movimiento y retroalimentación

- Transiciones breves de 150 a 250 ms.
- Confirmaciones visuales inmediatas.
- Esqueletos durante cargas previsibles.
- Animación sutil después de reservar correctamente.
- Reducción o eliminación del movimiento cuando el sistema lo solicite.
- Ninguna animación debe retrasar acciones urgentes o clínicas.

## 17. Tamaños de trabajo

- Base de diseño: 390 px de ancho.
- Validación mínima obligatoria: 360 px.
- Validación adicional: 430 px.
- Los componentes utilizarán Auto Layout y restricciones responsivas.
- El contenido no dependerá de alturas fijas ni de un único modelo de teléfono.

## 18. Proceso de implementación

### Fase 1 — Arquitectura

- Confirmar contenido y navegación.
- Diagramar Visitante y Cliente.
- Identificar acciones públicas y protegidas.
- Definir reanudación de intención después de autenticarse.
- Diagramar el flujo completo de asistencia y sus variantes de horario, cobertura, ubicación, especies y grupos.

### Fase 2 — Wireframes

- Crear pantallas en escala de grises.
- Validar jerarquía y cantidad de pasos.
- Probar agenda, alta de mascota y acceso clínico.
- Probar los cuatro niveles sin depender solo del color, el aviso de empeoramiento, la reevaluación de 24 horas y la degradación sin mapa.
- Revisar errores, permisos y concurrencia.

### Fase 3 — Fundamentos y componentes

- Crear variables de color, tipografía y espaciado.
- Construir componentes y variantes.
- Validar los tamaños de pantalla definidos.
- Documentar reglas de uso.

### Fase 4 — Alta fidelidad

- Aplicar la identidad visual seleccionada.
- Diseñar primero Visitante y Cliente.
- Incorporar textos reales o representativos.
- Completar estados y casos especiales.

Esta fase está condicionada al cierre de registro, pseudocódigo restante y parámetros abiertos de alertas.

### Fase 5 — Prototipo y validación

Se probarán como mínimo estos recorridos:

1. Visitante consulta servicios.
2. Visitante intenta agendar y se registra.
3. El sistema retoma la reserva.
4. Cliente registra una mascota.
5. Cliente agenda un turno.
6. Cliente pierde un horario por concurrencia y recibe nuevas opciones.
7. Cliente consulta la historia clínica.
8. Cliente modifica preferencias.
9. Cliente sigue una internación.
10. Cliente cancela o reprograma.
11. Cliente solicita visita para un animal individual y comparte GPS con consentimiento.
12. Cliente solicita asistencia para un grupo de animales e ingresa dirección rural manual.
13. Una alerta amarilla informa empeoramiento, pasa a roja y activa guardia.
14. Una alerta naranja queda pendiente al cierre y aparece antes que las amarillas del día siguiente.
15. El veterinario acepta, inicia el traslado y el cliente alterna entre notificaciones y mapa.
16. No hay veterinario elegible o la ubicación está fuera de cobertura y Recepción coordina una alternativa.
17. Una solicitud pendiente cumple 24 horas y pide reevaluación sin cerrarse por falta de respuesta.

### Fase 6 — Entrega a desarrollo

- Componentes nombrados consistentemente.
- Variables documentadas.
- Pantallas relacionadas con RF, RN y RNF.
- Comportamientos responsivos especificados.
- Textos de error y estados definidos.
- Recursos con procedencia registrada.
- Flujo navegable aprobado.
- Pendientes señalados expresamente.

## 19. Decisión final de diseño

- Dirección visual: clínica cálida.
- Paleta: verde petróleo y coral.
- Tipografía: Inter.
- Navegación global: barra inferior funcional.
- Navegación de mascota: contextual dentro de su detalle.
- Tema inicial: claro.
- Base de diseño: 390 px, validada desde 360 px.
- Orden de trabajo: Visitante → autenticación → Cliente → Veterinario.
- Método: arquitectura → wireframes → validación → componentes → alta fidelidad → prototipo → entrega.
- Alta fidelidad: condicionada al cierre de registro, pseudocódigo restante y parámetros abiertos de alertas.

## 20. Criterio de aprobación

El diseño estará listo para desarrollo cuando:

- los recorridos principales puedan completarse sin ambigüedad;
- la agenda respete el máximo de cuatro pasos;
- las acciones protegidas retomen su intención después de autenticarse;
- todos los estados relevantes estén representados;
- los componentes cumplan contraste, foco y área táctil;
- las decisiones de privacidad sean claras y trazables;
- los niveles rojo, naranja, amarillo y verde se comprendan sin depender del color;
- el cliente pueda informar empeoramiento desde cualquier estado activo pertinente;
- la ubicación y el tiempo estimado solo aparezcan durante las etapas autorizadas;
- las variables y componentes puedan trasladarse a React Native;
- exista trazabilidad con los requisitos vigentes;
- no queden recursos visuales sin licencia o procedencia conocida.
