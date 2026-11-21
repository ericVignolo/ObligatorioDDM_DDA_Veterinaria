# Especificación de recorridos demostrativos de escritorio (A-030)

Las advertencias comunes (autoridad de la API, límites de los prototipos) están en `CONTEXTO_BASE.md`, sección 3.1.
Esta tarea no agrega requisitos de la matriz: ordena la demostración de lo ya construido (A-001 a A-029).

## Reglas

| Id | Regla |
|---|---|
| RD-01 | Hay exactamente cinco recorridos, uno por rol y en este orden: Visitante, Cliente, Recepción, Veterinario, Administrador. |
| RD-02 | Cada recorrido tiene título, resumen y al menos cinco pasos. Cada paso tiene acción, ruta y resultado esperado, todos con texto. |
| RD-03 | `DeRol(rol)` devuelve el recorrido de ese rol; un rol desconocido devuelve nulo. |
| RD-04 | El Visitante no tiene correo (no necesita cuenta). Los demás tienen el correo con el que se ingresa y es una cuenta existente. |
| RD-05 | Todo recorrido con correo puede iniciarse con la contraseña de demostración (`PersonalService.ContrasenaDemo`): el personal entra como personal y el Cliente entra como Cliente. |
| RD-06 | Los clientes de demostración (Ana, Pedro) pueden ingresar con la contraseña de demostración; su correo ya está verificado. Una contraseña incorrecta se rechaza como en cualquier cuenta. |
| RD-07 | Toda ruta de un paso corresponde a una pantalla existente (`@page`); las rutas con parámetros no se usan en los pasos. |
| RD-08 | Los datos de partida son repetibles: dos sesiones nuevas tienen la misma cantidad de cuentas de clientes, personal, animales y servicios, y los mismos animales de Ana. |
| RD-09 | La demostración muestra una marca de simulación en todas las pantallas (franja con enlace a `/demo`) y la guía `/demo` indica que todo es simulado, que no se envían correos ni se cobra y que recargar la pestaña reinicia los datos. |
| RD-10 | Ana tiene dos animales (Toby y Mishi) y Pedro dos (el rodeo y Canela); un cliente no ve los animales del otro. |
| RD-11 | Aclaraciones del probador: `DeRol` distingue mayúsculas y tildes y con rol nulo devuelve nulo sin error. El correo al ingresar se normaliza (sin espacios en los extremos y sin distinguir mayúsculas). Las cuentas de cliente de demostración son exactamente dos: `ana.demo@vitalia.uy` (Mishi y Toby) y `pedro.demo@vitalia.uy` (Canela y «Rodeo La Esperanza»). Las rutas de paso empiezan con «/», no llevan parámetros, consulta ni ancla, y el inicio es «/». `SesionService.Iniciada` es verdadera solo para un Cliente; `HaySesion` lo es para cualquier persona con sesión (Cliente o personal). |
| RD-12 | Los recorridos de Cliente, Recepción y Veterinario incluyen los pasos de las reglas RN-08, 09, 16, 17 y 35 (castración con seña, sobreturno, adelanto de vacuna y alergia). |
| RD-13 | `AgendaService.ProximoDiaConHorarios(profesional, servicio, animal, desde)` devuelve el primer día, desde `desde` y hasta el último día reservable, con al menos un horario libre; si no hay ninguno devuelve nulo. Al entrar al paso «Fecha y hora» de Reservar, si el primer día no tiene lugar se pasa solo a ese día y se avisa; si el día elegido a mano no tiene lugar, aparece el botón «Ir al próximo día con horarios». |
| RD-14 | Con los datos de partida reales, Ana puede reservar la castración de Toby (queda «Pendiente de seña»), Recepción cobra la seña y el turno pasa a Confirmado; el recorrido se puede repetir sin agotar los horarios. |

## Decisiones

- **D-DEM-01:** los clientes de demostración entran con la misma contraseña que el personal, para poder hacer el recorrido del Cliente sin registrarse. Solo existe en la demostración y debe retirarse al pasar a la API (C-034).
- **D-DEM-02:** «repetible» significa mismo punto de partida al recargar la pestaña; los horarios de los turnos de ejemplo dependen del día de la semana porque cada profesional trabaja ciertos días.
- **D-DEM-03:** el conjunto grande de datos (30 clientes, 50 animales, 200 consultas históricas, casos de caballo y oveja) sigue pendiente en G-009 y se prepara para la presentación final.

## Verificación

- Automática: `RecorridosTests` (RD-01 a RD-08, RD-10) y `DemostracionCompletaTests` (RD-13, RD-14) y la prueba de caja negra del probador.
- Manual (Eric, con F5): abrir `/demo`, seguir cada recorrido, reiniciar con el botón y comprobar que se vuelve al punto de partida.
