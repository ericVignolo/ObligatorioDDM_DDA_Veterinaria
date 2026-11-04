# Registro de Cliente y reanudación de una acción protegida

Fecha: 2026-10-08

Estado: decisiones D-REG-01 a D-REG-05 **cerradas por Diego el 2026-10-29** (sección «Decisiones cerradas»). Listo para fijar DTO y endpoints; no implementado.

## Alcance y trazabilidad

Este documento detalla `CU-VIS-03` y `HU-VIS-07`, vinculados a **RF-VIS-07**, **RN-37**, **RN-38**, **RN-43**, **RNF-LEG-03** y **RNF-LEG-06**. Es una especificación funcional y de pseudocódigo, no una autorización para diseñar pantallas finales o implementar identidad con campos supuestos. El texto canónico está en [MATRIZ_REQUISITOS.md](../contexto-base/MATRIZ_REQUISITOS.md).

## Comportamiento confirmado

1. Desde cualquier punto público se puede comenzar el registro. Ante una acción protegida se explica el motivo, se ofrece iniciar sesión o registrarse y se conserva solo el contexto no sensible necesario para retomarla.
2. El formulario enlaza las versiones vigentes de Privacidad y Términos accesibles sin cuenta. La API registra por separado cada respuesta exigida por finalidad, documento y versión, fecha, canal y respuesta; las opciones facultativas no están premarcadas ni bloquean el alta si se rechazan.
3. La API valida los datos, la unicidad del correo y las aceptaciones obligatorias. Un mismo correo no identifica dos cuentas de Cliente.
4. La cuenta no puede registrar animales ni reservar turnos hasta estar verificada. El modo invitado no puede consultar información personal.
5. Tras la verificación y autenticación, el cliente vuelve a la acción original. Se revalidan permisos, disponibilidad y vigencia de los datos: haber conservado la intención no implica ejecutar automáticamente una reserva.

**D-REG-01, respuesta de Eric (2026-10-08):** nombre, apellido, correo electrónico, contraseña, teléfono, documento y dirección del hogar serán obligatorios al crear la cuenta. Para cada dato se documentan finalidad, quién puede verlo, conservación y destino según RNF-LEG-06 (tabla siguiente); en particular, documento y dirección no se enviarán a clientes o endpoints que no los necesiten. La contraseña se recibe solo para crear la credencial segura y nunca se devuelve ni registra en logs (RNF-SEG-01/11). Formatos y ficha de datos: ver la tabla siguiente.

**D-REG-02, respuesta de Eric (2026-10-08):** la verificación será mediante un **código enviado al correo electrónico**. Duración, intentos, reenvíos y caducidad: ver D-REG-02 en «Decisiones cerradas». El código no se guarda en claro ni se expone en logs.

| Dato obligatorio | Finalidad | Quién lo ve | Conservación y destino |
|---|---|---|---|
| Nombre y apellido | Identificar al responsable de los animales y de los cargos. | El propio Cliente; personal de la clínica. | Mientras la cuenta esté activa; tras la baja, solo vinculado a la historia clínica y a comprobantes que deban conservarse (RN-14, RNF-LEG-04). |
| Correo | Identificar la cuenta (RN-37), enviar códigos y comunicaciones operativas. | El propio Cliente; Recepción y Administración. | Igual que el nombre. No se comparte con terceros salvo el proveedor de correo (RNF-SEG-13). |
| Contraseña | Autenticar. Solo se guarda su derivado (RNF-SEG-01). | Nadie. | Se reemplaza al cambiarla; se elimina con la cuenta. |
| Teléfono | Contactar por turnos, asistencia y urgencias. | El propio Cliente; Recepción y el veterinario asignado a una asistencia. | Igual que el nombre. |
| Documento | Identificar al titular ante cobros, comprobantes y transferencias de titularidad (RN-42). | El propio Cliente; Recepción y Administración. Nunca en endpoints públicos ni en la app del Veterinario. | Igual que el nombre. |
| Dirección del hogar | Contacto, comprobantes y dirección por defecto para visitas a domicilio. | El propio Cliente; Recepción; el veterinario solo durante una asistencia asignada. | Igual que el nombre. |

Formatos:

- **Documento:** tipo + número. Tipos: cédula uruguaya (7 u 8 dígitos con dígito verificador validado), pasaporte y otro documento extranjero (alfanumérico, 5 a 20 caracteres). Único por tipo y número.
- **Teléfono:** se guarda normalizado en formato E.164. Si no tiene prefijo internacional se asume +598.
- **Dirección:** calle, número, apartamento (opcional), localidad, departamento y referencias (opcional). Los campos opcionales se marcan como tales.

Estas finalidades no autorizan usos nuevos. Cualquier uso opcional, como marketing, requiere una decisión separada conforme a RN-43.

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

Los pasos de creación/evidencia se diseñarán para no dejar una cuenta verificada sin las aceptaciones obligatorias. No se incluyen contraseñas, códigos ni tokens en registros de actividad. Los tiempos y reintentos están en «Decisiones cerradas». Un registro parcial no existe: la cuenta y las aceptaciones obligatorias se guardan en la misma transacción.

## Decisiones cerradas

Cerradas por Diego el 2026-10-29. Impacto: API (identidad, contrato de registro), Blazor y móvil (formularios y mensajes), pruebas REG-01 a REG-09.

| ID | Clasificación | Decisión |
|---|---|---|
| D-REG-01 | Requerimiento funcional / datos | Siete campos obligatorios (Eric, 2026-10-08) con la ficha y los formatos de arriba. |
| D-REG-02 | Decisión técnica / seguridad | Código de 6 dígitos enviado al correo, válido 15 minutos. Máximo 5 intentos por código; al agotarlos, el código se invalida. Hasta 3 reenvíos por hora. Una cuenta no verificada en 7 días se elimina y el correo queda libre. Si el usuario perdió acceso al correo, se registra de nuevo con otro correo o lo resuelve Recepción verificando su documento. El código se guarda solo como hash. |
| D-REG-03 | Pseudocódigo / seguridad | Recuperación con código por correo, mismos límites que D-REG-02. La respuesta siempre es «Si el correo está registrado, te enviamos un código», exista o no la cuenta. Además de los límites por cuenta, se limitan los pedidos por origen (RNF-SEG-09). Al cambiar la contraseña se revocan todas las sesiones y tokens de refresco. |
| D-REG-04 | Consentimiento/alerta | En el registro se piden dos aceptaciones obligatorias y separadas: Términos y Condiciones v1.0 y Política de Privacidad v1.0. No se ofrece ninguna casilla opcional en esta versión (no hay marketing ni analítica). Las notificaciones operativas son parte del servicio y su canal se configura después (RF-CLI-18). Los textos v1.0 son de una clínica ficticia y se identifican como borrador académico. |
| D-REG-05 | Pseudocódigo | La intención se guarda solo en el cliente (Blazor o app), nunca en el servidor: identificador de la acción, servicio, profesional y horario elegidos. Dura 30 minutos. Al retomarla se recalcula la disponibilidad; si el horario ya no está, se muestran los horarios actuales manteniendo servicio y profesional. Pasados los 30 minutos se descarta sin aviso y se vuelve al catálogo. |

## Pruebas que deberá superar

| ID | Escenario | Resultado |
|---|---|---|
| REG-01 | Registrarse desde cualquier punto público | Se ofrece el flujo sin perder el lugar de origen. |
| REG-02 | Correo ya registrado | No se crea duplicado ni se expone información privada. |
| REG-03 | Falta aceptación obligatoria | No se completa el alta; se explica cuál falta. |
| REG-04 | Rechazar opción facultativa | El registro continúa sin activar ese tratamiento. |
| REG-05 | Cuenta no verificada intenta registrar mascota/reservar | API deniega la operación, aunque la interfaz muestre otra cosa. |
| REG-06 | Verificar, autenticar y retomar reserva | Se conserva intención no sensible, se recalcula disponibilidad y se pide confirmación. |
| REG-07 | Sexto intento con un código | Código invalidado; hay que pedir uno nuevo. |
| REG-08 | Recuperación con correo inexistente | Misma respuesta y mismo tiempo de respuesta que con un correo registrado. |
| REG-09 | Retomar intención después de 30 minutos | Se descarta y se vuelve al catálogo, sin reserva. |

Con D-REG-01 a 05 cerradas ya se puede fijar el contrato de endpoints y DTO. Estado: **especificado**, no implementado ni verificado.
