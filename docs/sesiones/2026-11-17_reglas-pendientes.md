# Sesión 2026-11-17 — Reglas pendientes de la sección A

Rama: `eric/sesion-2026-11-17-reglas-pendientes`. Antes se mezclaron A-030 (PR #35) y A-031 (PR #36).

## Decisiones de Eric
- D-COM-03: el aviso de refuerzo de vacunas llega por correo por defecto (es necesario); se puede apagar.
- Construir RN-08, 09, 16, 17, 35 y 36 con: 3 ausencias en 12 meses (aviso desde 2), seña del 30 %, aviso por deuda vencida desde $ 3.000; los tres configurables por el Administrador.

## Qué se hizo
- RN-08: aviso y bloqueo de la reserva en línea por ausencias; Recepción puede dar el turno.
- RN-09: «Dar un turno en el mostrador» en la agenda de Recepción, con sobreturno, motivo y registro en la bitácora.
- RN-16: intervalo mínimo por producto; adelantar exige justificación, que queda en la aplicación.
- RN-17: si el tratamiento incluye una alergia registrada, firmar pide confirmación explícita y queda asentado.
- RN-35: la cirugía queda «Pendiente de seña» hasta cobrar el 30 %; la seña se descuenta del cargo final.
- RN-36: aviso al agendar si la deuda vencida supera el límite; nunca bloquea.
- Parámetros en `/panel/admin/parametros`. Especificación: `docs/analisis/ESPECIFICACION_REGLAS_PENDIENTES.md`.

## Cómo probarlo (F5)
1. Como Ana, reservá una castración: queda «Pendiente de seña» y el paso 4 avisa la seña.
2. Como Recepción, en Cobros aparece la seña; cobrala completa: el turno pasa a Confirmado.
3. En la agenda de Recepción, dá un turno por mostrador y un sobreturno en un horario ocupado (con motivo).
4. Como Ferreira, en una consulta de Toby registrá la vacuna polivalente: pide justificación (se aplicó hace 60 días).
5. Escribí «Penicilina» en alergias y en tratamiento y firmá: pide confirmar.
6. Como Administrador, cambiá los parámetros.

## Después de la prueba con F5 (misma rama)
- **Castración sin horarios (Eric, F5):** la cirugía dura 90 min, solo la hace la Dra. Ferreira (9 a 14 con descanso 11:30–12:00) y los turnos de demostración de hoy y mañana no dejaban lugar. Ahora `AgendaService.ProximoDiaConHorarios` y Reservar saltan solos al primer día con lugar y avisan, o muestran «Ir al próximo día con horarios» (RD-12 a RD-14 en `ESPECIFICACION_RECORRIDOS_DEMO.md`; pruebas en `DemostracionCompletaTests.cs`). Los recorridos de `/demo` incluyen ahora la seña, el sobreturno, el adelanto de vacuna y la alergia.
- **Sistema de diseño (propuesta para que Diego elija):** `design/vitalia/` con la Opción A (Eric: verde salud, naranja cálido, Nunito, redondeo suave, iconos de contorno, claro y oscuro, esqueletos + avisos) y la Opción B (Claude: verde azulado, coral, fondo arena, Plus Jakarta Sans, iconos duotono). Cada una trae 12 imágenes en `exports/`, `vars.json` para OpenPencil y variables CSS `--vt-*` en `web/`; `README.md` explica todo y `scripts/generar_exports.py` lo regenera. Vista lado a lado: `design/vitalia/web/previsualizacion.html`. Contrastes WCAG AA verificados (se corrigió el verde de éxito: 4,37:1 → 5,33:1). Los tiempos de carga son una propuesta, no una medición.
- **OpenPencil:** instalado por Eric (`winget install --id ZSeven-W.OpenPencil -e`, 0.8.5). Falta instalar el CLI (`winget install --id ZSeven-W.OpenPencil.CLI -e`) y abrir Claude Code en el repo local para que construya el archivo `.op` con `op` (como `diegoolherry/Obligatorio-DDA-DDM`, carpeta `design/`). La sesión en la nube no puede tipear en la terminal de Windows ni llegar al puerto local.

## Qué falta (para Diego y para la próxima sesión)
1. **Diego elige la Opción A o la B** mirando `design/vitalia/web/previsualizacion.html` y las imágenes de `opcion-*/exports/`. Registrar la decisión en `DECISIONES_PARA_DESARROLLO.md`.
2. Con la elección: copiar `tokens-opcion-X.css`, `componentes.css` y `fuentes/` a `desktop/Veterinaria.Blazor/wwwroot/`, enlazarlos en `App.razor` y migrar las pantallas por recorrido (una rama por sesión, con su prueba). Primero el layout y los componentes comunes (`MainLayout`, `PrestacionCard`, `EstadoTurno`).
3. Armar el archivo de OpenPencil con el CLI (ver arriba) y exportar las pantallas de Blazor y de React Native.
4. Conjunto grande de datos de demostración (G-009: 30 clientes, 50 animales, 200 consultas, casos de caballo y oveja).
5. Aplicación móvil React Native y luego integración con la API.
6. Límites conocidos: una seña pagada de una cirugía cancelada no se devuelve sola (nota de crédito en Recepción); la seña impaga no vence; la demostración no tiene deudas viejas, así que el aviso RN-36 solo se ve con el límite en $ 0; sigue sin auditarse con lector de pantalla; abiertas: proveedor SMTP, tarifa de asistencia de ejemplo y guardia de la Dra. Ferreira.
