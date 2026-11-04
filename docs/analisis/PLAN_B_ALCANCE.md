# Plan B de alcance

Fecha: 2026-10-29

Responsable: Diego (decisión delegada por Eric el 2026-10-09). Revisión: Eric.

Estado: **vigente como plan de contingencia**. No recorta nada hoy: los 47 RF Must siguen en la base de [ALCANCE_ENTREGA.md](ALCANCE_ENTREGA.md). Define qué se defiende sí o sí y en qué orden se recorta si el tiempo no alcanza, para que el recorte sea una decisión preparada y no una improvisación al final.

## 1. Núcleo que no se recorta

Es el recorrido mínimo que demuestra la arquitectura y las reglas críticas. Si solo se llega a esto, la entrega sigue siendo defendible.

| Bloque | RF principales | Reglas que tienen que verse funcionando |
|---|---|---|
| Área pública y legales | RF-VIS-01, 02, 03, 06, 07, 10 | Navegación sin cuenta; reanudación de la acción protegida. |
| Identidad y roles | RF-ADM-01 | RN-37, RN-38; RNF-SEG-01/03/05; 2FA de Administrador (RNF-SEG-04). |
| Animales | RF-CLI-01, RF-REC-03 | RN-18, RN-41. |
| Agenda | RF-CLI-02, 03, 04, RF-REC-01, RF-VET-01, RF-ADM-02, 04 | RN-01 a RN-07, RN-11; dos reservas simultáneas → una sola gana (RNF-CON-09). |
| Consulta e historia | RF-VET-02, 03, 07, 12, 13, RF-CLI-05, 08, 10 | RN-12, 13, 20, 40; cierre atómico (RNF-CON-03); correo con reintento (RNF-CON-04). |
| Stock en la consulta | RF-VET-06, RF-ADM-05 | RN-26 a RN-31 (FEFO, no negativo, alerta de mínimo). |
| Cargo y cobro | RF-REC-05 | RN-32, 33, 34. |
| Auditoría | RF-ADM-08 | RNF-SEG-10. |

## 2. Recortes ordenados

Se aplican **en este orden**, de a uno, solo cuando el disparador de la sección 3 lo indique. Cada recorte de un Must exige, antes de aplicarse: actualizar la prioridad en [MATRIZ_REQUISITOS.md](../contexto-base/MATRIZ_REQUISITOS.md) con fecha y motivo, mover el RF a «Extensión» en `ALCANCE_ENTREGA.md` y avisar al docente. Lo recortado queda documentado como extensión, no se borra.

| Orden | Recorte | RF afectados | Qué queda en su lugar |
|---|---|---|---|
| R1 | El Veterinario trabaja solo en Blazor; el móvil queda para Visitante y Cliente. | Canal móvil de RF-VET-01/02/16/17 | Las mismas funciones en escritorio. No cambia ningún RF, solo el canal. |
| R2 | Asistencia sin mapa ni ubicación en vivo del veterinario. | RF-CLI-23 (parcial), RF-VET-17 (parcial), RN-57 | Estados y notificaciones de avance; tiempo estimado ingresado por el veterinario al aceptar. RNF-DIS-05 ya prevé este modo. |
| R3 | Rojo sin oferta automática a guardias: escala directo a Recepción y muestra el teléfono de guardia. | RF-VET-16 (parcial), RN-49, RN-54 | RN-55 (escalamiento) y RF-VIS-06. Recepción asigna a mano. |
| R4 | Internación sin tiempo real: las novedades se ven al recargar. | RF-CLI-09 (parcial) | RNF-DIS-04 ya admite este comportamiento como respaldo. |
| R5 | Should de la base pasan a extensión. | RF-REC-02, RF-REC-06, RF-CLI-18, RF-CLI-20, RF-ADM-07, RF-ADM-10 | Parámetros fijos en configuración (RNF-MAN-04 queda como deuda); ausencias marcadas sin política automática. |
| R6 | Derechos sobre datos como trámite manual. | RF-CLI-19 | El Cliente lo solicita por contacto y la clínica lo resuelve; se mantiene RNF-LEG-02 en el procedimiento. |
| R7 | Asistencia completa (H4) pasa a extensión. | RF-CLI-21 a 25, RF-VET-16/17, RF-REC-08/09, RF-ADM-13 | Información de urgencias y teléfono de guardia (RF-VIS-06). |

R7 es el último recurso: la asistencia es el módulo más original del proyecto y conviene defender aunque sea con R2 y R3 aplicados.

## 3. Disparador

1. Cuando exista fecha de entrega, se registra en `CONTEXTO_BASE.md` y se calcula el tiempo disponible.
2. Al cerrar el **primer recorrido demostrable** (catálogo → registro → reserva en Blazor, sección A) se mide cuánto tardó.
3. Si ese recorrido consumió más del 15 % del tiempo disponible, se aplican R1 y R2. Si consumió más del 25 %, se aplica hasta R4. Más del 35 %: hasta R6 y se replantea R7 con el docente.
4. La misma medición se repite al terminar H2. Cada aplicación se registra en este documento con fecha y quién la decidió.

## 4. Registro de aplicación

| Fecha | Recorte | Decidió | Motivo |
|---|---|---|---|
| — | Ninguno aplicado | — | — |
