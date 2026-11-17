# Pruebas automáticas

`Veterinaria.Tests` es un proyecto xUnit que prueba las **reglas de negocio** de la sección A
directamente sobre los servicios (sin abrir el navegador). GitHub las corre solo en cada PR
y en cada push a `desarrollo` o `main` (workflow «Compilar y probar», `.github/workflows/ci.yml`).

## Cómo correrlas en la PC

- **Visual Studio:** abrir `desktop/Veterinaria.Blazor/Veterinaria.Blazor.slnx` → menú *Prueba* →
  *Explorador de pruebas* → *Ejecutar todo*.
- **Terminal:** `dotnet test tests/Veterinaria.Tests/Veterinaria.Tests.csproj`

## Qué cubre cada archivo

| Archivo | Tareas | Reglas |
|---|---|---|
| `ValidacionDatosTests` | A-006 | Cédula con dígito verificador, pasaporte, teléfono en E.164, aceptaciones obligatorias (RN-43) |
| `CuentaYSesionTests` | A-006 a A-008, A-010 | Correo y documento únicos, 5 intentos por código, 3 reenvíos por hora, sin verificar no entra (RN-38), recuperación, mensaje genérico, roles del personal |
| `AnimalesYCatalogoTests` | A-003, A-004, A-012, A-013 | Lo no publicado no aparece, profesionales por servicio y especie, microchip único (RN-41), solo animales propios (RN-18) |
| `AgendaTests` | A-014 a A-017 | Horarios cada 15 min, turno bloquea su duración, descansos, ausencias, feriados, 60 días, 24 h para gestionar (RN-06), cancelar libera, reprogramar conserva el número, acciones de Recepción |
| `ConsultaTests` | A-019, A-020 | Una consulta por turno, solo el veterinario asignado, versión, firma con contraseña y matrícula (RN-13), diagnóstico obligatorio (RN-20), firmada inmutable y enmiendas (RN-12), historia sin borradores, lecturas auditadas (RN-40, P-RC-05) |
| `VacunaTests` | A-021 | Refuerzo calculado por el producto (RN-19), lote vencido no se aplica (RN-15), producto no aplicable a la especie, un producto por consulta, solo el veterinario de la consulta abierta, el carnet cuenta solo consultas firmadas, estados vigente / refuerzo próximo / vencida, alertas solo de los animales propios (RN-18) |
| `InventarioTests` | A-022 | Baja automática de lotes vencidos (RN-29), FEFO (RN-27), sin stock no se registra ni cambia nada (RN-26), apartar al registrar y descontar al firmar, lo apartado no lo usa otra consulta, alerta al cruzar el mínimo (RN-28), tablero, compras, ajustes y bajas solo del Administrador con motivo (RN-31), movimientos con origen (RN-30) |
| `InternacionTests` | A-024 | Solo Recepción admite y anula, una internación abierta por animal (RN-21), grupos no, ingreso clínico solo del Veterinario, box exclusivo (RN-22) y del tipo de la especie, notas internas vs. visibles (RN-23), rectificar sin borrar (RN-24), alta con evolución e indicaciones que libera el box (RN-25), el cliente ve solo lo suyo (RN-18) |
| `ConsentimientoTests` | A-025 | La castración pide consentimiento, firma con casilla y nombre de la cuenta, huella y versión, solo el responsable (RN-18), rechazo y nueva firma sin borrar, versión nueva invalida lo firmado, sin consentimiento la cirugía no pasa a En curso (RN-10) |
| `AsistenciaTests` | A-026 | Orden de la cola (RN-47, RN-48), rojo a la guardia (RN-49), sin elegibles o 10 min sin aceptar a Recepción (RN-55), reevaluación a las 24 h (RN-50), empeoramiento solo sube y activa guardia (RN-51), solo el veterinario baja con motivo (RN-52), estimación al aceptar (RN-53), elegibilidad por especie (RN-54), visita sin destino (RN-56), ubicación solo en camino (RN-57), cargo al finalizar (RN-58), idempotencia |
| `AdministracionTests` | A-027 | Solo el Administrador y queda en la bitácora, servicios (duración, nombre único, despublicar), tarifas futuras y cargo con el precio de la fecha del turno (RN-33), bloqueo, último Administrador, guardia, alta de veterinario con ficha libre, ausencias y feriados sin pisar turnos, boxes, parámetros de asistencia y avisos, versión del consentimiento |
| `AuditoriaEIndicadoresTests` | A-027 | Bitácora solo para el Administrador, junta todas las fuentes sin texto clínico (RN-40) y filtra (RF-ADM-08); ingresos sin anulados, ausencias, rotación y ocupación (RF-ADM-07) |
| `ComunicacionesTests` | A-028 | Resumen al firmar la consulta (RF-CLI-10), canal por preferencia (RF-CLI-18, RN-43), lo esencial no se apaga, tres intentos y reintento, reenvío bajo pedido (RF-REC-07), comprobante una vez, avisos de refuerzo sin duplicar (RNF-CON-10), alerta solo a quien la activó, aislamiento entre clientes |
| `DerechosTests` | A-028 | Exportación en JSON sin datos ajenos ni contraseña, pedido de eliminación con confirmación y obstáculos (saldo, turno), cancelar, aprobar anonimiza y conserva animales, rechazar con motivo, solo el Administrador, auditoría sin datos personales (RF-CLI-19, RNF-LEG-02) |
| `AccesibilidadTests` | A-029 | Revisión estática de `.razor` y CSS: imágenes con `alt`, tablas con título y desplazables, campos con etiqueta, enlaces externos con `rel`, salto al contenido, puntos de referencia, foco visible, movimiento reducido, pantallas angostas y textos del sistema en español (RNF-USA-03 a 07, WCAG 2.1 AA) |
| `RecorridosTests` | A-030 | Cinco recorridos por rol, pasos completos, cuentas que ingresan con la contraseña de demostración, cada paso apunta a una pantalla existente, datos de partida repetibles, cada cliente ve solo sus animales |
| `RevisionSeccionATests` | A-031 | Enlaces internos a pantallas existentes, páginas de error en español, motivo de ausencia no expuesto, resumen clínico oculto en la bandeja |
| `CobroTests` | A-023 | Cargo solo al firmar y sin duplicarse (RN-32), precio de la prestación (RN-33), cobro solo de Recepción, total y parcial, sin superar el saldo ni cobrar un cargo pagado, medios válidos y número de operación en transferencias, comprobantes correlativos, anulación solo del Administrador con motivo y nota de crédito sin modificar el comprobante (RN-34), totales sin anulados, cada cliente ve solo sus cargos (RN-18) |

**Caja negra (`CajaNegra/`):** pruebas que escribe el agente probador (`.claude/agents/probador.md`) leyendo solo la especificación, los modelos y las firmas públicas, sin ver la implementación. Si una falla, se revisa si el error está en el código o en la especificación.

`Fabrica.cs` arma los servicios igual que `Program.cs` y tiene ayudas (cliente verificado,
animal nuevo, primer día en que atiende un profesional).

## Al agregar una regla nueva

Cada tarea que agrega o cambia una regla del backend agrega o ajusta su prueba en el mismo PR.
Las pruebas no reemplazan la prueba con F5: verifican reglas, no cómo se ve la pantalla.
