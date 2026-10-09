# Especificación funcional — Alertas y asistencia veterinaria

Versión: 0.1

Fecha: 2026-09-26

Estado: Borrador funcional con decisiones confirmadas y parámetros pendientes

Fuente canónica de requisitos: `../contexto-base/MATRIZ_REQUISITOS.md`

## 1. Propósito

Definir el comportamiento del módulo mediante el cual un cliente solicita orientación o asistencia veterinaria para uno o varios animales, informa la gravedad percibida, comparte una ubicación, recibe una clasificación de cuatro niveles y sigue la asignación y llegada de un profesional.

Este módulo no realiza un diagnóstico automático. La clasificación inicial sirve para priorizar y escalar la solicitud hasta que un veterinario la revise.

## 2. Alcance confirmado

- Atiende animales domésticos, de granja, producción, equinos, aves, reptiles y demás especies que la clínica configure.
- Una solicitud puede involucrar un animal identificado, un animal todavía no registrado o un grupo de animales.
- Admite dos modalidades: visita del veterinario al lugar y orientación para coordinar el traslado a la clínica.
- Combina gravedad percibida por el cliente, cuestionario de orientación y confirmación profesional.
- Utiliza cuatro niveles: rojo, naranja, amarillo y verde.
- Permite informar empeoramiento en cualquier momento mientras la solicitud esté activa.
- Solicita una reevaluación al cumplirse 24 horas si la solicitud continúa pendiente.
- Admite GPS con consentimiento o dirección y referencias manuales.
- Muestra tiempo estimado de llegada únicamente después de la aceptación profesional.
- Comunica el avance mediante estados, notificaciones y mapa cuando exista un veterinario en traslado.
- Genera un costo conforme a la tarifa o criterio vigente, sin bloquear una urgencia roja por deuda previa o falla de pago.

## 3. Actores y responsabilidades

| Actor | Responsabilidad |
|---|---|
| Cliente | Crea la solicitud, identifica los animales, informa síntomas y gravedad percibida, decide la modalidad, proporciona ubicación, acepta las condiciones de costo, responde reevaluaciones e informa empeoramiento. |
| API y dominio | Valida, clasifica preliminarmente, prioriza, conserva trazabilidad, selecciona candidatos elegibles, aplica escalamiento y publica cambios de estado. |
| Recepcionista | Gestiona la cola verde, amarilla y naranja; asigna profesionales elegibles; atiende escalaciones y contacta al solicitante cuando no puede concretarse el flujo normal. |
| Veterinario | Acepta o rechaza asignaciones, confirma o modifica con motivo el nivel, actualiza el traslado, comparte ubicación durante la asistencia y registra la atención. |
| Administrador | Configura horarios, cobertura, guardias, competencias por especie, equipamiento, tarifas y parámetros operativos. |
| Sistema temporal | Detecta solicitudes pendientes durante 24 horas, genera reevaluaciones y ejecuta escalaciones configuradas. |
| Proveedor de mapas | Calcula ruta y estimación sin convertirse en autoridad de la solicitud ni recibir datos clínicos innecesarios. |

## 4. Clasificación y prioridad

| Nivel | Significado operativo | Horario | Tratamiento de cola |
|---|---|---|---|
| Rojo | Posible riesgo vital o deterioro crítico. | Todo horario mediante guardia. | Prioridad máxima; no se difiere automáticamente. |
| Naranja | Urgencia alta sin riesgo vital confirmado. | Horario de la clínica. | Si queda pendiente al cierre, pasa al siguiente día antes que las amarillas. |
| Amarillo | Requiere atención prioritaria, pero tolera una espera controlada. | Horario de la clínica. | Si queda pendiente al cierre, pasa al siguiente día después de las naranjas. |
| Verde | Situación estable o programable. | Horario de la clínica. | Se atiende o agenda después de los niveles superiores. |

Orden obligatorio: `rojo → naranja → amarillo → verde`.

Dentro del mismo nivel se utiliza la antigüedad de la solicitud, salvo que una reevaluación, un cambio clínico o una decisión profesional trazada altere la prioridad.

El nivel nunca se comunica solo mediante color. Cada presentación incluye nombre, texto, icono y acción recomendada.

## 5. Datos mínimos de la solicitud

- Cliente y medio de contacto vigente.
- Modalidad solicitada: visita o coordinación de traslado.
- Animal registrado, animal no registrado o grupo.
- Especie o categoría; raza cuando corresponda.
- Cantidad de animales afectados.
- Peso o tamaño aproximado cuando sea relevante.
- Síntomas, momento de inicio y gravedad percibida.
- Respuestas al cuestionario aplicable.
- Riesgos de acceso, manipulación o contención.
- Equipamiento o condiciones especiales conocidas.
- Ubicación GPS consentida o dirección y referencias manuales.
- Resultado de clasificación y su historial.
- Tarifa, estimación o criterio de cálculo informado.

El cuestionario y los campos obligatorios deben variar por especie, cantidad y contexto. Su contenido clínico requiere aprobación profesional antes de implementarse.

## 6. Estados propuestos

```text
Borrador
→ Enviada
→ Clasificada
→ Pendiente de asignación
→ Ofrecida o asignada
→ Aceptada
→ En preparación
→ En camino
→ En el lugar
→ En atención
→ Finalizada
```

Estados alternativos:

- `Requiere coordinación de Recepción`.
- `Derivada o traslado coordinado`.
- `Cancelada por el cliente`.
- `Cancelada por la clínica`, con motivo y contacto previo cuando corresponda.
- `No concretada`, con motivo registrado.

Una solicitud roja no puede finalizar como no concretada por una automatización silenciosa. Debe quedar evidencia del escalamiento y de la intervención o intento de contacto humano.

## 7. Flujo principal

1. El cliente inicia una solicitud protegida.
2. Elige visita al lugar u orientación para traslado.
3. Selecciona un animal, registra datos mínimos o declara un grupo.
4. Informa síntomas, riesgos y gravedad percibida.
5. El sistema presenta preguntas adaptadas y calcula un nivel preliminar.
6. El resultado conserva como mínimo el nivel indicado por el cliente; el sistema puede elevarlo.
7. El cliente proporciona GPS con consentimiento o dirección manual.
8. El sistema informa costo, estimación o criterio de cálculo y las condiciones aplicables.
9. El cliente confirma la solicitud.
10. La API persiste la solicitud de forma idempotente, aplica prioridad y busca profesionales elegibles.
11. Verde, amarillo y naranja ingresan al tablero de Recepción. Rojo activa el circuito inmediato de profesionales elegibles y guardia.
12. Un veterinario acepta.
13. El sistema muestra al cliente el profesional asignado, los estados y el tiempo estimado de llegada.
14. Al iniciar el traslado, habilita el mapa y las notificaciones de avance.
15. El veterinario llega, atiende y registra el resultado correspondiente.
16. El seguimiento de ubicación termina al llegar, cancelar o finalizar, según la finalidad operativa definida.

## 8. Pseudocódigo de clasificación inicial

```text
crearSolicitud(entrada, cliente):
    validarSesionYCuenta(cliente)
    validarAnimalOGrupo(entrada.animales)
    validarModalidadYDestino(entrada.modalidad, entrada.ubicacion)
    informarFinalidadDeUbicacionYRegistrarDecision(entrada.consentimiento)

    nivelCliente = entrada.gravedadPercibida
    nivelReglas = evaluarCuestionario(
        especie = entrada.especie,
        cantidad = entrada.cantidad,
        sintomas = entrada.sintomas,
        respuestas = entrada.respuestas
    )

    nivelPreliminar = maximoPorPrioridad(nivelCliente, nivelReglas)
    costo = calcularOCaracterizarCosto(entrada, nivelPreliminar)
    informarCostoOCriterio(costo)

    solicitud = guardarIdempotente(
        entrada,
        nivelPreliminar,
        costo,
        estado = "Pendiente de asignacion"
    )

    si nivelPreliminar == ROJO:
        activarGuardiaYBuscarElegibles(solicitud)
    si no:
        publicarEnColaDeRecepcion(solicitud)

    programarReevaluacion24Horas(solicitud)
    devolver solicitud
```

## 9. Pseudocódigo de prioridad al cambiar el día

```text
alCerrarHorarioClinica():
    pendientes = obtenerPendientes(VERDE, AMARILLO, NARANJA)

    para cada solicitud en pendientes:
        conservarHistorialYFechaOriginal(solicitud)
        marcarParaSiguienteDiaDeAtencion(solicitud)

    ordenarSiguienteDia(
        primero = NARANJA,
        despues = AMARILLO,
        despues = VERDE,
        desempate = fechaCreacionAscendente
    )
```

Las rojas no participan de este traspaso: continúan en el circuito inmediato de guardia.

## 10. Pseudocódigo de empeoramiento

```text
informarEmpeoramiento(solicitudId, nuevasRespuestas, cliente):
    solicitud = obtenerSolicitudActivaAutorizada(solicitudId, cliente)
    registrarEventoInmutable("Empeoramiento informado", nuevasRespuestas)

    nivelCalculado = reevaluar(solicitud, nuevasRespuestas)
    nuevoNivel = maximoPorPrioridad(solicitud.nivelActual, nivelCalculado)

    si nuevoNivel > solicitud.nivelActual:
        actualizarNivelConAuditoria(solicitud, nuevoNivel)
        reordenarCola(solicitud)
        notificarRecepcionYProfesionalesCorrespondientes(solicitud)

        si nuevoNivel == ROJO:
            activarGuardiaInmediatamente(solicitud)

    confirmarAlClienteElEstadoVigente(solicitud)
```

El cliente informa el cambio y el sistema puede elevar el nivel. Una reducción posterior exige revisión veterinaria y motivo registrado.

## 11. Pseudocódigo de reevaluación a las 24 horas

```text
revisarSolicitudesPendientes():
    solicitudes = obtenerActivasCon24HorasDesdeCreacion()

    para cada solicitud en solicitudes:
        si no tieneReevaluacion24hSolicitada:
            crearTareaDeReevaluacion(solicitud)
            notificarCliente(solicitud, "¿El estado se mantuvo o empeoró?")

        si clienteInformaEmpeoramiento:
            ejecutar informarEmpeoramiento(...)

        si clienteNoResponde:
            mantenerNivelYEstado()
            noCerrarNiReducirAutomaticamente()
```

La repetición de nuevas reevaluaciones después de la primera permanece como parámetro pendiente de definición.

## 12. Selección y asignación profesional

La lista de candidatos se filtra en este orden:

1. Habilitación y matrícula vigentes.
2. Competencia para la especie, categoría y tipo de caso.
3. Capacidad para atender la cantidad de animales.
4. Equipamiento necesario.
5. Disponibilidad ordinaria o guardia, según nivel y horario.
6. Zona de cobertura.
7. Proximidad y tiempo estimado.

La cercanía no puede convertir en elegible a un profesional que no cumple los criterios anteriores.

Recepción asigna verde, amarillo y naranja desde su tablero. Las rojas se ofrecen o notifican inmediatamente a los veterinarios elegibles disponibles o de guardia. La estrategia exacta de oferta simultánea o escalonada queda pendiente de parametrización.

## 13. Excepciones y continuidad

### Sin veterinario elegible

- Cambiar a `Requiere coordinación de Recepción`.
- Alertar a Recepción con prioridad coherente al nivel.
- Contactar al cliente y coordinar guardia, traslado, punto de encuentro o derivación.
- No mostrar un profesional ni una estimación inexistentes.

### Fuera de cobertura

- Conservar la solicitud y ubicación.
- Informar claramente que la visita no está confirmada.
- Escalar a Recepción para acordar una alternativa.

### Sin GPS

- Permitir dirección y referencias manuales.
- Solicitar validación de Recepción cuando la dirección no pueda confirmarse automáticamente.

### Sin ninguna ubicación

- Impedir la confirmación de una visita sin destino.
- Mantener disponibles la orientación, el contacto y la coordinación de traslado a la clínica.

### Fallo de mapa, ruta o notificaciones

- Mantener estado y dirección textual en la aplicación.
- Mantener disponible el contacto telefónico.
- No modificar la asignación clínica por una falla exclusivamente visual o de mensajería.

## 14. Ubicación y privacidad

- El GPS del cliente requiere una explicación y decisión explícitas para la asistencia concreta.
- La dirección manual debe estar disponible como alternativa.
- Solo roles autorizados acceden a la ubicación precisa y cada acceso relevante se audita.
- Las coordenadas no se incluyen en logs, analítica ni texto de notificaciones en pantalla bloqueada.
- La ubicación del veterinario se comparte únicamente durante el tramo operativo autorizado.
- Un proveedor de mapas recibe solo los datos indispensables para ruta o estimación y nunca contenido clínico, credenciales o tokens.
- Deben definirse y documentarse conservación, eliminación o disociación de coordenadas después de la asistencia.

## 15. Notificaciones mínimas

- Solicitud recibida.
- Nivel preliminar asignado o elevado.
- Solicitud pendiente para el siguiente día de atención.
- Reevaluación requerida a las 24 horas.
- Veterinario asignado y aceptación confirmada.
- Veterinario en preparación.
- Veterinario en camino y estimación disponible.
- Cambio significativo en la estimación.
- Llegada al lugar.
- Necesidad de contacto por Recepción.
- Derivación, cancelación o finalización.

Las alertas operativas esenciales no dependen de las preferencias de comunicaciones promocionales. Debe definirse qué canales son obligatorios por seguridad y cuáles son configurables.

## 16. Costos

La administración configura la tarifa o criterio vigente. Puede considerar tarifa base, zona o distancia, horario, prioridad, especie o categoría, cantidad de animales y equipamiento especial.

Antes de confirmar se muestra un importe cuando pueda calcularse o una estimación y sus variables. Una solicitud roja se crea y escala aunque exista deuda previa o falle el pago; el cargo y su resolución posterior quedan trazados.

## 17. Pruebas de aceptación mínimas

1. Una solicitud amarilla pendiente pasa al día siguiente detrás de todas las naranjas pendientes.
2. Dos solicitudes naranjas mantienen entre sí el orden de creación, salvo cambio trazado de prioridad.
3. Una amarilla activa informa empeoramiento, se reclasifica como roja y activa guardia inmediatamente.
4. A las 24 horas se solicita reevaluación sin cerrar ni reducir el nivel por falta de respuesta.
5. Una solicitud roja fuera de horario busca guardia y no se difiere al día siguiente.
6. Sin veterinario elegible se alerta a Recepción y nunca se inventan profesional ni tiempo de llegada.
7. El tiempo estimado no aparece antes de la aceptación profesional.
8. Sin GPS se admite dirección manual; sin ningún destino no se confirma visita.
9. La ubicación del veterinario deja de compartirse al terminar o cancelar la asistencia.
10. Un reintento de red no duplica la solicitud, la aceptación ni el aviso de empeoramiento.
11. Un caso de ganado o grupo de aves no exige datos exclusivos de perros o gatos y filtra profesionales competentes.
12. Una deuda o falla de pago no bloquea la creación de una solicitud roja.

## 18. Decisiones pendientes

- Preguntas clínicas y señales de elevación específicas por especie o categoría, aprobadas por profesionales.
- Tiempo objetivo de respuesta de Recepción para cada nivel.
- Estrategia exacta de oferta de solicitudes rojas a profesionales elegibles.
- Frecuencia de reevaluación posterior a la primera solicitud de las 24 horas.
- Política de cancelación y cargos cuando el profesional ya comenzó el traslado.
- Fórmula definitiva de tarifas y tratamiento de presupuestos no calculables de antemano.
- Zonas de cobertura, puntos de encuentro y posibles centros de derivación.
- Conservación exacta de coordenadas y evidencia de consentimiento.
- Canales obligatorios y opcionales de cada notificación.
- Reglas para solicitudes grupales y creación posterior de historias clínicas individuales.

## 19. Trazabilidad principal

- RF-CLI-21 a RF-CLI-25.
- RF-VET-16 y RF-VET-17.
- RF-REC-08 y RF-REC-09.
- RF-ADM-13.
- RN-36 y RN-46 a RN-59.
- RNF-SEG-13 y 14; RNF-CON-10; RNF-DIS-05; RNF-USA-07; RNF-LEG-06, 08 y 09.
