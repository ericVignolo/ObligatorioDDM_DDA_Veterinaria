# Registro de Cliente y reanudación de una acción protegida

Fecha: 2026-10-08

Estado: borrador en conversación; faltan decisiones de Eric antes de fijar DTO/endpoints.

## Alcance y trazabilidad

Este documento detalla `CU-VIS-03` y `HU-VIS-07`, vinculados a **RF-VIS-07**, **RN-37**, **RN-38**, **RN-43**, **RNF-LEG-03** y **RNF-LEG-06**. Es una especificación funcional y de pseudocódigo, no una autorización para diseñar pantallas finales o implementar identidad con campos supuestos. El texto canónico está en [MATRIZ_REQUISITOS.md](../contexto-base/MATRIZ_REQUISITOS.md).

## Comportamiento confirmado

1. Desde cualquier punto público se puede comenzar el registro. Ante una acción protegida se explica el motivo, se ofrece iniciar sesión o registrarse y se conserva solo el contexto no sensible necesario para retomarla.
2. El formulario enlaza las versiones vigentes de Privacidad y Términos accesibles sin cuenta. La API registra por separado cada respuesta exigida por finalidad, documento y versión, fecha, canal y respuesta; las opciones facultativas no están premarcadas ni bloquean el alta si se rechazan.
3. La API valida los datos, la unicidad del correo y las aceptaciones obligatorias. Un mismo correo no identifica dos cuentas de Cliente.
4. La cuenta no puede registrar animales ni reservar turnos hasta estar verificada. El modo invitado no puede consultar información personal.
5. Tras la verificación y autenticación, el cliente vuelve a la acción original. Se revalidan permisos, disponibilidad y vigencia de los datos: haber conservado la intención no implica ejecutar automáticamente una reserva.

**D-REG-01, respuesta de Eric (2026-10-08):** nombre, apellido, correo electrónico, contraseña, teléfono, documento y dirección del hogar serán obligatorios al crear la cuenta. Antes de fijar el DTO se documentará para cada dato su finalidad, quién puede verlo, conservación y destino según RNF-LEG-06; en particular, documento y dirección no se enviarán a clientes o endpoints que no los necesiten. La contraseña se recibe solo para crear la credencial segura y nunca se devuelve ni registra en logs (RNF-SEG-01/11). Formato de documento, normalización del teléfono y estructura de dirección siguen por precisar.

**D-REG-02, respuesta de Eric (2026-10-08):** la verificación será mediante un **código enviado al correo electrónico**. Falta definir duración del código, cantidad de intentos, reenvío, caducidad de cuentas no verificadas y respuesta ante pérdida del correo. El código tampoco se guarda en claro ni se expone en logs.

| Dato obligatorio decidido | Finalidad preliminar a validar | Acceso, conservación y destino |
|---|---|---|
| Nombre y apellido | Identificar a la persona responsable. | Pendiente de ficha de datos. |
| Correo | Identificar cuenta única y recibir código de verificación. | Pendiente de ficha de datos. |
| Contraseña | Autenticar; almacenar solo derivado seguro, nunca texto plano. | Pendiente de política de credenciales. |
| Teléfono | Contacto; precisar usos permitidos. | Pendiente de ficha de datos. |
| Documento | Precisar necesidad al alta: identificación, facturación u otra. | Pendiente de ficha de datos. |
| Dirección del hogar | Precisar necesidad al alta: contacto, facturación, asistencia u otra. | Pendiente de ficha de datos. |

Las finalidades marcadas como preliminares no autorizan usos nuevos. Cualquier uso opcional, como marketing, requiere una decisión separada conforme a RN-43.

## Pseudocódigo funcional preliminar

```text
al_iniciar_accion_protegida(intencion):
    conservar_solo_identificador_de_accion_y_contexto_no_sensible(intencion)
    ofrecer_iniciar_sesion_o_registro()

al_enviar_registro(datos, decisiones):
    validar_campos_definidos_y_correo_unico()
    validar_documentos_vigentes_y_aceptaciones_obligatorias_por_separado()
    crear_cuenta_no_verificada_y_registrar_evidencia_de_decisiones()
    enviar_codigo_de_verificacion_al_correo()

al_verificar_y_autenticar():
    marcar_cuenta_verificada_segun_mecanismo_aprobado()
    recuperar_intencion_no_sensible_si_sigue_vigente()
    revalidar_autorizacion_y_disponibilidad()
    mostrar_paso_de_confirmacion_al_usuario()
```

Los pasos de creación/evidencia se diseñarán para no dejar una cuenta verificada sin las aceptaciones obligatorias. No se incluyen contraseñas, códigos ni tokens en registros de actividad. Los tiempos, reintentos y el tratamiento de un registro parcial siguen abiertos.

## Decisiones necesarias antes del contrato de registro

| ID | Clasificación | Pregunta |
|---|---|---|
| D-REG-01 | Requerimiento funcional / datos | **Parcial 2026-10-08:** Eric fijó nombre, apellido, correo, contraseña, teléfono, documento y dirección del hogar como obligatorios. Pendiente: formatos y ficha de finalidad/acceso/conservación/destino por campo. |
| D-REG-02 | Decisión técnica / seguridad | **Parcial 2026-10-08:** código enviado al correo. Definir caducidad, límite de intentos/reenvíos y qué hacer con cuentas no verificadas. |
| D-REG-03 | Pseudocódigo / seguridad | ¿Cómo se recupera acceso y cómo se limita el abuso sin revelar si un correo existe? |
| D-REG-04 | Consentimiento/alerta | Identificar las versiones iniciales de Privacidad y Términos y las decisiones opcionales realmente necesarias; no crear casillas inventadas. |
| D-REG-05 | Pseudocódigo | ¿Cuánto tiempo se conserva la intención de reserva y qué ocurre si cambia la disponibilidad? |

## Pruebas que deberá superar

| ID | Escenario | Resultado |
|---|---|---|
| REG-01 | Registrarse desde cualquier punto público | Se ofrece el flujo sin perder el lugar de origen. |
| REG-02 | Correo ya registrado | No se crea duplicado ni se expone información privada. |
| REG-03 | Falta aceptación obligatoria | No se completa el alta; se explica cuál falta. |
| REG-04 | Rechazar opción facultativa | El registro continúa sin activar ese tratamiento. |
| REG-05 | Cuenta no verificada intenta registrar mascota/reservar | API deniega la operación, aunque la interfaz muestre otra cosa. |
| REG-06 | Verificar, autenticar y retomar reserva | Se conserva intención no sensible, se recalcula disponibilidad y se pide confirmación. |

El contrato de endpoints y DTO se fijará después de resolver D-REG-01 a 05. El estado es **especificado parcialmente**, no implementado ni verificado.
