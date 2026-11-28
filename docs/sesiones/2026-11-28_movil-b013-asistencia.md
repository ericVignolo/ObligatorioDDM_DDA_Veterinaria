# Sesión 2026-11-28 — Móvil: solicitud de asistencia (B-013)

Rama: `eric/sesion-2026-11-28-movil-b013`, desde `eric/sesion-2026-11-26-movil-b012`.

## Qué se hizo
- **Pedir asistencia** (`app/asistencia/nueva.tsx`; RF-CLI-21, RF-CLI-22, RF-CLI-25; CU-CLI-19): modalidad (visita u orientación y traslado), animal o grupo propio o «Sin registrar o grupo» con especie y cantidad, síntomas, desde cuándo, peso y riesgos opcionales, gravedad percibida, dirección y referencias para la visita, teléfono y criterio de costo.
- **Constancia y detalle** (`app/asistencia/[id].tsx`): nivel con símbolo, nombre y descripción (RN-47), estado real sin prometer profesional ni horario (RN-53) y datos enviados.
- **Mis solicitudes** (`app/asistencia/index.tsx`): solo las propias (RN-18).
- **Accesos:** Inicio del Cliente, Urgencias y ficha del animal (llega elegido). Un Visitante que toca «Pedir asistencia» ingresa o se registra y vuelve al formulario (`services/intencionService.ts`).
- **Reglas** (`services/asistenciaService.ts`, las de `AsistenciaService` de Blazor): nivel preliminar = el que indica el cliente (D-ASI-01); rojo se ofrece a los elegibles y a la guardia, sin nadie pasa a Recepción (RN-49, RN-54, RN-55); aviso de próximo día de atención fuera de horario (RN-48); visita sin dirección no se envía (RN-56); reenvío con la misma clave no duplica; no se consulta deuda (RN-58).
- **Rojo:** muestra el teléfono de la guardia y no exige aceptar el costo para enviar.

## Decisiones tomadas en la implementación (confirmar con Diego)
- La app no conoce la distancia: muestra «desde» base + animales extra y el valor por km; se acepta ese criterio y Recepción confirma la distancia. Con la API habrá que definir qué distancia manda el móvil (hoy `DistanciaKm` = 0).
- Se piden «desde cuándo» (obligatorio), peso y riesgos (opcionales), de la sección 5 de la especificación. El escritorio y `SolicitudAppDto` de la API no tienen esos tres campos.
- Se informa que la clínica cubre hasta 30 km; una visita fuera de cobertura la detecta Recepción.

## Fuera de esta tarea
- B-014: seguimiento, ETA, mapa, GPS consentido, informar empeoramiento, plazos de 10 minutos y 24 horas.
- Cuestionario clínico: requiere aprobación profesional, no se implementó.
- Varios animales registrados en una misma solicitud (RF-CLI-21): el modelo admite un animal; para varios se usa «Sin registrar o grupo» con la cantidad.

## Cómo probarlo
1. `cd mobile/Veterinaria.Movil` y `npx expo start`; abrir con Expo Go.
2. Sin sesión: **Urgencias → Pedir asistencia** → ingresar como Ana → abre el formulario.
3. Tocar «Enviar solicitud» vacío (errores). Elegir Visita sin dirección (error).
4. Visita, Toby, Verde, con dirección: aceptar el costo y enviar. Cambiar el nivel o la cantidad desmarca la aceptación.
5. Orientación, Toby, Rojo: aparece la guardia y queda «Ofrecida».
6. Visita, «Sin registrar o grupo», Caballo, 3, Rojo, de noche o fin de semana: queda «Requiere coordinación de Recepción».
7. **Inicio → Mis solicitudes**; Pedro no ve las de Ana. Desde la ficha de un animal, «Pedir asistencia» lo trae elegido.

## Comprobado
- `tsc --noEmit` sin errores y paquete web generado.
- En Chrome, sin errores de consola registrados: pasos 2 a 6, aviso fuera de horario, lista y solicitud ajena no disponible.
- **No** se probó en Expo Go, ni el grupo registrado de Pedro, el registro de un Visitante con vuelta al formulario, los botones de llamada ni el doble envío.
- No se actualizaron los documentos del Project de Claude (sin acceso desde la sesión).

## Siguiente
- B-014: seguimiento e informes de cambio.
