# Especificación: Configuración y administración (A-027)

Estado: parte 1 implementada en escritorio con datos de demostración y aprobada por Eric (2026-11-16). Parte 2 (bitácora de auditoría e indicadores, RF-ADM-07 y RF-ADM-08) implementada el 2026-11-16, pendiente de la prueba con F5. La advertencia común de los prototipos está en `CONTEXTO_BASE.md` §3.1.

Requisitos: RF-ADM-01, RF-ADM-02, RF-ADM-03, RF-ADM-04, RF-ADM-10, RF-ADM-13, RN-33, RN-34 (no aplica), K-07 (consentimiento).

## Reglas comunes

| Id | Regla |
|---|---|
| AD-01 | Todas las acciones exigen una cuenta **Administrador activa**; las demás devuelven error y no cambian nada. |
| AD-02 | Cada acción que cambia algo deja un registro en la bitácora (usuario, rol, entidad, acción, detalle, fecha). Las rechazadas no dejan registro. |

## Servicios y tarifas (RF-ADM-02, RN-33) — `/panel/admin/servicios`

| Id | Regla |
|---|---|
| SV-01 | Alta: nombre obligatorio (hasta 80) y único sin importar mayúsculas, descripción hasta 300, precio entre $ 1 y $ 1.000.000, duración entre 0 y 480 min. |
| SV-02 | Un servicio que se reserva en línea necesita al menos 15 minutos de duración. |
| SV-03 | Editar cambia nombre, descripción, duración, publicado y reserva en línea; **no** el precio. Un servicio no publicado desaparece del catálogo, pero los turnos ya reservados siguen y su cargo se genera igual. |
| SV-04 | El precio se cambia con una **tarifa** con fecha de vigencia de hoy en adelante (nunca pasada). Una tarifa para la misma fecha reemplaza a la anterior de esa fecha. |
| SV-05 | El precio que muestra el catálogo es el vigente hoy. El cargo de una consulta usa el precio vigente **en la fecha del turno** (RN-33). |

## Usuarios y roles (RF-ADM-01) — `/panel/admin/personal`

| Id | Regla |
|---|---|
| US-01 | Alta: nombre (hasta 80), correo válido y único en el personal, rol Recepción, Veterinario o Administrador. Contraseña inicial de demostración. |
| US-02 | Un Veterinario necesita una ficha de profesional existente que no esté asociada a otra cuenta. |
| US-03 | Una cuenta bloqueada no puede ingresar. Desbloquear la habilita otra vez. Nada se borra. |
| US-04 | El Administrador no se bloquea ni se cambia el rol a sí mismo. Siempre queda al menos un Administrador activo. |
| US-05 | No se bloquea ni se cambia de rol al veterinario de guardia sin asignar antes otra guardia. Un veterinario bloqueado no es elegible para asistencias. |

## Horarios (RF-ADM-04) — `/panel/admin/horarios`

| Id | Regla |
|---|---|
| HO-01 | Ausencia: profesional, desde, hasta y motivo (hasta 100). Desde hoy en adelante; hasta ≥ desde; no se superpone con otra del mismo profesional. |
| HO-02 | No se carga una ausencia ni un feriado sobre turnos activos (Confirmado o En espera): primero se reprograman o cancelan. |
| HO-03 | Feriado: fecha de hoy en adelante, no repetida. Quitar una ausencia o un feriado vuelve a habilitar esos días. |
| HO-04 | Las franjas de la jornada se consultan; se editan con la API. |

## Parámetros (RF-ADM-03, RF-ADM-10, RF-ADM-13) — `/panel/admin/parametros`

| Id | Regla |
|---|---|
| PA-01 | Guardia: un veterinario activo. |
| PA-02 | Asistencia: cobertura 1–200 km, espera del rojo 1–60 min, tarifa base 0–100.000, por km 0–10.000, por animal extra 0–10.000. Rigen para las solicitudes nuevas. |
| PA-03 | Avisos: próximo a vencer 1–180 días; aviso de refuerzo 3–5 días (decisión del 31/10). |
| PA-04 | Stock mínimo por artículo: 0–100.000. |
| PA-05 | Box nuevo: nombre único (hasta 40) y tipo pequeños o grandes animales. Un box ocupado no se saca de servicio; uno fuera de servicio no recibe internaciones. |
| PA-06 | Consentimiento: versión nueva distinta de la vigente y texto obligatorio (K-07, K-13). |
| PA-07 | Las reglas de la agenda (24 h, 2 h, 60 días, 15 min) se muestran pero no se editan en la demostración. |

## Auditoría (RF-ADM-08, RN-40) — `/panel/admin/auditoria`

| Id | Regla |
|---|---|
| AU-01 | Solo un Administrador activo consulta la bitácora; para otro rol o sin sesión devuelve una lista vacía. |
| AU-02 | Junta: cambios de configuración, lecturas de historia, consultas y enmiendas firmadas, movimientos de stock, cobros y notas de crédito, consentimientos, admisiones, ingresos y altas de internación y eventos de asistencia. Entidades: Configuración, Historia clínica, Inventario, Cobros, Consentimiento, Internación, Asistencia. |
| AU-03 | Nunca copia texto clínico (motivo, diagnóstico, síntomas, novedades): solo números de consulta, animal, solicitud o comprobante, y el motivo de acceso de las lecturas. |
| AU-04 | Filtros combinables: usuario (contiene, sin mayúsculas), entidad (exacta), acción (contiene, sin mayúsculas), desde y hasta (inclusive). Orden: lo más reciente primero. |

## Indicadores (RF-ADM-07) — `/panel/admin/indicadores`

| Id | Regla |
|---|---|
| IN-01 | Ocupación del día por profesional publicado: minutos con turnos activos sobre minutos de atención (sin descansos). 0 % si ese día no atiende, está ausente o es feriado. |
| IN-02 | Ingresos por servicio en un período: suma de comprobantes **no anulados**, agrupada por la descripción del cargo, de mayor a menor. |
| IN-03 | Ausencias en un período: turnos totales, finalizados, ausentes y cancelados; tasa = ausentes / (finalizados + ausentes), redondeada; 0 si no hay. |
| IN-04 | Rotación: consumos por consultas en los últimos N días por artículo, stock actual y días que alcanza (stock ÷ consumo diario, hacia abajo); sin consumo, «sin dato». |

## Aclaraciones (preguntas del agente probador)

- La consulta de la bitácora no cambia nada y la usa la pantalla del Administrador; en la API exige el rol.
- Nombres de servicios y boxes: se recortan los espacios y se comparan sin importar mayúsculas, contra todos (publicados o no). Correo del personal: en minúsculas y sin espacios.
- Una tarifa para la misma fecha reemplaza a la anterior también en el historial (queda una sola).
- El Administrador puede cambiar el rol de otro Administrador si queda otro activo. Un veterinario que pasa a otro rol libera su ficha.
- Una ausencia puede empezar hoy; dos ausencias que comparten un día se superponen.
- Feriados: cuentan los turnos activos de cualquier profesional. Las tarifas de asistencia son en pesos enteros.
- Publicar un consentimiento solo se puede para una prestación que ya tiene documento (hoy, la castración).

## Pruebas

`tests/Veterinaria.Tests/AdministracionTests.cs`, `AuditoriaEIndicadoresTests.cs` y en `CajaNegra/` las de administración y de auditoría e indicadores.
